namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using DomFlowRules = Storage.DOM.SlcResource_Studio.FlowrulesInstance;

	// Flow rules are only written through the object that owns them, so they are persisted under the lock of that object.
	internal sealed class DomFlowRulesHandler : DomInstanceApiObjectValidator<DomFlowRules>
	{
		private readonly MediaOpsPlanApi planApi;

		private DomFlowRulesHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<FlowRules> apiFlowRules, out DomInstanceBulkOperationResult<DomFlowRules> result)
		{
			var handler = new DomFlowRulesHandler(planApi);
			handler.CreateOrUpdate(apiFlowRules ?? throw new ArgumentNullException(nameof(apiFlowRules)));

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

		private void CreateOrUpdate(ICollection<FlowRules> apiFlowRules)
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
