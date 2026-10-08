namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Logging;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	using JobError = Skyline.DataMiner.Solutions.MediaOps.Plan.API.JobError;
	using ResourcePool = Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool;

	[TestClass]
	public sealed class SetOrchestrationStateSimulationTests
	{
		private static (IMediaOpsPlanApi Api, CollectingLogger Logger) CreateContext()
		{
			var dms = MediaOpsPlanSimulation.Create();
			var api = dms.CreateConnection().GetMediaOpsPlanApi();

			var logger = new CollectingLogger();
			api.SetLogger(logger);

			return (api, logger);
		}

		private static Job CreateDraftJob(IMediaOpsPlanApi api, DateTime start, DateTime end, bool withResourceNode = false)
		{
			var prefix = Guid.NewGuid();

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = start,
				End = end,
				PreRollStart = start,
				PostRollEnd = end,
			};

			if (withResourceNode)
			{
				var pool = api.ResourcePools.Complete(api.ResourcePools.Create(new ResourcePool { Name = $"{prefix}_Pool" }));
				var resource = api.Resources.Complete(api.Resources.Create(new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool)));
				job.NodeGraph.Add(new JobResourceNode(pool, resource));
			}

			return api.Jobs.Create(job);
		}

		private static OrchestrationUpdateDetails Failed(OrchestrationEventType eventType, string message)
		{
			return new OrchestrationUpdateDetails { Event = eventType, EventState = OrchestrationEventState.Failed, Message = message };
		}

		[TestMethod]
		[DataRow(OrchestrationEventType.PrerollStart, "LIV101")]
		[DataRow(OrchestrationEventType.PrerollStop, "LIV102")]
		[DataRow(OrchestrationEventType.PostrollStart, "LIV103")]
		[DataRow(OrchestrationEventType.PostrollStop, "LIV104")]
		public void Failed_AddsMatchingError(OrchestrationEventType eventType, string expectedCode)
		{
			var (api, logger) = CreateContext();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();
			var job = CreateDraftJob(api, currentTime.AddMinutes(10), currentTime.AddMinutes(20));

			api.Jobs.SetOrchestrationState(job.Id, Failed(eventType, "failure"));

			var read = api.Jobs.Read(job.Id);
			Assert.IsTrue(read.Errors.Contains(new JobError(expectedCode, "failure")));
			Assert.IsTrue(logger.Information.Any(x => x.Contains($"Set error {expectedCode}")), "Expected the persisted error to be logged.");
			Assert.AreEqual(0, logger.Errors.Count, String.Join(" | ", logger.Errors));
		}

		[TestMethod]
		public void Succeeded_IsIgnoredAndLogged()
		{
			var (api, logger) = CreateContext();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();
			var job = CreateDraftJob(api, currentTime.AddMinutes(10), currentTime.AddMinutes(20));

			api.Jobs.SetOrchestrationState(job.Id, new OrchestrationUpdateDetails { Event = OrchestrationEventType.PrerollStart, EventState = OrchestrationEventState.Succeeded });

			Assert.AreEqual(0, api.Jobs.Read(job.Id).Errors.Count);
			Assert.IsTrue(logger.Information.Any(x => x.Contains("Received orchestration update")), "Expected the incoming update to be logged.");
			Assert.IsTrue(logger.Information.Any(x => x.Contains("Ignored orchestration update")), "Expected the ignored update to be logged.");
		}

		[TestMethod]
		public void FailedTwice_UpdatesMessageOfSingleError()
		{
			var (api, logger) = CreateContext();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();
			var job = CreateDraftJob(api, currentTime.AddMinutes(10), currentTime.AddMinutes(20));

			api.Jobs.SetOrchestrationState(job.Id, Failed(OrchestrationEventType.PrerollStart, "first"));
			api.Jobs.SetOrchestrationState(job.Id, Failed(OrchestrationEventType.PrerollStart, "second"));
			api.Jobs.SetOrchestrationState(job.Id, Failed(OrchestrationEventType.PrerollStart, "second"));

			var errors = api.Jobs.Read(job.Id).Errors;
			Assert.AreEqual(1, errors.Count);
			Assert.AreEqual(new JobError("LIV101", "second"), errors.Single());
			Assert.IsTrue(logger.Information.Any(x => x.Contains("already present")), "Expected the unchanged error to be logged.");
		}

		[TestMethod]
		public void Failed_PreservesExistingErrors()
		{
			var (api, _) = CreateContext();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = new Job
			{
				Name = $"{Guid.NewGuid()}_Job",
				Start = currentTime.AddMinutes(10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(10),
				PostRollEnd = currentTime.AddMinutes(20),
			};
			job.AddError(new JobError("LIV102", "existing")).AddError(new JobError("CUSTOM", "custom"));
			job = api.Jobs.Create(job);

			api.Jobs.SetOrchestrationState(job.Id, Failed(OrchestrationEventType.PrerollStart, "failure"));

			CollectionAssert.AreEquivalent(
				new[] { new JobError("LIV102", "existing"), new JobError("CUSTOM", "custom"), new JobError("LIV101", "failure") },
				api.Jobs.Read(job.Id).Errors.ToArray());
		}

		[TestMethod]
		public void Failed_OnCompletedJob_AddsError()
		{
			var (api, logger) = CreateContext();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();
			var job = CreateDraftJob(api, currentTime.AddMinutes(-20), currentTime.AddMinutes(-10));
			job = api.Jobs.MarkAsCompleted(job);
			Assert.AreEqual(JobState.Completed, job.State);

			api.Jobs.SetOrchestrationState(job.Id, Failed(OrchestrationEventType.PostrollStop, "failure"));

			Assert.IsTrue(api.Jobs.Read(job.Id).Errors.Contains(new JobError("LIV104", "failure")));
			Assert.AreEqual(0, logger.Errors.Count, String.Join(" | ", logger.Errors));
		}

		[TestMethod]
		public void Failed_OnCanceledJob_AddsError()
		{
			var (api, logger) = CreateContext();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();
			var job = CreateDraftJob(api, currentTime.AddMinutes(10), currentTime.AddMinutes(20), withResourceNode: true);
			job = api.Jobs.Cancel(api.Jobs.SaveAsTentative(job));
			Assert.AreEqual(JobState.Canceled, job.State);

			api.Jobs.SetOrchestrationState(job.Id, Failed(OrchestrationEventType.PrerollStart, "failure"));

			Assert.IsTrue(api.Jobs.Read(job.Id).Errors.Contains(new JobError("LIV101", "failure")));
			Assert.AreEqual(0, logger.Errors.Count, String.Join(" | ", logger.Errors));
		}

		[TestMethod]
		public void Failed_UnknownJob_ThrowsAndLogsError()
		{
			var (api, logger) = CreateContext();

			var exception = Assert.ThrowsException<MediaOpsException>(() =>
				api.Jobs.SetOrchestrationState(Guid.NewGuid(), Failed(OrchestrationEventType.PrerollStart, "failure")));

			Assert.IsTrue(exception.TraceData.ErrorData.OfType<JobNotFoundError>().Any());
			Assert.IsTrue(logger.Errors.Any(x => x.Contains("Failed to process orchestration update")), "Expected the failure to be logged.");
		}

		[TestMethod]
		public void Confirm_WithSnapshotReadBeforeLiveError_KeepsLiveError()
		{
			var (api, _) = CreateContext();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			// The start in the past makes the confirm adapt and re-save the node start times.
			var job = CreateDraftJob(api, currentTime.AddMinutes(-5), currentTime.AddMinutes(20), withResourceNode: true);
			var tentativeJob = api.Jobs.SaveAsTentative(job);

			api.Jobs.SetOrchestrationState(tentativeJob.Id, Failed(OrchestrationEventType.PrerollStart, "failure"));

			// The handler is invoked directly because the repository re-reads the job by ID and would hide the stale snapshot.
			Assert.IsTrue(DomJobHandler.TryConfirm((MediaOpsPlanApi)api, [tentativeJob], out _));

			var confirmed = api.Jobs.Read(job.Id);
			Assert.AreEqual(JobState.Confirmed, confirmed.State);
			Assert.IsTrue(confirmed.Errors.Contains(new JobError("LIV101", "failure")));
		}

		private sealed class CollectingLogger : ILogger
		{
			public List<string> Information { get; } = new List<string>();

			public List<string> Errors { get; } = new List<string>();

			public void Debug(object callerInstance, string message, object[]? args = null, string methodName = "")
			{
			}

			public void Debug(string message)
			{
			}

			public void Error(object callerInstance, string message, object[]? args = null, string methodName = "")
			{
				Errors.Add(Format(message, args));
			}

			public void Error(string message)
			{
				Errors.Add(message);
			}

			void ILogger.Information(object callerInstance, string message, object[]? args, string methodName)
			{
				Information.Add(Format(message, args));
			}

			void ILogger.Information(string message)
			{
				Information.Add(message);
			}

			public void Warning(object callerInstance, string message, object[]? args = null, string methodName = "")
			{
			}

			public void Warning(string message)
			{
			}

			private static string Format(string message, object[]? args)
			{
				return args != null && args.Length > 0 ? String.Format(message, args) : message;
			}
		}
	}
}
