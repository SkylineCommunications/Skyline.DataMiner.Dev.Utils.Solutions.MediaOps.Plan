namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Generic helper that clones a <see cref="NodeGraph{TSource}"/> into a <see cref="NodeGraph{TDest}"/> using a caller-supplied node factory.
	/// </summary>
	/// <remarks>
	/// The cloner is intended to be reused for any "build X from Y" scenario where X and Y both expose a node graph.
	/// Typical examples are <c>Job</c> from <c>Workflow</c>, <c>RecurringJob</c> from <c>Workflow</c> or <c>RecurringJob</c> from <c>Job</c>.
	///
	/// The cloner is responsible for:
	/// - Iterating the source nodes and asking the caller to produce a destination node for each one.
	/// - Recreating connections so they point at the new destination nodes.
	/// - Returning a map from each source node id to the new destination node id, which callers can use
	///   to re-target other identifiers (e.g. <see cref="DataReference.NodeId"/>) that live outside the graph.
	///
	/// The cloner deliberately knows nothing about orchestration settings, properties or any other side data;
	/// callers compose this with other helpers (such as <see cref="OrchestrationSettingsCloner"/>) for those concerns.
	/// </remarks>
	internal static class NodeGraphCloner
	{
		/// <summary>
		/// Copies all nodes and connections from <paramref name="source"/> into <paramref name="destination"/>,
		/// regenerating node and connection identifiers in the process.
		/// </summary>
		/// <typeparam name="TSource">The source node type.</typeparam>
		/// <typeparam name="TDest">The destination node type.</typeparam>
		/// <param name="source">The source graph to clone.</param>
		/// <param name="destination">The destination graph to populate. Must be empty.</param>
		/// <param name="nodeFactory">
		/// Function that produces a destination node for each source node. Return <see langword="null"/> to skip a node;
		/// connections involving that node will be skipped as well.
		/// </param>
		/// <returns>
		/// A dictionary mapping each cloned source node id to the corresponding new destination node id.
		/// The dictionary uses ordinal-ignore-case comparison to match the resolver behavior.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/>, <paramref name="destination"/> or <paramref name="nodeFactory"/> is <see langword="null"/>.</exception>
		public static Dictionary<string, string> Clone<TSource, TDest>(
			NodeGraph<TSource> source,
			NodeGraph<TDest> destination,
			Func<TSource, TDest> nodeFactory)
			where TSource : NodeBase
			where TDest : NodeBase
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			if (nodeFactory == null)
			{
				throw new ArgumentNullException(nameof(nodeFactory));
			}

			var nodeMap = new Dictionary<TSource, TDest>();
			var nodeIdMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			foreach (var sourceNode in source.Nodes)
			{
				var destinationNode = nodeFactory(sourceNode);
				if (destinationNode == null)
				{
					continue;
				}

				nodeMap.Add(sourceNode, destinationNode);
				nodeIdMap.Add(sourceNode.Id, destinationNode.Id);
				destination.Add(destinationNode);
			}

			foreach (var connection in source.Connections)
			{
				if (!nodeMap.TryGetValue(connection.From, out var from) ||
					!nodeMap.TryGetValue(connection.To, out var to))
				{
					continue;
				}

				destination.Connect(from, to);
			}

			foreach (var link in source.Links)
			{
				if (!nodeMap.TryGetValue(link.Value, out var parent) ||
					!nodeMap.TryGetValue(link.Key, out var child))
				{
					continue;
				}

				destination.Link(parent, child);
			}

			return nodeIdMap;
		}
	}
}
