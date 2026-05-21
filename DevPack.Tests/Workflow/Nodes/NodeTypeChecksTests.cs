namespace RT_MediaOps.Plan.Workflow.Nodes
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class NodeTypeChecksTests
	{
		[TestMethod]
		public void WorkflowNodes_IsResourceNode_ReturnsConcreteType()
		{
			WorkflowNode resourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			WorkflowNode resourcePoolNode = new WorkflowResourcePoolNode(Guid.NewGuid());

			Assert.IsTrue(resourceNode.IsResourceNode(out WorkflowResourceNode asResource));
			Assert.AreSame((object)resourceNode, asResource);
			Assert.IsFalse(resourceNode.IsResourcePoolNode(out WorkflowResourcePoolNode asPoolFromResource));
			Assert.IsNull(asPoolFromResource);

			Assert.IsTrue(resourcePoolNode.IsResourcePoolNode(out WorkflowResourcePoolNode asPool));
			Assert.AreSame((object)resourcePoolNode, asPool);
			Assert.IsFalse(resourcePoolNode.IsResourceNode(out WorkflowResourceNode asResourceFromPool));
			Assert.IsNull(asResourceFromPool);
		}

		[TestMethod]
		public void WorkflowNodes_IsResourceNode_OnNodeBase_ReturnsInterfaceType()
		{
			NodeBase resourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			NodeBase resourcePoolNode = new WorkflowResourcePoolNode(Guid.NewGuid());

			Assert.IsTrue(resourceNode.IsResourceNode(out IResourceNode asResource));
			Assert.AreSame((object)resourceNode, asResource);
			Assert.IsTrue(resourcePoolNode.IsResourcePoolNode(out IResourcePoolNode asPool));
			Assert.AreSame((object)resourcePoolNode, asPool);

			Assert.IsFalse(resourceNode.IsResourcePoolNode(out IResourcePoolNode none1));
			Assert.IsNull(none1);
			Assert.IsFalse(resourcePoolNode.IsResourceNode(out IResourceNode none2));
			Assert.IsNull(none2);
		}

		[TestMethod]
		public void JobNodes_IsResourceNode_ReturnsConcreteType()
		{
			JobNode resourceNode = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			JobNode resourcePoolNode = new JobResourcePoolNode(Guid.NewGuid());

			Assert.IsTrue(resourceNode.IsResourceNode(out JobResourceNode asResource));
			Assert.AreSame((object)resourceNode, asResource);
			Assert.IsFalse(resourceNode.IsResourcePoolNode(out JobResourcePoolNode asPoolFromResource));
			Assert.IsNull(asPoolFromResource);

			Assert.IsTrue(resourcePoolNode.IsResourcePoolNode(out JobResourcePoolNode asPool));
			Assert.AreSame((object)resourcePoolNode, asPool);
			Assert.IsFalse(resourcePoolNode.IsResourceNode(out JobResourceNode asResourceFromPool));
			Assert.IsNull(asResourceFromPool);
		}

		[TestMethod]
		public void JobNodes_IsResourceNode_OnNodeBase_ReturnsInterfaceType()
		{
			NodeBase resourceNode = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			NodeBase resourcePoolNode = new JobResourcePoolNode(Guid.NewGuid());

			Assert.IsTrue(resourceNode.IsResourceNode(out IResourceNode asResource));
			Assert.AreSame((object)resourceNode, asResource);
			Assert.IsTrue(resourcePoolNode.IsResourcePoolNode(out IResourcePoolNode asPool));
			Assert.AreSame((object)resourcePoolNode, asPool);
		}

		[TestMethod]
		public void RecurringJobNodes_IsResourceNode_ReturnsConcreteType()
		{
			RecurringJobNode resourceNode = new RecurringJobResourceNode();
			RecurringJobNode resourcePoolNode = new RecurringJobResourcePoolNode();

			Assert.IsTrue(resourceNode.IsResourceNode(out RecurringJobResourceNode asResource));
			Assert.AreSame((object)resourceNode, asResource);
			Assert.IsFalse(resourceNode.IsResourcePoolNode(out RecurringJobResourcePoolNode asPoolFromResource));
			Assert.IsNull(asPoolFromResource);

			Assert.IsTrue(resourcePoolNode.IsResourcePoolNode(out RecurringJobResourcePoolNode asPool));
			Assert.AreSame((object)resourcePoolNode, asPool);
			Assert.IsFalse(resourcePoolNode.IsResourceNode(out RecurringJobResourceNode asResourceFromPool));
			Assert.IsNull(asResourceFromPool);
		}

		[TestMethod]
		public void RecurringJobNodes_IsResourceNode_OnNodeBase_ReturnsInterfaceType()
		{
			NodeBase resourceNode = new RecurringJobResourceNode();
			NodeBase resourcePoolNode = new RecurringJobResourcePoolNode();

			Assert.IsTrue(resourceNode.IsResourceNode(out IResourceNode asResource));
			Assert.AreSame((object)resourceNode, asResource);
			Assert.IsTrue(resourcePoolNode.IsResourcePoolNode(out IResourcePoolNode asPool));
			Assert.AreSame((object)resourcePoolNode, asPool);
		}
	}
}
