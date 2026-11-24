namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.ManagerStore.Select;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	using JobInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.JobInfo;
	using Nodetype = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Enums.Nodetype;

	[TestClass]
	public sealed class JobPartialFieldMapperTests
	{
		[TestMethod]
		public void CreateSelectedFields_SelectsEachDomFieldOnceIncludingFallbacks()
		{
			var selectedFields = JobPartialFieldMapper.CreateSelectedFields(new FieldExposer[] { JobExposers.Start, JobExposers.PreRollStart, JobExposers.Duration, JobExposers.Name });

			CollectionAssert.AreEquivalent(
				new[] { FieldName(JobInfo.JobStart), FieldName(JobInfo.Preroll), FieldName(JobInfo.JobEnd), FieldName(JobInfo.JobName) },
				selectedFields.Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_StateOrOnlyId_SelectsStatus()
		{
			CollectionAssert.AreEqual(new[] { "StatusId" }, JobPartialFieldMapper.CreateSelectedFields(new[] { JobExposers.State }).Fields.Select(x => x.FieldName).ToList());
			CollectionAssert.AreEqual(new[] { "StatusId" }, JobPartialFieldMapper.CreateSelectedFields(new[] { JobExposers.Id }).Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_UnsupportedField_ThrowsNotSupportedException()
		{
			var unknownField = new Exposer<Job, string>(x => x.Name, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => JobPartialFieldMapper.CreateSelectedFields(new[] { unknownField }));
			Assert.ThrowsException<NotSupportedException>(() => JobPartialFieldMapper.CreateSelectedFields(new[] { ResourceExposers.Name }));
		}

		[TestMethod]
		public void CreateSelectedFields_MultiSectionAndExternalFields_SelectTheFieldsTheyAreCombinedFrom()
		{
			var selectedFields = JobPartialFieldMapper.CreateSelectedFields(new FieldExposer[] { JobExposers.ResourcePools, JobExposers.Errors.Message, JobExposers.Capabilities.CapabilityId });

			CollectionAssert.AreEquivalent(
				new[]
				{
					FieldName(SlcWorkflowIds.Sections.Nodes.NodeType),
					FieldName(SlcWorkflowIds.Sections.Nodes.NodeReferenceID),
					FieldName(SlcWorkflowIds.Sections.Nodes.NodeParentReferenceID),
					FieldName(SlcWorkflowIds.Sections.Errors.ErrorCode),
					FieldName(SlcWorkflowIds.Sections.Errors.ErrorMessage),
					FieldName(SlcWorkflowIds.Sections.JobExecution.JobConfiguration),
				},
				selectedFields.Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreatePartialJob_ConvertsValuesLikeAFullRead()
		{
			var id = Guid.NewGuid();
			var recurringJobId = Guid.NewGuid();
			var start = new DateTime(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);
			var end = start.AddHours(2);

			var domPartialObject = CreateDomPartialObject(
				id,
				Field<string>(JobInfo.JobName, "Job"),
				Field<DateTime>(JobInfo.JobStart, start),
				Field<DateTime>(JobInfo.JobEnd, end),
				Field<string>(JobInfo.JobSeriesID, recurringJobId.ToString()),
				Field<int>(JobInfo.JobPriority, (int)SlcWorkflowIds.Enums.Jobpriority.High),
				("StatusId", new PartialObjectValue<string> { Value = SlcWorkflowIds.Behaviors.Job_Behavior.Statuses.Confirmed }));

			var partialJob = CreatePartialJob(domPartialObject, new FieldExposer[]
			{
				JobExposers.Id,
				JobExposers.Name,
				JobExposers.Start,
				JobExposers.PreRollStart,
				JobExposers.PostRollEnd,
				JobExposers.Duration,
				JobExposers.RecurringJobId,
				JobExposers.Priority,
				JobExposers.State,
			});

			Assert.AreEqual(id, partialJob.Id);
			Assert.AreEqual(id, partialJob.GetValue(JobExposers.Id));
			Assert.AreEqual("Job", partialJob.GetValue(JobExposers.Name));
			Assert.AreEqual(new DateTimeOffset(start), partialJob.GetValue(JobExposers.Start));
			Assert.AreEqual(new DateTimeOffset(start), partialJob.GetValue(JobExposers.PreRollStart), "Without pre-roll, the pre-roll start falls back to the start.");
			Assert.AreEqual(new DateTimeOffset(end), partialJob.GetValue(JobExposers.PostRollEnd), "Without post-roll, the post-roll end falls back to the end.");
			Assert.AreEqual(TimeSpan.FromHours(2), partialJob.GetValue(JobExposers.Duration));
			Assert.AreEqual(recurringJobId, partialJob.GetValue(JobExposers.RecurringJobId));
			Assert.AreEqual(JobPriority.High, partialJob.GetValue(JobExposers.Priority));
			Assert.AreEqual(JobState.Confirmed, partialJob.GetValue(JobExposers.State));
			Assert.IsFalse(partialJob.HasField(JobExposers.End), "Fields only read as a fallback aren't exposed.");
		}

		[TestMethod]
		public void CreatePartialJob_MissingValues_UseTheDefaultsOfAFullRead()
		{
			var partialJob = CreatePartialJob(CreateDomPartialObject(Guid.NewGuid()), new FieldExposer[]
			{
				JobExposers.Name,
				JobExposers.RecurringJobId,
				JobExposers.ActionRequired,
				JobExposers.Priority,
				JobExposers.ConfigurationState,
				JobExposers.OrganizationId,
			});

			Assert.IsNull(partialJob.GetValue(JobExposers.Name));
			Assert.AreEqual(Guid.Empty, partialJob.GetValue(JobExposers.RecurringJobId));
			Assert.IsFalse(partialJob.GetValue(JobExposers.ActionRequired));
			Assert.AreEqual(JobPriority.Normal, partialJob.GetValue(JobExposers.Priority));
			Assert.AreEqual(ConfigurationState.Unknown, partialJob.GetValue(JobExposers.ConfigurationState));
			Assert.AreEqual(Guid.Empty, partialJob.GetValue(JobExposers.OrganizationId));
		}

		[TestMethod]
		public void CreatePartialJob_NodeSections_AreCombinedPerSection()
		{
			var poolId = Guid.NewGuid();
			var otherPoolId = Guid.NewGuid();
			var resourceId = Guid.NewGuid();

			// A resource pool node, a resource node and a resource pool node. Only the resource node has a parent reference.
			var domPartialObject = CreateDomPartialObject(
				Guid.NewGuid(),
				Fields(SlcWorkflowIds.Sections.Nodes.NodeType, (int)Nodetype.ResourcePool, (int)Nodetype.Resource, (int)Nodetype.ResourcePool),
				Fields(SlcWorkflowIds.Sections.Nodes.NodeReferenceID, poolId.ToString(), resourceId.ToString(), otherPoolId.ToString()),
				Fields(SlcWorkflowIds.Sections.Nodes.NodeParentReferenceID, poolId.ToString()),
				Fields(
					SlcWorkflowIds.Sections.Nodes.NodeConfigurationStatus,
					(int)SlcWorkflowIds.Enums.Nodeconfigurationstatus.AllValuesProvided,
					(int)SlcWorkflowIds.Enums.Nodeconfigurationstatus.MandatoryValuesMissing,
					(int)SlcWorkflowIds.Enums.Nodeconfigurationstatus.NoParametersDefined));

			var partialJob = CreatePartialJob(domPartialObject, JobExposers.Resources, JobExposers.ResourcePools, JobExposers.Nodes.ConfigurationState);

			CollectionAssert.AreEqual(new[] { resourceId }, partialJob.GetValues(JobExposers.Resources).ToList());
			CollectionAssert.AreEqual(new[] { poolId, poolId, otherPoolId }, partialJob.GetValues(JobExposers.ResourcePools).ToList());
			CollectionAssert.AreEqual(
				new[] { ConfigurationState.AllValuesProvided, ConfigurationState.MandatoryValuesMissing, ConfigurationState.NoParametersDefined },
				partialJob.GetValues(JobExposers.Nodes.ConfigurationState).ToList());
		}

		[TestMethod]
		public void CreatePartialJob_ErrorSections_AreParsedLikeAFullRead()
		{
			// The second occurrence of code A keeps the position of the first one but replaces its message, and an error without a code is ignored.
			var domPartialObject = CreateDomPartialObject(
				Guid.NewGuid(),
				Fields(SlcWorkflowIds.Sections.Errors.ErrorCode, "A", "B", " ", "A"),
				Fields(SlcWorkflowIds.Sections.Errors.ErrorMessage, "First A", "B", "Ignored", "Second A"));

			var partialJob = CreatePartialJob(domPartialObject, JobExposers.HasError, JobExposers.Errors.Code, JobExposers.Errors.Message);

			Assert.IsTrue(partialJob.GetValue(JobExposers.HasError));
			CollectionAssert.AreEqual(new[] { "A", "B" }, partialJob.GetValues(JobExposers.Errors.Code).ToList());
			CollectionAssert.AreEqual(new[] { "Second A", "B" }, partialJob.GetValues(JobExposers.Errors.Message).ToList());
		}

		[TestMethod]
		public void CreatePartialJob_SectionsThatCantBeCombined_ReadTheFullInstance()
		{
			// One of the two nodes has no configuration state, so the states can't be matched to the nodes by position.
			var domPartialObject = CreateDomPartialObject(
				Guid.NewGuid(),
				Fields(SlcWorkflowIds.Sections.Nodes.NodeType, (int)Nodetype.ResourcePool, (int)Nodetype.ResourcePool),
				Fields(SlcWorkflowIds.Sections.Nodes.NodeConfigurationStatus, (int)SlcWorkflowIds.Enums.Nodeconfigurationstatus.AllValuesProvided));

			var exception = Assert.ThrowsException<InvalidOperationException>(() => CreatePartialJob(domPartialObject, JobExposers.Nodes.ConfigurationState));
			StringAssert.Contains(exception.Message, "stored outside the job DOM instance");
		}

		[TestMethod]
		public void CreatePartialJob_NoErrorsOrNodes_ReturnsNoValues()
		{
			var partialJob = CreatePartialJob(CreateDomPartialObject(Guid.NewGuid()), JobExposers.HasError, JobExposers.Errors.Code, JobExposers.Resources, JobExposers.Nodes.ConfigurationState);

			Assert.IsFalse(partialJob.GetValue(JobExposers.HasError));
			Assert.AreEqual(0, partialJob.GetValues(JobExposers.Errors.Code).Count());
			Assert.AreEqual(0, partialJob.GetValues(JobExposers.Resources).Count());
			Assert.AreEqual(0, partialJob.GetValues(JobExposers.Nodes.ConfigurationState).Count());
		}

		private static Skyline.DataMiner.SDM.PartialObject<Job> CreatePartialJob(PartialObject<DomInstance, DomInstanceId> domPartialObject, params FieldExposer[] fields)
		{
			return JobPartialFieldMapper.CreatePartialJobs(null, new[] { domPartialObject }, fields).Single();
		}

		private static string FieldName(FieldDescriptorID descriptor)
		{
			return $"FieldValues.{descriptor.Id}";
		}

		// The DOM stores a selected field value as a list under the name of its field exposer, to support multiple sections.
		private static (string Key, IPartialObjectValue Value) Field<T>(FieldDescriptorID descriptor, T value)
		{
			return Fields(descriptor, value);
		}

		private static (string Key, IPartialObjectValue Value) Fields<T>(FieldDescriptorID descriptor, params T[] values)
		{
			return (FieldName(descriptor), new PartialObjectValue<List<T>> { Value = values.ToList() });
		}

		private static PartialObject<DomInstance, DomInstanceId> CreateDomPartialObject(Guid id, params (string Key, IPartialObjectValue Value)[] values)
		{
			return new PartialObject<DomInstance, DomInstanceId>(new DomInstanceId(id), values.ToDictionary(x => x.Key, x => x.Value));
		}
	}
}
