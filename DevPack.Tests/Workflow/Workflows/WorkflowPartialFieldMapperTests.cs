namespace RT_MediaOps.Plan.Workflow.Workflows
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

	using Nodes = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.Nodes;
	using Nodetype = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Enums.Nodetype;
	using WorkflowInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.WorkflowInfo;

	[TestClass]
	public sealed class WorkflowPartialFieldMapperTests
	{
		[TestMethod]
		public void CreateSelectedFields_SelectsEachDomFieldOnce()
		{
			var selectedFields = WorkflowPartialFieldMapper.CreateSelectedFields(new FieldExposer[] { WorkflowExposers.Name, WorkflowExposers.PreRoll, WorkflowExposers.Resources, WorkflowExposers.ResourcePools });

			CollectionAssert.AreEquivalent(
				new[] { FieldName(WorkflowInfo.WorkflowName), FieldName(WorkflowInfo.Preroll), FieldName(Nodes.NodeType), FieldName(Nodes.NodeReferenceID), FieldName(Nodes.NodeParentReferenceID) },
				selectedFields.Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_StateOrOnlyId_SelectsStatus()
		{
			CollectionAssert.AreEqual(new[] { "StatusId" }, WorkflowPartialFieldMapper.CreateSelectedFields(new[] { WorkflowExposers.State }).Fields.Select(x => x.FieldName).ToList());
			CollectionAssert.AreEqual(new[] { "StatusId" }, WorkflowPartialFieldMapper.CreateSelectedFields(new[] { WorkflowExposers.Id }).Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_UnsupportedField_ThrowsNotSupportedException()
		{
			var unknownField = new Exposer<Workflow, string>(x => x.Name, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => WorkflowPartialFieldMapper.CreateSelectedFields(new[] { unknownField }));
			Assert.ThrowsException<NotSupportedException>(() => WorkflowPartialFieldMapper.CreateSelectedFields(new[] { JobExposers.Name }));
		}

		[TestMethod]
		public void CreatePartialWorkflow_ConvertsValuesLikeAFullRead()
		{
			var id = Guid.NewGuid();

			var domPartialObject = CreateDomPartialObject(
				id,
				Field<string>(WorkflowInfo.WorkflowName, "Workflow"),
				Field<string>(WorkflowInfo.WorkflowDescription, "Description"),
				Field<string>(WorkflowInfo.WorkflowNotes, "Notes"),
				Field<string>(WorkflowInfo.JobType, "Category"),
				Field<int>(WorkflowInfo.Priority, (int)SlcWorkflowIds.Enums.Priority.High),
				Field<bool>(WorkflowInfo.Favorite, true),
				Field<TimeSpan>(WorkflowInfo.Preroll, TimeSpan.FromSeconds(30)),
				Field<TimeSpan>(WorkflowInfo.Postroll, TimeSpan.FromMinutes(2)),
				("StatusId", new PartialObjectValue<string> { Value = SlcWorkflowIds.Behaviors.Workflow_Behavior.Statuses.Complete }));

			var partialWorkflow = CreatePartialWorkflow(domPartialObject, new FieldExposer[]
			{
				WorkflowExposers.Id,
				WorkflowExposers.Name,
				WorkflowExposers.Description,
				WorkflowExposers.Notes,
				WorkflowExposers.JobTypeCategoryId,
				WorkflowExposers.Priority,
				WorkflowExposers.IsFavorite,
				WorkflowExposers.PreRoll,
				WorkflowExposers.PostRoll,
				WorkflowExposers.State,
			});

			Assert.AreEqual(id, partialWorkflow.Id);
			Assert.AreEqual(id, partialWorkflow.GetValue(WorkflowExposers.Id));
			Assert.AreEqual("Workflow", partialWorkflow.GetValue(WorkflowExposers.Name));
			Assert.AreEqual("Description", partialWorkflow.GetValue(WorkflowExposers.Description));
			Assert.AreEqual("Notes", partialWorkflow.GetValue(WorkflowExposers.Notes));
			Assert.AreEqual("Category", partialWorkflow.GetValue(WorkflowExposers.JobTypeCategoryId));
			Assert.AreEqual(WorkflowPriority.High, partialWorkflow.GetValue(WorkflowExposers.Priority));
			Assert.IsTrue(partialWorkflow.GetValue(WorkflowExposers.IsFavorite));
			Assert.AreEqual(TimeSpan.FromSeconds(30), partialWorkflow.GetValue(WorkflowExposers.PreRoll));
			Assert.AreEqual(TimeSpan.FromMinutes(2), partialWorkflow.GetValue(WorkflowExposers.PostRoll));
			Assert.AreEqual(WorkflowState.Complete, partialWorkflow.GetValue(WorkflowExposers.State));
		}

		[TestMethod]
		public void CreatePartialWorkflow_MissingValues_UseTheDefaultsOfAFullRead()
		{
			var partialWorkflow = CreatePartialWorkflow(CreateDomPartialObject(Guid.NewGuid()), new FieldExposer[]
			{
				WorkflowExposers.Name,
				WorkflowExposers.JobTypeCategoryId,
				WorkflowExposers.Priority,
				WorkflowExposers.IsFavorite,
				WorkflowExposers.PreRoll,
				WorkflowExposers.PostRoll,
			});

			Assert.IsNull(partialWorkflow.GetValue(WorkflowExposers.Name));
			Assert.IsNull(partialWorkflow.GetValue(WorkflowExposers.JobTypeCategoryId));
			Assert.AreEqual(WorkflowPriority.Normal, partialWorkflow.GetValue(WorkflowExposers.Priority));
			Assert.IsFalse(partialWorkflow.GetValue(WorkflowExposers.IsFavorite));
			Assert.AreEqual(TimeSpan.Zero, partialWorkflow.GetValue(WorkflowExposers.PreRoll));
			Assert.AreEqual(TimeSpan.Zero, partialWorkflow.GetValue(WorkflowExposers.PostRoll));
		}

		[TestMethod]
		public void CreatePartialWorkflow_NodeSections_AreCombinedPerSection()
		{
			var poolId = Guid.NewGuid();
			var otherPoolId = Guid.NewGuid();
			var resourceId = Guid.NewGuid();

			// A resource pool node, a resource node and a resource pool node. Only the resource node has a parent reference.
			var domPartialObject = CreateDomPartialObject(
				Guid.NewGuid(),
				Fields(Nodes.NodeType, (int)Nodetype.ResourcePool, (int)Nodetype.Resource, (int)Nodetype.ResourcePool),
				Fields(Nodes.NodeReferenceID, poolId.ToString(), resourceId.ToString(), otherPoolId.ToString()),
				Fields(Nodes.NodeParentReferenceID, poolId.ToString()));

			var partialWorkflow = CreatePartialWorkflow(domPartialObject, WorkflowExposers.Resources, WorkflowExposers.ResourcePools);

			CollectionAssert.AreEqual(new[] { resourceId }, partialWorkflow.GetValues(WorkflowExposers.Resources).ToList());
			CollectionAssert.AreEqual(new[] { poolId, poolId, otherPoolId }, partialWorkflow.GetValues(WorkflowExposers.ResourcePools).ToList());
		}

		[TestMethod]
		public void CreatePartialWorkflow_SectionsThatCantBeCombined_ReadTheFullInstance()
		{
			// One of the two nodes has no reference, so the references can't be matched to the nodes by position.
			var domPartialObject = CreateDomPartialObject(
				Guid.NewGuid(),
				Fields(Nodes.NodeType, (int)Nodetype.ResourcePool, (int)Nodetype.ResourcePool),
				Fields(Nodes.NodeReferenceID, Guid.NewGuid().ToString()));

			var exception = Assert.ThrowsException<InvalidOperationException>(() => CreatePartialWorkflow(domPartialObject, WorkflowExposers.Resources));
			StringAssert.Contains(exception.Message, "full workflow DOM instance");
		}

		[TestMethod]
		public void CreatePartialWorkflow_NoNodes_ReturnsNoValues()
		{
			var partialWorkflow = CreatePartialWorkflow(CreateDomPartialObject(Guid.NewGuid()), WorkflowExposers.Resources, WorkflowExposers.ResourcePools);

			Assert.AreEqual(0, partialWorkflow.GetValues(WorkflowExposers.Resources).Count());
			Assert.AreEqual(0, partialWorkflow.GetValues(WorkflowExposers.ResourcePools).Count());
		}

		private static Skyline.DataMiner.SDM.PartialObject<Workflow> CreatePartialWorkflow(PartialObject<DomInstance, DomInstanceId> domPartialObject, params FieldExposer[] fields)
		{
			return WorkflowPartialFieldMapper.CreatePartialWorkflows(null, new[] { domPartialObject }, fields).Single();
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
