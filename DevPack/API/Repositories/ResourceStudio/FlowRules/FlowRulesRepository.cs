namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	/// <summary>
	/// Provides read access to flow rules and evaluates connections against them.
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
		public FlowRules Read(FlowRuleObjectType ownerType, Guid ownerId)
		{
			if (ownerId == Guid.Empty)
			{
				throw new ArgumentException("Owner ID cannot be empty.", nameof(ownerId));
			}

			return Read(ownerType, [ownerId]).TryGetValue(ownerId, out var flowRules) ? flowRules : null;
		}

		/// <inheritdoc/>
		public IReadOnlyDictionary<Guid, FlowRules> Read(FlowRuleObjectType ownerType, IEnumerable<Guid> ownerIds)
		{
			FlowRuleObject.ValidateType(ownerType, nameof(ownerType));

			if (ownerIds == null)
			{
				throw new ArgumentNullException(nameof(ownerIds));
			}

			return Read(ownerIds.Select(x => new FlowRuleObject(ownerType, x)))
				.ToDictionary(x => x.Key.Id, x => x.Value);
		}

		/// <inheritdoc/>
		public IEnumerable<FlowRules> Read()
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(StorageResourceStudio.SlcResource_StudioIds.Definitions.Flowrules.Id);

			foreach (var instance in PlanApi.DomHelpers.SlcResourceStudioHelper.GetFlowRules(filter))
			{
				var owner = new FlowRuleObject(FlowRuleObject.FromStorage(instance.Flowrulesinfo.Ownertype), instance.Flowrulesinfo.OwnerId);
				yield return new FlowRules(instance, owner);
			}
		}

		/// <inheritdoc/>
		public FlowRuleEvaluation Evaluate(FlowRuleObjectType fromType, Guid fromId, FlowRuleObjectType toType, Guid toId)
		{
			return Evaluate([(fromType, fromId, toType, toId)]).Single();
		}

		/// <inheritdoc/>
		public IReadOnlyCollection<FlowRuleEvaluation> Evaluate(IEnumerable<(FlowRuleObjectType FromType, Guid FromId, FlowRuleObjectType ToType, Guid ToId)> connections)
		{
			if (connections == null)
			{
				throw new ArgumentNullException(nameof(connections));
			}

			var list = connections
				.Select(x => (From: ToObject(x.FromType, x.FromId, nameof(connections)), To: ToObject(x.ToType, x.ToId, nameof(connections))))
				.ToList();

			var flowRulesByOwner = Read(list.SelectMany(x => new[] { x.From, x.To }));

			return list
				.Select(x => FlowRuleEvaluator.Evaluate(x.From, x.To, owner => GetOrNull(flowRulesByOwner, owner)))
				.ToList();
		}

		/// <inheritdoc/>
		public IReadOnlyDictionary<Guid, FlowRuleEvaluation> EvaluateCandidates(FlowRuleObjectType type, Guid id, FlowRuleDirection direction, FlowRuleObjectType candidateType, IEnumerable<Guid> candidateIds)
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

			var candidates = candidateIds
				.Where(x => x != Guid.Empty)
				.Distinct()
				.Select(x => new FlowRuleObject(candidateType, x))
				.ToList();
			var flowRulesByOwner = Read(candidates.Concat([existing]));

			FlowRules GetFlowRules(FlowRuleObject owner) => GetOrNull(flowRulesByOwner, owner);

			return candidates.ToDictionary(
				candidate => candidate.Id,
				candidate => direction == FlowRuleDirection.After
					? FlowRuleEvaluator.Evaluate(existing, candidate, GetFlowRules)
					: FlowRuleEvaluator.Evaluate(candidate, existing, GetFlowRules));
		}

		/// <inheritdoc/>
		public IReadOnlyCollection<FlowRuleConnectionEvaluation> Evaluate<TNode>(NodeGraph<TNode> nodeGraph) where TNode : NodeBase
		{
			if (nodeGraph == null)
			{
				throw new ArgumentNullException(nameof(nodeGraph));
			}

			var flowRulesByOwner = Read(FlowRuleEvaluator.GetConnectedObjects(nodeGraph));
			return FlowRuleEvaluator.Evaluate(nodeGraph, owner => GetOrNull(flowRulesByOwner, owner));
		}

		internal IReadOnlyDictionary<FlowRuleObject, FlowRules> Read(IEnumerable<FlowRuleObject> owners)
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
						throw new NotSupportedException($"Flow rules of type '{group.Key}' are not supported.");
				}
			}

			return result;
		}

		// The pointer on the pool identifies its rule set, so the pools are read without their other configuration.
		private void ReadResourcePoolFlowRules(ICollection<Guid> resourcePoolIds, IDictionary<FlowRuleObject, FlowRules> result)
		{
			var flowRulesIdByPoolId = PlanApi.DomHelpers.SlcResourceStudioHelper
				.GetResourcePools(resourcePoolIds)
				.Where(x => x.ConfigurationInfo.FlowRules.HasValue && x.ConfigurationInfo.FlowRules.Value != Guid.Empty)
				.ToDictionary(x => x.ID.Id, x => x.ConfigurationInfo.FlowRules.Value);

			var flowRulesInstancesById = PlanApi.DomHelpers.SlcResourceStudioHelper
				.GetFlowRules(flowRulesIdByPoolId.Values)
				.ToDictionary(x => x.ID.Id);

			foreach (var pair in flowRulesIdByPoolId)
			{
				if (flowRulesInstancesById.TryGetValue(pair.Value, out var instance))
				{
					var owner = FlowRuleObject.ResourcePool(pair.Key);
					result[owner] = new FlowRules(instance, owner);
				}
			}
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
	}
}
