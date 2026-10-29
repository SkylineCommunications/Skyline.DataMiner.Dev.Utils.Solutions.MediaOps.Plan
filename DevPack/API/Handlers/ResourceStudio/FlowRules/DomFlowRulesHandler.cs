namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using DomFlowRules = Storage.DOM.SlcResource_Studio.FlowrulesInstance;

	// Flow rules are only written through the object that owns them, so they are persisted under the lock of that object.
	// The owner validates them first (TryValidate) and persists them once its own validation passed (TryPersist).
	internal sealed class DomFlowRulesHandler : DomInstanceApiObjectValidator<DomFlowRules>
	{
		private readonly MediaOpsPlanApi planApi;

		private readonly FlowRulesValidationContext validationContext;

		private DomFlowRulesHandler(MediaOpsPlanApi planApi, FlowRulesValidationContext validationContext = null)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
			this.validationContext = validationContext ?? FlowRulesValidationContext.Empty;
		}

		internal static bool TryValidate(MediaOpsPlanApi planApi, ICollection<FlowRules> apiFlowRules, FlowRulesValidationContext validationContext, out DomInstanceBulkOperationResult<DomFlowRules> result)
		{
			var handler = new DomFlowRulesHandler(planApi, validationContext);
			handler.Validate(apiFlowRules ?? throw new ArgumentNullException(nameof(apiFlowRules)));

			result = new DomInstanceBulkOperationResult<DomFlowRules>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryPersist(MediaOpsPlanApi planApi, ICollection<FlowRules> apiFlowRules, out DomInstanceBulkOperationResult<DomFlowRules> result)
		{
			var handler = new DomFlowRulesHandler(planApi);
			handler.Persist(apiFlowRules ?? throw new ArgumentNullException(nameof(apiFlowRules)));

			result = new DomInstanceBulkOperationResult<DomFlowRules>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<FlowRules> apiFlowRules, out DomInstanceBulkOperationResult<DomFlowRules> result)
		{
			var handler = new DomFlowRulesHandler(planApi);
			handler.Delete(apiFlowRules ?? throw new ArgumentNullException(nameof(apiFlowRules)));

			result = new DomInstanceBulkOperationResult<DomFlowRules>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		private void Validate(ICollection<FlowRules> apiFlowRules)
		{
			if (apiFlowRules.Count == 0)
			{
				return;
			}

			ValidateDuplicates(apiFlowRules);
			ValidateMessages(apiFlowRules);
			ValidateTargets(apiFlowRules);
		}

		private void ValidateDuplicates(ICollection<FlowRules> apiFlowRules)
		{
			foreach (var flowRules in apiFlowRules)
			{
				foreach (var duplicate in flowRules.Rules.GroupBy(x => (x.Direction, x.Target)).Where(x => x.Count() > 1))
				{
					var target = duplicate.Key.Target.HasValue ? $"target {duplicate.Key.Target.Value.Type} '{duplicate.Key.Target.Value.Id}'" : "the default";
					ReportError(flowRules.Id, CreateRuleError<FlowRulesDuplicateRuleError>(
						flowRules,
						duplicate.Key.Direction,
						duplicate.Key.Target,
						$"Flow rule '{duplicate.Key.Direction}' for {target} is defined {duplicate.Count()} times."));
				}
			}
		}

		private void ValidateMessages(ICollection<FlowRules> apiFlowRules)
		{
			foreach (var flowRules in apiFlowRules)
			{
				foreach (var rule in flowRules.Rules.Where(x => !String.IsNullOrEmpty(x.Message) && !InputValidator.HasValidTextLength(x.Message)))
				{
					ReportError(flowRules.Id, CreateRuleError<FlowRulesInvalidMessageError>(
						flowRules,
						rule.Direction,
						rule.Target,
						$"Flow rule message exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters."));
				}
			}
		}

		// Only overrides that were not stored before are checked against their target, so an existing override that targets
		// an object that got deprecated afterwards does not block unrelated updates. The direction and target of a stored
		// rule cannot change, so a stored override is never new.
		private void ValidateTargets(ICollection<FlowRules> apiFlowRules)
		{
			var newOverrides = apiFlowRules
				.SelectMany(flowRules => flowRules.Rules
					.Where(rule => !rule.IsDefault && rule.IsNew && rule.Target.Value != flowRules.Owner)
					.Select(rule => (FlowRules: flowRules, Rule: rule)))
				.ToList();

			if (newOverrides.Count == 0)
			{
				return;
			}

			var storedTargets = ReadStoredTargets(newOverrides
				.Select(x => x.Rule.Target.Value)
				.Where(x => !validationContext.TryGet(x, out _))
				.Distinct()
				.ToList());

			foreach (var (flowRules, rule) in newOverrides)
			{
				var target = rule.Target.Value;

				if (!validationContext.TryGet(target, out var isDeprecated) && !storedTargets.TryGetValue(target, out isDeprecated))
				{
					ReportError(flowRules.Id, CreateRuleError<FlowRulesTargetNotFoundError>(
						flowRules,
						rule.Direction,
						target,
						$"Flow rule target {target.Type} with ID '{target.Id}' does not exist."));
				}
				else if (isDeprecated)
				{
					ReportError(flowRules.Id, CreateRuleError<FlowRulesTargetInvalidStateError>(
						flowRules,
						rule.Direction,
						target,
						$"Flow rule target {target.Type} with ID '{target.Id}' is deprecated."));
				}
			}
		}

		// Returns whether each stored target is deprecated; targets that do not exist are not included.
		private IReadOnlyDictionary<FlowRuleObject, bool> ReadStoredTargets(ICollection<FlowRuleObject> targets)
		{
			var result = new Dictionary<FlowRuleObject, bool>();

			foreach (var group in targets.GroupBy(x => x.Type))
			{
				switch (group.Key)
				{
					case FlowRuleObjectType.ResourcePool:
						foreach (var pool in planApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(group.Select(x => x.Id).ToList()))
						{
							result[FlowRuleObject.ResourcePool(pool.ID.Id)] = pool.Status == SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.StatusesEnum.Deprecated;
						}

						break;

					default:
						throw new NotSupportedException($"Flow rule targets of type '{group.Key}' are not supported.");
				}
			}

			return result;
		}

		private static T CreateRuleError<T>(FlowRules flowRules, FlowRuleDirection direction, FlowRuleObject? target, string message)
			where T : FlowRulesInvalidRuleError, new()
		{
			return new T
			{
				Id = flowRules.Id,
				OwnerType = flowRules.OwnerType,
				OwnerId = flowRules.OwnerId,
				Direction = direction,
				TargetType = target?.Type,
				TargetId = target?.Id,
				ErrorMessage = message,
			};
		}

		private void Persist(ICollection<FlowRules> apiFlowRules)
		{
			if (apiFlowRules.Count == 0)
			{
				return;
			}

			var toCreate = apiFlowRules.Where(x => x.IsNew).ToList();
			var toUpdate = apiFlowRules.Except(toCreate).ToList();

			var changeResults = GetItemsWithChanges<FlowRules, DomFlowRules>(
				toUpdate,
				x => x.OriginalInstance,
				x => x.GetInstanceWithChanges(),
				ids => planApi.DomHelpers.SlcResourceStudioHelper.GetFlowRules(ids),
				x => new FlowRulesNotFoundError { ErrorMessage = $"Flow rules with ID '{x.Id}' of {x.OwnerType} '{x.OwnerId}' no longer exist.", Id = x.Id, OwnerType = x.OwnerType, OwnerId = x.OwnerId },
				(x, msg) => new FlowRulesValueAlreadyChangedError { ErrorMessage = msg, Id = x.Id, OwnerType = x.OwnerType, OwnerId = x.OwnerId })
				.ToList();

			var instances = toCreate
				.Select(x => x.GetInstanceWithChanges())
				.Concat(changeResults.Where(IsValid).Select(x => new DomFlowRules(x.Instance)))
				.Select(x => x.ToInstance())
				.ToList();

			if (instances.Count == 0)
			{
				return;
			}

			planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(instances, out var domResult);

			foreach (var id in domResult.UnsuccessfulIds)
			{
				ReportError(id.Id);

				if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					var mediaOpsTraceData = new MediaOpsTraceData();
					mediaOpsTraceData.Add(new MediaOpsErrorData { ErrorMessage = traceData.ToString() });

					PassTraceData(id.Id, mediaOpsTraceData);
				}
			}

			ReportSuccess(domResult.SuccessfulItems.Select(x => new DomFlowRules(x)));
		}

		private void Delete(ICollection<FlowRules> apiFlowRules)
		{
			var instances = apiFlowRules
				.Where(x => !x.IsNew && x.OriginalInstance != null)
				.Select(x => x.OriginalInstance.ToInstance())
				.ToList();

			if (instances.Count == 0)
			{
				return;
			}

			planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryDeleteInBatches(instances, out var domResult);

			foreach (var id in domResult.UnsuccessfulIds)
			{
				ReportError(id.Id);

				if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					var mediaOpsTraceData = new MediaOpsTraceData();
					mediaOpsTraceData.Add(new MediaOpsErrorData { ErrorMessage = traceData.ToString() });

					PassTraceData(id.Id, mediaOpsTraceData);
				}
			}

			ReportSuccess(instances.Where(x => domResult.SuccessfulIds.Contains(x.ID)).Select(x => new DomFlowRules(x)).ToList());
		}
	}
}
