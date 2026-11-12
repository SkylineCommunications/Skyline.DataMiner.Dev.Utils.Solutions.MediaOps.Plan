namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a named group of nodes within a <see cref="NodeGraph{TNode}"/>.
	/// </summary>
	/// <remarks>
	/// A group is identified by its <see cref="Id"/>, which is assigned automatically when the group is created and is
	/// kept across updates. Group names are not required to be unique and may be empty; it is up to the consumer to
	/// give groups meaningful names.
	/// </remarks>
	/// <typeparam name="TNode">The type of nodes in the group, must derive from <see cref="NodeBase"/>.</typeparam>
	public sealed class NodeGroup<TNode> where TNode : NodeBase
	{
		private readonly NodeGraph<TNode> graph;
		private readonly List<TNode> nodes = [];

		/// <summary>
		/// Initializes a new instance of the <see cref="NodeGroup{TNode}"/> class with a new identifier.
		/// </summary>
		/// <param name="graph">The graph that owns this group.</param>
		/// <param name="name">The name of the group.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="graph"/> is null.</exception>
		internal NodeGroup(NodeGraph<TNode> graph, string name)
		{
			this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
			Id = Guid.NewGuid();
			Name = name;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NodeGroup{TNode}"/> class from a storage section.
		/// </summary>
		/// <param name="graph">The graph that owns this group.</param>
		/// <param name="section">The storage node groups section to parse.</param>
		/// <param name="nodeResolver">A function that resolves node identifiers to nodes of the graph, returning null for unknown nodes.</param>
		/// <param name="onWarning">Receives a message for each inconsistency in the stored data that is repaired while parsing.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="graph"/>, <paramref name="section"/>, <paramref name="nodeResolver"/> or <paramref name="onWarning"/> is null.</exception>
		internal NodeGroup(NodeGraph<TNode> graph, StorageWorkflow.NodeGroupsSection section, Func<string, TNode> nodeResolver, Action<string> onWarning)
		{
			this.graph = graph ?? throw new ArgumentNullException(nameof(graph));

			if (section == null)
			{
				throw new ArgumentNullException(nameof(section));
			}

			if (nodeResolver == null)
			{
				throw new ArgumentNullException(nameof(nodeResolver));
			}

			if (onWarning == null)
			{
				throw new ArgumentNullException(nameof(onWarning));
			}

			Id = ParseId(section, onWarning);
			Name = section.GroupName;

			foreach (var nodeId in section.GroupNodeIds ?? [])
			{
				var node = nodeId == null ? null : nodeResolver(nodeId);
				if (node == null)
				{
					onWarning($"Node group '{section.GroupName}' references unknown node '{nodeId}'. This node will be ignored.");
					continue;
				}

				Add(node);
			}
		}

		/// <summary>
		/// Gets the unique identifier of the group within its graph.
		/// </summary>
		/// <remarks>
		/// The identifier is assigned automatically and cannot be changed.
		/// </remarks>
		public Guid Id { get; }

		/// <summary>
		/// Gets or sets the name of the group.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets a read-only collection of all nodes in the group.
		/// </summary>
		public IReadOnlyCollection<TNode> Nodes => nodes.AsReadOnly();

		/// <summary>
		/// Adds a node to the group.
		/// </summary>
		/// <param name="node">The node to add.</param>
		/// <returns>The current <see cref="NodeGroup{TNode}"/> instance for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the node is not part of the graph that owns this group.</exception>
		public NodeGroup<TNode> Add(TNode node)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			if (!graph.Contains(node))
			{
				throw new InvalidOperationException("Node is not part of the graph that owns this group.");
			}

			if (!nodes.Contains(node))
			{
				nodes.Add(node);
			}

			return this;
		}

		/// <summary>
		/// Removes a node from the group. The node itself remains part of the graph.
		/// </summary>
		/// <param name="node">The node to remove.</param>
		/// <returns>true when the node was a member of the group; otherwise, false.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
		public bool Remove(TNode node)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			return nodes.Remove(node);
		}

		/// <summary>
		/// Determines whether the specified node is a member of the group.
		/// </summary>
		/// <param name="node">The node to look for.</param>
		/// <returns>true when the node is a member of the group; otherwise, false.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
		public bool Contains(TNode node)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			return nodes.Contains(node);
		}

		/// <summary>
		/// Removes all nodes from the group. The nodes themselves remain part of the graph.
		/// </summary>
		public void Clear()
		{
			nodes.Clear();
		}

		/// <summary>
		/// Replaces a node with another node, preserving its position within the group.
		/// </summary>
		/// <param name="oldNode">The node to replace.</param>
		/// <param name="newNode">The node that takes its place.</param>
		internal void ReplaceNode(TNode oldNode, TNode newNode)
		{
			var index = nodes.IndexOf(oldNode);
			if (index < 0)
			{
				return;
			}

			if (nodes.Contains(newNode))
			{
				nodes.RemoveAt(index);
				return;
			}

			nodes[index] = newNode;
		}

		/// <summary>
		/// Gets a stable representation of the group used for equality and hash code calculation of the owning graph.
		/// </summary>
		internal string GetComparisonKey()
		{
			var nameKey = Name == null ? "null" : $"value:{Name}";
			return $"{Id}:{nameKey}:{String.Join(",", nodes.Select(node => node?.Id).OrderBy(id => id, StringComparer.Ordinal))}";
		}

		/// <summary>
		/// Builds the storage section representing the current state of the group.
		/// </summary>
		/// <returns>A new <see cref="StorageWorkflow.NodeGroupsSection"/> for the group.</returns>
		internal StorageWorkflow.NodeGroupsSection ToSection()
		{
			var section = new StorageWorkflow.NodeGroupsSection
			{
				GroupID = Id.ToString(),
				GroupName = Name,
			};

			foreach (var node in nodes)
			{
				section.GroupNodeIds.Add(node.Id);
			}

			return section;
		}

		private Guid ParseId(StorageWorkflow.NodeGroupsSection section, Action<string> onWarning)
		{
			var id = section.GetUniqueGroupId(graph.ContainsGroup);
			if (id != section.EffectiveGroupId)
			{
				onWarning($"Node group '{section.GroupName}' has duplicate ID '{section.EffectiveGroupId}'. ID '{id}' will be used instead.");
			}

			return id;
		}
	}
}
