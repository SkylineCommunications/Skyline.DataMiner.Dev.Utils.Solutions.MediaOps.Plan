namespace RT_MediaOps.Plan.Workflow.Workflows
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class WorkflowPropertyValuesTests
	{
		[TestMethod]
		public void Workflow_NewInstance_CustomPropertyValuesIsEmpty()
		{
			var workflow = new Workflow { Name = "Test" };

			Assert.IsNotNull(workflow.CustomPropertySettings);
			Assert.AreEqual(0, workflow.CustomPropertySettings.Count);
		}

		[TestMethod]
		public void Workflow_NewInstance_PropertyValuesIsEmpty()
		{
			var workflow = new Workflow { Name = "Test" };

			Assert.IsNotNull(workflow.PropertySettings);
			Assert.AreEqual(0, workflow.PropertySettings.Count);
		}

		[TestMethod]
		public void Workflow_NewInstanceWithId_CustomPropertyValuesIsEmpty()
		{
			var workflow = new Workflow(Guid.NewGuid()) { Name = "Test" };

			Assert.IsNotNull(workflow.CustomPropertySettings);
			Assert.AreEqual(0, workflow.CustomPropertySettings.Count);
		}

		[TestMethod]
		public void Workflow_NewInstanceWithId_PropertyValuesIsEmpty()
		{
			var workflow = new Workflow(Guid.NewGuid()) { Name = "Test" };

			Assert.IsNotNull(workflow.PropertySettings);
			Assert.AreEqual(0, workflow.PropertySettings.Count);
		}

		[TestMethod]
		public void WorkflowResourceNode_NewInstance_CustomPropertyValuesIsEmpty()
		{
			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());

			Assert.IsNotNull(node.CustomPropertySettings);
			Assert.AreEqual(0, node.CustomPropertySettings.Count);
		}

		[TestMethod]
		public void WorkflowResourceNode_NewInstance_PropertyValuesIsEmpty()
		{
			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());

			Assert.IsNotNull(node.PropertySettings);
			Assert.AreEqual(0, node.PropertySettings.Count);
		}

		[TestMethod]
		public void WorkflowResourcePoolNode_NewInstance_CustomPropertyValuesIsEmpty()
		{
			var node = new WorkflowResourcePoolNode(Guid.NewGuid());

			Assert.IsNotNull(node.CustomPropertySettings);
			Assert.AreEqual(0, node.CustomPropertySettings.Count);
		}

		[TestMethod]
		public void WorkflowResourcePoolNode_NewInstance_PropertyValuesIsEmpty()
		{
			var node = new WorkflowResourcePoolNode(Guid.NewGuid());

			Assert.IsNotNull(node.PropertySettings);
			Assert.AreEqual(0, node.PropertySettings.Count);
		}

		[TestMethod]
		public void NewWorkflow_NodeAddedThenProperties_PersistenceActionTargetsWorkflowAndNode()
		{
			// Arrange: brand-new workflow with a new node added before any properties are configured.
			var workflow = new Workflow(Guid.NewGuid()) { Name = "Test" };
			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(node);

			node.AddCustomProperty(new CustomPropertySetting("Tag") { Value = "live" });

			// Act: simulate the save path - DomWorkflowHandler calls EnsureContext() on each workflow.
			workflow.EnsureContext();

			var ownerScopes = new[]
			{
				new KeyValuePair<Guid, Skyline.DataMiner.Solutions.MediaOps.Plan.API.PropertySettingsScope>(workflow.Id, workflow.PropertySettingsScope),
				new KeyValuePair<Guid, Skyline.DataMiner.Solutions.MediaOps.Plan.API.PropertySettingsScope>(workflow.Id, node.PropertySettingsScope),
			};

			var (toCreateOrUpdate, toDelete, ownerByCollectionId) = ownerScopes.BuildPersistenceActions();

			// Assert
			Assert.AreEqual(0, toDelete.Count, "Nothing to delete - nothing was previously persisted.");
			Assert.AreEqual(1, toCreateOrUpdate.Count, "Only the node has dirty properties.");

			var collection = toCreateOrUpdate[0];
			Assert.AreEqual(workflow.Id.ToString(), collection.LinkedObjectId, "LinkedObjectId should be the owning workflow id.");
			Assert.AreEqual(PropertySettingsContext.MediaOpsScope, collection.Scope, "Scope should be the MediaOps scope.");
			Assert.AreEqual(node.Id, collection.SubId, "SubId should be the node id.");
			Assert.AreEqual(1, collection.CustomSettings.Count, "The custom property added on the node should be present.");
			Assert.AreEqual("Tag", collection.CustomSettings.First().Name);

			Assert.IsTrue(ownerByCollectionId.TryGetValue(collection.Id, out var ownerId));
			Assert.AreEqual(workflow.Id, ownerId, "Failure reporting must map back to the workflow id.");
		}

		[TestMethod]
		public void NewWorkflow_NodeAddedAfterContextEnsured_PersistenceActionTargetsWorkflowAndNode()
		{
			// Arrange: workflow whose context has already been created (e.g. user touched properties
			// before adding the node). This simulates the loaded-workflow scenario where the context
			// existed before a freshly created node was attached to the graph.
			var workflow = new Workflow(Guid.NewGuid()) { Name = "Test" };

			// Trigger context creation up-front so the new node is added AFTER the context exists.
			workflow.EnsureContext();

			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			node.AddCustomProperty(new CustomPropertySetting("Channel") { Value = "1" });
			workflow.NodeGraph.Add(node);

			// Act: save path re-runs EnsureContext, which must re-wire newly added nodes.
			workflow.EnsureContext();

			var ownerScopes = new[]
			{
				new KeyValuePair<Guid, Skyline.DataMiner.Solutions.MediaOps.Plan.API.PropertySettingsScope>(workflow.Id, workflow.PropertySettingsScope),
				new KeyValuePair<Guid, Skyline.DataMiner.Solutions.MediaOps.Plan.API.PropertySettingsScope>(workflow.Id, node.PropertySettingsScope),
			};

			var (toCreateOrUpdate, toDelete, ownerByCollectionId) = ownerScopes.BuildPersistenceActions();

			// Assert
			Assert.AreEqual(0, toDelete.Count);
			Assert.AreEqual(1, toCreateOrUpdate.Count);

			var collection = toCreateOrUpdate[0];
			Assert.AreEqual(workflow.Id.ToString(), collection.LinkedObjectId, "Node added after the context was created must still resolve to the workflow id.");
			Assert.AreEqual(PropertySettingsContext.MediaOpsScope, collection.Scope);
			Assert.AreEqual(node.Id, collection.SubId);

			Assert.IsTrue(ownerByCollectionId.TryGetValue(collection.Id, out var ownerId));
			Assert.AreEqual(workflow.Id, ownerId);
		}
	}
}
