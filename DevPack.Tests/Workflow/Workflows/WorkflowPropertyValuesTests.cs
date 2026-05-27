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

			Assert.IsNotNull(workflow.CustomPropertyValues);
			Assert.AreEqual(0, workflow.CustomPropertyValues.Count);
		}

		[TestMethod]
		public void Workflow_NewInstance_PropertyValuesIsEmpty()
		{
			var workflow = new Workflow { Name = "Test" };

			Assert.IsNotNull(workflow.PropertyValues);
			Assert.AreEqual(0, workflow.PropertyValues.Count);
		}

		[TestMethod]
		public void Workflow_NewInstanceWithId_CustomPropertyValuesIsEmpty()
		{
			var workflow = new Workflow(Guid.NewGuid()) { Name = "Test" };

			Assert.IsNotNull(workflow.CustomPropertyValues);
			Assert.AreEqual(0, workflow.CustomPropertyValues.Count);
		}

		[TestMethod]
		public void Workflow_NewInstanceWithId_PropertyValuesIsEmpty()
		{
			var workflow = new Workflow(Guid.NewGuid()) { Name = "Test" };

			Assert.IsNotNull(workflow.PropertyValues);
			Assert.AreEqual(0, workflow.PropertyValues.Count);
		}

		[TestMethod]
		public void WorkflowResourceNode_NewInstance_CustomPropertyValuesIsEmpty()
		{
			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());

			Assert.IsNotNull(node.CustomPropertyValues);
			Assert.AreEqual(0, node.CustomPropertyValues.Count);
		}

		[TestMethod]
		public void WorkflowResourceNode_NewInstance_PropertyValuesIsEmpty()
		{
			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());

			Assert.IsNotNull(node.PropertyValues);
			Assert.AreEqual(0, node.PropertyValues.Count);
		}

		[TestMethod]
		public void WorkflowResourcePoolNode_NewInstance_CustomPropertyValuesIsEmpty()
		{
			var node = new WorkflowResourcePoolNode(Guid.NewGuid());

			Assert.IsNotNull(node.CustomPropertyValues);
			Assert.AreEqual(0, node.CustomPropertyValues.Count);
		}

		[TestMethod]
		public void WorkflowResourcePoolNode_NewInstance_PropertyValuesIsEmpty()
		{
			var node = new WorkflowResourcePoolNode(Guid.NewGuid());

			Assert.IsNotNull(node.PropertyValues);
			Assert.AreEqual(0, node.PropertyValues.Count);
		}
	}
}
