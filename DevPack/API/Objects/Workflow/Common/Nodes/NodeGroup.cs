namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Represents a named group of nodes within a <see cref="NodeGraph{TNode}"/>.
	/// </summary>
	/// <remarks>
	/// Group names are not required to be unique and may be empty; it is up to the consumer to give groups meaningful
	/// names. A group is identified by its object reference, not by its name.
	/// </remarks>
	/// <typeparam name="TNode">The type of nodes in the group, must derive from <see cref="NodeBase"/>.</typeparam>
	public sealed class NodeGroup<TNode> where TNode : NodeBase
	{
		private readonly NodeGraph<TNode> graph;
		private readonly List<TNode> nodes = [];

		/// <summary>
		/// Initializes a new instance of the <see cref="NodeGroup{TNode}"/> class.
		/// </summary>
		/// <param name="graph">The graph that owns this group.</param>
		/// <param name="name">The name of the group.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="graph"/> is null.</exception>
		internal NodeGroup(NodeGraph<TNode> graph, string name)
		{
			this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
			Name = name;
		}

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
			return $"{Name}:{String.Join(",", nodes.Select(node => node?.Id).OrderBy(id => id, StringComparer.Ordinal))}";
		}
	}
}
