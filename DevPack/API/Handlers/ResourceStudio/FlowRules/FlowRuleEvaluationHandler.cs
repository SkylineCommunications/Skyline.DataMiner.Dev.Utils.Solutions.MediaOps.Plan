namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;

	/// <summary>
	/// Evaluates connections against the stored flow rules: reads the rule sets of all objects involved in one batch and
	/// applies the <see cref="FlowRuleEvaluator"/> to them.
	/// </summary>
	internal class FlowRuleEvaluationHandler
	{
		private readonly MediaOpsPlanApi planApi;

		private FlowRuleEvaluationHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		public static IReadOnlyCollection<FlowRuleEvaluation> Evaluate(MediaOpsPlanApi planApi, IEnumerable<(FlowRuleObjectType FromType, Guid FromId, FlowRuleObjectType ToType, Guid ToId)> connections)
		{
			if (connections == null)
			{
				throw new ArgumentNullException(nameof(connections));
			}

			var handler = new FlowRuleEvaluationHandler(planApi);

			return ActivityHelper.Track(
				nameof(FlowRuleEvaluationHandler),
				nameof(Evaluate),
				act => handler.Evaluate(connections));
		}

		public static IReadOnlyDictionary<Guid, FlowRuleEvaluation> EvaluateCandidates(MediaOpsPlanApi planApi, FlowRuleObjectType type, Guid id, FlowRuleDirection direction, FlowRuleObjectType candidateType, IEnumerable<Guid> candidateIds)
		{
			var existing = ToObject(type, id, nameof(id));
			FlowRuleObject.ValidateType(candidateType, nameof(candidateType));

			if (candidateIds == null)
			{
				throw new ArgumentNullException(nameof(candidateIds));
			}

			if (!Enum.IsDefined(typeof(FlowRuleDirection), direction))
			{
				throw new ArgumentException($"Value '{direction}' is not a valid flow rule direction.", nameof(direction));
			}

			var handler = new FlowRuleEvaluationHandler(planApi);

			return ActivityHelper.Track(
				nameof(FlowRuleEvaluationHandler),
				nameof(EvaluateCandidates),
				act => handler.EvaluateCandidates(existing, direction, candidateType, candidateIds));
		}

		public static IReadOnlyCollection<FlowRuleConnectionEvaluation> Evaluate<TNode>(MediaOpsPlanApi planApi, NodeGraph<TNode> nodeGraph) where TNode : NodeBase
		{
			if (nodeGraph == null)
			{
				throw new ArgumentNullException(nameof(nodeGraph));
			}

			var handler = new FlowRuleEvaluationHandler(planApi);

			return ActivityHelper.Track(
				nameof(FlowRuleEvaluationHandler),
				nameof(Evaluate),
				act => handler.Evaluate(nodeGraph));
		}

		// Looks the rule sets up through their owner (for resource pools: the pointer on the stored pool), in batches.
		public static IReadOnlyDictionary<FlowRuleObject, FlowRules> ReadByOwner(MediaOpsPlanApi planApi, IEnumerable<FlowRuleObject> owners)
		{
			if (owners == null)
			{
				throw new ArgumentNullException(nameof(owners));
			}

			return new FlowRuleEvaluationHandler(planApi).ReadByOwner(owners);
		}

		private static FlowRuleObject ToObject(FlowRuleObjectType type, Guid id, string paramName)
		{
			FlowRuleObject.ValidateType(type, paramName);

			if (id == Guid.Empty)
			{
				throw new ArgumentException("Object ID cannot be empty.", paramName);
			}

			return new FlowRuleObject(type, id);
		}

		private static FlowRules GetOrNull(IReadOnlyDictionary<FlowRuleObject, FlowRules> flowRulesByOwner, FlowRuleObject owner)
		{
			return flowRulesByOwner.TryGetValue(owner, out var flowRules) ? flowRules : null;
		}

		private IReadOnlyCollection<FlowRuleEvaluation> Evaluate(IEnumerable<(FlowRuleObjectType FromType, Guid FromId, FlowRuleObjectType ToType, Guid ToId)> connections)
		{
			var list = connections
				.Select(x => (From: ToObject(x.FromType, x.FromId, nameof(connections)), To: ToObject(x.ToType, x.ToId, nameof(connections))))
				.ToList();

			var flowRulesByOwner = ReadByOwner(list.SelectMany(x => new[] { x.From, x.To }));

			return list
				.Select(x => FlowRuleEvaluator.Evaluate(x.From, x.To, owner => GetOrNull(flowRulesByOwner, owner)))
				.ToList();
		}

		private IReadOnlyDictionary<Guid, FlowRuleEvaluation> EvaluateCandidates(FlowRuleObject existing, FlowRuleDirection direction, FlowRuleObjectType candidateType, IEnumerable<Guid> candidateIds)
		{
			var candidates = candidateIds
				.Where(x => x != Guid.Empty)
				.Distinct()
				.Select(x => new FlowRuleObject(candidateType, x))
				.ToList();
			var flowRulesByOwner = ReadByOwner(candidates.Concat([existing]));

			FlowRules GetFlowRules(FlowRuleObject owner) => GetOrNull(flowRulesByOwner, owner);

			return candidates.ToDictionary(
				candidate => candidate.Id,
				candidate => direction == FlowRuleDirection.After
					? FlowRuleEvaluator.Evaluate(existing, candidate, GetFlowRules)
					: FlowRuleEvaluator.Evaluate(candidate, existing, GetFlowRules));
		}

		private IReadOnlyCollection<FlowRuleConnectionEvaluation> Evaluate<TNode>(NodeGraph<TNode> nodeGraph) where TNode : NodeBase
		{
			var flowRulesByOwner = ReadByOwner(FlowRuleEvaluator.GetConnectedObjects(nodeGraph));
			return FlowRuleEvaluator.Evaluate(nodeGraph, owner => GetOrNull(flowRulesByOwner, owner));
		}

		// Resource pools are resolved through the pointer on the stored pool, exactly like a pool read, so a rule set that is
		// not attached to its pool (for example left behind by a failed save) is never evaluated.
		private IReadOnlyDictionary<FlowRuleObject, FlowRules> ReadByOwner(IEnumerable<FlowRuleObject> owners)
		{
			var result = new Dictionary<FlowRuleObject, FlowRules>();

			foreach (var group in owners.Where(x => !x.IsEmpty).Distinct().GroupBy(x => x.Type))
			{
				switch (group.Key)
				{
					case FlowRuleObjectType.ResourcePool:
						ReadResourcePoolFlowRules(group.Select(x => x.Id).ToList(), result);
						break;

					default:
						throw new NotSupportedException($"Flow rules of owners of type '{group.Key}' are not supported.");
				}
			}

			return result;
		}

		private void ReadResourcePoolFlowRules(ICollection<Guid> poolIds, IDictionary<FlowRuleObject, FlowRules> result)
		{
			var poolIdByFlowRulesId = planApi.DomHelpers.SlcResourceStudioHelper
				.GetResourcePools(poolIds)
				.Where(x => x.ConfigurationInfo.FlowRules.HasValue && x.ConfigurationInfo.FlowRules.Value != Guid.Empty)
				.GroupBy(x => x.ConfigurationInfo.FlowRules.Value)
				.ToDictionary(x => x.Key, x => x.First().ID.Id);

			if (poolIdByFlowRulesId.Count == 0)
			{
				return;
			}

			foreach (var instance in planApi.DomHelpers.SlcResourceStudioHelper.GetFlowRules(poolIdByFlowRulesId.Keys))
			{
				var owner = FlowRuleObject.ResourcePool(poolIdByFlowRulesId[instance.ID.Id]);
				var flowRules = new FlowRules(instance);

				if (flowRules.Owner != owner)
				{
					planApi.Logger.Warning(this, "Flow rules {0} of resource pool {1} are stored with owner {2}.", [flowRules.Id, owner.Id, flowRules.Owner]);
				}

				result[owner] = flowRules;
			}
		}
	}
}
