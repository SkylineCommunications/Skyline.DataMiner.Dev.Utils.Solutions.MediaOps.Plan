namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Evaluates flow rules for connections between resource pools. A connection <c>A → B</c> is evaluated against the
	/// <see cref="FlowRuleDirection.After"/> rules of <c>A</c> and the <see cref="FlowRuleDirection.Before"/> rules of
	/// <c>B</c>; the most restrictive outcome wins. An override takes precedence over the default of its direction, and a
	/// missing rule set or rule results in <see cref="FlowRuleOutcome.Allowed"/>.
	/// </summary>
	internal static class FlowRuleEvaluator
	{
		public static FlowRuleEvaluation Evaluate(Guid fromResourcePoolId, Guid toResourcePoolId, Func<Guid, FlowRules> getFlowRules)
		{
			if (getFlowRules == null)
			{
				throw new ArgumentNullException(nameof(getFlowRules));
			}

			var fromRule = getFlowRules(fromResourcePoolId)?.GetApplicableRule(FlowRuleDirection.After, toResourcePoolId);
			var toRule = getFlowRules(toResourcePoolId)?.GetApplicableRule(FlowRuleDirection.Before, fromResourcePoolId);

			return new FlowRuleEvaluation(fromResourcePoolId, toResourcePoolId, fromRule, toRule);
		}

		public static IReadOnlyCollection<FlowRuleConnectionEvaluation> Evaluate<TNode>(NodeGraph<TNode> nodeGraph, Func<Guid, FlowRules> getFlowRules)
			where TNode : NodeBase
		{
			if (nodeGraph == null)
			{
				throw new ArgumentNullException(nameof(nodeGraph));
			}

			if (getFlowRules == null)
			{
				throw new ArgumentNullException(nameof(getFlowRules));
			}

			var result = new List<FlowRuleConnectionEvaluation>();
			foreach (var connection in nodeGraph.Connections)
			{
				if (connection?.From == null
					|| connection.To == null
					|| !TryGetResourcePoolId(connection.From, out var fromResourcePoolId)
					|| !TryGetResourcePoolId(connection.To, out var toResourcePoolId))
				{
					continue;
				}

				var evaluation = Evaluate(fromResourcePoolId, toResourcePoolId, getFlowRules);
				result.Add(new FlowRuleConnectionEvaluation(connection.Id, connection.From.Id, connection.To.Id, evaluation));
			}

			return result;
		}

		public static IReadOnlyCollection<Guid> GetConnectedResourcePoolIds<TNode>(NodeGraph<TNode> nodeGraph)
			where TNode : NodeBase
		{
			if (nodeGraph == null)
			{
				throw new ArgumentNullException(nameof(nodeGraph));
			}

			var ids = new HashSet<Guid>();
			foreach (var connection in nodeGraph.Connections)
			{
				if (connection?.From != null && TryGetResourcePoolId(connection.From, out var fromResourcePoolId))
				{
					ids.Add(fromResourcePoolId);
				}

				if (connection?.To != null && TryGetResourcePoolId(connection.To, out var toResourcePoolId))
				{
					ids.Add(toResourcePoolId);
				}
			}

			return ids;
		}

		// Returns the connections of a stored node graph as (connection ID, from pool, to pool), so a save can tell which
		// forbidden connections it introduces and which ones were already stored before (for example before a rule change).
		public static ISet<(string ConnectionId, Guid FromResourcePoolId, Guid ToResourcePoolId)> GetStoredPoolConnections(
			IEnumerable<StorageWorkflow.NodesSection> storedNodes,
			IEnumerable<StorageWorkflow.ConnectionsSection> storedConnections)
		{
			var result = new HashSet<(string, Guid, Guid)>();
			if (storedNodes == null || storedConnections == null)
			{
				return result;
			}

			var poolIdByNodeId = new Dictionary<string, Guid>(StringComparer.Ordinal);
			foreach (var node in storedNodes.Where(x => x.NodeID != null))
			{
				var poolId = node.NodeType switch
				{
					StorageWorkflow.SlcWorkflowIds.Enums.Nodetype.ResourcePool => node.ReferenceId,
					StorageWorkflow.SlcWorkflowIds.Enums.Nodetype.Resource => node.ParentReferenceId,
					_ => Guid.Empty,
				};

				if (poolId != Guid.Empty)
				{
					poolIdByNodeId[node.NodeID] = poolId;
				}
			}

			foreach (var connection in storedConnections.Where(x => x.ConnectionID != null && x.SourceNodeID != null && x.DestinationNodeID != null))
			{
				if (poolIdByNodeId.TryGetValue(connection.SourceNodeID, out var fromPoolId)
					&& poolIdByNodeId.TryGetValue(connection.DestinationNodeID, out var toPoolId))
				{
					result.Add((connection.ConnectionID, fromPoolId, toPoolId));
				}
			}

			return result;
		}

		public static bool TryGetResourcePoolId(NodeBase node, out Guid resourcePoolId)
		{
			switch (node)
			{
				case IResourceNode resourceNode:
					resourcePoolId = resourceNode.ResourcePoolId;
					break;

				case IResourcePoolNode resourcePoolNode:
					resourcePoolId = resourcePoolNode.ResourcePoolId;
					break;

				default:
					resourcePoolId = Guid.Empty;
					break;
			}

			return resourcePoolId != Guid.Empty;
		}

		public static string ComposeMessage(FlowRuleEvaluation evaluation, string fromDisplayName, string toDisplayName)
		{
			if (evaluation == null)
			{
				throw new ArgumentNullException(nameof(evaluation));
			}

			var message = $"Connection from {fromDisplayName} to {toDisplayName} is {evaluation.Outcome.ToString().ToLowerInvariant()} by the flow rules.";
			if (evaluation.Messages.Count > 0)
			{
				message += $" {String.Join(" ", evaluation.Messages.Select(x => x.Trim()))}";
			}

			return message;
		}
	}
}
