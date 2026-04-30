namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using DomWorkflow = Storage.DOM.SlcWorkflow.WorkflowsInstance;

	internal class DomWorkflowHandler : DomInstanceApiObjectValidator<DomWorkflow>
	{
		private readonly MediaOpsPlanApi planApi;

		private DomWorkflowHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<Workflow> apiWorkflows, out DomInstanceBulkOperationResult<DomWorkflow> result)
		{
			var handler = new DomWorkflowHandler(planApi);
			handler.CreateOrUpdate(apiWorkflows);

			result = new DomInstanceBulkOperationResult<DomWorkflow>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<Workflow> apiWorkflows, out DomInstanceBulkOperationResult<DomWorkflow> result)
		{
			var handler = new DomWorkflowHandler(planApi);
			handler.Delete(apiWorkflows);

			result = new DomInstanceBulkOperationResult<DomWorkflow>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		private void CreateOrUpdate(ICollection<Workflow> apiWorkflows)
		{
			if (apiWorkflows == null)
			{
				throw new ArgumentNullException(nameof(apiWorkflows));
			}

			if (apiWorkflows.Count == 0)
			{
				return;
			}

			var toCreate = apiWorkflows.Where(x => x.IsNew).ToList();

			ValidateIdsNotInUse(toCreate);

			var lockResult = planApi.LockManager.LockAndExecute(apiWorkflows.Where(IsValid).ToList(), CreateOrUpdateLocked);
			ReportError(lockResult);
		}

		private void CreateOrUpdateLocked(ICollection<Workflow> apiWorkflows)
		{
			if (apiWorkflows == null)
			{
				throw new ArgumentNullException(nameof(apiWorkflows));
			}

			if (apiWorkflows.Count == 0)
			{
				return;
			}

			if (apiWorkflows.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided workflows are valid", nameof(apiWorkflows));
			}

			var toCreate = apiWorkflows.Where(x => x.IsNew).ToList();
			var toUpdate = apiWorkflows.Except(toCreate).ToList();

			var changeResults = GetWorkflowsWithChanges(toUpdate);

			CreateOrUpdateOrchestrationSettings(apiWorkflows.Where(IsValid).ToList());

			var toCreateDomInstances = toCreate
				.Where(IsValid)
				.Select(x => x.GetInstanceWithChanges())
				.ToList();

			var toUpdateDomInstances = changeResults
				.Where(IsValid)
				.Select(x => new DomWorkflow(x.Instance))
				.ToList();

			CreateOrUpdateDomWorkflows(toCreateDomInstances.Concat(toUpdateDomInstances).ToList());
		}

		private void CreateOrUpdateDomWorkflows(ICollection<DomWorkflow> domWorkflows)
		{
			if (domWorkflows == null)
			{
				throw new ArgumentNullException(nameof(domWorkflows));
			}

			if (domWorkflows.Count == 0)
			{
				return;
			}

			planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(domWorkflows.Select(x => x.ToInstance()), out var domResult);

			foreach (var id in domResult.UnsuccessfulIds)
			{
				ReportError(id.Id);

				if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					var mediaOpsTraceData = new MediaOpsTraceData();
					mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = traceData.ToString() });

					PassTraceData(id.Id, mediaOpsTraceData);
				}
			}

			ReportSuccess(domResult.SuccessfulItems.Select(x => new DomWorkflow(x)));
		}

		private void CreateOrUpdateOrchestrationSettings(ICollection<Workflow> apiWorkflows)
		{
			if (apiWorkflows == null)
			{
				throw new ArgumentNullException(nameof(apiWorkflows));
			}

			if (apiWorkflows.Count == 0)
			{
				return;
			}

			if (apiWorkflows.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided workflows are valid", nameof(apiWorkflows));
			}

			var workflowIdByOrchestrationSettingsId = apiWorkflows.ToDictionary(x => x.OrchestrationSettings.Id, x => x.Id);

			DomWorkflowOrchestrationSettingsHandler.TryCreateOrUpdate(planApi, apiWorkflows.Select(x => x.OrchestrationSettings).ToList(), out var domResult);

			foreach (var id in domResult.UnsuccessfulIds)
			{
				if (!workflowIdByOrchestrationSettingsId.TryGetValue(id, out var jobId))
				{
					planApi.Logger.Error(this, $"Failed to find workflow ID for orchestration settings ID", [id]);
					continue;
				}

				ReportError(jobId);

				if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					PassTraceData(jobId, traceData);
				}
			}
		}

		private void Delete(ICollection<Workflow> apiWorkflows)
		{
			if (apiWorkflows == null)
			{
				throw new ArgumentNullException(nameof(apiWorkflows));
			}

			if (apiWorkflows.Count == 0)
			{
				return;
			}

			var lockResult = planApi.LockManager.LockAndExecute(apiWorkflows.Where(IsValid).ToList(), DeleteLocked);
			ReportError(lockResult);
		}

		private void DeleteLocked(ICollection<Workflow> apiWorkflows)
		{
			if (apiWorkflows == null)
			{
				throw new ArgumentNullException(nameof(apiWorkflows));
			}

			if (apiWorkflows.Count == 0)
			{
				return;
			}

			if (apiWorkflows.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided workflows are valid", nameof(apiWorkflows));
			}

			DeleteOrchestrationSettings(apiWorkflows);

			var domWorkflowsById = apiWorkflows.ToDictionary(x => x.Id, x => x.OriginalInstance);

			var instancesToDelete = domWorkflowsById.Values.Select(x => x.ToInstance()).ToArray();
			planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances.TryDeleteInBatches(instancesToDelete, out var domResult);

			foreach (var id in domResult.UnsuccessfulIds)
			{
				ReportError(id.Id);

				if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					var mediaOpsTraceData = new MediaOpsTraceData();
					mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = traceData.ToString() });

					PassTraceData(id.Id, mediaOpsTraceData);
				}
			}

			ReportSuccess(instancesToDelete.Where(x => domResult.SuccessfulIds.Contains(x.ID)).Select(x => new DomWorkflow(x)).ToArray());
		}

		private void DeleteOrchestrationSettings(ICollection<Workflow> apiWorkflows)
		{
			if (apiWorkflows == null)
			{
				throw new ArgumentNullException(nameof(apiWorkflows));
			}

			if (apiWorkflows.Count == 0)
			{
				return;
			}

			if (apiWorkflows.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided workflows are valid", nameof(apiWorkflows));
			}

			DomWorkflowOrchestrationSettingsHandler.TryDelete(planApi, apiWorkflows.Select(x => x.OrchestrationSettings).ToList(), out _);
		}

		private void ValidateIdsNotInUse(ICollection<Workflow> apiWorkflows)
		{
			if (apiWorkflows == null)
			{
				throw new ArgumentNullException(nameof(apiWorkflows));
			}

			if (apiWorkflows.Count == 0)
			{
				return;
			}

			var workflowsRequiringValidation = apiWorkflows.Where(x => x.IsNew && x.HasUserDefinedId).ToList();
			if (workflowsRequiringValidation.Count == 0)
			{
				return;
			}

			var workflowsWithDuplicateIds = workflowsRequiringValidation
				.GroupBy(pool => pool.Id)
				.Where(g => g.Count() > 1)
				.SelectMany(x => x)
				.ToList();

			foreach (var workflow in workflowsWithDuplicateIds)
			{
				var error = new WorkflowDuplicateIdError
				{
					ErrorMessage = $"Workflow '{workflow.Name}' has a duplicate ID.",
					Id = workflow.Id,
				};

				ReportError(workflow.Id, error);

				workflowsRequiringValidation.Remove(workflow);
			}

			foreach (var foundInstance in planApi.DomHelpers.SlcWorkflowHelper.GetWorkflowInstances(workflowsRequiringValidation.Select(x => x.Id)))
			{
				planApi.Logger.Information(this, $"ID is already in use by a Workflow instance.", [foundInstance.ID.Id]);

				var error = new WorkflowIdInUseError
				{
					ErrorMessage = "ID is already in use.",
					Id = foundInstance.ID.Id,
				};

				ReportError(foundInstance.ID.Id, error);
			}
		}

		private ICollection<DomChangeResults> GetWorkflowsWithChanges(ICollection<Workflow> apiWorkflows)
		{
			return GetItemsWithChanges<Workflow, DomWorkflow>(
				apiWorkflows,
				w => w.OriginalInstance,
				w => w.GetInstanceWithChanges(),
				ids => planApi.DomHelpers.SlcWorkflowHelper.GetWorkflows(ids),
				w => new WorkflowNotFoundError { ErrorMessage = $"Workflow with ID '{w.Id}' no longer exists.", Id = w.Id },
				(w, msg) => new WorkflowValueAlreadyChangedError { ErrorMessage = msg, Id = w.Id })
				.ToList();
		}
	}
}
