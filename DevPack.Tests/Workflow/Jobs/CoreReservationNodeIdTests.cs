namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class CoreReservationNodeIdTests
	{
		[TestMethod]
		public void Add_AssignsUniquePositiveCoreReservationNodeIds()
		{
			var job = new Job { Name = "Job" };

			var firstNode = new JobResourcePoolNode(Guid.NewGuid());
			var secondNode = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());

			job.NodeGraph
				.Add(firstNode)
				.Add(secondNode);

			Assert.AreEqual(1, GetId(firstNode));
			Assert.AreEqual(2, GetId(secondNode));
			Assert.AreEqual(2, DistinctIdCount(job.NodeGraph));
		}

		[TestMethod]
		public void Add_AfterRemovingHighestNode_DoesNotReuseFreedId()
		{
			var job = new Job { Name = "Job" };

			var firstNode = new JobResourcePoolNode(Guid.NewGuid());
			var secondNode = new JobResourcePoolNode(Guid.NewGuid());

			job.NodeGraph
				.Add(firstNode)
				.Add(secondNode);

			// Remove the node that currently holds the highest assigned id.
			job.NodeGraph.Remove(secondNode);

			var thirdNode = new JobResourcePoolNode(Guid.NewGuid());
			job.NodeGraph.Add(thirdNode);

			// The freed id (2) must not be reused; the high-water mark keeps handing out new ids.
			Assert.AreEqual(1, GetId(firstNode));
			Assert.AreEqual(3, GetId(thirdNode));
		}

		[TestMethod]
		public void Add_WithExistingAssignedNode_AssignsUniqueId()
		{
			var existingNode = new JobResourcePoolNode(Guid.NewGuid());
			existingNode.SetCoreReservationNodeId(10);

			var graph = new NodeGraph<JobNode>(new List<JobNode> { existingNode });

			var newNode = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			graph.Add(newNode);

			Assert.AreEqual(10, GetId(existingNode));
			Assert.AreEqual(11, GetId(newNode));
		}

		[TestMethod]
		public void Swap_AssignsCoreReservationNodeIdToNewNode()
		{
			var job = new Job { Name = "Job" };

			var oldNode = new JobResourcePoolNode(Guid.NewGuid());
			job.NodeGraph.Add(oldNode);

			var newNode = new JobResourcePoolNode(Guid.NewGuid());
			job.NodeGraph.Swap(oldNode, newNode);

			Assert.AreEqual(2, GetId(newNode));
			Assert.IsTrue(job.NodeGraph.Nodes.Contains(newNode));
			Assert.IsFalse(job.NodeGraph.Nodes.Contains(oldNode));
		}

		[TestMethod]
		public void EnsureCoreReservationNodeIds_FillsLoadedNodesWithoutCollisions()
		{
			// Simulate a job loaded from storage: one node already has an id, others were never assigned one.
			var loadedNode = new JobResourcePoolNode(Guid.NewGuid());
			loadedNode.SetCoreReservationNodeId(5);

			var unassignedNode = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var anotherUnassignedNode = new JobResourcePoolNode(Guid.NewGuid());

			var graph = new NodeGraph<JobNode>(new List<JobNode> { loadedNode, unassignedNode, anotherUnassignedNode });

			graph.EnsureCoreReservationNodeIds();

			Assert.AreEqual(5, GetId(loadedNode));
			Assert.AreEqual(6, GetId(unassignedNode));
			Assert.AreEqual(7, GetId(anotherUnassignedNode));
			Assert.AreEqual(3, DistinctIdCount(graph));
		}

		[TestMethod]
		public void ResolveCoreReservationNodeId_LegacyNodeId_TakesPrecedenceOverStoredValue()
		{
			// For a legacy node the core reservation node id must equal its integer NodeID, so the NodeID wins.
			var result = JobNode.ResolveCoreReservationNodeId(42L, "999");

			Assert.IsTrue(result.HasValue);
			Assert.AreEqual(999, result.Value);
		}

		[TestMethod]
		public void ResolveCoreReservationNodeId_UsesStoredValue_WhenNodeIdIsNotLegacy()
		{
			var result = JobNode.ResolveCoreReservationNodeId(42L, Guid.NewGuid().ToString());

			Assert.IsTrue(result.HasValue);
			Assert.AreEqual(42, result.Value);
		}

		[TestMethod]
		public void ResolveCoreReservationNodeId_FallsBackToLegacyPositiveNodeId()
		{
			var result = JobNode.ResolveCoreReservationNodeId(null, "7");

			Assert.IsTrue(result.HasValue);
			Assert.AreEqual(7, result.Value);
		}

		[DataTestMethod]
		[DataRow(0L)]
		[DataRow(-1L)]
		public void ResolveCoreReservationNodeId_TreatsNonPositiveStoredValueAsUnassigned(long storedValue)
		{
			var result = JobNode.ResolveCoreReservationNodeId(storedValue, Guid.NewGuid().ToString());

			Assert.IsFalse(result.HasValue);
		}

		[DataTestMethod]
		[DataRow("0")]
		[DataRow("-1")]
		[DataRow("abc")]
		[DataRow("")]
		[DataRow("3f2504e0-4f89-41d3-9a0c-0305e82c3301")]
		public void ResolveCoreReservationNodeId_IgnoresNonPositiveOrNonIntegerLegacyNodeId(string nodeId)
		{
			var result = JobNode.ResolveCoreReservationNodeId(null, nodeId);

			Assert.IsFalse(result.HasValue);
		}

		[TestMethod]
		public void Add_NonJobNodes_DoesNotAssignOrThrow()
		{
			var workflow = new Workflow { Name = "Workflow" };

			var poolNode = new WorkflowResourcePoolNode(Guid.NewGuid());
			var resourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());

			workflow.NodeGraph
				.Add(poolNode)
				.Add(resourceNode);

			Assert.AreEqual(2, workflow.NodeGraph.Nodes.Count);
		}

		private static int GetId(JobNode node)
		{
			Assert.IsTrue(node.CoreReservationNodeId.HasValue, "Expected the job node to have a core reservation node id assigned.");
			return node.CoreReservationNodeId.Value;
		}

		private static int DistinctIdCount(NodeGraph<JobNode> graph)
		{
			return graph.Nodes
				.Select(n => n.CoreReservationNodeId)
				.Where(id => id.HasValue)
				.Select(id => id.Value)
				.Distinct()
				.Count();
		}
	}
}
