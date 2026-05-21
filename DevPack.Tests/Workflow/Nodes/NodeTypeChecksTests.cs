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
		public void WorkflowNodes_AsAccessors_ReturnConcreteTypes()
		{
			WorkflowNode resourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			WorkflowNode resourcePoolNode = new WorkflowResourcePoolNode(Guid.NewGuid());

			WorkflowResourceNode asResource = resourceNode.AsResourceNode();
			WorkflowResourcePoolNode asPool = resourcePoolNode.AsResourcePoolNode();

			Assert.AreSame(resourceNode, asResource);
			Assert.AreSame(resourcePoolNode, asPool);
			Assert.IsNull(resourceNode.AsResourcePoolNode());
			Assert.IsNull(resourcePoolNode.AsResourceNode());

			NodeBase baseResource = resourceNode;
			NodeBase basePool = resourcePoolNode;
			Assert.AreSame((object)resourceNode, baseResource.AsResourceNode());
			Assert.AreSame((object)resourcePoolNode, basePool.AsResourcePoolNode());
			Assert.IsInstanceOfType(baseResource.AsResourceNode(), typeof(IResourceNode));
			Assert.IsInstanceOfType(basePool.AsResourcePoolNode(), typeof(IResourcePoolNode));
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
		public void JobNodes_AsAccessors_ReturnConcreteTypes()
		{
			JobNode resourceNode = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			JobNode resourcePoolNode = new JobResourcePoolNode(Guid.NewGuid());

			JobResourceNode asResource = resourceNode.AsResourceNode();
			JobResourcePoolNode asPool = resourcePoolNode.AsResourcePoolNode();

			Assert.AreSame(resourceNode, asResource);
			Assert.AreSame(resourcePoolNode, asPool);
			Assert.IsNull(resourceNode.AsResourcePoolNode());
			Assert.IsNull(resourcePoolNode.AsResourceNode());

			NodeBase baseResource = resourceNode;
			NodeBase basePool = resourcePoolNode;
			Assert.AreSame((object)resourceNode, baseResource.AsResourceNode());
			Assert.AreSame((object)resourcePoolNode, basePool.AsResourcePoolNode());
			Assert.IsInstanceOfType(baseResource.AsResourceNode(), typeof(IResourceNode));
			Assert.IsInstanceOfType(basePool.AsResourcePoolNode(), typeof(IResourcePoolNode));
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

		[TestMethod]
		public void RecurringJobNodes_AsAccessors_ReturnConcreteTypes()
		{
			RecurringJobNode resourceNode = new RecurringJobResourceNode();
			RecurringJobNode resourcePoolNode = new RecurringJobResourcePoolNode();

			RecurringJobResourceNode asResource = resourceNode.AsResourceNode();
			RecurringJobResourcePoolNode asPool = resourcePoolNode.AsResourcePoolNode();

			Assert.AreSame(resourceNode, asResource);
			Assert.AreSame(resourcePoolNode, asPool);
			Assert.IsNull(resourceNode.AsResourcePoolNode());
			Assert.IsNull(resourcePoolNode.AsResourceNode());

			NodeBase baseResource = resourceNode;
			NodeBase basePool = resourcePoolNode;
			Assert.AreSame((object)resourceNode, baseResource.AsResourceNode());
			Assert.AreSame((object)resourcePoolNode, basePool.AsResourcePoolNode());
			Assert.IsInstanceOfType(baseResource.AsResourceNode(), typeof(IResourceNode));
			Assert.IsInstanceOfType(basePool.AsResourcePoolNode(), typeof(IResourcePoolNode));
		}
	}
}
