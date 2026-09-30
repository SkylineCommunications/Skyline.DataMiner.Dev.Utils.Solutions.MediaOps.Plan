namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using DomWorkflowOrchestrationSetting = Storage.DOM.SlcWorkflow.ConfigurationInstance;

	internal sealed class DomWorkflowOrchestrationSettingsHandler : DomOrchestrationSettingsHandler<WorkflowOrchestrationSettings, DomWorkflowOrchestrationSetting>
	{
		private DomWorkflowOrchestrationSettingsHandler(MediaOpsPlanApi planApi, OrchestrationReferenceValidationContext referenceValidationContext = null)
			: base(planApi, referenceValidationContext)
		{
		}

		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<OrchestrationSettings> apiOrchestrationSettings, out DomInstanceBulkOperationResult<DomWorkflowOrchestrationSetting> result)
		{
			return TryCreateOrUpdate(planApi, apiOrchestrationSettings, null, out result);
		}

		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<OrchestrationSettings> apiOrchestrationSettings, OrchestrationReferenceValidationContext referenceValidationContext, out DomInstanceBulkOperationResult<DomWorkflowOrchestrationSetting> result)
		{
			var handler = new DomWorkflowOrchestrationSettingsHandler(planApi, referenceValidationContext);
			handler.CreateOrUpdate(apiOrchestrationSettings.OfType<WorkflowOrchestrationSettings>().ToList());

			result = new DomInstanceBulkOperationResult<DomWorkflowOrchestrationSetting>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		/// <summary>
		/// Validates the orchestration settings without persisting them.
		/// </summary>
		/// <param name="planApi">The API used to read the referenced data.</param>
		/// <param name="apiOrchestrationSettings">The orchestration settings to validate.</param>
		/// <param name="referenceValidationContext">The context describing how the orchestration event references are resolved and reported.</param>
		/// <param name="result">The validation result.</param>
		/// <returns><see langword="true"/> when every settings instance is valid; otherwise, <see langword="false"/>.</returns>
		internal static bool TryValidate(MediaOpsPlanApi planApi, ICollection<OrchestrationSettings> apiOrchestrationSettings, OrchestrationReferenceValidationContext referenceValidationContext, out DomInstanceBulkOperationResult<DomWorkflowOrchestrationSetting> result)
		{
			var handler = new DomWorkflowOrchestrationSettingsHandler(planApi, referenceValidationContext);
			handler.Validate(apiOrchestrationSettings.OfType<WorkflowOrchestrationSettings>().ToList());

			result = new DomInstanceBulkOperationResult<DomWorkflowOrchestrationSetting>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		/// <summary>
		/// Persists orchestration settings that were already validated by <see cref="TryValidate"/>, treating every unit as
		/// all-or-nothing: the settings of a unit are either all persisted or none of them is.
		/// </summary>
		/// <param name="planApi">The API used to persist the settings.</param>
		/// <param name="units">The orchestration settings to persist, grouped per unit.</param>
		/// <param name="result">The result of the persistence.</param>
		/// <returns><see langword="true"/> when every settings instance was persisted; otherwise, <see langword="false"/>.</returns>
		internal static bool TryPersist(MediaOpsPlanApi planApi, ICollection<ICollection<OrchestrationSettings>> units, out DomInstanceBulkOperationResult<DomWorkflowOrchestrationSetting> result)
		{
			var handler = new DomWorkflowOrchestrationSettingsHandler(planApi);
			handler.PersistUnits(units
				.Select(x => (ICollection<WorkflowOrchestrationSettings>)x.OfType<WorkflowOrchestrationSettings>().ToList())
				.ToList());

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

		private void PersistUnits(ICollection<ICollection<WorkflowOrchestrationSettings>> units)
		{
			var unitBySettingsId = new Dictionary<Guid, ICollection<WorkflowOrchestrationSettings>>();
			foreach (var unit in units)
			{
				foreach (var settings in unit)
				{
					unitBySettingsId[settings.Id] = unit;
				}
			}

			var lockResult = planApi.LockManager.LockGroupsAndExecute(units, settings => PersistLockedUnits(settings, unitBySettingsId));
			ReportError(lockResult);
		}

		private void PersistLockedUnits(ICollection<WorkflowOrchestrationSettings> apiOrchestrationSettings, IReadOnlyDictionary<Guid, ICollection<WorkflowOrchestrationSettings>> unitBySettingsId)
		{
			var toCreate = apiOrchestrationSettings.Where(x => x.IsNew).ToList();
			var toUpdate = apiOrchestrationSettings.Except(toCreate).ToList();

			var storedBeforeChanges = new Dictionary<Guid, DomWorkflowOrchestrationSetting>();
			var changeResults = GetSettingsWithChanges(toUpdate, storedBeforeChanges);

			// A conflict on one member skips its whole unit before anything of that unit is written.
			var failedUnits = new HashSet<ICollection<WorkflowOrchestrationSettings>>(apiOrchestrationSettings
				.Where(x => !IsValid(x))
				.Select(x => unitBySettingsId[x.Id]));

			var domInstances = toCreate
				.Select(x => x.GetInstanceWithChanges())
				.Concat(changeResults.Select(x => new DomWorkflowOrchestrationSetting(x.Instance)))
				.Where(x => !failedUnits.Contains(unitBySettingsId[x.ID.Id]))
				.Select(x => x.ToInstance())
				.ToList();

			var writtenInstances = new List<DomInstance>();
			if (domInstances.Count > 0)
			{
				planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(domInstances, out var domResult);

				foreach (var id in domResult.UnsuccessfulIds)
				{
					failedUnits.Add(unitBySettingsId[id.Id]);

					var mediaOpsTraceData = new MediaOpsTraceData();
					mediaOpsTraceData.Add(new MediaOpsErrorData
					{
						ErrorMessage = domResult.TraceDataPerItem.TryGetValue(id, out var traceData) ? traceData.ToString() : $"Failed to save orchestration settings '{id.Id}'.",
					});

					ReportError(id.Id);
					PassTraceData(id.Id, mediaOpsTraceData);
				}

				writtenInstances.AddRange(domResult.SuccessfulItems);
			}

			RollBack(writtenInstances.Where(x => failedUnits.Contains(unitBySettingsId[x.ID.Id])).ToList(), storedBeforeChanges);

			foreach (var settings in failedUnits.SelectMany(x => x).Where(x => IsValid(x) && !SuccessfulIds.Contains(x.Id)))
			{
				ReportError(settings.Id, new MediaOpsErrorData
				{
					ErrorMessage = $"Orchestration settings '{settings.Id}' were not saved because other orchestration settings that are saved together with them failed.",
				});
			}

			ReportSuccess(writtenInstances
				.Where(x => !failedUnits.Contains(unitBySettingsId[x.ID.Id]))
				.Select(x => new DomWorkflowOrchestrationSetting(x)));
		}

		// Best effort: restores updated instances to their stored state and removes created ones.
		private void RollBack(ICollection<DomInstance> writtenInstances, IReadOnlyDictionary<Guid, DomWorkflowOrchestrationSetting> storedBeforeChanges)
		{
			if (writtenInstances.Count == 0)
			{
				return;
			}

			var toRestore = writtenInstances
				.Where(x => storedBeforeChanges.ContainsKey(x.ID.Id))
				.Select(x => storedBeforeChanges[x.ID.Id].ToInstance())
				.ToList();
			var toDelete = writtenInstances
				.Where(x => !storedBeforeChanges.ContainsKey(x.ID.Id))
				.ToList();

			var domInstances = planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances;

			if (toRestore.Count > 0)
			{
				domInstances.TryCreateOrUpdateInBatches(toRestore, out var restoreResult);
				foreach (var id in restoreResult.UnsuccessfulIds)
				{
					planApi.Logger.Error(this, "Failed to roll back orchestration settings", [id.Id]);
				}
			}

			if (toDelete.Count > 0)
			{
				domInstances.TryDeleteInBatches(toDelete, out var deleteResult);
				foreach (var id in deleteResult.UnsuccessfulIds)
				{
					planApi.Logger.Error(this, "Failed to roll back orchestration settings", [id.Id]);
				}
			}
		}

		private ICollection<DomChangeResults> GetSettingsWithChanges(ICollection<WorkflowOrchestrationSettings> apiOrchestrationSettings, IDictionary<Guid, DomWorkflowOrchestrationSetting> storedBeforeChanges = null)
		{
			return GetItemsWithChanges<WorkflowOrchestrationSettings, DomWorkflowOrchestrationSetting>(
				apiOrchestrationSettings,
				p => p.OriginalInstance,
				p => p.GetInstanceWithChanges(),
				ids => ReadStoredSettings(ids, storedBeforeChanges),
				p => new OrchestrationSettingsNotFoundError { ErrorMessage = $"Workflow orchestration setting with ID '{p.Id}' no longer exists.", Id = p.Id },
				(p, msg) => new OrchestrationSettingsValueAlreadyChangedError { ErrorMessage = msg, Id = p.Id })
				.ToList();
		}

		// The change handling merges into the stored instances, so a copy is kept when a rollback may be needed.
		private List<DomWorkflowOrchestrationSetting> ReadStoredSettings(IEnumerable<Guid> ids, IDictionary<Guid, DomWorkflowOrchestrationSetting> storedBeforeChanges)
		{
			var stored = planApi.DomHelpers.SlcWorkflowHelper.GetConfigurations(ids).ToList();

			if (storedBeforeChanges != null)
			{
				foreach (var instance in stored)
				{
					storedBeforeChanges[instance.ID.Id] = instance.Clone();
				}
			}

			return stored;
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
