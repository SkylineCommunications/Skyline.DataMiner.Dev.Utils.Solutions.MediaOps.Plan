namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	/// <summary>
	/// Provides read access to the flow rules of resource pools and evaluates connections against them.
	/// </summary>
	internal class FlowRulesRepository : Repository, IFlowRulesRepository
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FlowRulesRepository"/> class.
		/// </summary>
		/// <param name="planApi">The MediaOps Plan API instance.</param>
		public FlowRulesRepository(MediaOpsPlanApi planApi) : base(planApi)
		{
		}

		/// <inheritdoc/>
		public FlowRules Read(Guid resourcePoolId)
		{
			if (resourcePoolId == Guid.Empty)
			{
				throw new ArgumentException("Resource pool ID cannot be empty.", nameof(resourcePoolId));
			}

			return Read([resourcePoolId]).TryGetValue(resourcePoolId, out var flowRules) ? flowRules : null;
		}

		/// <inheritdoc/>
		public IReadOnlyDictionary<Guid, FlowRules> Read(IEnumerable<Guid> resourcePoolIds)
		{
			if (resourcePoolIds == null)
			{
				throw new ArgumentNullException(nameof(resourcePoolIds));
			}

			var ids = resourcePoolIds.Where(x => x != Guid.Empty).Distinct().ToList();
			if (ids.Count == 0)
			{
				return new Dictionary<Guid, FlowRules>();
			}

			// The pointer on the pool identifies its rule set, so the pools are read without their other configuration.
			var flowRulesIdByPoolId = PlanApi.DomHelpers.SlcResourceStudioHelper
				.GetResourcePools(ids)
				.Where(x => x.ConfigurationInfo.FlowRules.HasValue && x.ConfigurationInfo.FlowRules.Value != Guid.Empty)
				.ToDictionary(x => x.ID.Id, x => x.ConfigurationInfo.FlowRules.Value);

			var flowRulesInstancesById = PlanApi.DomHelpers.SlcResourceStudioHelper
				.GetFlowRules(flowRulesIdByPoolId.Values)
				.ToDictionary(x => x.ID.Id);

			var result = new Dictionary<Guid, FlowRules>();
			foreach (var pair in flowRulesIdByPoolId)
			{
				if (flowRulesInstancesById.TryGetValue(pair.Value, out var instance))
				{
					result[pair.Key] = new FlowRules(instance, pair.Key);
				}
			}

			return result;
		}

		/// <inheritdoc/>
		public IEnumerable<FlowRules> Read()
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(StorageResourceStudio.SlcResource_StudioIds.Definitions.Flowrules.Id);

			foreach (var instance in PlanApi.DomHelpers.SlcResourceStudioHelper.GetFlowRules(filter))
			{
				yield return new FlowRules(instance, instance.Flowrulesinfo.OwnerId);
			}
		}

		/// <inheritdoc/>
		public FlowRuleEvaluation Evaluate(Guid fromResourcePoolId, Guid toResourcePoolId)
		{
			return Evaluate([(fromResourcePoolId, toResourcePoolId)]).Single();
		}

		/// <inheritdoc/>
		public IReadOnlyCollection<FlowRuleEvaluation> Evaluate(IEnumerable<(Guid FromResourcePoolId, Guid ToResourcePoolId)> connections)
		{
			if (connections == null)
			{
				throw new ArgumentNullException(nameof(connections));
			}

			var list = connections.ToList();
			if (list.Any(x => x.FromResourcePoolId == Guid.Empty || x.ToResourcePoolId == Guid.Empty))
			{
				throw new ArgumentException("Resource pool IDs cannot be empty.", nameof(connections));
			}

			var flowRulesByPoolId = Read(list.SelectMany(x => new[] { x.FromResourcePoolId, x.ToResourcePoolId }));

			return list
				.Select(x => FlowRuleEvaluator.Evaluate(x.FromResourcePoolId, x.ToResourcePoolId, id => GetOrNull(flowRulesByPoolId, id)))
				.ToList();
		}

		/// <inheritdoc/>
		public IReadOnlyDictionary<Guid, FlowRuleEvaluation> EvaluateCandidates(Guid resourcePoolId, FlowRuleDirection direction, IEnumerable<Guid> candidateResourcePoolIds)
		{
			if (resourcePoolId == Guid.Empty)
			{
				throw new ArgumentException("Resource pool ID cannot be empty.", nameof(resourcePoolId));
			}

			if (candidateResourcePoolIds == null)
			{
				throw new ArgumentNullException(nameof(candidateResourcePoolIds));
			}

			if (!Enum.IsDefined(typeof(FlowRuleDirection), direction))
			{
				throw new ArgumentException($"Value '{direction}' is not a valid flow rule direction.", nameof(direction));
			}

			var candidates = candidateResourcePoolIds.Where(x => x != Guid.Empty).Distinct().ToList();
			var flowRulesByPoolId = Read(candidates.Concat([resourcePoolId]));

			return candidates.ToDictionary(
				candidate => candidate,
				candidate => direction == FlowRuleDirection.After
					? FlowRuleEvaluator.Evaluate(resourcePoolId, candidate, id => GetOrNull(flowRulesByPoolId, id))
					: FlowRuleEvaluator.Evaluate(candidate, resourcePoolId, id => GetOrNull(flowRulesByPoolId, id)));
		}

		/// <inheritdoc/>
		public IReadOnlyCollection<FlowRuleConnectionEvaluation> Evaluate<TNode>(NodeGraph<TNode> nodeGraph) where TNode : NodeBase
		{
			if (nodeGraph == null)
			{
				throw new ArgumentNullException(nameof(nodeGraph));
			}

			var flowRulesByPoolId = Read(FlowRuleEvaluator.GetConnectedResourcePoolIds(nodeGraph));
			return FlowRuleEvaluator.Evaluate(nodeGraph, id => GetOrNull(flowRulesByPoolId, id));
		}

		private static FlowRules GetOrNull(IReadOnlyDictionary<Guid, FlowRules> flowRulesByPoolId, Guid resourcePoolId)
		{
			return flowRulesByPoolId.TryGetValue(resourcePoolId, out var flowRules) ? flowRules : null;
		}
	}
}
