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

			var connectionsToRemove = connections.Where(c => c.From == node || c.To == node).ToList();
			foreach (var connection in connectionsToRemove)
			{
				Disconnect(connection);
			}

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
			return nodes.ScrambledEquals(other.nodes) && connections.ScrambledEquals(other.connections);
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

				return hash;
			}
		}

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
