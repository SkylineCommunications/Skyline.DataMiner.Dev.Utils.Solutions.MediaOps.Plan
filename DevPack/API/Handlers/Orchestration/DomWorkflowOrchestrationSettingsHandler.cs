namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using DomWorkflowOrchestrationSetting = Storage.DOM.SlcWorkflow.ConfigurationInstance;

	internal sealed class DomWorkflowOrchestrationSettingsHandler : DomOrchestrationSettingsHandler<WorkflowOrchestrationSettings, DomWorkflowOrchestrationSetting>
	{
		private DomWorkflowOrchestrationSettingsHandler(MediaOpsPlanApi planApi) : base(planApi)
		{
		}

		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<OrchestrationSettings> apiOrchestrationSettings, out DomInstanceBulkOperationResult<DomWorkflowOrchestrationSetting> result)
		{
			var handler = new DomWorkflowOrchestrationSettingsHandler(planApi);
			handler.CreateOrUpdate(apiOrchestrationSettings.OfType<WorkflowOrchestrationSettings>().ToList());

			result = new DomInstanceBulkOperationResult<DomWorkflowOrchestrationSetting>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<OrchestrationSettings> apiOrchestrationSettings, out DomInstanceBulkOperationResult<DomWorkflowOrchestrationSetting> result)
		{
			var handler = new DomWorkflowOrchestrationSettingsHandler(planApi);
			handler.Delete(apiOrchestrationSettings.OfType<WorkflowOrchestrationSettings>().ToList());

			result = new DomInstanceBulkOperationResult<DomWorkflowOrchestrationSetting>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		protected override void CreateOrUpdateDomInstances(ICollection<WorkflowOrchestrationSettings> apiOrchestrationSettings)
		{
			if (apiOrchestrationSettings.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided orchestration settings are valid", nameof(apiOrchestrationSettings));
			}

			var toCreate = apiOrchestrationSettings.Where(x => x.IsNew).ToList();
			var toUpdate = apiOrchestrationSettings.Except(toCreate).ToList();

			var changeResults = GetSettingsWithChanges(toUpdate);

			var toCreateDomInstances = toCreate
				.Where(IsValid)
				.Select(x => x.GetInstanceWithChanges())
				.ToList();

			var toUpdateDomInstances = changeResults
				.Where(IsValid)
				.Select(x => new DomWorkflowOrchestrationSetting(x.Instance))
				.ToList();

			PersistDomInstances(toCreateDomInstances.Concat(toUpdateDomInstances).ToList());
		}

		protected override void DeleteDomInstances(ICollection<WorkflowOrchestrationSettings> apiOrchestrationSettings)
		{
			if (apiOrchestrationSettings.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided orchestration settings are valid", nameof(apiOrchestrationSettings));
			}

			DeleteDomWorkflowInstances(apiOrchestrationSettings.Select(x => x.OriginalInstance).ToList());
		}

		private ICollection<DomChangeResults> GetSettingsWithChanges(ICollection<WorkflowOrchestrationSettings> apiOrchestrationSettings)
		{
			var changeResults = new List<DomChangeResults>();
			if (apiOrchestrationSettings.Count == 0)
			{
				return changeResults;
			}

			var toValidate = apiOrchestrationSettings.Where(x => !x.IsNew && x.HasChanges).ToList();
			if (toValidate.Count == 0)
			{
				return changeResults;
			}

			var storedById = planApi.DomHelpers.SlcWorkflowHelper.GetConfigurations(toValidate.Select(x => x.Id)).ToDictionary(x => x.ID.Id);
			foreach (var orchestrationSetting in toValidate)
			{
				if (!storedById.TryGetValue(orchestrationSetting.Id, out var stored))
				{
					var error = new OrchestrationSettingsNotFoundError
					{
						ErrorMessage = $"Workflow orchestration setting with ID '{orchestrationSetting.Id}' no longer exists.",
						Id = orchestrationSetting.Id,
					};

					ReportError(orchestrationSetting.Id, error);
					continue;
				}

				var changeResult = DomChangeHandler.HandleChanges(orchestrationSetting.OriginalInstance, orchestrationSetting.GetInstanceWithChanges(), stored);
				if (changeResult.HasErrors)
				{
					foreach (var errorDetails in changeResult.Errors)
					{
						var error = new OrchestrationSettingsValueAlreadyChangedError
						{
							ErrorMessage = errorDetails.Message,
							Id = orchestrationSetting.Id,
						};

						ReportError(orchestrationSetting.Id, error);
					}

					continue;
				}

				changeResults.Add(changeResult);
			}

			return changeResults;
		}

		private void PersistDomInstances(ICollection<DomWorkflowOrchestrationSetting> domInstances)
		{
			if (domInstances.Count == 0)
			{
				return;
			}

			var instancesToCreateOrUpdate = domInstances.Select(x => x.ToInstance()).ToList();
			planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(instancesToCreateOrUpdate, out var domResult);

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

			ReportSuccess(domResult.SuccessfulItems.Select(x => new DomWorkflowOrchestrationSetting(x)));
		}

		private void DeleteDomWorkflowInstances(ICollection<DomWorkflowOrchestrationSetting> domInstances)
		{
			if (domInstances.Count == 0)
			{
				return;
			}

			var instancesToDelete = domInstances.Select(x => x.ToInstance()).ToList();
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

			ReportSuccess(instancesToDelete.Where(x => domResult.SuccessfulIds.Contains(x.ID)).Select(x => new DomWorkflowOrchestrationSetting(x)).ToArray());
		}
	}
}
