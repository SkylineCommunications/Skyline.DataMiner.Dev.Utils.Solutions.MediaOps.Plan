namespace RT_MediaOps.Plan.Workflow.RecurringJobs
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
	using RecurringInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.RecurringInfo;

	[TestClass]
	public sealed class RecurringJobPartialFieldMapperTests
	{
		[TestMethod]
		public void CreateSelectedFields_SelectsEachDomFieldOnce()
		{
			var selectedFields = RecurringJobPartialFieldMapper.CreateSelectedFields(new FieldExposer[] { RecurringJobExposers.Start, RecurringJobExposers.Duration, RecurringJobExposers.Name, RecurringJobExposers.Name, RecurringJobExposers.Pattern.EndDate });

			CollectionAssert.AreEquivalent(
				new[] { FieldName(JobInfo.JobStart), FieldName(RecurringInfo.Duration), FieldName(JobInfo.JobName), FieldName(RecurringInfo.RecurringPattern) },
				selectedFields.Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_OnlyFieldsNotReadFromDomFields_SelectsStatus()
		{
			CollectionAssert.AreEqual(new[] { "StatusId" }, RecurringJobPartialFieldMapper.CreateSelectedFields(new[] { RecurringJobExposers.Id }).Fields.Select(x => x.FieldName).ToList());
			CollectionAssert.AreEqual(new[] { "StatusId" }, RecurringJobPartialFieldMapper.CreateSelectedFields(new[] { RecurringJobExposers.Priority }).Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_UnsupportedField_ThrowsNotSupportedException()
		{
			var unknownField = new Exposer<RecurringJob, string>(x => x.Name, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => RecurringJobPartialFieldMapper.CreateSelectedFields(new[] { unknownField }));
			Assert.ThrowsException<NotSupportedException>(() => RecurringJobPartialFieldMapper.CreateSelectedFields(new[] { JobExposers.Name }));
		}

		[TestMethod]
		public void CreateSelectedFields_NodeFields_SelectTheFieldsTheyAreCombinedFrom()
		{
			var selectedFields = RecurringJobPartialFieldMapper.CreateSelectedFields(new FieldExposer[] { RecurringJobExposers.Resources, RecurringJobExposers.ResourcePools });

			CollectionAssert.AreEquivalent(
				new[]
				{
					FieldName(SlcWorkflowIds.Sections.Nodes.NodeType),
					FieldName(SlcWorkflowIds.Sections.Nodes.NodeReferenceID),
					FieldName(SlcWorkflowIds.Sections.Nodes.NodeParentReferenceID),
				},
				selectedFields.Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreatePartialRecurringJob_ConvertsValuesLikeAFullRead()
		{
			var id = Guid.NewGuid();
			var start = new DateTime(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);
			var endDate = new DateTimeOffset(2026, 12, 24, 0, 0, 0, TimeSpan.Zero);
			var pattern = new RecurringPattern { RepeatType = RepeatType.Daily, RepeatEvery = 1, EndDate = endDate };

			var domPartialObject = CreateDomPartialObject(
				id,
				Field<string>(JobInfo.JobName, "Recurring job"),
				Field<string>(JobInfo.JobSource, "Category"),
				Field<DateTime>(JobInfo.JobStart, start),
				Field<TimeSpan>(RecurringInfo.Duration, TimeSpan.FromHours(2)),
				Field<int>(RecurringInfo.DesiredJobStatus, (int)SlcWorkflowIds.Enums.Desiredjobstatus.Tentative),
				Field<string>(RecurringInfo.RecurringPattern, pattern.Serialize()));

			var partialRecurringJob = CreatePartialRecurringJob(domPartialObject, new FieldExposer[]
			{
				RecurringJobExposers.Id,
				RecurringJobExposers.Name,
				RecurringJobExposers.JobTypeCategoryId,
				RecurringJobExposers.Start,
				RecurringJobExposers.Duration,
				RecurringJobExposers.DesiredJobState,
				RecurringJobExposers.Pattern.EndDate,
			});

			Assert.AreEqual(id, partialRecurringJob.Id);
			Assert.AreEqual(id, partialRecurringJob.GetValue(RecurringJobExposers.Id));
			Assert.AreEqual("Recurring job", partialRecurringJob.GetValue(RecurringJobExposers.Name));
			Assert.AreEqual("Category", partialRecurringJob.GetValue(RecurringJobExposers.JobTypeCategoryId));
			Assert.AreEqual(new DateTimeOffset(start), partialRecurringJob.GetValue(RecurringJobExposers.Start));
			Assert.AreEqual(TimeSpan.FromHours(2), partialRecurringJob.GetValue(RecurringJobExposers.Duration));
			Assert.AreEqual(DesiredJobState.Tentative, partialRecurringJob.GetValue(RecurringJobExposers.DesiredJobState));
			Assert.AreEqual(endDate, partialRecurringJob.GetValue(RecurringJobExposers.Pattern.EndDate));
			Assert.IsFalse(partialRecurringJob.HasField(RecurringJobExposers.Description), "Fields that weren't selected aren't exposed.");
		}

		[TestMethod]
		public void CreatePartialRecurringJob_MissingValues_UseTheDefaultsOfAFullRead()
		{
			var partialRecurringJob = CreatePartialRecurringJob(CreateDomPartialObject(Guid.NewGuid()), new FieldExposer[]
			{
				RecurringJobExposers.Name,
				RecurringJobExposers.Duration,
				RecurringJobExposers.DesiredJobState,
				RecurringJobExposers.Pattern.EndDate,
			});

			Assert.IsNull(partialRecurringJob.GetValue(RecurringJobExposers.Name));
			Assert.AreEqual(TimeSpan.Zero, partialRecurringJob.GetValue(RecurringJobExposers.Duration));
			Assert.AreEqual(DesiredJobState.Draft, partialRecurringJob.GetValue(RecurringJobExposers.DesiredJobState));
			Assert.AreEqual(default(DateTimeOffset), partialRecurringJob.GetValue(RecurringJobExposers.Pattern.EndDate));
		}

		[TestMethod]
		public void CreatePartialRecurringJob_ValuesNotParsedByAFullRead_ReturnTheDefaultsOfAFullRead()
		{
			// RecurringJob doesn't parse the priority, organization and owner from its DOM instance.
			var domPartialObject = CreateDomPartialObject(
				Guid.NewGuid(),
				Field<int>(JobInfo.JobPriority, (int)SlcWorkflowIds.Enums.Jobpriority.High),
				Field<Guid>(SlcWorkflowIds.Sections.CostingAndBilling.Organization, Guid.NewGuid()),
				Field<Guid>(SlcWorkflowIds.Sections.CostingAndBilling.JobOwner, Guid.NewGuid()));

			var partialRecurringJob = CreatePartialRecurringJob(domPartialObject, RecurringJobExposers.Priority, RecurringJobExposers.OrganizationId, RecurringJobExposers.OwnerId);

			Assert.AreEqual(RecurringJobPriority.Normal, partialRecurringJob.GetValue(RecurringJobExposers.Priority));
			Assert.AreEqual(Guid.Empty, partialRecurringJob.GetValue(RecurringJobExposers.OrganizationId));
			Assert.AreEqual(Guid.Empty, partialRecurringJob.GetValue(RecurringJobExposers.OwnerId));
		}

		[TestMethod]
		public void CreatePartialRecurringJob_NodeSections_AreCombinedPerSection()
		{
			var poolId = Guid.NewGuid();
			var otherPoolId = Guid.NewGuid();
			var resourceId = Guid.NewGuid();

			// A resource pool node, a resource node and a resource pool node. Only the resource node has a parent reference.
			var domPartialObject = CreateDomPartialObject(
				Guid.NewGuid(),
				Fields(SlcWorkflowIds.Sections.Nodes.NodeType, (int)Nodetype.ResourcePool, (int)Nodetype.Resource, (int)Nodetype.ResourcePool),
				Fields(SlcWorkflowIds.Sections.Nodes.NodeReferenceID, poolId.ToString(), resourceId.ToString(), otherPoolId.ToString()),
				Fields(SlcWorkflowIds.Sections.Nodes.NodeParentReferenceID, poolId.ToString()));

			var partialRecurringJob = CreatePartialRecurringJob(domPartialObject, RecurringJobExposers.Resources, RecurringJobExposers.ResourcePools);

			CollectionAssert.AreEqual(new[] { resourceId }, partialRecurringJob.GetValues(RecurringJobExposers.Resources).ToList());
			CollectionAssert.AreEqual(new[] { poolId, poolId, otherPoolId }, partialRecurringJob.GetValues(RecurringJobExposers.ResourcePools).ToList());
		}

		[TestMethod]
		public void CreatePartialRecurringJob_SectionsThatCantBeCombined_ReadTheFullInstance()
		{
			// One of the two nodes has no reference, so the references can't be matched to the nodes by position.
			var domPartialObject = CreateDomPartialObject(
				Guid.NewGuid(),
				Fields(SlcWorkflowIds.Sections.Nodes.NodeType, (int)Nodetype.ResourcePool, (int)Nodetype.ResourcePool),
				Fields(SlcWorkflowIds.Sections.Nodes.NodeReferenceID, Guid.NewGuid().ToString()));

			var exception = Assert.ThrowsException<InvalidOperationException>(() => CreatePartialRecurringJob(domPartialObject, RecurringJobExposers.ResourcePools));
			StringAssert.Contains(exception.Message, "full recurring job DOM instance");
		}

		[TestMethod]
		public void CreatePartialRecurringJob_NoNodes_ReturnsNoValues()
		{
			var partialRecurringJob = CreatePartialRecurringJob(CreateDomPartialObject(Guid.NewGuid()), RecurringJobExposers.Resources, RecurringJobExposers.ResourcePools);

			Assert.AreEqual(0, partialRecurringJob.GetValues(RecurringJobExposers.Resources).Count());
			Assert.AreEqual(0, partialRecurringJob.GetValues(RecurringJobExposers.ResourcePools).Count());
		}

		private static Skyline.DataMiner.SDM.PartialObject<RecurringJob> CreatePartialRecurringJob(PartialObject<DomInstance, DomInstanceId> domPartialObject, params FieldExposer[] fields)
		{
			return RecurringJobPartialFieldMapper.CreatePartialRecurringJobs(null, new[] { domPartialObject }, fields).Single();
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
