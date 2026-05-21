namespace RT_MediaOps.Plan.Workflow.Nodes
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class NodeTypeChecksTests
	{
		[TestMethod]
		public void WorkflowNodes_ExposeResourceTypeChecks()
		{
			WorkflowNode resourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			WorkflowNode resourcePoolNode = new WorkflowResourcePoolNode(Guid.NewGuid());

			Assert.IsTrue(resourceNode.IsResourceNode);
			Assert.IsFalse(resourceNode.IsResourcePoolNode);
			Assert.IsFalse(resourcePoolNode.IsResourceNode);
			Assert.IsTrue(resourcePoolNode.IsResourcePoolNode);
		}

		[TestMethod]
		public void JobNodes_ExposeResourceTypeChecks()
		{
			JobNode resourceNode = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			JobNode resourcePoolNode = new JobResourcePoolNode(Guid.NewGuid());

			Assert.IsTrue(resourceNode.IsResourceNode);
			Assert.IsFalse(resourceNode.IsResourcePoolNode);
			Assert.IsFalse(resourcePoolNode.IsResourceNode);
			Assert.IsTrue(resourcePoolNode.IsResourcePoolNode);
		}

		[TestMethod]
		public void RecurringJobNodes_ExposeResourceTypeChecks()
		{
			RecurringJobNode resourceNode = new RecurringJobResourceNode();
			RecurringJobNode resourcePoolNode = new RecurringJobResourcePoolNode();

			Assert.IsTrue(resourceNode.IsResourceNode);
			Assert.IsFalse(resourceNode.IsResourcePoolNode);
			Assert.IsFalse(resourcePoolNode.IsResourceNode);
			Assert.IsTrue(resourcePoolNode.IsResourcePoolNode);
		}
	}
}
