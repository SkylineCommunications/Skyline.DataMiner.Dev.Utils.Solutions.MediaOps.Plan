namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Extensions;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Logging;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	using LiveConstants = Skyline.DataMiner.Solutions.MediaOps.Live.Constants;
	using LiveEnums = Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using LiveOrchestration = Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using PlanResource = Skyline.DataMiner.Solutions.MediaOps.Plan.API.Resource;
	using ProfileParameterValue = Skyline.DataMiner.Net.Profiles.ParameterValue;
	using ResourcePool = Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool;

	/// <summary>
	/// Deterministic, simulation-backed tests for the orchestration configuration that the Plan solution pushes to an
	/// installed MediaOps Live. This state lives entirely outside the Plan DOM module, so it is verified by reading it
	/// back through the MediaOps Live API and by inspecting the scheduler tasks and script executions of the simulated
	/// agent. The simulation has no SRM engine, so the reservation status is controlled explicitly to bring a job into
	/// the Running state.
	/// </summary>
	[TestClass]
	public sealed class LiveOrchestrationSimulationTests
	{
		private const string OrchestrationScriptName = "Node Orchestration Script";

		private const string OrchestrationModuleId = "(slc)orchestration";

		// The MediaOps Live storage model is internal, so the event state is addressed by its DOM identifiers.
		private static readonly SectionDefinitionID OrchestrationEventInfoSectionId =
			new SectionDefinitionID(new Guid("43b3a199-8d6d-4cc9-ac63-3504f50e5ee5")) { ModuleId = OrchestrationModuleId };

		private static readonly FieldDescriptorID OrchestrationEventStateFieldId =
			new FieldDescriptorID(new Guid("2d7ececb-585e-4786-8a89-329486b59f92"));

		private static readonly FieldDescriptorID OrchestrationEventActualStartTimeFieldId =
			new FieldDescriptorID(new Guid("b344ff54-e25e-4bac-9d4f-9c8f241e9572"));

		private static readonly TimeSpan NowTolerance = TimeSpan.FromSeconds(60);

		private static TestSetup CreateSetup()
		{
			var dms = MediaOpsPlanSimulation.Create(installMediaOpsLive: true);
			dms.AddScript(OrchestrationScriptName, MediaOpsPlanSimulation.OrchestrationScriptFolder);

			var connection = dms.CreateConnection();

			var logger = new ErrorCollectingLogger();
			var api = connection.GetMediaOpsPlanApi();
			api.SetLogger(logger);

			return new TestSetup(
				dms,
				connection,
				api,
				connection.GetMediaOpsLiveApi(),
				new ResourceManagerHelper(connection.HandleSingleResponseMessage),
				logger);
		}

		private static (ResourcePool Pool, PlanResource Resource) CreatePoolAndResource(TestSetup setup, string name)
		{
			var pool = setup.Api.ResourcePools.Create(new ResourcePool { Name = $"{name}_Pool" });
			pool = setup.Api.ResourcePools.Complete(pool);

			var inputVsg = setup.LiveApi.VirtualSignalGroups.Create(new VirtualSignalGroup { Name = $"{name}_In", Role = EndpointRole.Destination });
			var outputVsg = setup.LiveApi.VirtualSignalGroups.Create(new VirtualSignalGroup { Name = $"{name}_Out", Role = EndpointRole.Source });

			var resource = new UnmanagedResource
			{
				Name = $"{name}_Resource",
				VirtualSignalGroupInputId = inputVsg.ID,
				VirtualSignalGroupOutputId = outputVsg.ID,
			}
			.AssignToPool(pool);

			resource = setup.Api.Resources.Create(resource);

			return (pool, setup.Api.Resources.Complete(resource));
		}

		private static Job CreateJob(TestSetup setup, DateTime preRollStart, DateTime start, DateTime end, DateTime postRollEnd, int numberOfNodes = 1)
		{
			var prefix = Guid.NewGuid();

			var job = new Job
			{
				Name = $"{prefix}_Job",
				PreRollStart = preRollStart,
				Start = start,
				End = end,
				PostRollEnd = postRollEnd,
			};

			for (var i = 0; i < numberOfNodes; i++)
			{
				var (pool, resource) = CreatePoolAndResource(setup, $"{prefix}_{i}");
				job.NodeGraph.Add(new JobResourceNode(pool, resource) { Alias = $"Node {i}" });
			}

			return setup.Api.Jobs.Create(job);
		}

		private static Job Confirm(TestSetup setup, Job job)
		{
			return setup.Api.Jobs.Confirm(setup.Api.Jobs.SaveAsTentative(job));
		}

		private static Job MakeRunning(TestSetup setup, Job confirmedJob)
		{
			// Mirror what SRM does on a live agent once the reservation start time is reached.
			SetReservationStatus(setup, confirmedJob.Id, ReservationStatus.Ongoing);

			return setup.Api.Jobs.TransitionToRunning(confirmedJob);
		}

		private static void SetReservationStatus(TestSetup setup, Guid jobId, ReservationStatus status)
		{
			var reservation = setup.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(jobId))).FirstOrDefault();
			Assert.IsNotNull(reservation, "Expected a core reservation for the job.");

			reservation.Status = status;
			setup.ResourceManagerHelper.AddOrUpdateReservationInstances(reservation);
		}

		private static LiveOrchestration.OrchestrationJobConfiguration GetLiveConfiguration(TestSetup setup, Job job)
		{
			// Failures while pushing the configuration to MediaOps Live are logged instead of thrown, so they would
			// otherwise surface as an unexplained absence of orchestration events.
			Assert.AreEqual(
				0,
				setup.Logger.Errors.Count,
				$"Expected no errors while synchronizing the orchestration configuration: {String.Join(" | ", setup.Logger.Errors)}");

			return setup.LiveApi.Orchestration.GetOrchestrationJobConfiguration(job.Id.ToString());
		}

		private static LiveOrchestration.OrchestrationEventConfiguration GetEvent(LiveOrchestration.OrchestrationJobConfiguration liveConfiguration, LiveEnums.EventType eventType)
		{
			var liveEvent = liveConfiguration.OrchestrationEvents.SingleOrDefault(x => x.EventType == eventType);
			Assert.IsNotNull(liveEvent, $"Expected an orchestration event of type {eventType}.");

			return liveEvent;
		}

		private static List<Guid> GetTriggeredEventIds(TestSetup setup)
		{
			// The orchestration events that must run immediately are handed to the MediaOps Live orchestration script.
			var options = setup.Dms.ExecutedScripts
				.Where(x => String.Equals(x.ScriptName, LiveConstants.OrchestrationScriptName, StringComparison.OrdinalIgnoreCase))
				.SelectMany(x => x.Options?.Sa ?? Array.Empty<string>())
				.ToList();

			return setup.LiveApi.Orchestration.GetAllJobConfigurations()
				.SelectMany(x => x.OrchestrationEvents)
				.Select(x => x.ID)
				.Where(id => options.Any(option => option.IndexOf(id.ToString(), StringComparison.OrdinalIgnoreCase) >= 0))
				.ToList();
		}

		private static int GetTriggerCount(TestSetup setup, Guid orchestrationEventId)
		{
			return setup.Dms.ExecutedScripts
				.Where(x => String.Equals(x.ScriptName, LiveConstants.OrchestrationScriptName, StringComparison.OrdinalIgnoreCase))
				.Count(x => (x.Options?.Sa ?? Array.Empty<string>())
					.Any(option => option.IndexOf(orchestrationEventId.ToString(), StringComparison.OrdinalIgnoreCase) >= 0));
		}

		private static void HideNode(TestSetup setup, Job job, string nodeId)
		{
			// No API operation marks a node as hidden yet; the soft delete is applied by the DOM CRUD layer.
			var domHelper = new DomHelper(setup.Connection.HandleMessages, "(slc)workflow");

			var instance = domHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(job.Id)).Single();

			var nodeSection = instance.Sections
				.Where(x => SlcWorkflowIds.Sections.Nodes.Id.Equals(x.SectionDefinitionID))
				.Select(x => new NodesSection(x))
				.Single(x => x.NodeID == nodeId);

			nodeSection.Hidden = true;

			domHelper.DomInstances.Update(instance);
		}

		private static void SetOrchestrationEventState(TestSetup setup, Guid orchestrationEventId, LiveEnums.EventState state)
		{
			// The MediaOps Live API only allows the draft, confirmed and cancelled states to be assigned.
			var domHelper = new DomHelper(setup.Connection.HandleMessages, OrchestrationModuleId);

			var instance = domHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(orchestrationEventId)).Single();

			var section = instance.Sections.Single(x => OrchestrationEventInfoSectionId.Equals(x.SectionDefinitionID));
			section.AddOrUpdateValue(OrchestrationEventStateFieldId, (int)state);

			domHelper.DomInstances.Update(instance);
		}

		private static void SetOrchestrationEventActualStartTime(TestSetup setup, Guid orchestrationEventId, DateTimeOffset actualStartTime)
		{
			// MediaOps Live records the hand-off to the deferred orchestration script; that setter is internal to MediaOps Live.
			var domHelper = new DomHelper(setup.Connection.HandleMessages, OrchestrationModuleId);

			var instance = domHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(orchestrationEventId)).Single();

			var section = instance.Sections.Single(x => OrchestrationEventInfoSectionId.Equals(x.SectionDefinitionID));
			section.AddOrUpdateValue(OrchestrationEventActualStartTimeFieldId, actualStartTime.UtcDateTime);

			domHelper.DomInstances.Update(instance);
		}

		[TestMethod]
		public void Confirm_JobWithTimingsInTheFuture_SchedulesAllOrchestrationEvents()
		{
			var setup = CreateSetup();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(5),
				start: currentTime.AddMinutes(10),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25));

			var confirmedJob = Confirm(setup, job);

			var liveConfiguration = GetLiveConfiguration(setup, confirmedJob);
			Assert.IsNotNull(liveConfiguration, "Expected an orchestration job configuration in MediaOps Live.");
			Assert.AreEqual(4, liveConfiguration.OrchestrationEvents.Count, "Expected a pre-roll and post-roll start and stop event.");

			var expectedTimes = new Dictionary<LiveEnums.EventType, DateTime>
			{
				[LiveEnums.EventType.PrerollStart] = currentTime.AddMinutes(5),
				[LiveEnums.EventType.PrerollStop] = currentTime.AddMinutes(10),
				[LiveEnums.EventType.PostrollStart] = currentTime.AddMinutes(20),
				[LiveEnums.EventType.PostrollStop] = currentTime.AddMinutes(25),
			};

			foreach (var expectedTime in expectedTimes)
			{
				var liveEvent = GetEvent(liveConfiguration, expectedTime.Key);

				Assert.AreEqual(LiveEnums.EventState.Confirmed, liveEvent.EventState, $"Expected the {expectedTime.Key} event to be confirmed.");
				Assert.AreEqual(expectedTime.Value, liveEvent.EventTime?.UtcDateTime, $"Expected the {expectedTime.Key} event to follow the job timings.");
				Assert.IsNotNull(liveEvent.SchedulerReference, $"Expected the {expectedTime.Key} event to be scheduled.");
			}

			Assert.AreEqual(4, setup.Dms.SchedulerTasks.Count, "Expected one scheduler task per distinct orchestration event time.");
			Assert.AreEqual(0, GetTriggeredEventIds(setup).Count, "Expected no orchestration event to be triggered immediately.");
		}

		[TestMethod]
		public void Confirm_JobWithStartInThePast_TriggersStartEventsImmediately()
		{
			var setup = CreateSetup();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(-10),
				start: currentTime.AddMinutes(-5),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25));

			var confirmedJob = Confirm(setup, job);

			var liveConfiguration = GetLiveConfiguration(setup, confirmedJob);

			var preRollStart = GetEvent(liveConfiguration, LiveEnums.EventType.PrerollStart);
			var preRollStop = GetEvent(liveConfiguration, LiveEnums.EventType.PrerollStop);

			Assert.AreEqual(LiveEnums.EventState.Draft, preRollStart.EventState, "Expected the pre-roll start event to be put in draft so it triggers immediately.");
			Assert.AreEqual(LiveEnums.EventState.Draft, preRollStop.EventState, "Expected the pre-roll stop event to be put in draft so it triggers immediately.");
			Assert.IsNull(preRollStart.SchedulerReference, "Expected an immediately triggered event not to be scheduled.");
			Assert.IsNull(preRollStop.SchedulerReference, "Expected an immediately triggered event not to be scheduled.");

			var postRollStart = GetEvent(liveConfiguration, LiveEnums.EventType.PostrollStart);
			Assert.AreEqual(LiveEnums.EventState.Confirmed, postRollStart.EventState, "Expected the post-roll start event to stay scheduled.");
			Assert.IsNotNull(postRollStart.SchedulerReference, "Expected the post-roll start event to be scheduled.");

			CollectionAssert.AreEquivalent(
				new[] { preRollStart.ID, preRollStop.ID },
				GetTriggeredEventIds(setup).ToArray(),
				"Expected exactly the pre-roll events that did not trigger yet to be executed immediately.");
		}

		[TestMethod]
		public void ReturnToTentative_ConfirmedJob_RemovesOrchestrationEvents()
		{
			var setup = CreateSetup();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(5),
				start: currentTime.AddMinutes(10),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25));

			var confirmedJob = Confirm(setup, job);
			Assert.AreEqual(4, GetLiveConfiguration(setup, confirmedJob).OrchestrationEvents.Count, "Expected the confirmed job to have orchestration events.");

			var tentativeJob = setup.Api.Jobs.ReturnToTentative(confirmedJob);

			Assert.AreEqual(JobState.Tentative, tentativeJob.State, "Expected the job to return to the tentative state.");

			var liveConfiguration = GetLiveConfiguration(setup, tentativeJob);
			Assert.AreEqual(0, liveConfiguration?.OrchestrationEvents.Count ?? 0, "Expected the orchestration events of a tentative job to be removed.");
			Assert.AreEqual(0, setup.Dms.SchedulerTasks.Count, "Expected the scheduler tasks of the removed orchestration events to be deleted.");
		}

		[TestMethod]
		public void Cancel_TentativeJob_KeepsOrchestrationEmpty()
		{
			var setup = CreateSetup();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(5),
				start: currentTime.AddMinutes(10),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25));

			var tentativeJob = setup.Api.Jobs.SaveAsTentative(job);
			var canceledJob = setup.Api.Jobs.Cancel(tentativeJob);

			Assert.AreEqual(JobState.Canceled, canceledJob.State, "Expected the job to be canceled.");

			var liveConfiguration = GetLiveConfiguration(setup, canceledJob);
			Assert.AreEqual(0, liveConfiguration?.OrchestrationEvents.Count ?? 0, "Expected a job that was canceled before it was ever confirmed to have no orchestration events.");
			Assert.AreEqual(0, setup.Dms.SchedulerTasks.Count, "Expected no scheduler tasks for a job that was never confirmed.");
		}

		[TestMethod]
		public void Cancel_ConfirmedJob_CancelsAndUnschedulesOrchestrationEvents()
		{
			var setup = CreateSetup();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(5),
				start: currentTime.AddMinutes(10),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25));

			var confirmedJob = Confirm(setup, job);
			var canceledJob = setup.Api.Jobs.Cancel(confirmedJob);

			Assert.AreEqual(JobState.Canceled, canceledJob.State, "Expected the job to be canceled.");

			var liveConfiguration = GetLiveConfiguration(setup, canceledJob);
			Assert.AreEqual(4, liveConfiguration.OrchestrationEvents.Count, "Expected the orchestration events to be kept for history.");
			Assert.IsTrue(
				liveConfiguration.OrchestrationEvents.All(x => x.EventState == LiveEnums.EventState.Cancelled),
				"Expected every orchestration event that did not trigger yet to be cancelled.");
			Assert.IsTrue(
				liveConfiguration.OrchestrationEvents.All(x => x.SchedulerReference == null),
				"Expected the scheduler reference of a cancelled orchestration event to be cleared.");
			Assert.AreEqual(0, setup.Dms.SchedulerTasks.Count, "Expected the scheduler tasks of the cancelled orchestration events to be deleted.");
		}

		[TestMethod]
		public void Update_ConfirmedJobWithNewTimings_ReschedulesOrchestrationEvents()
		{
			var setup = CreateSetup();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(5),
				start: currentTime.AddMinutes(10),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25));

			var confirmedJob = Confirm(setup, job);

			confirmedJob.PreRollStart = currentTime.AddMinutes(35);
			confirmedJob.Start = currentTime.AddMinutes(40);
			confirmedJob.End = currentTime.AddMinutes(50);
			confirmedJob.PostRollEnd = currentTime.AddMinutes(55);

			var updatedJob = setup.Api.Jobs.Update(confirmedJob);

			var liveConfiguration = GetLiveConfiguration(setup, updatedJob);

			var expectedTimes = new Dictionary<LiveEnums.EventType, DateTime>
			{
				[LiveEnums.EventType.PrerollStart] = currentTime.AddMinutes(35),
				[LiveEnums.EventType.PrerollStop] = currentTime.AddMinutes(40),
				[LiveEnums.EventType.PostrollStart] = currentTime.AddMinutes(50),
				[LiveEnums.EventType.PostrollStop] = currentTime.AddMinutes(55),
			};

			foreach (var expectedTime in expectedTimes)
			{
				var liveEvent = GetEvent(liveConfiguration, expectedTime.Key);

				Assert.AreEqual(LiveEnums.EventState.Confirmed, liveEvent.EventState, $"Expected the {expectedTime.Key} event to stay confirmed.");
				Assert.AreEqual(expectedTime.Value, liveEvent.EventTime?.UtcDateTime, $"Expected the {expectedTime.Key} event to follow the rescheduled job timings.");
			}

			Assert.AreEqual(4, setup.Dms.SchedulerTasks.Count, "Expected one scheduler task per distinct orchestration event time.");
			CollectionAssert.AreEquivalent(
				expectedTimes.Values.Select(x => new DateTimeOffset(x, TimeSpan.Zero).LocalDateTime).ToArray(),
				setup.Dms.SchedulerTasks.Select(x => x.StartTime).ToArray(),
				"Expected the scheduler tasks to be moved to the rescheduled orchestration event times.");
		}

		[TestMethod]
		public void Update_ConfirmedJobWithOnlyLinkChanges_DoesNotSynchronizeLive()
		{
			var setup = CreateSetup();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(5),
				start: currentTime.AddMinutes(10),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25));

			var confirmedJob = Confirm(setup, job);

			// Tampering with a scheduled event gives the synchronization something to undo, so the assertion below
			// fails as soon as a link-only save reaches MediaOps Live.
			var prerollStart = GetEvent(GetLiveConfiguration(setup, confirmedJob), LiveEnums.EventType.PrerollStart);
			SetOrchestrationEventState(setup, prerollStart.ID, LiveEnums.EventState.Cancelled);

			var objectType = setup.Api.RelationshipObjectTypes.Create(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_Booking" });
			confirmedJob.AddLink(new JobLink(objectType, "booking-1"));

			var updatedJob = setup.Api.Jobs.Update(confirmedJob);

			Assert.AreEqual(1, updatedJob.Links.Count, "Expected the link to be saved.");
			Assert.AreEqual(
				LiveEnums.EventState.Cancelled,
				GetEvent(GetLiveConfiguration(setup, updatedJob), LiveEnums.EventType.PrerollStart).EventState,
				"A save that only changed job links must not push anything to MediaOps Live.");
		}

		[TestMethod]
		public void Start_ConfirmedJob_TriggersPreRollStartOnTransitionToRunning()
		{
			var setup = CreateSetup();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(9),
				start: currentTime.AddMinutes(10),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25));

			var startedJob = setup.Api.Jobs.Start(Confirm(setup, job));

			// A manual start only moves the core reservation start; the Confirmed-to-Running transition it drives
			// synchronizes MediaOps Live.
			var runningJob = MakeRunning(setup, startedJob);

			var liveConfiguration = GetLiveConfiguration(setup, runningJob);

			var preRollStart = GetEvent(liveConfiguration, LiveEnums.EventType.PrerollStart);
			Assert.AreEqual(LiveEnums.EventState.Draft, preRollStart.EventState, "Expected the pre-roll start event to be put in draft so it triggers immediately.");
			Assert.IsTrue(
				(DateTimeOffset.UtcNow - preRollStart.EventTime.Value).Duration() < NowTolerance,
				"Expected the pre-roll start event to follow the manual start.");
			CollectionAssert.Contains(GetTriggeredEventIds(setup), preRollStart.ID, "Expected the pre-roll start event to be executed immediately.");

			var preRollStop = GetEvent(liveConfiguration, LiveEnums.EventType.PrerollStop);
			Assert.AreEqual(LiveEnums.EventState.Confirmed, preRollStop.EventState, "Expected the pre-roll stop event to stay scheduled.");
			Assert.AreEqual(currentTime.AddMinutes(10), preRollStop.EventTime?.UtcDateTime, "Expected the job start time to be kept when a pre-roll is configured.");
		}

		[TestMethod]
		public void Stop_RunningJob_TriggersPostRollStartAndReschedulesPostRollStop()
		{
			var setup = CreateSetup();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(-10),
				start: currentTime.AddMinutes(-5),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25));

			var runningJob = MakeRunning(setup, Confirm(setup, job));

			var newPostRollEnd = currentTime.AddMinutes(15);
			var stoppedJob = setup.Api.Jobs.Stop(runningJob, new JobStopOptions { NewPostRollEnd = newPostRollEnd });

			var liveConfiguration = GetLiveConfiguration(setup, stoppedJob);

			var postRollStart = GetEvent(liveConfiguration, LiveEnums.EventType.PostrollStart);
			Assert.AreEqual(LiveEnums.EventState.Draft, postRollStart.EventState, "Expected the post-roll start event to be put in draft so it triggers immediately.");
			Assert.IsTrue(
				(DateTimeOffset.UtcNow - postRollStart.EventTime.Value).Duration() < NowTolerance,
				"Expected the post-roll start event to be moved to (approximately) the current time.");
			CollectionAssert.Contains(GetTriggeredEventIds(setup), postRollStart.ID, "Expected the post-roll start event to be executed immediately.");

			var postRollStop = GetEvent(liveConfiguration, LiveEnums.EventType.PostrollStop);
			Assert.AreEqual(LiveEnums.EventState.Confirmed, postRollStop.EventState, "Expected the post-roll stop event to stay scheduled.");
			Assert.AreEqual(newPostRollEnd, postRollStop.EventTime?.UtcDateTime, "Expected the post-roll stop event to follow the new post-roll end time.");
			Assert.IsNotNull(postRollStop.SchedulerReference, "Expected the post-roll stop event to be rescheduled.");

			CollectionAssert.Contains(
				setup.Dms.SchedulerTasks.Select(x => x.StartTime).ToList(),
				postRollStop.EventTime.Value.LocalDateTime,
				"Expected a scheduler task at the new post-roll end time.");
		}

		[TestMethod]
		public void Stop_RunningJobWithoutPostRoll_TriggersAllEndEvents()
		{
			var setup = CreateSetup();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(-10),
				start: currentTime.AddMinutes(-5),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(20));

			var runningJob = MakeRunning(setup, Confirm(setup, job));

			var stoppedJob = setup.Api.Jobs.Stop(runningJob);

			var liveConfiguration = GetLiveConfiguration(setup, stoppedJob);
			var postRollStart = GetEvent(liveConfiguration, LiveEnums.EventType.PostrollStart);
			var postRollStop = GetEvent(liveConfiguration, LiveEnums.EventType.PostrollStop);

			Assert.AreEqual(LiveEnums.EventState.Draft, postRollStart.EventState, "Expected the post-roll start event to trigger immediately.");
			Assert.AreEqual(LiveEnums.EventState.Draft, postRollStop.EventState, "Expected the post-roll stop event to trigger immediately when there is no post-roll.");

			var triggeredEventIds = GetTriggeredEventIds(setup);
			CollectionAssert.Contains(triggeredEventIds, postRollStart.ID, "Expected the post-roll start event to be executed immediately.");
			CollectionAssert.Contains(triggeredEventIds, postRollStop.ID, "Expected the post-roll stop event to be executed immediately.");
		}

		[TestMethod]
		public void Stop_RunningJob_DoesNotReExecuteEndEventsThatAlreadyRan()
		{
			var setup = CreateSetup();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(-10),
				start: currentTime.AddMinutes(-5),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25));

			var runningJob = MakeRunning(setup, Confirm(setup, job));

			// Mirror MediaOps Live having executed the post-roll start event already.
			var liveJob = setup.LiveApi.Orchestration.GetOrchestrationJobConfiguration(runningJob.Id.ToString());
			var executedEvent = liveJob.OrchestrationEvents.Single(x => x.EventType == LiveEnums.EventType.PostrollStart);
			var executedEventTime = executedEvent.EventTime;
			SetOrchestrationEventState(setup, executedEvent.ID, LiveEnums.EventState.Completed);

			var stoppedJob = setup.Api.Jobs.Stop(runningJob, new JobStopOptions { NewPostRollEnd = currentTime.AddMinutes(22) });

			var liveConfiguration = GetLiveConfiguration(setup, stoppedJob);
			var postRollStart = GetEvent(liveConfiguration, LiveEnums.EventType.PostrollStart);

			Assert.AreEqual(LiveEnums.EventState.Completed, postRollStart.EventState, "Expected an event that already ran to keep its result.");
			Assert.AreEqual(executedEventTime, postRollStart.EventTime, "Expected an event that already ran not to be re-timed.");
			CollectionAssert.DoesNotContain(GetTriggeredEventIds(setup), postRollStart.ID, "Expected an event that already ran not to be executed again.");
		}

		[TestMethod]
		public void TransitionToRunning_DoesNotReExecuteEventsThatWereAlreadyHandedOff()
		{
			var setup = CreateSetup();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(-10),
				start: currentTime.AddMinutes(-5),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25));

			var confirmedJob = Confirm(setup, job);

			var liveConfiguration = GetLiveConfiguration(setup, confirmedJob);
			var preRollStartId = GetEvent(liveConfiguration, LiveEnums.EventType.PrerollStart).ID;
			var preRollStopId = GetEvent(liveConfiguration, LiveEnums.EventType.PrerollStop).ID;

			Assert.AreEqual(1, GetTriggerCount(setup, preRollStartId), "Expected the pre-roll start event to be executed once on confirm.");
			Assert.AreEqual(1, GetTriggerCount(setup, preRollStopId), "Expected the pre-roll stop event to be executed once on confirm.");

			// MediaOps Live records the hand-off to the deferred orchestration script; the events stay in draft state
			// until that script actually starts.
			var handOffTime = DateTimeOffset.UtcNow;
			SetOrchestrationEventActualStartTime(setup, preRollStartId, handOffTime);
			SetOrchestrationEventActualStartTime(setup, preRollStopId, handOffTime);

			var runningJob = MakeRunning(setup, confirmedJob);

			var preRollStart = GetEvent(GetLiveConfiguration(setup, runningJob), LiveEnums.EventType.PrerollStart);
			Assert.AreEqual(LiveEnums.EventState.Draft, preRollStart.EventState, "Expected the event to still be waiting for the deferred script.");
			Assert.AreEqual(1, GetTriggerCount(setup, preRollStartId), "Expected an event that was already handed off not to be executed a second time.");
			Assert.AreEqual(1, GetTriggerCount(setup, preRollStopId), "Expected an event that was already handed off not to be executed a second time.");
		}

		[TestMethod]
		public void Confirm_JobWithHiddenNode_ExcludesHiddenNodeAndItsConnections()
		{
			var setup = CreateSetup();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(5),
				start: currentTime.AddMinutes(10),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25),
				numberOfNodes: 3);

			var nodes = job.NodeGraph.Nodes.ToList();
			job.NodeGraph.Connect(nodes[0], nodes[1]);
			job.NodeGraph.Connect(nodes[1], nodes[2]);
			job = setup.Api.Jobs.Update(job);

			HideNode(setup, job, nodes[1].Id);

			var confirmedJob = Confirm(setup, setup.Api.Jobs.Read(job.Id));

			var liveConfiguration = GetLiveConfiguration(setup, confirmedJob);
			var liveEvent = GetEvent(liveConfiguration, LiveEnums.EventType.PrerollStart);

			CollectionAssert.AreEquivalent(
				new[] { nodes[0].Id, nodes[2].Id },
				liveEvent.Configuration.NodeConfigurations.Select(x => x.NodeId).ToArray(),
				"Expected the hidden node to be excluded from the orchestration configuration.");

			Assert.AreEqual(
				0,
				liveEvent.Configuration.Connections.Count,
				"Expected every connection touching the hidden node to be excluded from the orchestration configuration.");
		}

		[TestMethod]
		public void Confirm_JobWithConnectedNodes_MapsShuffledLevelsOntoLiveConnection()
		{
			var setup = CreateSetup();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(5),
				start: currentTime.AddMinutes(10),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25),
				numberOfNodes: 2);

			var nodes = job.NodeGraph.Nodes.ToList();
			job.NodeGraph.Connect(nodes[0], nodes[1]);
			job.NodeGraph.Connections.Single().Configuration = new ShuffleLevelBasedConnectionConfiguration()
				.AddLevelMapping(destinationLevel: 2, sourceLevel: 1);
			job = setup.Api.Jobs.Update(job);

			var sourceResource = setup.Api.Resources.Read(((JobResourceNode)nodes[0]).ResourceId);
			var destinationResource = setup.Api.Resources.Read(((JobResourceNode)nodes[1]).ResourceId);

			var confirmedJob = Confirm(setup, job);

			var liveConfiguration = GetLiveConfiguration(setup, confirmedJob);
			var liveConnection = GetEvent(liveConfiguration, LiveEnums.EventType.PrerollStart).Configuration.Connections.Single();

			Assert.AreEqual(nodes[0].Id, liveConnection.SourceNodeId, "Expected the connection to start at the source node.");
			Assert.AreEqual(nodes[1].Id, liveConnection.DestinationNodeId, "Expected the connection to end at the destination node.");
			Assert.AreEqual(sourceResource.VirtualSignalGroupOutputId, liveConnection.SourceVsg?.ID, "Expected the output virtual signal group of the source resource.");
			Assert.AreEqual(destinationResource.VirtualSignalGroupInputId, liveConnection.DestinationVsg?.ID, "Expected the input virtual signal group of the destination resource.");

			var levelMapping = liveConnection.LevelMappings.Single();
			Assert.AreEqual(1, levelMapping.Source.Number, "Expected the shuffled source level to be resolved.");
			Assert.AreEqual(2, levelMapping.Destination.Number, "Expected the shuffled destination level to be resolved.");
		}

		[TestMethod]
		public void Update_ConfirmedJobWithOnlyPropertyChanges_SynchronizesLive()
		{
			var setup = CreateSetup();
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var jobProperty = (StringProperty)setup.Api.SchedulingProperties.Create(new StringProperty
			{
				Name = $"{prefix}_JobProperty",
				SectionName = "General",
			});

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(5),
				start: currentTime.AddMinutes(10),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25));

			var confirmedJob = Confirm(setup, job);

			var prerollStart = GetEvent(GetLiveConfiguration(setup, confirmedJob), LiveEnums.EventType.PrerollStart);
			SetOrchestrationEventState(setup, prerollStart.ID, LiveEnums.EventState.Cancelled);

			confirmedJob.AddProperty(new StringPropertySetting(jobProperty) { Value = "Property value" });

			var updatedJob = setup.Api.Jobs.Update(confirmedJob);

			Assert.AreEqual(
				LiveEnums.EventState.Confirmed,
				GetEvent(GetLiveConfiguration(setup, updatedJob), LiveEnums.EventType.PrerollStart).EventState,
				"A property change must still reach MediaOps Live.");
		}

		[TestMethod]
		public void Update_ConfirmedJobWithOnlyOrchestrationChanges_SynchronizesLive()
		{
			var setup = CreateSetup();
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var textConfiguration = (TextConfiguration)setup.Api.Configurations.Create(new TextConfiguration { Name = $"{prefix}_Text" });

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(5),
				start: currentTime.AddMinutes(10),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25));

			var node = job.NodeGraph.Nodes.Single();
			node.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
			{
				new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails(OrchestrationScriptName)
						.AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "Before" }),
				},
			});

			job = setup.Api.Jobs.Update(job);

			var confirmedJob = Confirm(setup, job);

			// Changing only a configured value keeps the node fully configured, so the job instance does not change.
			var confirmedNode = confirmedJob.NodeGraph.Nodes.Single();
			confirmedNode.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
			{
				new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails(OrchestrationScriptName)
						.AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "After" }),
				},
			});

			var updatedJob = setup.Api.Jobs.Update(confirmedJob);

			var nodeConfiguration = GetEvent(GetLiveConfiguration(setup, updatedJob), LiveEnums.EventType.PrerollStart)
				.Configuration.NodeConfigurations.Single();

			Assert.AreEqual(
				"After",
				nodeConfiguration.Profile.Values.Single(x => x.Name == textConfiguration.Id.ToString()).Value.StringValue,
				"An orchestration change must still reach MediaOps Live.");
		}

		[TestMethod]
		public void Update_ConfirmedJobWithLinkAndPropertyChanges_SynchronizesLive()
		{
			var setup = CreateSetup();
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var jobProperty = (StringProperty)setup.Api.SchedulingProperties.Create(new StringProperty
			{
				Name = $"{prefix}_JobProperty",
				SectionName = "General",
			});

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(5),
				start: currentTime.AddMinutes(10),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25));

			var confirmedJob = Confirm(setup, job);

			var prerollStart = GetEvent(GetLiveConfiguration(setup, confirmedJob), LiveEnums.EventType.PrerollStart);
			SetOrchestrationEventState(setup, prerollStart.ID, LiveEnums.EventState.Cancelled);

			// Property values are stored outside the job instance, so only the property scope marks this as more
			// than a link-only save. An orchestration parameter can reference a job property.
			var objectType = setup.Api.RelationshipObjectTypes.Create(new RelationshipObjectType { Name = $"{prefix}_Booking" });
			confirmedJob.AddLink(new JobLink(objectType, "booking-1"));
			confirmedJob.AddProperty(new StringPropertySetting(jobProperty) { Value = "Property value" });

			var updatedJob = setup.Api.Jobs.Update(confirmedJob);

			Assert.AreEqual(1, updatedJob.Links.Count, "Expected the link to be saved.");
			Assert.AreEqual(
				LiveEnums.EventState.Confirmed,
				GetEvent(GetLiveConfiguration(setup, updatedJob), LiveEnums.EventType.PrerollStart).EventState,
				"A property change must still reach MediaOps Live, even when links changed in the same save.");
		}

		[TestMethod]
		public void Update_ConfirmedJobWithLinkAndOrchestrationChanges_SynchronizesLive()
		{
			var setup = CreateSetup();
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var textConfiguration = (TextConfiguration)setup.Api.Configurations.Create(new TextConfiguration { Name = $"{prefix}_Text" });

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(5),
				start: currentTime.AddMinutes(10),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25));

			var node = job.NodeGraph.Nodes.Single();
			node.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
			{
				new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails(OrchestrationScriptName)
						.AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "Before" }),
				},
			});

			job = setup.Api.Jobs.Update(job);

			var confirmedJob = Confirm(setup, job);

			var prerollStart = GetEvent(GetLiveConfiguration(setup, confirmedJob), LiveEnums.EventType.PrerollStart);
			SetOrchestrationEventState(setup, prerollStart.ID, LiveEnums.EventState.Cancelled);

			var objectType = setup.Api.RelationshipObjectTypes.Create(new RelationshipObjectType { Name = $"{prefix}_Booking" });
			confirmedJob.AddLink(new JobLink(objectType, "booking-1"));

			// Changing only a configured value keeps the node fully configured, so the job instance itself does not
			// change. The orchestration settings still have to reach MediaOps Live.
			var confirmedNode = confirmedJob.NodeGraph.Nodes.Single();
			confirmedNode.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
			{
				new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails(OrchestrationScriptName)
						.AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "After" }),
				},
			});

			var updatedJob = setup.Api.Jobs.Update(confirmedJob);

			Assert.AreEqual(1, updatedJob.Links.Count, "Expected the link to be saved.");

			var liveConfiguration = GetLiveConfiguration(setup, updatedJob);
			var nodeConfiguration = GetEvent(liveConfiguration, LiveEnums.EventType.PrerollStart).Configuration.NodeConfigurations.Single();

			Assert.AreEqual(
				"After",
				nodeConfiguration.Profile.Values.Single(x => x.Name == textConfiguration.Id.ToString()).Value.StringValue,
				"An orchestration change must still reach MediaOps Live, even when links changed in the same save.");
		}

		[TestMethod]
		public void Confirm_JobWithNodeScriptSettings_MapsProfileInputsOntoLiveNodeConfiguration()
		{
			var setup = CreateSetup();
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var textConfiguration = (TextConfiguration)setup.Api.Configurations.Create(new TextConfiguration { Name = $"{prefix}_Text" });
			var numberConfiguration = (NumberConfiguration)setup.Api.Configurations.Create(new NumberConfiguration { Name = $"{prefix}_Number" });

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(5),
				start: currentTime.AddMinutes(10),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25));

			var node = job.NodeGraph.Nodes.Single();
			node.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
			{
				new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails(OrchestrationScriptName)
						.AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "HelloWorld" })
						.AddConfiguration(new NumberConfigurationSetting(numberConfiguration) { Value = 42 }),
				},
			});

			job = setup.Api.Jobs.Update(job);

			var confirmedJob = Confirm(setup, job);

			var liveConfiguration = GetLiveConfiguration(setup, confirmedJob);
			var nodeConfiguration = GetEvent(liveConfiguration, LiveEnums.EventType.PrerollStart).Configuration.NodeConfigurations.Single();

			Assert.AreEqual(OrchestrationScriptName, nodeConfiguration.OrchestrationScriptName, "Expected the orchestration script of the node to be pushed to MediaOps Live.");

			var textValue = nodeConfiguration.Profile.Values.Single(x => x.Name == textConfiguration.Id.ToString());
			Assert.AreEqual(ProfileParameterValue.ValueType.String, textValue.Value.Type, "Expected a text configuration to become a string profile value.");
			Assert.AreEqual("HelloWorld", textValue.Value.StringValue, "Expected the configured text value.");

			var numberValue = nodeConfiguration.Profile.Values.Single(x => x.Name == numberConfiguration.Id.ToString());
			Assert.AreEqual(ProfileParameterValue.ValueType.Double, numberValue.Value.Type, "Expected a number configuration to become a double profile value.");
			Assert.AreEqual(42d, numberValue.Value.DoubleValue, "Expected the configured number value.");

			// Only the node has script settings, so the event itself has no global script.
			Assert.IsNull(GetEvent(liveConfiguration, LiveEnums.EventType.PrerollStart).GlobalOrchestrationScript, "Expected no global orchestration script on the event.");
			Assert.AreEqual(
				0,
				GetEvent(liveConfiguration, LiveEnums.EventType.PrerollStop).Configuration.NodeConfigurations.Single().Profile.Values.Count,
				"Expected the profile inputs to be limited to the orchestration event type they are configured for.");
		}

		[TestMethod]
		public void Delete_ConfirmedJob_RemovesOrchestrationJobAndEvents()
		{
			var setup = CreateSetup();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = CreateJob(
				setup,
				preRollStart: currentTime.AddMinutes(5),
				start: currentTime.AddMinutes(10),
				end: currentTime.AddMinutes(20),
				postRollEnd: currentTime.AddMinutes(25));

			var confirmedJob = Confirm(setup, job);
			Assert.AreEqual(4, GetLiveConfiguration(setup, confirmedJob).OrchestrationEvents.Count, "Expected the confirmed job to have orchestration events.");

			setup.Api.Jobs.Delete(confirmedJob, new JobDeleteOptions { ForceDelete = true });

			Assert.IsNull(setup.Api.Jobs.Read(confirmedJob.Id), "Expected the job to be deleted.");

			var liveConfiguration = GetLiveConfiguration(setup, confirmedJob);
			Assert.AreEqual(0, liveConfiguration?.OrchestrationEvents.Count ?? 0, "Expected the orchestration events of a deleted job to be removed.");
			Assert.AreEqual(0, setup.Dms.SchedulerTasks.Count, "Expected the scheduler tasks of the deleted orchestration events to be removed.");
		}

		private sealed class TestSetup
		{
			public TestSetup(SimulatedDms dms, IConnection connection, IMediaOpsPlanApi api, IMediaOpsLiveApi liveApi, ResourceManagerHelper resourceManagerHelper, ErrorCollectingLogger logger)
			{
				Dms = dms;
				Connection = connection;
				Api = api;
				LiveApi = liveApi;
				ResourceManagerHelper = resourceManagerHelper;
				Logger = logger;
			}

			public SimulatedDms Dms { get; }

			public IConnection Connection { get; }

			public IMediaOpsPlanApi Api { get; }

			public IMediaOpsLiveApi LiveApi { get; }

			public ResourceManagerHelper ResourceManagerHelper { get; }

			public ErrorCollectingLogger Logger { get; }
		}

		private sealed class ErrorCollectingLogger : ILogger
		{
			public List<string> Errors { get; } = new List<string>();

			public void Debug(object callerInstance, string message, object[]? args = null, string methodName = "")
			{
			}

			public void Debug(string message)
			{
			}

			public void Error(object callerInstance, string message, object[]? args = null, string methodName = "")
			{
				Errors.Add(message);
			}

			public void Error(string message)
			{
				Errors.Add(message);
			}

			public void Information(object callerInstance, string message, object[]? args = null, string methodName = "")
			{
			}

			public void Information(string message)
			{
			}

			public void Warning(object callerInstance, string message, object[]? args = null, string methodName = "")
			{
			}

			public void Warning(string message)
			{
			}
		}
	}
}
