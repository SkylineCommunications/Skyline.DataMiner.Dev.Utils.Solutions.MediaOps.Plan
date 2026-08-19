namespace RT_MediaOps.Plan.Workflow.Nodes
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class NodeGroupTests
	{
		[TestMethod]
		public void AddGroup_AllowsDuplicateAndEmptyNames()
		{
			var workflow = new Workflow { Name = "Workflow" };

			var first = workflow.NodeGraph.AddGroup("Group");
			var second = workflow.NodeGraph.AddGroup("Group");
			var third = workflow.NodeGraph.AddGroup(String.Empty);
			var fourth = workflow.NodeGraph.AddGroup(null);

			Assert.AreEqual(4, workflow.NodeGraph.Groups.Count);
			Assert.AreNotSame(first, second);
			Assert.AreEqual("Group", second.Name);
			Assert.AreEqual(String.Empty, third.Name);
			Assert.IsNull(fourth.Name);
		}

		[TestMethod]
		public void RemoveGroup_RemovesGroupButKeepsNodes()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(node);

			var group = workflow.NodeGraph.AddGroup("Group");
			group.Add(node);

			Assert.IsTrue(workflow.NodeGraph.RemoveGroup(group));
			Assert.AreEqual(0, workflow.NodeGraph.Groups.Count);
			Assert.IsTrue(workflow.NodeGraph.Nodes.Contains(node));
			Assert.IsFalse(workflow.NodeGraph.RemoveGroup(group));
		}

		[TestMethod]
		public void GroupAdd_NodeNotPartOfGraph_Throws()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var group = workflow.NodeGraph.AddGroup("Group");

			var foreignNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());

			Assert.ThrowsException<InvalidOperationException>(() => group.Add(foreignNode));
		}

		[TestMethod]
		public void GroupAdd_SameNodeTwice_IsAddedOnce()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(node);

			var group = workflow.NodeGraph.AddGroup("Group");
			group.Add(node).Add(node);

			Assert.AreEqual(1, group.Nodes.Count);
		}

		[TestMethod]
		public void RemoveNode_RemovesNodeFromAllGroupsAndKeepsEmptyGroups()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var otherNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(node).Add(otherNode);

			var groupA = workflow.NodeGraph.AddGroup("A");
			groupA.Add(node);

			var groupB = workflow.NodeGraph.AddGroup("B");
			groupB.Add(node).Add(otherNode);

			workflow.NodeGraph.Remove(node);

			Assert.AreEqual(2, workflow.NodeGraph.Groups.Count);
			Assert.AreEqual(0, groupA.Nodes.Count);
			CollectionAssert.AreEqual(new[] { otherNode }, groupB.Nodes.ToArray());
		}

		[TestMethod]
		public void RemoveNode_RemovesCascadedChildrenFromGroups()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var poolNode = new WorkflowResourcePoolNode(Guid.NewGuid());
			var childNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(poolNode).Add(childNode).Link(poolNode, childNode);

			var group = workflow.NodeGraph.AddGroup("Group");
			group.Add(poolNode).Add(childNode);

			workflow.NodeGraph.Remove(poolNode);

			Assert.AreEqual(0, group.Nodes.Count);
		}

		[TestMethod]
		public void RemoveNode_NodeInNoGroup_LeavesGroupsUntouched()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var groupedNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(node).Add(groupedNode);

			var group = workflow.NodeGraph.AddGroup("Group");
			group.Add(groupedNode);

			workflow.NodeGraph.Remove(node);

			CollectionAssert.AreEqual(new[] { groupedNode }, group.Nodes.ToArray());
		}

		[TestMethod]
		public void Swap_MovesGroupMembershipToNewNodeAtSamePosition()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var firstNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var oldNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var lastNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(firstNode).Add(oldNode).Add(lastNode);

			var group = workflow.NodeGraph.AddGroup("Group");
			group.Add(firstNode).Add(oldNode).Add(lastNode);

			var newNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Swap(oldNode, newNode);

			CollectionAssert.AreEqual(new[] { firstNode, newNode, lastNode }, group.Nodes.ToArray());
		}

		[TestMethod]
		public void RestoreSwappedOutNode_RestoresMembershipCapturedAtSwapTime()
		{
			// A swap on a running job keeps the swapped-out node next to its replacement, so both must end up in the
			// groups. The original follows the membership it had at swap time, not the membership of its replacement.
			var workflow = new Workflow { Name = "Workflow" };
			var node1 = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(node1);

			var groupA = workflow.NodeGraph.AddGroup("A");
			var groupB = workflow.NodeGraph.AddGroup("B");
			var groupC = workflow.NodeGraph.AddGroup("C");
			groupA.Add(node1);
			groupB.Add(node1);

			var node2 = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Swap(node1, node2);

			groupC.Add(node2);
			groupA.Remove(node2);

			workflow.NodeGraph.RestoreSwappedOutNode(node1);

			CollectionAssert.AreEquivalent(new[] { "A", "B" }, GroupNamesOf(workflow, node1));
			CollectionAssert.AreEquivalent(new[] { "B", "C" }, GroupNamesOf(workflow, node2));
		}

		[TestMethod]
		public void RestoreSwappedOutNode_CalledTwice_DoesNotDuplicateMembership()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var oldNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(oldNode);

			var group = workflow.NodeGraph.AddGroup("Group");
			group.Add(oldNode);

			var newNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Swap(oldNode, newNode);

			workflow.NodeGraph.RestoreSwappedOutNode(oldNode);
			workflow.NodeGraph.RestoreSwappedOutNode(oldNode);

			Assert.AreEqual(2, group.Nodes.Count);
			CollectionAssert.AreEquivalent(new[] { oldNode, newNode }, group.Nodes.ToArray());
		}

		[TestMethod]
		public void RestoreSwappedOutNode_GroupRemovedAfterSwap_IsNotResurrected()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var oldNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(oldNode);

			var group = workflow.NodeGraph.AddGroup("Group");
			group.Add(oldNode);

			var newNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Swap(oldNode, newNode);
			workflow.NodeGraph.RemoveGroup(group);

			workflow.NodeGraph.RestoreSwappedOutNode(oldNode);

			Assert.AreEqual(0, workflow.NodeGraph.Groups.Count);
			Assert.IsFalse(group.Contains(oldNode));
		}

		[TestMethod]
		public void RestoreSwappedOutNode_AfterChainedSwap_RestoresOriginalMembership()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var original = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(original);

			var group = workflow.NodeGraph.AddGroup("Group");
			group.Add(original);

			var intermediate = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var final = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Swap(original, intermediate);
			workflow.NodeGraph.Swap(intermediate, final);

			workflow.NodeGraph.RestoreSwappedOutNode(original);

			CollectionAssert.AreEquivalent(new[] { original, final }, group.Nodes.ToArray());
		}

		[TestMethod]
		public void Groups_AreTakenIntoAccountForEqualityAndHashCode()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(node);

			var hashWithoutGroup = workflow.NodeGraph.GetHashCode();

			var group = workflow.NodeGraph.AddGroup("Group");
			var hashWithEmptyGroup = workflow.NodeGraph.GetHashCode();
			Assert.AreNotEqual(hashWithoutGroup, hashWithEmptyGroup);

			group.Add(node);
			var hashWithMember = workflow.NodeGraph.GetHashCode();
			Assert.AreNotEqual(hashWithEmptyGroup, hashWithMember);

			group.Name = "Renamed";
			Assert.AreNotEqual(hashWithMember, workflow.NodeGraph.GetHashCode());
		}

		private static List<string> GroupNamesOf(Workflow workflow, WorkflowNode node)
		{
			return workflow.NodeGraph.Groups.Where(group => group.Contains(node)).Select(group => group.Name).ToList();
		}
	}
}
