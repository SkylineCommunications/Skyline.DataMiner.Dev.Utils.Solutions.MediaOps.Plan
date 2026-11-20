namespace RT_MediaOps.Plan.Workflow.Nodes
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Parent-child links must stay reachable after a linked node is modified. The hash code of a node includes its alias,
	/// icon and orchestration settings, so the graph must not look up links by node value.
	/// </summary>
	[TestClass]
	public sealed class NodeGraphLinkTests
	{
		[TestMethod]
		public void ChildAliasChanged_GetParent_ReturnsParent()
		{
			ChildAliasChanged_GetParent_ReturnsParent(JobGraph());
			ChildAliasChanged_GetParent_ReturnsParent(WorkflowGraph());
			ChildAliasChanged_GetParent_ReturnsParent(RecurringJobGraph());
		}

		[TestMethod]
		public void ChildCapabilityAdded_UnlinkAndLinkToOtherParent_ReplacesLink()
		{
			ChildCapabilityAdded_UnlinkAndLinkToOtherParent_ReplacesLink(JobGraph());
			ChildCapabilityAdded_UnlinkAndLinkToOtherParent_ReplacesLink(WorkflowGraph());
			ChildCapabilityAdded_UnlinkAndLinkToOtherParent_ReplacesLink(RecurringJobGraph());
		}

		[TestMethod]
		public void ChildIconChanged_Remove_RemovesLink()
		{
			ChildIconChanged_Remove_RemovesLink(JobGraph());
			ChildIconChanged_Remove_RemovesLink(WorkflowGraph());
			ChildIconChanged_Remove_RemovesLink(RecurringJobGraph());
		}

		[TestMethod]
		public void ChildAliasChanged_Swap_MovesLinkToReplacement()
		{
			ChildAliasChanged_Swap_MovesLinkToReplacement(JobGraph());
			ChildAliasChanged_Swap_MovesLinkToReplacement(WorkflowGraph());
			ChildAliasChanged_Swap_MovesLinkToReplacement(RecurringJobGraph());
		}

		[TestMethod]
		public void Swap_ThenCopyOrchestrationSettingsFrom_KeepsParent()
		{
			Swap_ThenCopyOrchestrationSettingsFrom_KeepsParent(JobGraph());
			Swap_ThenCopyOrchestrationSettingsFrom_KeepsParent(WorkflowGraph());
			Swap_ThenCopyOrchestrationSettingsFrom_KeepsParent(RecurringJobGraph());
		}

		[TestMethod]
		public void SwappedOutNodeAliasChanged_RestoreSwappedOutNode_RejoinsGroup()
		{
			SwappedOutNodeAliasChanged_RestoreSwappedOutNode_RejoinsGroup(JobGraph());
			SwappedOutNodeAliasChanged_RestoreSwappedOutNode_RejoinsGroup(WorkflowGraph());
			SwappedOutNodeAliasChanged_RestoreSwappedOutNode_RejoinsGroup(RecurringJobGraph());
		}

		private static void ChildAliasChanged_GetParent_ReturnsParent<TNode>(GraphUnderTest<TNode> test)
			where TNode : NodeBase
		{
			var (parent, child) = test.AddLinkedPair();

			child.Alias = "Child renamed";

			Assert.AreSame(parent, test.Graph.GetParent(child), test.Name);
			CollectionAssert.AreEqual(new[] { child }, test.Graph.GetChildren(parent).ToList(), test.Name);
			test.AssertLinks((parent, child));
		}

		private static void ChildCapabilityAdded_UnlinkAndLinkToOtherParent_ReplacesLink<TNode>(GraphUnderTest<TNode> test)
			where TNode : NodeBase
		{
			var (_, child) = test.AddLinkedPair();
			var other = test.CreateNode("Other");
			test.Graph.Add(other);

			child.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()));
			test.Graph.Unlink(child);
			test.Graph.Link(other, child);

			Assert.AreSame(other, test.Graph.GetParent(child), test.Name);
			test.AssertLinks((other, child));
		}

		private static void ChildIconChanged_Remove_RemovesLink<TNode>(GraphUnderTest<TNode> test)
			where TNode : NodeBase
		{
			var (parent, child) = test.AddLinkedPair();

			child.IconImage = "encoder";
			test.Graph.Remove(child);

			CollectionAssert.AreEqual(new[] { parent }, test.Graph.Nodes.ToList(), test.Name);
			test.AssertLinks();
		}

		private static void ChildAliasChanged_Swap_MovesLinkToReplacement<TNode>(GraphUnderTest<TNode> test)
			where TNode : NodeBase
		{
			var (parent, child) = test.AddLinkedPair();
			var replacement = test.CreateNode("Replacement");

			child.Alias = "Child renamed";
			test.Graph.Swap(child, replacement);

			Assert.AreSame(parent, test.Graph.GetParent(replacement), test.Name);
			test.AssertLinks((parent, replacement));
		}

		private static void Swap_ThenCopyOrchestrationSettingsFrom_KeepsParent<TNode>(GraphUnderTest<TNode> test)
			where TNode : NodeBase
		{
			var parent = test.CreateNode("Parent");
			var child = test.CreateNode("Child");
			child.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()));
			test.Graph.Add(parent).Add(child).Link(parent, child);
			var replacement = test.CreateNode("Replacement");

			test.Graph.Swap(child, replacement);
			replacement.CopyOrchestrationSettingsFrom(child);

			Assert.AreSame(parent, test.Graph.GetParent(replacement), test.Name);
			test.AssertLinks((parent, replacement));
		}

		private static void SwappedOutNodeAliasChanged_RestoreSwappedOutNode_RejoinsGroup<TNode>(GraphUnderTest<TNode> test)
			where TNode : NodeBase
		{
			var node = test.CreateNode("Node");
			test.Graph.Add(node);
			var group = test.Graph.AddGroup("Group");
			group.Add(node);
			var replacement = test.CreateNode("Replacement");

			test.Graph.Swap(node, replacement);
			node.Alias = "Node renamed";
			test.Graph.RestoreSwappedOutNode(node);

			Assert.IsTrue(group.Contains(node), test.Name);
			Assert.IsTrue(group.Contains(replacement), test.Name);
		}

		private static GraphUnderTest<JobNode> JobGraph()
			=> new(nameof(Job), new Job().NodeGraph, alias => new JobResourcePoolNode(Guid.NewGuid()) { Alias = alias });

		private static GraphUnderTest<WorkflowNode> WorkflowGraph()
			=> new(nameof(Workflow), new Workflow { Name = "Workflow" }.NodeGraph, alias => new WorkflowResourcePoolNode(Guid.NewGuid()) { Alias = alias });

		private static GraphUnderTest<RecurringJobNode> RecurringJobGraph()
			=> new(nameof(RecurringJob), new RecurringJob().NodeGraph, alias => new RecurringJobResourcePoolNode(Guid.NewGuid()) { Alias = alias });

		private sealed class GraphUnderTest<TNode>
			where TNode : NodeBase
		{
			private readonly Func<string, TNode> createNode;

			public GraphUnderTest(string name, NodeGraph<TNode> graph, Func<string, TNode> createNode)
			{
				Name = name;
				Graph = graph;
				this.createNode = createNode;
			}

			public string Name { get; }

			public NodeGraph<TNode> Graph { get; }

			public TNode CreateNode(string alias) => createNode(alias);

			public (TNode Parent, TNode Child) AddLinkedPair()
			{
				var parent = CreateNode("Parent");
				var child = CreateNode("Child");
				Graph.Add(parent).Add(child).Link(parent, child);
				return (parent, child);
			}

			public void AssertLinks(params (TNode Parent, TNode Child)[] expected)
			{
				var actual = Graph.Links.ToList();

				Assert.AreEqual(expected.Length, actual.Count, $"{Name}: {String.Join(", ", actual.Select(Describe))}");
				foreach (var (parent, child) in expected)
				{
					Assert.IsTrue(
						actual.Any(link => ReferenceEquals(link.Value, parent) && ReferenceEquals(link.Key, child)),
						$"{Name}: missing link {parent.Alias}->{child.Alias} in {String.Join(", ", actual.Select(Describe))}");
				}
			}

			private static string Describe(KeyValuePair<TNode, TNode> link) => $"{link.Value?.Alias}->{link.Key?.Alias}";
		}
	}
}
