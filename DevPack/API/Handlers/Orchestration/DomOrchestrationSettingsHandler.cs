namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API.Handlers.Orchestration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
    using Skyline.DataMiner.Utils.DOM.Extensions;

    using DomResourceStudioOrchestrationSetting = Storage.DOM.SlcResource_Studio.ConfigurationInstance;

    internal class DomOrchestrationSettingsHandler : ApiObjectValidator
    {
        private readonly MediaOpsPlanApi planApi;

        private DomOrchestrationSettingsHandler(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<OrchestrationSettings> apiOrchestrationSettings, out BulkOperationResult<Guid> result)
        {
            var handler = new DomOrchestrationSettingsHandler(planApi);
            handler.CreateOrUpdate(apiOrchestrationSettings);

            result = new BulkOperationResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures;
        }

        internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<OrchestrationSettings> apiOrchestrationSettings, out BulkOperationResult<Guid> result)
        {
            var handler = new DomOrchestrationSettingsHandler(planApi);
            handler.Delete(apiOrchestrationSettings);

            result = new BulkOperationResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures;
        }

        private void CreateOrUpdate(ICollection<OrchestrationSettings> apiOrchestrationSettings)
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

        private void CreateOrUpdateDomInstances(ICollection<OrchestrationSettings> apiOrchestrationSettings)
        {
            if (apiOrchestrationSettings == null)
            {
                throw new ArgumentNullException(nameof(apiOrchestrationSettings));
            }

            if (apiOrchestrationSettings.Any(x => !IsValid(x)))
            {
                throw new ArgumentException($"Not all provided orchestration settings are valid", nameof(apiOrchestrationSettings));
            }

            var resourceStudioOrchestrationSettings = apiOrchestrationSettings.OfType<ResourceStudioOrchestrationSettings>().ToList();
            var resourceStudioOrchestrationSettingsToCreate = resourceStudioOrchestrationSettings.Where(x => x.IsNew).ToList();
            var resourceStudioOrchestrationSettingsToUpdate = resourceStudioOrchestrationSettings.Except(resourceStudioOrchestrationSettingsToCreate).ToList();

            var resourceStudioChangeResults = GetResourceStudioOrchestrationSettingsWithChanges(resourceStudioOrchestrationSettingsToUpdate);

            var resourceStudioToCreateDomInstances = resourceStudioOrchestrationSettingsToCreate
                .Where(IsValid)
                .Select(x => x.GetInstanceWithChanges())
                .ToList();
            var resourceStudioToUpdateDomInstances = resourceStudioChangeResults
                .Where(IsValid)
                .Select(x => new DomResourceStudioOrchestrationSetting(x.Instance))
                .ToList();

            CreateOrUpdateDomResourceStudioInstances(resourceStudioToCreateDomInstances.Concat(resourceStudioToUpdateDomInstances).ToList());
        }

        private void CreateOrUpdateDomResourceStudioInstances(ICollection<DomResourceStudioOrchestrationSetting> domResourceStudioOrchestrationSettings)
        {
            if (domResourceStudioOrchestrationSettings == null)
            {
                throw new ArgumentNullException(nameof(domResourceStudioOrchestrationSettings));
            }

            if (domResourceStudioOrchestrationSettings.Count == 0)
            {
                return;
            }

            planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(domResourceStudioOrchestrationSettings.Select(x => x.ToInstance()), out var domResult);

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

            ReportSuccess(domResult.SuccessfulIds.Select(x => x.Id));
        }

        private ICollection<DomChangeResults> GetResourceStudioOrchestrationSettingsWithChanges(ICollection<ResourceStudioOrchestrationSettings> apiOrchestrationSettings)
        {
            if (apiOrchestrationSettings == null)
            {
                throw new ArgumentNullException(nameof(apiOrchestrationSettings));
            }

            var changeResults = new List<DomChangeResults>();
            if (apiOrchestrationSettings.Count == 0)
            {
                return changeResults;
            }

            var orchestrationSettingsRequiringValidation = apiOrchestrationSettings.Where(x => !x.IsNew && x.HasChanges).ToList();
            if (orchestrationSettingsRequiringValidation.Count == 0)
            {
                return changeResults;
            }

            var storedDomConfigurationsById = planApi.DomHelpers.SlcResourceStudioHelper.GetConfigurations(orchestrationSettingsRequiringValidation.Select(x => x.Id)).ToDictionary(x => x.ID.Id);
            foreach (var orchestrationSetting in orchestrationSettingsRequiringValidation)
            {
                if (!storedDomConfigurationsById.TryGetValue(orchestrationSetting.Id, out var stored))
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

        private void Delete(ICollection<OrchestrationSettings> apiOrchestrationSettings)
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

        private void DeleteDomInstances(ICollection<OrchestrationSettings> apiOrchestrationSettings)
        {
            if (apiOrchestrationSettings == null)
            {
                throw new ArgumentNullException(nameof(apiOrchestrationSettings));
            }

            if (apiOrchestrationSettings.Any(x => !IsValid(x)))
            {
                throw new ArgumentException($"Not all provided orchestration settings are valid", nameof(apiOrchestrationSettings));
            }

            DeleteDomResourceStudioInstances(apiOrchestrationSettings.OfType<ResourceStudioOrchestrationSettings>().Select(x => x.OriginalInstance).ToList());
        }

        private void DeleteDomResourceStudioInstances(ICollection<DomResourceStudioOrchestrationSetting> domResourceStudioOrchestrationSettings)
        {
            if (domResourceStudioOrchestrationSettings == null)
            {
                throw new ArgumentNullException(nameof(domResourceStudioOrchestrationSettings));
            }

            if (domResourceStudioOrchestrationSettings.Count == 0)
            {
                return;
            }

            planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryDeleteInBatches(domResourceStudioOrchestrationSettings.Select(x => x.ToInstance()), out var domResult);

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

            ReportSuccess(domResult.SuccessfulIds.Select(x => x.Id));
        }

        private void ValidateCapacities(ICollection<OrchestrationSettings> apiOrchestrationSettings)
        {
            if (apiOrchestrationSettings == null)
            {
                throw new ArgumentNullException(nameof(apiOrchestrationSettings));
            }

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
                foreach (var capacitySetting in orchestrationSettings.Capacities)
                {
                    if (capacitySetting.Id == Guid.Empty)
                    {
                        var error = new OrchestrationSettingsInvalidCapacitySettingsError
                        {
                            ErrorMessage = "Capacity ID cannot be empty.",
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
                        };

                        ReportError(orchestrationSettings.Id, error);
                    }

                    // Not needed as long as there are no values assigned
                    //PassTraceData(OrchestrationSettingsCapacitySettingValidator.Validate(orchestrationSettings.Id, capacity, capacitySetting));
                }
            }
        }

        private void ValidateCapabilities(ICollection<OrchestrationSettings> apiOrchestrationSettings)
        {
            if (apiOrchestrationSettings == null)
            {
                throw new ArgumentNullException(nameof(apiOrchestrationSettings));
            }

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
                foreach (var capabilitySetting in orchestrationSettings.Capabilities)
                {
                    if (capabilitySetting.Id == Guid.Empty)
                    {
                        var error = new OrchestrationSettingsInvalidCapabilitySettingsError
                        {
                            ErrorMessage = "Capability ID cannot be empty.",
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
                        };

                        ReportError(capabilitySetting.Id, error);
                        //continue;
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

        private void ValidateConfigurations(ICollection<OrchestrationSettings> apiOrchestrationSettings)
        {
            if (apiOrchestrationSettings == null)
            {
                throw new ArgumentNullException(nameof(apiOrchestrationSettings));
            }

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
                foreach (var configurationSetting in orchestrationSettings.Configurations)
                {
                    if (configurationSetting.Id == Guid.Empty)
                    {
                        var error = new OrchestrationSettingsInvalidConfigurationSettingsError
                        {
                            ErrorMessage = "Configuration ID cannot be empty.",
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
                        };

                        ReportError(configurationSetting.Id, error);
                        //continue;
                    }

                    // Add dedicated validation for configuration setting values here if needed
                }
            }
        }
    }
}
