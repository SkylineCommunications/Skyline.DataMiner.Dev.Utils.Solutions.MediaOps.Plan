namespace RT_MediaOps.Plan.Workflow.Nodes
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using StorageWorkflow = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

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
		public void AddGroup_AssignsUniqueIdThatSurvivesRenamesAndMembershipChanges()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(node);

			var first = workflow.NodeGraph.AddGroup("Group");
			var second = workflow.NodeGraph.AddGroup("Group");
			var id = first.Id;

			Assert.AreNotEqual(Guid.Empty, first.Id);
			Assert.AreNotEqual(Guid.Empty, second.Id);
			Assert.AreNotEqual(first.Id, second.Id);

			first.Name = "Renamed";
			first.Add(node);

			Assert.AreEqual(id, first.Id);
		}

		[TestMethod]
		public void AddGroups_ParsesStoredSectionsAndRoundTripsThroughToSection()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(node);

			var storedId = Guid.NewGuid();
			var section = new StorageWorkflow.NodeGroupsSection { GroupID = storedId.ToString(), GroupName = "Group" };
			section.GroupNodeIds.Add(node.Id);

			var warnings = new List<string>();
			workflow.NodeGraph.AddGroups(new[] { section }, id => workflow.NodeGraph.Nodes.FirstOrDefault(x => x.Id == id), warnings.Add);

			var group = workflow.NodeGraph.Groups.Single();
			Assert.AreEqual(storedId, group.Id);
			Assert.AreEqual("Group", group.Name);
			CollectionAssert.AreEqual(new[] { node }, group.Nodes.ToArray());
			Assert.AreEqual(0, warnings.Count);

			var written = group.ToSection();
			Assert.AreEqual(storedId.ToString(), written.GroupID);
			Assert.AreEqual("Group", written.GroupName);
			CollectionAssert.AreEqual(new[] { node.Id }, written.GroupNodeIds.ToArray());
		}

		[TestMethod]
		public void AddGroups_DuplicateStoredId_FallsBackToTheSectionIdAndWarns()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var sharedId = Guid.NewGuid().ToString();
			var first = new StorageWorkflow.NodeGroupsSection { GroupID = sharedId, GroupName = "First" };
			var second = new StorageWorkflow.NodeGroupsSection { GroupID = sharedId, GroupName = "Second" };

			var warnings = new List<string>();
			workflow.NodeGraph.AddGroups(new[] { first, second }, _ => null, warnings.Add);

			CollectionAssert.AreEqual(
				new[] { Guid.Parse(sharedId), second.ID.Id },
				workflow.NodeGraph.Groups.Select(group => group.Id).ToArray());
			Assert.AreEqual(1, warnings.Count);
		}

		[TestMethod]
		public void AddGroups_UnknownNode_IsIgnoredWithWarning()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var section = new StorageWorkflow.NodeGroupsSection { GroupID = Guid.NewGuid().ToString(), GroupName = "Group" };
			section.GroupNodeIds.Add("unknown");

			var warnings = new List<string>();
			workflow.NodeGraph.AddGroups(new[] { section }, _ => null, warnings.Add);

			Assert.AreEqual(0, workflow.NodeGraph.Groups.Single().Nodes.Count);
			Assert.AreEqual(1, warnings.Count);
		}

		[TestMethod]
		public void AddGroups_StoredAndSectionIdBothTaken_ResolvesToTheSameDerivedIdOnEveryRead()
		{
			var sections = DoubleCollisionSections();

			var firstRead = new Workflow { Name = "Workflow" };
			firstRead.NodeGraph.AddGroups(sections, _ => null, _ => { });
			var secondRead = new Workflow { Name = "Workflow" };
			secondRead.NodeGraph.AddGroups(sections, _ => null, _ => { });

			var firstIds = firstRead.NodeGraph.Groups.Select(group => group.Id).ToArray();
			CollectionAssert.AreEqual(firstIds, secondRead.NodeGraph.Groups.Select(group => group.Id).ToArray());
			CollectionAssert.AllItemsAreUnique(firstIds);
			CollectionAssert.DoesNotContain(firstIds, Guid.Empty);
		}

		[TestMethod]
		public void Equality_GraphsDifferingOnlyInGroupId_AreNotEqual()
		{
			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var section = new StorageWorkflow.NodeGroupsSection { GroupID = Guid.NewGuid().ToString(), GroupName = "Group" };
			section.GroupNodeIds.Add(node.Id);
			var otherSection = new StorageWorkflow.NodeGroupsSection { GroupID = Guid.NewGuid().ToString(), GroupName = "Group" };
			otherSection.GroupNodeIds.Add(node.Id);

			var graph = GraphWithGroups(node, section);
			var other = GraphWithGroups(node, otherSection);

			Assert.AreNotEqual(graph, other);
		}

		[TestMethod]
		public void Equality_GroupStorageRoundTrip_IsEqualWithEqualHashCode()
		{
			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var graph = new NodeGraph<WorkflowNode>(new WorkflowNode[] { node });
			graph.AddGroup("Group").Add(node);

			var roundTripped = GraphWithGroups(node, graph.Groups.Single().ToSection());

			Assert.AreEqual(graph, roundTripped);
			Assert.AreEqual(graph.GetHashCode(), roundTripped.GetHashCode());
		}

		[TestMethod]
		public void Equality_GroupReplacedBySameNameAndMembers_IsDetectedAsChange()
		{
			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var section = new StorageWorkflow.NodeGroupsSection { GroupID = Guid.NewGuid().ToString(), GroupName = "Group" };
			section.GroupNodeIds.Add(node.Id);

			var original = GraphWithGroups(node, section);
			var changed = GraphWithGroups(node, section);
			Assert.AreEqual(original, changed);

			changed.RemoveGroup(changed.Groups.Single());
			changed.AddGroup("Group").Add(node);

			Assert.AreNotEqual(original, changed);
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

		/// <summary>
		/// Builds stored group sections where the second group's stored ID and section ID are both already used by the
		/// first group, e.g. after a section was copied within the instance before the original.
		/// </summary>
		private static StorageWorkflow.NodeGroupsSection[] DoubleCollisionSections()
		{
			var original = new StorageWorkflow.NodeGroupsSection { GroupName = "Original" };
			original.GroupID = original.ID.Id.ToString();

			var copy = new StorageWorkflow.NodeGroupsSection { GroupID = original.GroupID, GroupName = "Copy" };

			return new[] { copy, original };
		}

		private static NodeGraph<WorkflowNode> GraphWithGroups(WorkflowNode node, params StorageWorkflow.NodeGroupsSection[] sections)
		{
			var graph = new NodeGraph<WorkflowNode>(new[] { node });
			graph.AddGroups(sections, id => id == node.Id ? node : null, _ => { });

			return graph;
		}

		private static List<string> GroupNamesOf(Workflow workflow, WorkflowNode node)
		{
			return workflow.NodeGraph.Groups.Where(group => group.Contains(node)).Select(group => group.Name).ToList();
		}
	}
}
