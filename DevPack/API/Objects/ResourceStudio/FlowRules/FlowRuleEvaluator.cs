namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Evaluates flow rules for connections between objects. A connection <c>A → B</c> is evaluated against the
	/// <see cref="FlowRuleDirection.After"/> rules of <c>A</c> and the <see cref="FlowRuleDirection.Before"/> rules of
	/// <c>B</c>; the most restrictive outcome wins. An override takes precedence over the default of its direction, and a
	/// missing rule set or rule results in <see cref="FlowRuleOutcome.Allowed"/>.
	/// </summary>
	internal static class FlowRuleEvaluator
	{
		public static FlowRuleEvaluation Evaluate(FlowRuleObject from, FlowRuleObject to, Func<FlowRuleObject, FlowRules> getFlowRules)
		{
			if (getFlowRules == null)
			{
				throw new ArgumentNullException(nameof(getFlowRules));
			}

			var fromRule = getFlowRules(from)?.GetApplicableRule(FlowRuleDirection.After, to);
			var toRule = getFlowRules(to)?.GetApplicableRule(FlowRuleDirection.Before, from);

			return new FlowRuleEvaluation(from, to, fromRule, toRule);
		}

		public static IReadOnlyCollection<FlowRuleConnectionEvaluation> Evaluate<TNode>(NodeGraph<TNode> nodeGraph, Func<FlowRuleObject, FlowRules> getFlowRules)
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
					|| !TryGetFlowRuleObject(connection.From, out var from)
					|| !TryGetFlowRuleObject(connection.To, out var to))
				{
					continue;
				}

				var evaluation = Evaluate(from, to, getFlowRules);
				result.Add(new FlowRuleConnectionEvaluation(connection.Id, connection.From.Id, connection.To.Id, evaluation));
			}

			return result;
		}

		public static IReadOnlyCollection<FlowRuleObject> GetConnectedObjects<TNode>(NodeGraph<TNode> nodeGraph)
			where TNode : NodeBase
		{
			if (nodeGraph == null)
			{
				throw new ArgumentNullException(nameof(nodeGraph));
			}

			var objects = new HashSet<FlowRuleObject>();
			foreach (var connection in nodeGraph.Connections)
			{
				if (connection?.From != null && TryGetFlowRuleObject(connection.From, out var from))
				{
					objects.Add(from);
				}

				if (connection?.To != null && TryGetFlowRuleObject(connection.To, out var to))
				{
					objects.Add(to);
				}
			}

			return objects;
		}

		// Returns the connections of a stored node graph as (connection ID, from object, to object), so a save can tell which
		// forbidden connections it introduces and which ones were already stored before (for example before a rule change).
		public static ISet<(string ConnectionId, FlowRuleObject From, FlowRuleObject To)> GetStoredConnections(
			IEnumerable<StorageWorkflow.NodesSection> storedNodes,
			IEnumerable<StorageWorkflow.ConnectionsSection> storedConnections)
		{
			var result = new HashSet<(string, FlowRuleObject, FlowRuleObject)>();
			if (storedNodes == null || storedConnections == null)
			{
				return result;
			}

			var objectByNodeId = new Dictionary<string, FlowRuleObject>(StringComparer.Ordinal);
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
					objectByNodeId[node.NodeID] = FlowRuleObject.ResourcePool(poolId);
				}
			}

			foreach (var connection in storedConnections.Where(x => x.ConnectionID != null && x.SourceNodeID != null && x.DestinationNodeID != null))
			{
				if (objectByNodeId.TryGetValue(connection.SourceNodeID, out var from)
					&& objectByNodeId.TryGetValue(connection.DestinationNodeID, out var to))
				{
					result.Add((connection.ConnectionID, from, to));
				}
			}

			return result;
		}

		// Resource nodes and resource pool nodes are evaluated against the flow rules of their resource pool.
		public static bool TryGetFlowRuleObject(NodeBase node, out FlowRuleObject flowRuleObject)
		{
			var resourcePoolId = node switch
			{
				IResourceNode resourceNode => resourceNode.ResourcePoolId,
				IResourcePoolNode resourcePoolNode => resourcePoolNode.ResourcePoolId,
				_ => Guid.Empty,
			};

			flowRuleObject = FlowRuleObject.ResourcePool(resourcePoolId);
			return !flowRuleObject.IsEmpty;
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
