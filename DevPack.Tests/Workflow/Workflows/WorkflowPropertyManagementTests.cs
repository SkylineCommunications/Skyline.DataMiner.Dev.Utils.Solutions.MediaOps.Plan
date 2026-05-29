namespace RT_MediaOps.Plan.Workflow.Workflows
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class WorkflowPropertyManagementTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public WorkflowPropertyManagementTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void CreateAndUpdateWorkflow_WithCustomPropertiesOnWorkflowAndNodes_PersistsAllProperties()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
			};
			workflow.AddCustomProperty(new CustomPropertyValue("Tag1") { Value = "Value1" });

			var node1 = new WorkflowResourcePoolNode(pool);
			node1.AddCustomProperty(new CustomPropertyValue("Tag1"){ Value = "Value1" });
			workflow.NodeGraph.Add(node1);

			workflow = objectCreator.CreateWorkflow(workflow);
			Assert.IsNotNull(workflow);
			Assert.AreEqual(1, workflow.CustomPropertyValues.Count);
			var customWorkflowProperty = workflow.CustomPropertyValues.First();
			Assert.AreEqual("Tag1", customWorkflowProperty.Name);
			Assert.AreEqual("Value1", customWorkflowProperty.Value);

			Assert.AreEqual(1, workflow.NodeGraph.Nodes.Count);
			foreach (var node in workflow.NodeGraph.Nodes)
			{
				Assert.AreEqual(1, node.CustomPropertyValues.Count);
				var customNodeProperty = node.CustomPropertyValues.First();
				Assert.AreEqual("Tag1", customNodeProperty.Name);
				Assert.AreEqual("Value1", customNodeProperty.Value);
			}

			// Update workflow and node properties
			workflow.AddCustomProperty(new CustomPropertyValue("Tag2") { Value = "Value2" });

			node1 = workflow.NodeGraph.Nodes.OfType<WorkflowResourcePoolNode>().First();
			node1.AddCustomProperty(new CustomPropertyValue("Tag2") { Value = "Value2" });

			var node2 = new WorkflowResourcePoolNode(pool);
			node2.AddCustomProperty(new CustomPropertyValue("Tag1") { Value = "Value1" });
			node2.AddCustomProperty(new CustomPropertyValue("Tag2") { Value = "Value2" });
			workflow.NodeGraph.Add(node2);

			workflow = TestContext.Api.Workflows.Update(workflow);
			Assert.IsNotNull(workflow);
			Assert.AreEqual(2, workflow.CustomPropertyValues.Count);
			var customWorkflowTag1Property = workflow.CustomPropertyValues.FirstOrDefault(x => x.Name == "Tag1");
			Assert.IsNotNull(customWorkflowTag1Property);
			Assert.AreEqual("Value1", customWorkflowTag1Property.Value);
			var customWorkflowTag2Property = workflow.CustomPropertyValues.FirstOrDefault(x => x.Name == "Tag2");
			Assert.IsNotNull(customWorkflowTag2Property);
			Assert.AreEqual("Value2", customWorkflowTag2Property.Value);

			Assert.AreEqual(2, workflow.NodeGraph.Nodes.Count);
			foreach (var node in workflow.NodeGraph.Nodes)
			{
				Assert.AreEqual(2, node.CustomPropertyValues.Count);
				var customNodeTag1Property = node.CustomPropertyValues.FirstOrDefault(x => x.Name == "Tag1");
				Assert.IsNotNull(customNodeTag1Property);
				Assert.AreEqual("Value1", customNodeTag1Property.Value);
				var customNodeTag2Property = node.CustomPropertyValues.FirstOrDefault(x => x.Name == "Tag2");
				Assert.IsNotNull(customNodeTag2Property);
				Assert.AreEqual("Value2", customNodeTag2Property.Value);
			}
		}
	}
}
