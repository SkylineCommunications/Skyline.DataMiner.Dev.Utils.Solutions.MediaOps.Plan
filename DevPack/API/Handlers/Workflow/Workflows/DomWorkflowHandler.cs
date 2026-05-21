namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;
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
			var toUpdate = apiWorkflows.Except(toCreate).ToList();

			ValidateIdsNotInUse(toCreate);
			ValidateStateForUpdateAction(toUpdate);

			ValidateNames(apiWorkflows);
			ValidatePreRoll(apiWorkflows);
			ValidatePostRoll(apiWorkflows);

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

			var toUpdateNameValidation = toUpdate.Where(x => changeResults.Any(y => y.Instance.ID.Id == x.Id && y.ChangedFields.Select(z => z.FieldDescriptorId).Contains(SlcWorkflowIds.Sections.WorkflowInfo.WorkflowName.Id)));
			ValidateDomNames(toCreate.Concat(toUpdateNameValidation).ToList());

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

			var workflowIdByOrchestrationSettingsId = new Dictionary<Guid, Guid>();
			var orchestrationSettings = new List<OrchestrationSettings>();

			foreach (var workflow in apiWorkflows)
			{
				workflowIdByOrchestrationSettingsId[workflow.OrchestrationSettings.Id] = workflow.Id;
				orchestrationSettings.Add(workflow.OrchestrationSettings);

				foreach (var node in workflow.NodeGraph.Nodes)
				{
					workflowIdByOrchestrationSettingsId[node.OrchestrationSettings.Id] = workflow.Id;
					orchestrationSettings.Add(node.OrchestrationSettings);
				}
			}

			DomWorkflowOrchestrationSettingsHandler.TryCreateOrUpdate(planApi, orchestrationSettings, out var domResult);

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

		private void ValidateStateForUpdateAction(ICollection<Workflow> apiWorkflows)
		{
			if (apiWorkflows == null)
			{
				throw new ArgumentNullException(nameof(apiWorkflows));
			}

			if (apiWorkflows.Count == 0)
			{
				return;
			}

			foreach (var workflow in apiWorkflows.Where(x => x.State == WorkflowState.Obsolete))
			{
				var error = new WorkflowInvalidStateError
				{
					ErrorMessage = "Not allowed to update a workfow in Obsolete state.",
					Id = workflow.Id,
				};

				ReportError(workflow.Id, error);
			}
		}

		private void ValidateStateForCompleteAction(ICollection<Workflow> apiWorkflows)
		{
			if (apiWorkflows == null)
			{
				throw new ArgumentNullException(nameof(apiWorkflows));
			}

			if (apiWorkflows.Count == 0)
			{
				return;
			}

			foreach (var workflow in apiWorkflows.Where(x => x.State != WorkflowState.Draft))
			{
				var error = new WorkflowInvalidStateError
				{
					ErrorMessage = "Not allowed to complete a workflow that is not in Draft state.",
					Id = workflow.Id,
				};

				ReportError(workflow.Id, error);
			}
		}

		private void ValidateStateForObsoleteAction(ICollection<Workflow> apiWorkflows)
		{
			if (apiWorkflows == null)
			{
				throw new ArgumentNullException(nameof(apiWorkflows));
			}

			if (apiWorkflows.Count == 0)
			{
				return;
			}

			foreach (var workflow in apiWorkflows.Where(x => x.State != WorkflowState.Complete))
			{
				var error = new WorkflowInvalidStateError
				{
					ErrorMessage = "Not allowed to make a workflow obsolete that is not in Complete state.",
					Id = workflow.Id,
				};

				ReportError(workflow.Id, error);
			}
		}

		private void ValidateNames(ICollection<Workflow> apiWorkflows)
		{
			if (apiWorkflows == null)
			{
				throw new ArgumentNullException(nameof(apiWorkflows));
			}

			if (apiWorkflows.Count == 0)
			{
				return;
			}

			var requiringValidation = apiWorkflows.ToList();

			foreach (var workflow in requiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Name)).ToArray())
			{
				var error = new WorkflowInvalidNameError
				{
					ErrorMessage = "Name cannot be empty.",
					Id = workflow.Id,
				};

				ReportError(workflow.Id, error);

				requiringValidation.Remove(workflow);
			}

			foreach (var workflow in requiringValidation.Where(x => !InputValidator.HasValidTextLength(x.Name)).ToArray())
			{
				var error = new WorkflowInvalidNameError
				{
					ErrorMessage = $"Name exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
					Id = workflow.Id,
					Name = workflow.Name,
				};

				ReportError(workflow.Id, error);

				requiringValidation.Remove(workflow);
			}

			var withDuplicateNames = requiringValidation
				.GroupBy(x => x.Name)
				.Where(g => g.Count() > 1)
				.SelectMany(g => g)
				.ToList();

			foreach (var workflow in withDuplicateNames)
			{
				var error = new WorkflowDuplicateNameError
				{
					ErrorMessage = $"Workflow '{workflow.Name}' has a duplicate name.",
					Id = workflow.Id,
					Name = workflow.Name,
				};

				ReportError(workflow.Id, error);
			}
		}

		private void ValidateDomNames(ICollection<Workflow> apiWorkflows)
		{
			if (apiWorkflows == null)
			{
				throw new ArgumentNullException(nameof(apiWorkflows));
			}

			if (apiWorkflows.Count == 0)
			{
				return;
			}

			FilterElement<DomInstance> Filter(string name) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Workflows.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.WorkflowInfo.WorkflowName).Equal(name));

			var domWorkflowsbyName = planApi.DomHelpers.SlcWorkflowHelper.GetWorkflows(apiWorkflows.Select(x => x.Name), Filter)
				.GroupBy(x => x.Name)
				.ToDictionary(x => x.Key, x => (IReadOnlyCollection<DomWorkflow>)x.ToList());

			foreach (var workflow in apiWorkflows)
			{
				if (!domWorkflowsbyName.TryGetValue(workflow.Name, out var domResources))
				{
					continue;
				}

				var existing = domResources.Where(x => x.ID.Id != workflow.Id).ToList();
				if (existing.Count == 0)
				{
					continue;
				}

				planApi.Logger.Information(this, $"Name '{workflow.Name}' is already in use by DOM workflow(s) with ID(s)", [existing.Select(x => x.ID.Id).ToArray()]);

				var error = new WorkflowNameExistsError
				{
					ErrorMessage = "Name is already in use.",
					Id = workflow.Id,
					Name = workflow.Name,
				};

				ReportError(workflow.Id, error);
			}
		}

		private void ValidatePreRoll(ICollection<Workflow> apiWorkflows)
		{
			if (apiWorkflows == null)
			{
				throw new ArgumentNullException(nameof(apiWorkflows));
			}

			if (apiWorkflows.Count == 0)
			{
				return;
			}

			foreach (var workflow in apiWorkflows.Where(x => x.PreRoll < TimeSpan.Zero))
			{
				var error = new WorkflowInvalidPreRollError
				{
					ErrorMessage = "Pre-roll cannot be negative.",
					Id = workflow.Id,
					PreRoll = workflow.PreRoll,
				};

				ReportError(workflow.Id, error);
			}
		}

		private void ValidatePostRoll(ICollection<Workflow> apiWorkflows)
		{
			if (apiWorkflows == null)
			{
				throw new ArgumentNullException(nameof(apiWorkflows));
			}

			if (apiWorkflows.Count == 0)
			{
				return;
			}

			foreach (var workflow in apiWorkflows.Where(x => x.PostRoll < TimeSpan.Zero))
			{
				var error = new WorkflowInvalidPostRollError
				{
					ErrorMessage = "Post-roll cannot be negative.",
					Id = workflow.Id,
					PostRoll = workflow.PostRoll,
				};

				ReportError(workflow.Id, error);
			}
		}

		private void ValidateNodeGraph(ICollection<Workflow> apiWorkflows)
		{
			if (apiWorkflows == null)
			{
				throw new ArgumentNullException(nameof(apiWorkflows));
			}

			if (apiWorkflows.Count == 0)
			{
				return;
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
