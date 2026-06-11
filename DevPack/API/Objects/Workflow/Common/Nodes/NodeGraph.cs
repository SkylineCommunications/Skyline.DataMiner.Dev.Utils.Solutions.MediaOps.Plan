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

		// Maps the original node that was swapped out to the node that currently represents it in the graph.
		// The original node is always kept as the key so future logic (e.g. running jobs) can reason about it.
		private readonly Dictionary<TNode, TNode> originalToCurrentSwap = [];

		private Action<IReadOnlyDictionary<string, string>> externalReferenceRetargeter;

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
		/// Replaces an existing node in the graph with a new, not-yet-added node, retargeting all connections,
		/// parent-child links and node-scoped <see cref="DataReference"/>s to the new node.
		/// </summary>
		/// <remarks>
		/// The new node receives its own identifier; all references to the old node's identifier are rewritten to
		/// the new one. The original node that was swapped out is preserved internally (see <see cref="GetOriginalNode"/>);
		/// when a previously swapped node is swapped again, the mapping keeps the original node and updates its current
		/// representation to <paramref name="newNode"/>.
		/// Context-specific type rules (e.g. a resource node can only be swapped to another resource node inside a job)
		/// are not enforced here; they are validated against the net original-to-final transition by the node graph
		/// validator when the owning job or workflow is saved, so all swap errors are aggregated with the other
		/// validation errors instead of failing on the first illegal swap.
		/// </remarks>
		/// <param name="oldNode">The node currently in the graph that should be replaced.</param>
		/// <param name="newNode">The new, freshly initialized node that is not part of the graph.</param>
		/// <returns>The current <see cref="NodeGraph{TNode}"/> instance for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="oldNode"/> or <paramref name="newNode"/> is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when <paramref name="oldNode"/> is not part of the graph, or when <paramref name="newNode"/> is already part of the graph or is not a newly initialized node.</exception>
		public NodeGraph<TNode> Swap(TNode oldNode, TNode newNode)
		{
			if (oldNode == null)
			{
				throw new ArgumentNullException(nameof(oldNode));
			}

			if (newNode == null)
			{
				throw new ArgumentNullException(nameof(newNode));
			}

			if (!nodes.Contains(oldNode))
			{
				throw new InvalidOperationException("Node to swap is not part of this graph.");
			}

			if (nodes.Contains(newNode))
			{
				throw new InvalidOperationException("The new node is already part of this graph. The node to swap to must be a new node.");
			}

			if (!newNode.IsNew)
			{
				throw new InvalidOperationException("The node to swap to must be a newly initialized node.");
			}

			// Context-specific type rules (e.g. resource -> resource only inside jobs) are deferred to the node graph
			// validator so that all swap errors are aggregated with the other validation errors at save time.

			// Replace the node in the node list, preserving its position.
			var index = nodes.IndexOf(oldNode);
			nodes[index] = newNode;

			// Retarget all connections that reference the old node.
			foreach (var connection in connections.Where(c => c.From == oldNode || c.To == oldNode).ToList())
			{
				connection.Retarget(oldNode, newNode);
			}

			// Retarget parent-child links: the old node can be both a parent and a child.
			RetargetLinks(oldNode, newNode);

			// Rewrite node-scoped DataReferences (orchestration settings of every node in the graph).
			var idMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				[oldNode.Id] = newNode.Id,
			};

			foreach (var node in nodes)
			{
				OrchestrationSettingsCloner.RetargetReferences(node.OrchestrationSettings, idMap);
			}

			// Let the owner retarget any references that live outside the graph (e.g. owner-level orchestration settings).
			externalReferenceRetargeter?.Invoke(idMap);

			// Track the swap so the original node stays available and re-swaps keep referencing the original.
			RecordSwap(oldNode, newNode);

			return this;
		}

		/// <summary>
		/// Gets the original node that a current node represents, if the current node is the result of one or more swaps.
		/// </summary>
		/// <param name="currentNode">The node that currently lives in the graph.</param>
		/// <returns>The original node that was swapped out, or <paramref name="currentNode"/> itself when it was never the result of a swap.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="currentNode"/> is null.</exception>
		public TNode GetOriginalNode(TNode currentNode)
		{
			if (currentNode == null)
			{
				throw new ArgumentNullException(nameof(currentNode));
			}

			foreach (var entry in originalToCurrentSwap)
			{
				if (entry.Value == currentNode)
				{
					return entry.Key;
				}
			}

			return currentNode;
		}

		/// <summary>
		/// Gets the internal mapping of each originally swapped-out node to the node that currently represents it.
		/// </summary>
		internal IReadOnlyDictionary<TNode, TNode> SwapMappings => originalToCurrentSwap;

		/// <summary>
		/// Re-adds a node that was previously swapped out back into the graph, keeping its original object identity
		/// (and therefore its underlying section) intact.
		/// </summary>
		/// <remarks>
		/// This is used by running-job timing logic to retain a swapped-out node next to its replacement instead of
		/// dropping it. The original node object is preserved so that, when the job is persisted, the change is seen as
		/// an update of the existing section rather than a removal followed by an add, which keeps the storage-layer
		/// merge field-level and conflict-aware. The method is idempotent: restoring a node that is already part of the
		/// graph is a no-op.
		/// </remarks>
		/// <param name="original">The originally swapped-out node to restore.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="original"/> is null.</exception>
		internal void RestoreSwappedOutNode(TNode original)
		{
			if (original == null)
			{
				throw new ArgumentNullException(nameof(original));
			}

			if (nodes.Contains(original))
			{
				return;
			}

			nodes.Add(original);
		}

		/// <summary>
		/// Sets the delegate that retargets references living outside the graph (e.g. owner-level orchestration settings)
		/// after a swap, using the old-id -> new-id map produced by the swap.
		/// </summary>
		/// <param name="retargeter">The retarget delegate, or <see langword="null"/> to clear it.</param>
		internal void SetExternalReferenceRetargeter(Action<IReadOnlyDictionary<string, string>> retargeter)
		{
			externalReferenceRetargeter = retargeter;
		}

		private void RetargetLinks(TNode oldNode, TNode newNode)
		{
			// Move a child link belonging to the old node onto the new node.
			if (childToParent.TryGetValue(oldNode, out var parent))
			{
				childToParent.Remove(oldNode);
				childToParent[newNode] = parent;
			}

			// Re-point any children whose parent was the old node.
			foreach (var child in childToParent.Where(kvp => kvp.Value == oldNode).Select(kvp => kvp.Key).ToList())
			{
				childToParent[child] = newNode;
			}
		}

		private void RecordSwap(TNode oldNode, TNode newNode)
		{
			// If the old node is already the current representation of an earlier swap, keep the original key and
			// update its current value. Otherwise, the old node becomes the original key of a new mapping.
			TNode originalKey = null;
			foreach (var entry in originalToCurrentSwap)
			{
				if (entry.Value == oldNode)
				{
					originalKey = entry.Key;
					break;
				}
			}

			if (originalKey != null)
			{
				originalToCurrentSwap[originalKey] = newNode;
			}
			else
			{
				originalToCurrentSwap[oldNode] = newNode;
			}
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
			if (ReferenceEquals(this, other))
			{
				return true;
			}

			if (other is null)
			{
				return false;
			}

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
