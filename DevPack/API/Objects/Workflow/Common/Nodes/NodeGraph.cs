namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Helper;

	/// <summary>
	/// Represents a directed graph of nodes with connections between them.
	/// </summary>
	/// <typeparam name="TNode">The type of nodes in the graph, must derive from <see cref="NodeBase"/>.</typeparam>
	public sealed class NodeGraph<TNode> : IEquatable<NodeGraph<TNode>> where TNode : NodeBase
	{
		private readonly List<TNode> nodes = [];
		private readonly List<NodeConnection<TNode>> connections = [];
		private readonly Dictionary<TNode, TNode> childToParent = [];

		/// <summary>
		/// Initializes a new instance of the <see cref="NodeGraph{TNode}"/> class.
		/// </summary>
		internal NodeGraph()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NodeGraph{TNode}"/> class with the specified nodes.
		/// </summary>
		/// <param name="nodes">The collection of nodes to add to the graph.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="nodes"/> is null.</exception>
		internal NodeGraph(ICollection<TNode> nodes)
		{
			if (nodes == null)
			{
				throw new ArgumentNullException(nameof(nodes));
			}

			this.nodes.AddRange(nodes);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NodeGraph{TNode}"/> class with the specified nodes and connections.
		/// </summary>
		/// <param name="nodes">The collection of nodes to add to the graph.</param>
		/// <param name="connections">The collection of connections between nodes.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="nodes"/> or <paramref name="connections"/> is null.</exception>
		internal NodeGraph(ICollection<TNode> nodes, ICollection<NodeConnection<TNode>> connections)
			: this(nodes)
		{
			if (connections == null)
			{
				throw new ArgumentNullException(nameof(connections));
			}

			this.connections.AddRange(connections);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NodeGraph{TNode}"/> class with the specified nodes and parent-child links.
		/// </summary>
		/// <param name="nodes">The collection of nodes to add to the graph.</param>
		/// <param name="links">The collection of parent-child links, where each entry maps a child node to its parent node.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="nodes"/> or <paramref name="links"/> is null.</exception>
		internal NodeGraph(ICollection<TNode> nodes, IEnumerable<KeyValuePair<TNode, TNode>> links)
			: this(nodes)
		{
			if (links == null)
			{
				throw new ArgumentNullException(nameof(links));
			}

			foreach (var link in links)
			{
				childToParent[link.Key] = link.Value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NodeGraph{TNode}"/> class with the specified nodes, connections and parent-child links.
		/// </summary>
		/// <param name="nodes">The collection of nodes to add to the graph.</param>
		/// <param name="connections">The collection of connections between nodes.</param>
		/// <param name="links">The collection of parent-child links, where each entry maps a child node to its parent node.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="nodes"/>, <paramref name="connections"/> or <paramref name="links"/> is null.</exception>
		internal NodeGraph(ICollection<TNode> nodes, ICollection<NodeConnection<TNode>> connections, IEnumerable<KeyValuePair<TNode, TNode>> links)
			: this(nodes, connections)
		{
			if (links == null)
			{
				throw new ArgumentNullException(nameof(links));
			}

			foreach (var link in links)
			{
				childToParent[link.Key] = link.Value;
			}
		}

		/// <summary>
		/// Gets a read-only collection of all nodes in the graph.
		/// </summary>
		public IReadOnlyCollection<TNode> Nodes => nodes.AsReadOnly();

		/// <summary>
		/// Gets a read-only collection of all connections in the graph.
		/// </summary>
		public IReadOnlyCollection<NodeConnection<TNode>> Connections => connections.AsReadOnly();

		/// <summary>
		/// Adds a node to the graph.
		/// </summary>
		/// <param name="node">The node to add.</param>
		/// <returns>The current <see cref="NodeGraph{TNode}"/> instance for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
		public NodeGraph<TNode> Add(TNode node)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			nodes.Add(node);

			return this;
		}

		/// <summary>
		/// Removes a node from the graph and all connections associated with it.
		/// When the node is a parent, all of its child nodes are removed as well.
		/// </summary>
		/// <param name="node">The node to remove.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the node is not part of the graph.</exception>
		public void Remove(TNode node)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			if (!nodes.Contains(node))
			{
				throw new InvalidOperationException("Node is not part of this graph.");
			}

			var children = GetChildren(node).ToList();
			foreach (var child in children)
			{
				Remove(child);
			}

			var connectionsToRemove = connections.Where(c => c.From == node || c.To == node).ToList();
			foreach (var connection in connectionsToRemove)
			{
				Disconnect(connection);
			}

			childToParent.Remove(node);

			nodes.Remove(node);
		}

		/// <summary>
		/// Creates a directed connection from one node to another.
		/// </summary>
		/// <param name="from">The source node.</param>
		/// <param name="to">The destination node.</param>
		/// <returns>The current <see cref="NodeGraph{TNode}"/> instance for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="from"/> or <paramref name="to"/> is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when either node is not part of the graph.</exception>
		public NodeGraph<TNode> Connect(TNode from, TNode to)
		{
			if (from == null)
			{
				throw new ArgumentNullException(nameof(from));
			}

			if (to == null)
			{
				throw new ArgumentNullException(nameof(to));
			}

			if (!nodes.Contains(from))
			{
				throw new InvalidOperationException("Source node is not part of this graph.");
			}

			if (!nodes.Contains(to))
			{
				throw new InvalidOperationException("Target node is not part of this graph.");
			}

			var connection = new NodeConnection<TNode>(from, to);
			connections.Add(connection);

			return this;
		}

		/// <summary>
		/// Removes a connection from the graph.
		/// </summary>
		/// <param name="connection">The connection to remove.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
		public void Disconnect(NodeConnection<TNode> connection)
		{
			if (connection == null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			connections.Remove(connection);
		}

		/// <summary>
		/// Gets all outgoing connections from the specified node.
		/// </summary>
		/// <param name="node">The source node.</param>
		/// <returns>An enumeration of connections where the specified node is the source.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
		public IEnumerable<NodeConnection<TNode>> GetOutgoing(TNode node)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			return connections.Where(c => c.From == node);
		}

		/// <summary>
		/// Gets all incoming connections to the specified node.
		/// </summary>
		/// <param name="node">The destination node.</param>
		/// <returns>An enumeration of connections where the specified node is the destination.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
		public IEnumerable<NodeConnection<TNode>> GetIncoming(TNode node)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			return connections.Where(c => c.To == node);
		}

		/// <summary>
		/// Links a child node to a parent node, establishing a parent-child relationship.
		/// </summary>
		/// <remarks>
		/// A parent-child link is distinct from a connection. A child can have at most one parent while a parent can have multiple children.
		/// This method only records the link; rules such as self-linking, cascaded linking or conflicts with connections are reported by the graph validator.
		/// </remarks>
		/// <param name="parent">The parent node.</param>
		/// <param name="child">The child node.</param>
		/// <returns>The current <see cref="NodeGraph{TNode}"/> instance for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="parent"/> or <paramref name="child"/> is null.</exception>
		public NodeGraph<TNode> Link(TNode parent, TNode child)
		{
			if (parent == null)
			{
				throw new ArgumentNullException(nameof(parent));
			}

			if (child == null)
			{
				throw new ArgumentNullException(nameof(child));
			}

			if (!nodes.Contains(parent))
			{
				throw new InvalidOperationException("Parent node is not part of this graph.");
			}

			if (!nodes.Contains(child))
			{
				throw new InvalidOperationException("Child node is not part of this graph.");
			}

			childToParent[child] = parent;

			return this;
		}

		/// <summary>
		/// Removes the parent-child link for the specified child node, if one exists.
		/// </summary>
		/// <param name="child">The child node whose parent link should be removed.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="child"/> is null.</exception>
		public void Unlink(TNode child)
		{
			if (child == null)
			{
				throw new ArgumentNullException(nameof(child));
			}

			childToParent.Remove(child);
		}

		/// <summary>
		/// Gets the parent of the specified node.
		/// </summary>
		/// <param name="node">The node whose parent should be retrieved.</param>
		/// <returns>The parent node, or <see langword="null"/> when the node has no parent.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
		public TNode GetParent(TNode node)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			return childToParent.TryGetValue(node, out var parent) ? parent : null;
		}

		/// <summary>
		/// Gets all child nodes of the specified parent node.
		/// </summary>
		/// <param name="node">The parent node whose children should be retrieved.</param>
		/// <returns>An enumeration of nodes that are linked to the specified node as children.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="node"/> is null.</exception>
		public IEnumerable<TNode> GetChildren(TNode node)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			return childToParent.Where(kvp => kvp.Value == node).Select(kvp => kvp.Key);
		}

		/// <summary>
		/// Gets the parent-child links in the graph, where each entry maps a child node to its parent node.
		/// </summary>
		internal IEnumerable<KeyValuePair<TNode, TNode>> Links => childToParent;

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not NodeGraph<TNode> other)
			{
				return false;
			}

			return Equals(other);
		}

		/// <summary>
		/// Determines whether the current <see cref="NodeGraph{TNode}"/> instance is equal to another instance.
		/// </summary>
		/// <param name="other">The instance to compare with the current instance.</param>
		/// <returns>true if the instances are equal; otherwise, false.</returns>
		public bool Equals(NodeGraph<TNode> other)
		{
			return nodes.ScrambledEquals(other.nodes)
				&& connections.ScrambledEquals(other.connections)
				&& LinkKeys().ScrambledEquals(other.LinkKeys());
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				foreach (var node in nodes.OrderBy(x => x.Id).ToArray())
				{
					hash = hash * 31 + (node?.GetHashCode() ?? 0);
				}

				foreach (var connection in connections.OrderBy(x => x.Id).ToArray())
				{
					hash = hash * 31 + (connection?.GetHashCode() ?? 0);
				}

				foreach (var link in LinkKeys().OrderBy(x => x).ToArray())
				{
					hash = hash * 31 + link.GetHashCode();
				}

				return hash;
			}
		}

		private IEnumerable<string> LinkKeys()
			=> childToParent.Select(kvp => $"{kvp.Key?.Id}->{kvp.Value?.Id}");

		/// <summary>
		/// Determines whether two <see cref="NodeGraph{TNode}"/> instances are equal.
		/// </summary>
		/// <param name="left">The left instance to compare.</param>
		/// <param name="right">The right instance to compare.</param>
		/// <returns>true if the instances are equal; otherwise, false.</returns>
		public static bool operator ==(NodeGraph<TNode> left, NodeGraph<TNode> right)
		{
			if (ReferenceEquals(left, right))
			{
				return true;
			}

			if (left is null || right is null)
			{
				return false;
			}

			return left.Equals(right);
		}

		/// <summary>
		/// Determines whether two <see cref="NodeGraph{TNode}"/> instances are not equal.
		/// </summary>
		/// <param name="left">The left instance to compare.</param>
		/// <param name="right">The right instance to compare.</param>
		/// <returns>true if the instances are not equal; otherwise, false.</returns>
		public static bool operator !=(NodeGraph<TNode> left, NodeGraph<TNode> right)
		{
			return !(left == right);
		}
	}
}
