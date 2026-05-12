namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	public sealed class NodeGraph<TNode> where TNode : NodeBase
	{
		private readonly List<TNode> nodes = [];
		private readonly List<NodeConnection<TNode>> connections = [];

		internal NodeGraph()
		{
		}

		internal NodeGraph(ICollection<TNode> nodes)
		{
			if (nodes == null)
			{
				throw new ArgumentNullException(nameof(nodes));
			}

			this.nodes.AddRange(nodes);
		}

		internal NodeGraph(ICollection<TNode> nodes, ICollection<NodeConnection<TNode>> connections)
			: this(nodes)
		{
			if (connections == null)
			{
				throw new ArgumentNullException(nameof(connections));
			}

			this.connections.AddRange(connections);
		}

		public IReadOnlyCollection<TNode> Nodes => nodes.AsReadOnly();

		public IReadOnlyCollection<NodeConnection<TNode>> Connections => connections.AsReadOnly();

		public NodeGraph<TNode> Add(TNode node)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			nodes.Add(node);

			return this;
		}

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

		public void Disconnect(NodeConnection<TNode> connection)
		{
			if (connection == null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			connections.Remove(connection);
		}

		public IEnumerable<NodeConnection<TNode>> GetOutgoing(TNode node)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			return connections.Where(c => c.From == node);
		}

		public IEnumerable<NodeConnection<TNode>> GetIncoming(TNode node)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			return connections.Where(c => c.To == node);
		}
	}

	public sealed class NodeConnection<TNode> : TrackableObject where TNode : NodeBase
	{
		private StorageWorkflow.ConnectionsSection originalSection;
		private StorageWorkflow.ConnectionsSection updatedSection;

		internal NodeConnection(TNode from, TNode to)
		{
			From = from ?? throw new ArgumentNullException(nameof(from));
			To = to ?? throw new ArgumentNullException(nameof(to));

			Id = Guid.NewGuid().ToString();
			IsNew = true;
		}

		internal NodeConnection(StorageWorkflow.ConnectionsSection section, Func<string, TNode> nodeResolver)
		{
			if (nodeResolver == null)
			{
				throw new ArgumentNullException(nameof(nodeResolver));
			}

			ParseSection(section, nodeResolver);
			InitTracking();
		}

		public string Id { get; private set; }

		public TNode From { get; private set; }

		public TNode To { get; private set; }

		internal StorageWorkflow.ConnectionsSection OriginalSection => originalSection;

		internal StorageWorkflow.ConnectionsSection GetSectionWithChanges()
		{
			if (updatedSection == null)
			{
				updatedSection = IsNew
					? new StorageWorkflow.ConnectionsSection()
					{
						ConnectionID = Id,
					}
					: originalSection.Clone();
			}

			updatedSection.SourceNodeID = From.Id;
			updatedSection.DestinationNodeID = To.Id;

			return updatedSection;
		}

		private void ParseSection(StorageWorkflow.ConnectionsSection section, Func<string, TNode> nodeResolver)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Id = section.ConnectionID;

			From = nodeResolver(section.SourceNodeID) ?? throw new InvalidOperationException($"Connection {Id} references unknown source node {section.SourceNodeID}.");
			To = nodeResolver(section.DestinationNodeID) ?? throw new InvalidOperationException($"Connection {Id} references unknown target node {section.DestinationNodeID}.");
		}
	}
}
