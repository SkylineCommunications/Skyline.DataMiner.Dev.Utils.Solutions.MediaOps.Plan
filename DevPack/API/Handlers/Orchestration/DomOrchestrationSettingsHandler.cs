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

        internal static BulkCreateOrUpdateResult<Guid> CreateOrUpdate(MediaOpsPlanApi planApi, ICollection<OrchestrationSettings> apiOrchestrationSettings)
        {
            var handler = new DomOrchestrationSettingsHandler(planApi);
            handler.CreateOrUpdate(apiOrchestrationSettings);

            var result = new BulkCreateOrUpdateResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
            result.ThrowOnFailure();

            return result;
        }

        internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<OrchestrationSettings> apiOrchestrationSettings, out BulkCreateOrUpdateResult<Guid> result)
        {
            var handler = new DomOrchestrationSettingsHandler(planApi);
            handler.CreateOrUpdate(apiOrchestrationSettings);

            result = new BulkCreateOrUpdateResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures();
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

            // todo: add validation logic here

            var lockResult = planApi.LockManager.LockAndExecute(apiOrchestrationSettings, CreateOrUpdateDomInstances);
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
                    var error = new ResourceStudioOrchestrationSettingsNotFoundError
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
                        var error = new ResourceStudioOrchestrationSettingsValueAlreadyChangedError
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
    }
}
