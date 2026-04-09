namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using DomResourceStudioOrchestrationSetting = Storage.DOM.SlcResource_Studio.ConfigurationInstance;
	using DomWorkflowOrchestrationSetting = Storage.DOM.SlcWorkflow.ConfigurationInstance;

	internal abstract class DomOrchestrationSettingsHandler<TApiSettings, TDomSetting> : DomInstanceApiObjectValidator<TDomSetting>
		where TApiSettings : OrchestrationSettings
		where TDomSetting : DomInstanceBase
	{
		protected readonly MediaOpsPlanApi planApi;

		protected DomOrchestrationSettingsHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		protected void CreateOrUpdate(ICollection<TApiSettings> apiOrchestrationSettings)
		{
			if (apiOrchestrationSettings == null)
			{
				throw new ArgumentNullException(nameof(apiOrchestrationSettings));
			}

			if (apiOrchestrationSettings.Count == 0)
			{
				return;
			}

			ValidateCapacities(apiOrchestrationSettings);
			ValidateCapabilities(apiOrchestrationSettings);
			ValidateConfigurations(apiOrchestrationSettings);

			var lockResult = planApi.LockManager.LockAndExecute(apiOrchestrationSettings.Where(IsValid).ToList(), CreateOrUpdateDomInstances);
			ReportError(lockResult);
		}

		protected void Delete(ICollection<TApiSettings> apiOrchestrationSettings)
		{
			if (apiOrchestrationSettings == null)
			{
				throw new ArgumentNullException(nameof(apiOrchestrationSettings));
			}

			if (apiOrchestrationSettings.Count == 0)
			{
				return;
			}

			var lockResult = planApi.LockManager.LockAndExecute(apiOrchestrationSettings.Where(x => !x.IsNew && IsValid(x)).ToList(), DeleteDomInstances);
			ReportError(lockResult);
		}

		protected abstract void CreateOrUpdateDomInstances(ICollection<TApiSettings> apiOrchestrationSettings);

		protected abstract void DeleteDomInstances(ICollection<TApiSettings> apiOrchestrationSettings);

		private void ValidateCapacities(ICollection<TApiSettings> apiOrchestrationSettings)
		{
			if (apiOrchestrationSettings.Count == 0)
			{
				return;
			}

			var capacityIds = apiOrchestrationSettings
				.SelectMany(x => x.Capacities)
				.Select(x => x.Id)
				.Distinct()
				.ToList();
			var capacitiesById = planApi.Capacities.Read(capacityIds).ToDictionary(x => x.Id);

			foreach (var orchestrationSettings in apiOrchestrationSettings)
			{
				var duplicateSettings = orchestrationSettings.Capacities
					.GroupBy(x => x.Id)
					.Where(g => g.Count() > 1)
					.ToDictionary(x => x.Key, x => x.Count());

				foreach (var kvp in duplicateSettings)
				{
					var error = new OrchestrationSettingsInvalidCapacitySettingsError
					{
						ErrorMessage = $"Capacity with ID '{kvp.Key}' is defined {kvp.Value} times. Duplicate capacity settings are not allowed.",
						CapacityId = kvp.Key,
						Id = orchestrationSettings.Id,
					};

					ReportError(orchestrationSettings.Id, error);
				}

				if (duplicateSettings.Count > 0)
				{
					continue;
				}

				foreach (var capacitySetting in orchestrationSettings.Capacities)
				{
					if (capacitySetting.Id == Guid.Empty)
					{
						var error = new OrchestrationSettingsInvalidCapacitySettingsError
						{
							ErrorMessage = "Capacity ID cannot be empty.",
							CapacityId = capacitySetting.Id,
							Id = orchestrationSettings.Id,
						};

						ReportError(orchestrationSettings.Id, error);
						continue;
					}

					if (!capacitiesById.TryGetValue(capacitySetting.Id, out _))
					{
						var error = new OrchestrationSettingsInvalidCapacitySettingsError
						{
							ErrorMessage = $"Capacity with ID '{capacitySetting.Id}' not found.",
							CapacityId = capacitySetting.Id,
							Id = orchestrationSettings.Id,
						};

						ReportError(orchestrationSettings.Id, error);
					}
				}
			}
		}

		private void ValidateCapabilities(ICollection<TApiSettings> apiOrchestrationSettings)
		{
			if (apiOrchestrationSettings.Count == 0)
			{
				return;
			}

			var capabilityIds = apiOrchestrationSettings
				.SelectMany(x => x.Capabilities)
				.Select(x => x.Id)
				.Distinct()
				.ToList();
			var capabilitiesById = planApi.Capabilities.Read(capabilityIds).ToDictionary(x => x.Id);

			foreach (var orchestrationSettings in apiOrchestrationSettings)
			{
				var duplicateSettings = orchestrationSettings.Capabilities
					.GroupBy(x => x.Id)
					.Where(g => g.Count() > 1)
					.ToDictionary(x => x.Key, x => x.Count());

				foreach (var kvp in duplicateSettings)
				{
					var error = new OrchestrationSettingsInvalidCapabilitySettingsError
					{
						ErrorMessage = $"Capability with ID '{kvp.Key}' is defined {kvp.Value} times. Duplicate capability settings are not allowed.",
						CapabilityId = kvp.Key,
						Id = orchestrationSettings.Id,
					};

					ReportError(orchestrationSettings.Id, error);
				}

				if (duplicateSettings.Count > 0)
				{
					continue;
				}

				foreach (var capabilitySetting in orchestrationSettings.Capabilities)
				{
					if (capabilitySetting.Id == Guid.Empty)
					{
						var error = new OrchestrationSettingsInvalidCapabilitySettingsError
						{
							ErrorMessage = "Capability ID cannot be empty.",
							CapabilityId = capabilitySetting.Id,
							Id = orchestrationSettings.Id,
						};

						ReportError(capabilitySetting.Id, error);
						continue;
					}

					if (!capabilitiesById.TryGetValue(capabilitySetting.Id, out _))
					{
						var error = new OrchestrationSettingsInvalidCapabilitySettingsError
						{
							ErrorMessage = $"Capability with ID '{capabilitySetting.Id}' not found.",
							CapabilityId = capabilitySetting.Id,
							Id = orchestrationSettings.Id,
						};

						ReportError(capabilitySetting.Id, error);
					}

					// Not needed as long as there are no values assigned
					/*if (capabilitySetting.Discretes.Count == 0)
					{
						var error = new OrchestrationSettingsInvalidCapabilitySettingsError
						{
							ErrorMessage = "At least one discrete value must be specified for the capability.",
							CapabilityId = capabilitySetting.Id,
						};

						ReportError(capabilitySetting.Id, error);
						continue;
					}

					foreach (var discreteValue in capabilitySetting.Discretes)
					{
						if (!capability.Discretes.Contains(discreteValue))
						{
							var error = new OrchestrationSettingsInvalidCapabilitySettingsError
							{
								ErrorMessage = $"Discrete value '{discreteValue}' is not valid for capability '{capability.Name}'.",
								CapabilityId = capabilitySetting.Id,
							};

							ReportError(capabilitySetting.Id, error);
						}
					}*/
				}
			}
		}

		private void ValidateConfigurations(ICollection<TApiSettings> apiOrchestrationSettings)
		{
			if (apiOrchestrationSettings.Count == 0)
			{
				return;
			}

			var configurationIds = apiOrchestrationSettings
				.SelectMany(x => x.Configurations)
				.Select(x => x.Id)
				.Distinct()
				.ToList();

			var configurationsById = planApi.Configurations.Read(configurationIds).ToDictionary(x => x.Id);

			foreach (var orchestrationSettings in apiOrchestrationSettings)
			{
				var duplicateSettings = orchestrationSettings.Configurations
					.GroupBy(x => x.Id)
					.Where(g => g.Count() > 1)
					.ToDictionary(x => x.Key, x => x.Count());

				foreach (var kvp in duplicateSettings)
				{
					var error = new OrchestrationSettingsInvalidConfigurationSettingsError
					{
						ErrorMessage = $"Configuration with ID '{kvp.Key}' is defined {kvp.Value} times. Duplicate configuration settings are not allowed.",
						ConfigurationId = kvp.Key,
						Id = orchestrationSettings.Id,
					};

					ReportError(orchestrationSettings.Id, error);
				}

				if (duplicateSettings.Count > 0)
				{
					continue;
				}

				foreach (var configurationSetting in orchestrationSettings.Configurations)
				{
					if (configurationSetting.Id == Guid.Empty)
					{
						var error = new OrchestrationSettingsInvalidConfigurationSettingsError
						{
							ErrorMessage = "Configuration ID cannot be empty.",
							ConfigurationId = configurationSetting.Id,
							Id = orchestrationSettings.Id,
						};

						ReportError(configurationSetting.Id, error);
						continue;
					}

					if (!configurationsById.TryGetValue(configurationSetting.Id, out _))
					{
						var error = new OrchestrationSettingsInvalidConfigurationSettingsError
						{
							ErrorMessage = $"Configuration with ID '{configurationSetting.Id}' not found.",
							ConfigurationId = configurationSetting.Id,
							Id = orchestrationSettings.Id,
						};

						ReportError(configurationSetting.Id, error);
					}

					// Add dedicated validation for configuration setting values here if needed
				}
			}
		}
	}

	internal sealed class DomResourceStudioOrchestrationSettingsHandler : DomOrchestrationSettingsHandler<ResourceStudioOrchestrationSettings, DomResourceStudioOrchestrationSetting>
	{
		private DomResourceStudioOrchestrationSettingsHandler(MediaOpsPlanApi planApi) : base(planApi)
		{
		}

		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<OrchestrationSettings> apiOrchestrationSettings, out DomInstanceBulkOperationResult<DomResourceStudioOrchestrationSetting> result)
		{
			var handler = new DomResourceStudioOrchestrationSettingsHandler(planApi);
			handler.CreateOrUpdate(apiOrchestrationSettings.OfType<ResourceStudioOrchestrationSettings>().ToList());

			result = new DomInstanceBulkOperationResult<DomResourceStudioOrchestrationSetting>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<OrchestrationSettings> apiOrchestrationSettings, out DomInstanceBulkOperationResult<DomResourceStudioOrchestrationSetting> result)
		{
			var handler = new DomResourceStudioOrchestrationSettingsHandler(planApi);
			handler.Delete(apiOrchestrationSettings.OfType<ResourceStudioOrchestrationSettings>().ToList());

			result = new DomInstanceBulkOperationResult<DomResourceStudioOrchestrationSetting>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		protected override void CreateOrUpdateDomInstances(ICollection<ResourceStudioOrchestrationSettings> apiOrchestrationSettings)
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
				.Select(x => new DomResourceStudioOrchestrationSetting(x.Instance))
				.ToList();

			PersistDomInstances(toCreateDomInstances.Concat(toUpdateDomInstances).ToList());
		}

		protected override void DeleteDomInstances(ICollection<ResourceStudioOrchestrationSettings> apiOrchestrationSettings)
		{
			if (apiOrchestrationSettings.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided orchestration settings are valid", nameof(apiOrchestrationSettings));
			}

			DeleteDomResourceStudioInstances(apiOrchestrationSettings.Select(x => x.OriginalInstance).ToList());
		}

		private ICollection<DomChangeResults> GetSettingsWithChanges(ICollection<ResourceStudioOrchestrationSettings> apiOrchestrationSettings)
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

			var storedById = planApi.DomHelpers.SlcResourceStudioHelper.GetConfigurations(toValidate.Select(x => x.Id)).ToDictionary(x => x.ID.Id);
			foreach (var orchestrationSetting in toValidate)
			{
				if (!storedById.TryGetValue(orchestrationSetting.Id, out var stored))
				{
					var error = new OrchestrationSettingsNotFoundError
					{
						ErrorMessage = $"Resource studio orchestration setting with ID '{orchestrationSetting.Id}' no longer exists.",
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

		private void PersistDomInstances(ICollection<DomResourceStudioOrchestrationSetting> domInstances)
		{
			if (domInstances.Count == 0)
			{
				return;
			}

			var instancesToCreateOrUpdate = domInstances.Select(x => x.ToInstance()).ToList();
			planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(instancesToCreateOrUpdate, out var domResult);

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

			ReportSuccess(domResult.SuccessfulItems.Select(x => new DomResourceStudioOrchestrationSetting(x)));
		}

		private void DeleteDomResourceStudioInstances(ICollection<DomResourceStudioOrchestrationSetting> domInstances)
		{
			if (domInstances.Count == 0)
			{
				return;
			}

			var instancesToDelete = domInstances.Select(x => x.ToInstance()).ToList();
			planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryDeleteInBatches(instancesToDelete, out var domResult);

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

			ReportSuccess(instancesToDelete.Where(x => domResult.SuccessfulIds.Contains(x.ID)).Select(x => new DomResourceStudioOrchestrationSetting(x)).ToArray());
		}
	}

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
