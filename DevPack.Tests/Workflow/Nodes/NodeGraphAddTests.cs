namespace RT_MediaOps.Plan.Workflow.Nodes
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class NodeGraphAddTests
	{
		[TestMethod]
		public void Add_SameNodeTwice_Throws()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());

			workflow.NodeGraph.Add(node);

			Assert.ThrowsException<ArgumentException>(() => workflow.NodeGraph.Add(node));
			Assert.AreEqual(1, workflow.NodeGraph.Nodes.Count);
		}

		[TestMethod]
		public void Add_NewNodes_AreAllAdded()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var first = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var second = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());

			workflow.NodeGraph.Add(first).Add(second);

			Assert.AreEqual(2, workflow.NodeGraph.Nodes.Count);
			Assert.IsTrue(workflow.NodeGraph.Nodes.Contains(first));
			Assert.IsTrue(workflow.NodeGraph.Nodes.Contains(second));
		}

		[TestMethod]
		public void Add_ExistingNode_Throws()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid())
			{
				// Simulates a node that was loaded from storage, e.g. a node of another workflow or job.
				IsNew = false,
			};

			Assert.ThrowsException<ArgumentException>(() => workflow.NodeGraph.Add(node));
			Assert.AreEqual(0, workflow.NodeGraph.Nodes.Count);
		}

		[TestMethod]
		public void Add_Null_Throws()
		{
			var workflow = new Workflow { Name = "Workflow" };

			Assert.ThrowsException<ArgumentNullException>(() => workflow.NodeGraph.Add(null));
		}
	}
}
