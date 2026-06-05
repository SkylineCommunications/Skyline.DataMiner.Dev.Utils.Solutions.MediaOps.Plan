namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a directed connection between two nodes in a graph.
	/// </summary>
	/// <typeparam name="TNode">The type of nodes connected, must derive from <see cref="NodeBase"/>.</typeparam>
	public sealed class NodeConnection<TNode> : TrackableObject where TNode : NodeBase
	{
		private StorageWorkflow.ConnectionsSection originalSection;
		private StorageWorkflow.ConnectionsSection updatedSection;

		/// <summary>
		/// Initializes a new instance of the <see cref="NodeConnection{TNode}"/> class with the specified source and destination nodes.
		/// </summary>
		/// <param name="from">The source node of the connection.</param>
		/// <param name="to">The destination node of the connection.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="from"/> or <paramref name="to"/> is null.</exception>
		internal NodeConnection(TNode from, TNode to)
		{
			From = from ?? throw new ArgumentNullException(nameof(from));
			To = to ?? throw new ArgumentNullException(nameof(to));

			Id = Guid.NewGuid().ToString();
			IsNew = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NodeConnection{TNode}"/> class from a storage section.
		/// </summary>
		/// <param name="section">The storage workflow connections section to parse.</param>
		/// <param name="nodeResolver">A function that resolves node identifiers to node instances.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="nodeResolver"/> is null.</exception>
		internal NodeConnection(StorageWorkflow.ConnectionsSection section, Func<string, TNode> nodeResolver)
		{
			if (nodeResolver == null)
			{
				throw new ArgumentNullException(nameof(nodeResolver));
			}

			ParseSection(section, nodeResolver);
			InitTracking();
		}

		/// <summary>
		/// Gets the unique identifier of the connection.
		/// </summary>
		public string Id { get; private set; }

		/// <summary>
		/// Gets the source node of the connection.
		/// </summary>
		public TNode From { get; private set; }

		/// <summary>
		/// Gets the destination node of the connection.
		/// </summary>
		public TNode To { get; private set; }

		/// <summary>
		/// Gets the original storage section for this connection.
		/// </summary>
		internal StorageWorkflow.ConnectionsSection OriginalSection => originalSection;

		/// <summary>
		/// Retargets this connection so that any endpoint currently referencing <paramref name="oldNode"/>
		/// points at <paramref name="newNode"/> instead.
		/// </summary>
		/// <param name="oldNode">The node that should be replaced.</param>
		/// <param name="newNode">The node that takes the place of <paramref name="oldNode"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="oldNode"/> or <paramref name="newNode"/> is null.</exception>
		internal void Retarget(TNode oldNode, TNode newNode)
		{
			if (oldNode == null)
			{
				throw new ArgumentNullException(nameof(oldNode));
			}

			if (newNode == null)
			{
				throw new ArgumentNullException(nameof(newNode));
			}

			if (From == oldNode)
			{
				From = newNode;
			}

			if (To == oldNode)
			{
				To = newNode;
			}
		}

		/// <summary>
		/// Gets or creates a section with the current changes applied.
		/// </summary>
		/// <returns>A <see cref="StorageWorkflow.ConnectionsSection"/> containing the current state of the connection.</returns>
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

			// Default values until correctly implemented. This will prevent some job integration tests from failing as the DOM CRUD is still adding these values in the background.
			updatedSection.ConnectionExecutionOrder = 0;
			updatedSection.ConnectionType = StorageWorkflow.SlcWorkflowIds.Enums.Connectiontype.LevelBased;
			updatedSection.ConnectionSubtype = StorageWorkflow.SlcWorkflowIds.Enums.Connectionsubtype.All;
			updatedSection.PredefinedSubset = StorageWorkflow.SlcWorkflowIds.Enums.Predefinedsubset.VAD;

			return updatedSection;
		}

		/// <summary>
		/// Parses properties from the specified storage section.
		/// </summary>
		/// <param name="section">The storage workflow connections section to parse.</param>
		/// <param name="nodeResolver">A function that resolves node identifiers to node instances.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="section"/> is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the source or destination node cannot be resolved.</exception>
		private void ParseSection(StorageWorkflow.ConnectionsSection section, Func<string, TNode> nodeResolver)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Id = section.ConnectionID;

			From = nodeResolver(section.SourceNodeID) ?? throw new InvalidOperationException($"Connection {Id} references unknown source node {section.SourceNodeID}.");
			To = nodeResolver(section.DestinationNodeID) ?? throw new InvalidOperationException($"Connection {Id} references unknown target node {section.DestinationNodeID}.");
		}
	}
}
