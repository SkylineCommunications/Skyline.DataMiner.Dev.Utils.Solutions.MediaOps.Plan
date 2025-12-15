namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Helper;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.SRM.Capacities;
    using Skyline.DataMiner.Protobuf.Shared.IdObjects.v1;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

    using static Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections;

    using CoreResource = Net.Messages.Resource;
    using DomResource = Storage.DOM.SlcResource_Studio.ResourceInstance;
    using DomResourcePool = Storage.DOM.SlcResource_Studio.ResourcepoolInstance;

    internal class CoreResourceHandler
    {
        private readonly MediaOpsPlanApi planApi;

        private readonly List<Guid> successfulIds = new List<Guid>();
        private readonly List<Guid> unsuccessfulIds = new List<Guid>();
        private readonly Dictionary<Guid, MediaOpsTraceData> traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();
        private readonly Dictionary<Guid, Action<CoreResource>> EnableDveActionByCoreId = new Dictionary<Guid, Action<CoreResource>>();

        private readonly IReadOnlyDictionary<Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type, Func<DomResource, CoreResource, bool>> typeSyncers;

        private readonly Lazy<Dictionary<Guid, Net.Profiles.Parameter>> lazyCoreCapabilitiesById;
        private readonly Lazy<Dictionary<Guid, Net.Profiles.Parameter>> lazyCoreTimeDependentCapabilitiesById;
        private readonly Lazy<Dictionary<Guid, Net.Profiles.Parameter>> lazyCoreCapacitiesById;
        private readonly Lazy<DomCapabilitiesHandler> lazyCapabilitiesHandler;

        private CoreResourceHandler(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));

            lazyCoreCapabilitiesById = new Lazy<Dictionary<Guid, Net.Profiles.Parameter>>(() => planApi.CoreHelpers.ProfileProvider.GetAllCapabilities().ToDictionary(x => x.ID));
            lazyCoreTimeDependentCapabilitiesById = new Lazy<Dictionary<Guid, Net.Profiles.Parameter>>(() => planApi.CoreHelpers.ProfileProvider.GetAllTimeDependentCapabilities().ToDictionary(x => x.ID));
            lazyCoreCapacitiesById = new Lazy<Dictionary<Guid, Net.Profiles.Parameter>>(() => planApi.CoreHelpers.ProfileProvider.GetAllCapacities().ToDictionary(x => x.ID));
            lazyCapabilitiesHandler = new Lazy<DomCapabilitiesHandler>(() => new DomCapabilitiesHandler(planApi));

            typeSyncers = new Dictionary<Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type, Func<DomResource, CoreResource, bool>>
            {
                [Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.Unmanaged] = ApplyUnmanagedResourceConfig,
                [Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.Element] = ApplyElementResourceConfig,
                [Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.Service] = ApplyServiceResourceConfig,
                [Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.VirtualFunction] = ApplyVirtualFunctionResourceConfig,
            };
        }

        private Dictionary<Guid, Net.Profiles.Parameter> CoreCapabilitiesById => lazyCoreCapabilitiesById.Value;

        private Dictionary<Guid, Net.Profiles.Parameter> CoreTimeDependentCapabilitiesById => lazyCoreTimeDependentCapabilitiesById.Value;

        private Dictionary<Guid, Net.Profiles.Parameter> CoreCapacitiesById => lazyCoreCapacitiesById.Value;

        private DomCapabilitiesHandler CapabilitiesHandler => lazyCapabilitiesHandler.Value;

        public static BulkCreateOrUpdateResult<Guid> CreateOrUpdate(MediaOpsPlanApi planApi, IEnumerable<DomResource> domResources)
        {
            var handler = new CoreResourceHandler(planApi);
            handler.CreateOrUpdate(domResources);

            var result = new BulkCreateOrUpdateResult<Guid>(handler.successfulIds, handler.unsuccessfulIds, handler.traceDataPerItem);
            result.ThrowOnFailure();

            return result;
        }

        public static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, IEnumerable<DomResource> domResources, out BulkCreateOrUpdateResult<Guid> result)
        {
            var handler = new CoreResourceHandler(planApi);
            ActivityHelper.Track(nameof(CoreResourceHandler), nameof(CreateOrUpdate), act => handler.CreateOrUpdate(domResources));

            result = new BulkCreateOrUpdateResult<Guid>(handler.successfulIds, handler.unsuccessfulIds, handler.traceDataPerItem);

            return !result.HasFailures();
        }

        public static BulkDeleteResult<Guid> Delete(MediaOpsPlanApi planApi, IEnumerable<DomResource> domResources)
        {
            var handler = new CoreResourceHandler(planApi);
            ActivityHelper.Track(nameof(CoreResourceHandler), nameof(Delete), act => handler.Delete(domResources));

            var result = new BulkDeleteResult<Guid>(handler.successfulIds, handler.unsuccessfulIds, handler.traceDataPerItem);
            result.ThrowOnFailure();

            return result;
        }

        public static bool TryDelete(MediaOpsPlanApi planApi, IEnumerable<DomResource> domResources, out BulkDeleteResult<Guid> result)
        {
            var handler = new CoreResourceHandler(planApi);
            ActivityHelper.Track(nameof(CoreResourceHandler), nameof(Delete), act => handler.Delete(domResources));

            result = new BulkDeleteResult<Guid>(handler.successfulIds, handler.unsuccessfulIds, handler.traceDataPerItem);

            return !result.HasFailures();
        }

        public static bool TryDeprecate(MediaOpsPlanApi planApi, IEnumerable<DomResource> domResources, out BulkCreateOrUpdateResult<Guid> result)
        {
            var handler = new CoreResourceHandler(planApi);
            handler.Deprecate(domResources);

            result = new BulkCreateOrUpdateResult<Guid>(handler.successfulIds, handler.unsuccessfulIds, handler.traceDataPerItem);

            return !result.HasFailures();
        }

        public static bool TryValidateVirtualFunctionConfiguration(MediaOpsPlanApi planApi, ResourceVirtualFunctionLinkConfiguration configuration, out ResourceConfigurationError error)
        {
            error = null;

            var handler = new CoreResourceHandler(planApi);
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var elementId = new DmsElementId(configuration.AgentId, configuration.ElementId);
            if (!handler.TryValidateElementLink(elementId, out string invalidElementInfoReason))
            {
                error = new ResourceConfigurationInvalidElementLinkError
                {
                    ErrorMessage = invalidElementInfoReason,
                    AgentId = configuration.AgentId,
                    ElementId = configuration.ElementId,
                };

                return false;
            }

            if (!handler.TryValidateVirtualFunctionResourceFunctionDefinition(configuration.FunctionId, out string invalidFunctionDefinitionReason))
            {
                error = new ResourceConfigurationInvalidFunctionLinkError
                {
                    ErrorMessage = invalidFunctionDefinitionReason,
                    FunctionId = configuration.FunctionId,
                };

                return false;
            }

            if (!handler.TryValidateVirtualFunctionResourceTableIndex(configuration.FunctionId, elementId, configuration.FunctionTableIndex, out string invalidTableIndexReason))
            {
                error = new ResourceConfigurationInvalidTableIndexLinkError
                {
                    ErrorMessage = invalidTableIndexReason,
                    AgentId = configuration.AgentId,
                    ElementId = configuration.ElementId,
                    FunctionId = configuration.FunctionId,
                    FunctionTableIndex = configuration.FunctionTableIndex,
                };

                return false;
            }

            return true;
        }

        public static bool TryValidateServiceConfiguration(MediaOpsPlanApi planApi, ResourceServiceLinkConfiguration configuration, out ResourceConfigurationError error)
        {
            error = null;

            var handler = new CoreResourceHandler(planApi);
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var serviceId = new DmsServiceId(configuration.AgentId, configuration.ServiceId);
            if (!handler.TryValidateServiceResourceServiceLink(serviceId, out var reason))
            {
                error = new ResourceConfigurationInvalidServiceLinkError
                {
                    ErrorMessage = reason,
                    AgentId = configuration.AgentId,
                    ServiceId = configuration.ServiceId,
                };

                return false;
            }

            return true;
        }

        public static bool TryValidateElementConfiguration(MediaOpsPlanApi planApi, ResourceElementLinkConfiguration configuration, out ResourceConfigurationError error)
        {
            error = null;

            var handler = new CoreResourceHandler(planApi);
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var elementId = new DmsElementId(configuration.AgentId, configuration.ElementId);
            if (!handler.TryValidateElementLink(elementId, out var reason))
            {
                error = new ResourceConfigurationInvalidElementLinkError
                {
                    ErrorMessage = reason,
                    AgentId = configuration.AgentId,
                    ElementId = configuration.ElementId,
                };

                return false;
            }

            return true;
        }

        private void Deprecate(IEnumerable<DomResource> domResources)
        {
            if (domResources == null)
            {
                throw new ArgumentNullException(nameof(domResources));
            }

            if (!domResources.Any())
            {
                return;
            }

            Deprecate(ResourceMapping.GetMappings(planApi, domResources));
        }

        private void Deprecate(IEnumerable<ResourceMapping> resourceMappings)
        {
            if (resourceMappings == null)
            {
                throw new ArgumentNullException(nameof(resourceMappings));
            }

            if (!resourceMappings.Any())
            {
                return;
            }

            var domIdByCoreId = new Dictionary<Guid, Guid>();
            var resourcesToDeprecate = new List<CoreResource>();

            foreach (var mapping in resourceMappings)
            {
                if (mapping.CoreResource == null)
                {
                    successfulIds.Add(mapping.DomResource.ID.Id);

                    continue;
                }

                mapping.CoreResource.Mode = Net.Messages.ResourceMode.Unavailable;

                resourcesToDeprecate.Add(mapping.CoreResource);
                domIdByCoreId.Add(mapping.CoreResource.ID, mapping.DomResource.ID.Id);
            }

            planApi.CoreHelpers.ResourceManagerHelper.TryCreateOrUpdateResourcesInBatches(resourcesToDeprecate, out var result);

            foreach (var id in result.UnsuccessfulIds)
            {
                if (!domIdByCoreId.TryGetValue(id, out var domId))
                {
                    planApi.Logger.LogError($"Failed to find DOM ID for CORE resource ID {id}.");
                    continue;
                }

                unsuccessfulIds.Add(domId);

                if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
                {
                    traceDataPerItem.Add(domId, traceData);
                }
            }

            foreach (var id in result.SuccessfulIds)
            {
                if (!domIdByCoreId.TryGetValue(id, out var domId))
                {
                    planApi.Logger.LogError($"Failed to find DOM ID for CORE resource ID", id);
                    continue;
                }

                successfulIds.Add(domId);
            }
        }

        private void CreateOrUpdate(IEnumerable<DomResource> domResources)
        {
            if (domResources == null)
            {
                throw new ArgumentNullException(nameof(domResources));
            }

            if (!domResources.Any())
            {
                return;
            }

            var resourceMappingByDomId = ResourceMapping.GetMappings(planApi, domResources).ToDictionary(x => x.DomResource.ID.Id);

            var elementResourcesToValidate = new List<DomResource>();
            var serviceResourcesToValidate = new List<DomResource>();
            var virtualFunctionResourcesToValidate = new List<DomResource>();
            foreach (var resourceMapping in resourceMappingByDomId.Values)
            {
                if (resourceMapping.DomResource.ResourceInfo.Type == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.Element)
                {
                    elementResourcesToValidate.Add(resourceMapping.DomResource);
                }
                else if (resourceMapping.DomResource.ResourceInfo.Type == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.Service)
                {
                    serviceResourcesToValidate.Add(resourceMapping.DomResource);
                }
                else if (resourceMapping.DomResource.ResourceInfo.Type == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.VirtualFunction
                    && resourceMapping.DomResource.Status == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum.Draft)
                {
                    virtualFunctionResourcesToValidate.Add(resourceMapping.DomResource);
                }
            }

            ValidateElementResources(elementResourcesToValidate);
            ValidateServiceResources(serviceResourcesToValidate);
            ValidateVirtualFunctionResources(virtualFunctionResourcesToValidate);
            ValidateNames(resourceMappingByDomId.Where(x => !traceDataPerItem.Keys.Contains(x.Key) && x.Value.NeedsNameValidation).Select(x => x.Value.DomResource));

            CreateOrUpdate(resourceMappingByDomId.Where(x => !traceDataPerItem.Keys.Contains(x.Key)).Select(x => x.Value));
        }

        private void CreateOrUpdate(IEnumerable<ResourceMapping> resourceMappings)
        {
            if (resourceMappings == null)
            {
                throw new ArgumentNullException(nameof(resourceMappings));
            }

            if (!resourceMappings.Any())
            {
                return;
            }

            var domResourcesById = new Dictionary<Guid, DomResource>();
            var domIdByCoreId = new Dictionary<Guid, Guid>();

            var resourcesToCreateOrUpdate = new List<CoreResource>();
            foreach (var mapping in resourceMappings)
            {
                var dom = mapping.DomResource;
                var core = mapping.CoreResource;

                if (!SyncDomResourceWitCoreResource(dom, core))
                {
                    planApi.Logger.LogInformation($"No CORE changes for DOM resource {mapping.DomResource.ID}");
                    continue;
                }

                resourcesToCreateOrUpdate.Add(core);

                domResourcesById.Add(dom.ID.Id, dom);
                domIdByCoreId.Add(core.ID, dom.ID.Id);
            }

            planApi.CoreHelpers.ResourceManagerHelper.TryCreateOrUpdateResourcesInBatches(resourcesToCreateOrUpdate, out var result, out var createdOrUpdatedResources);

            foreach (var id in result.UnsuccessfulIds)
            {
                if (!domIdByCoreId.TryGetValue(id, out var domId))
                {
                    planApi.Logger.LogError($"Failed to find DOM ID for CORE resource ID {id}.");
                    continue;
                }

                unsuccessfulIds.Add(domId);

                if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
                {
                    traceDataPerItem.Add(domId, traceData);
                }
            }

            var createdOrUpdatedResourcesByCoreId = createdOrUpdatedResources?.ToDictionary(x => x.ID) ?? new Dictionary<Guid, CoreResource>();
            foreach (var id in result.SuccessfulIds)
            {
                if (!domIdByCoreId.TryGetValue(id, out var domId))
                {
                    planApi.Logger.LogError($"Failed to find DOM ID for CORE resource pool ID {id}.");
                    continue;
                }

                domResourcesById[domId].ResourceInternalProperties.Resource_Id = id;

                if (EnableDveActionByCoreId.TryGetValue(id, out var enableDveAction))
                {
                    enableDveAction?.Invoke(createdOrUpdatedResourcesByCoreId[id]);
                }

                successfulIds.Add(domId);
            }
        }

        private bool SyncDomResourceWitCoreResource(DomResource dom, CoreResource core)
        {
            bool updateRequired = false;

            if (core == null)
            {
                core = BuildCoreResource(dom.ResourceInfo.Type.Value);
                updateRequired = true;
            }

            updateRequired |= SyncName(dom, core);
            updateRequired |= SyncType(dom, core);
            updateRequired |= SyncCapacities(dom, core);
            updateRequired |= SyncCapabilities(dom, core);
            updateRequired |= SyncConcurrency(dom, core);
            updateRequired |= SyncPools(dom, core);

            return updateRequired;
        }

        private void Delete(IEnumerable<DomResource> domResources)
        {
            if (domResources == null)
            {
                throw new ArgumentNullException(nameof(domResources));
            }

            if (!domResources.Any())
            {
                return;
            }

            var domResourcesById = new Dictionary<Guid, DomResource>();
            var domIdByCoreId = new Dictionary<Guid, Guid>();

            foreach (var domResource in domResources)
            {
                domResourcesById.Add(domResource.ID.Id, domResource);

                if (domResource.ResourceInternalProperties.Resource_Id.GetValueOrDefault() == Guid.Empty)
                {
                    // No CORE resource linked to the DOM resource
                    continue;
                }

                domIdByCoreId.Add(domResource.ResourceInternalProperties.Resource_Id.Value, domResource.ID.Id);
            }

            FilterElement<CoreResource> filter(Guid resourceId) => Skyline.DataMiner.Net.Messages.ResourceExposers.ID.Equal(resourceId);
            var coreResourceById = planApi.CoreHelpers.ResourceManagerHelper.GetResources(domIdByCoreId.Keys, filter).ToDictionary(x => x.ID);

            // DOM resources without a CORE can be removed.
            successfulIds.AddRange(domResources
                .Where(x => !x.ResourceInternalProperties.Resource_Id.HasValue
                || x.ResourceInternalProperties.Resource_Id.Value == Guid.Empty
                || !coreResourceById.ContainsKey(x.ResourceInternalProperties.Resource_Id.Value))
                .Select(x => x.ID.Id));

            var options = new Net.Messages.ResourceDeleteOptions
            {
                IgnoreCanceledReservations = true,
                IgnorePastReservation = true,
            };

            planApi.CoreHelpers.ResourceManagerHelper.TryDeleteResourcesInBatches(coreResourceById.Values.ToArray(), options, out var result);

            foreach (var id in result.UnsuccessfulIds)
            {
                if (!domIdByCoreId.TryGetValue(id, out var domId))
                {
                    planApi.Logger.LogError($"Failed to find DOM ID for CORE resource ID {id}.");
                    continue;
                }

                unsuccessfulIds.Add(domId);

                if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
                {
                    traceDataPerItem.Add(domId, traceData);
                }
            }

            foreach (var id in result.SuccessfulIds)
            {
                if (!domIdByCoreId.TryGetValue(id, out var domId))
                {
                    planApi.Logger.LogError("Failed to find DOM ID for CORE resource ID {id}.");
                    continue;
                }

                successfulIds.Add(domId);
            }
        }

        private CoreResource BuildCoreResource(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type resourceType)
        {
            if (resourceType == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.VirtualFunction)
            {
                return new Net.ResourceManager.Objects.FunctionResource()
                {
                    ID = Guid.NewGuid(),
                };
            }

            return new CoreResource(Guid.NewGuid());
        }

        private bool ApplyUnmanagedResourceConfig(DomResource domResource, CoreResource coreResource)
        {
            return SetResourceType(coreResource, "Unlinked Resource");
        }

        private bool ApplyElementResourceConfig(DomResource domResource, CoreResource coreResource)
        {
            var elementInfo = new DmsElementId(domResource.ResourceInternalProperties.Metadata.LinkedElementInfo);

            bool updateRequired = false;
            if (coreResource.DmaID != elementInfo.AgentId)
            {
                coreResource.DmaID = elementInfo.AgentId;
                updateRequired = true;
            }

            if (coreResource.ElementID != elementInfo.ElementId)
            {
                coreResource.ElementID = elementInfo.ElementId;
                updateRequired = true;
            }

            updateRequired |= SetResourceType(coreResource, "Element");
            return updateRequired;
        }

        private bool ApplyServiceResourceConfig(DomResource domResource, CoreResource coreResource)
        {
            bool updateRequired = false;
            var serviceLinkProperty = coreResource.Properties.FirstOrDefault(x => String.Equals(x.Name, "Service Link"));
            if (serviceLinkProperty == null)
            {
                serviceLinkProperty = new Net.Messages.ResourceManagerProperty("Service Link", domResource.ResourceInternalProperties.Metadata.LinkedServiceInfo);
                coreResource.Properties.Add(serviceLinkProperty);
                updateRequired = true;
            }
            else if (!String.Equals(serviceLinkProperty.Value, domResource.ResourceInternalProperties.Metadata.LinkedServiceInfo))
            {
                serviceLinkProperty.Value = domResource.ResourceInternalProperties.Metadata.LinkedServiceInfo;
                updateRequired = true;
            }
            else
            {
                // no property update required
            }

            updateRequired |= SetResourceType(coreResource, "Service");
            return updateRequired;
        }

        private bool ApplyVirtualFunctionResourceConfig(DomResource domResource, CoreResource coreResource)
        {
            if (coreResource is not Net.ResourceManager.Objects.FunctionResource functionResource)
            {
                throw new InvalidOperationException($"Core Resource {coreResource.Name} ({coreResource.ID}) is not a FunctionResource.");
            }

            bool updateRequired = false;
            var functionDefinition = planApi.CoreHelpers.ProtocolFunctionHelperCache.GetFunctionDefinition(domResource.ResourceInternalProperties.Metadata.LinkedFunctionId);
            if (functionResource.FunctionGUID != functionDefinition.GUID)
            {
                functionResource.FunctionGUID = functionDefinition.GUID;
                updateRequired = true;
            }

            var elementInfo = new DmsElementId(domResource.ResourceInternalProperties.Metadata.LinkedElementInfo);
            if (functionResource.MainDVEDmaID != elementInfo.AgentId)
            {
                functionResource.MainDVEDmaID = elementInfo.AgentId;
                updateRequired = true;
            }

            if (functionResource.MainDVEElementID != elementInfo.ElementId)
            {
                functionResource.MainDVEElementID = elementInfo.ElementId;
                updateRequired = true;
            }

            if (functionDefinition.EntryPoints.Any())
            {
                int parameterId = functionDefinition.EntryPoints.First().ParameterId;
                string tableIndex = domResource.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex;

                if (functionResource.LinkerTableEntries.Any())
                {
                    var existingEntry = functionResource.LinkerTableEntries.First();
                    if (existingEntry.Item1 != parameterId || !String.Equals(existingEntry.Item2, tableIndex))
                    {
                        functionResource.LinkerTableEntries = [new Tuple<int, string>(parameterId, tableIndex)];
                        updateRequired = true;
                    }
                }
                else
                {
                    functionResource.LinkerTableEntries = [new Tuple<int, string>(parameterId, tableIndex)];
                    updateRequired = true;
                }
            }

            updateRequired |= SetResourceType(coreResource, "Virtual Function");

            Action<CoreResource> enableDveAction = (createdResource) =>
            {
                if (createdResource is not Net.ResourceManager.Objects.FunctionResource fResource)
                {
                    return;
                }

                var element = planApi.CoreHelpers.DmsCache.GetElement(elementInfo);
                var genericDveTable = element.GetTable(65132);
                var dveStateColumn = genericDveTable.GetColumn<int?>(65136);
                dveStateColumn.SetValue(fResource.PK, 1);
            };

            EnableDveActionByCoreId.Add(coreResource.ID, enableDveAction);

            return updateRequired;
        }

        // TODO should this move to CoreCapabilitiesHandler?
        private bool SetResourceType(CoreResource coreResource, string resourceTypeValue)
        {
            bool updateRequired = false;
            var resourceTypeCapability = coreResource.Capabilities.FirstOrDefault(x => x.CapabilityProfileID == CoreCapabilities.ResourceType.Id);
            var capabilityValue = new Net.Profiles.CapabilityParameterValue(new List<string> { resourceTypeValue });
            if (resourceTypeCapability == null)
            {
                coreResource.Capabilities.Add(new Net.SRM.Capabilities.ResourceCapability(CoreCapabilities.ResourceType.Id)
                {
                    Value = new Net.Profiles.CapabilityParameterValue(new List<string> { resourceTypeValue }),
                });

                updateRequired = true;
            }
            else if (!resourceTypeCapability.Value.Equals(capabilityValue))
            {
                resourceTypeCapability.Value = capabilityValue;
            }

            return updateRequired;
        }

        private void ValidateNames(IEnumerable<DomResource> domResources)
        {
            if (domResources == null)
            {
                throw new ArgumentNullException(nameof(domResources));
            }

            if (!domResources.Any())
            {
                return;
            }

            var resourcesRequiringValidation = domResources.ToList();
            var resourcesWithDuplicateNames = resourcesRequiringValidation
                .GroupBy(resource => resource.ResourceInfo.Name)
                .Where(g => g.Count() > 1)
                .SelectMany(x => x)
                .ToList();

            foreach (var resource in resourcesWithDuplicateNames)
            {
                var error = new ResourceConfigurationDuplicateNameError
                {
                    ErrorMessage = $"Resource '{resource.ResourceInfo.Name}' has a duplicate name.",
                    Id = resource.ID.Id,
                    Name = resource.ResourceInfo.Name,
                };
                AddError(resource.ID.Id, error);

                resourcesRequiringValidation.Remove(resource);
            }

            var coreResourceNames = resourcesRequiringValidation.Select(x => x.ResourceInfo.Name);
            FilterElement<CoreResource> filter(string name) => Net.Messages.ResourceExposers.Name.Equal(name);
            var coreResourcesByName = planApi.CoreHelpers.ResourceManagerHelper.GetResources(coreResourceNames, filter)
                .GroupBy(x => x.Name)
                .ToDictionary(x => x.Key, x => (IReadOnlyCollection<CoreResource>)x.ToList());

            foreach (var resource in resourcesRequiringValidation)
            {
                if (!coreResourcesByName.TryGetValue(resource.ResourceInfo.Name, out var coreResources))
                {
                    continue;
                }

                var existingResources = coreResources.Where(x => x.ID != resource.ResourceInternalProperties.Resource_Id.GetValueOrDefault()).ToList();
                if (existingResources.Count == 0)
                {
                    continue;
                }

                planApi.Logger.LogInformation($"Name '{resource.ResourceInfo.Name}' is already in use by CORE resource(s) with ID(s): {string.Join(" ,", existingResources.Select(x => x.ID))}");

                var error = new ResourceConfigurationNameExistsError
                {
                    ErrorMessage = "Name is already in use.",
                    Id = resource.ID.Id,
                    Name = resource.ResourceInfo.Name,
                };

                AddError(resource.ID.Id, error);
            }
        }

        private void ValidateElementResources(IEnumerable<DomResource> domResources)
        {
            if (domResources == null)
            {
                throw new ArgumentNullException(nameof(domResources));
            }

            foreach (var domResource in domResources)
            {
                var elementId = new DmsElementId(domResource.ResourceInternalProperties.Metadata.LinkedElementInfo);
                if (!TryValidateElementLink(elementId, out string reason))
                {
                    var error = new ResourceConfigurationInvalidElementLinkError
                    {
                        ErrorMessage = reason,
                        Id = domResource.ID.Id,
                        AgentId = elementId.AgentId,
                        ElementId = elementId.ElementId,
                    };

                    AddError(domResource.ID.Id, error);
                }
            }
        }

        public bool TryValidateElementLink(DmsElementId elementId, out string reason)
        {
            reason = String.Empty;
            var element = planApi.CoreHelpers.DmsCache.GetElement(elementId);

            if (element == null)
            {
                reason = $"No element found with ID '{elementId}'.";
                return false;
            }

            if (element.FunctionSettings.IsFunctionElement)
            {
                reason = $"Element '{element.Name}' is a function element and cannot be linked to a resource.";
                return false;
            }

            return true;
        }

        private void ValidateServiceResources(IEnumerable<DomResource> domResources)
        {
            if (domResources == null)
            {
                throw new ArgumentNullException(nameof(domResources));
            }

            foreach (var domResource in domResources)
            {
                var serviceId = new DmsServiceId(domResource.ResourceInternalProperties.Metadata.LinkedServiceInfo);
                if (!TryValidateServiceResourceServiceLink(serviceId, out string reason))
                {
                    var error = new ResourceConfigurationInvalidServiceLinkError
                    {
                        ErrorMessage = reason,
                        Id = domResource.ID.Id,
                        AgentId = serviceId.AgentId,
                        ServiceId = serviceId.ServiceId,
                    };

                    AddError(domResource.ID.Id, error);
                }
            }
        }

        private bool TryValidateServiceResourceServiceLink(DmsServiceId serviceId, out string reason)
        {
            reason = String.Empty;

            var service = planApi.CoreHelpers.DmsCache.GetService(serviceId);

            if (service == null)
            {
                reason = $"No service found with ID '{serviceId}'.";
                return false;
            }

            return true;
        }

        private void ValidateVirtualFunctionResources(IEnumerable<DomResource> domResources)
        {
            if (domResources == null)
            {
                throw new ArgumentNullException(nameof(domResources));
            }

            if (!domResources.Any())
            {
                return;
            }

            // Validate elements
            var domResourcesToValidate = new List<DomResource>(domResources);
            var invalidDomResources = new List<DomResource>();
            foreach (var domResource in domResourcesToValidate)
            {
                var elementId = new DmsElementId(domResource.ResourceInternalProperties.Metadata.LinkedElementInfo);
                if (!TryValidateElementLink(elementId, out string invalidElementInfoReason))
                {
                    var error = new ResourceConfigurationInvalidElementLinkError
                    {
                        ErrorMessage = invalidElementInfoReason,
                        Id = domResource.ID.Id,
                        AgentId = elementId.AgentId,
                        ElementId = elementId.ElementId,
                    };

                    AddError(domResource.ID.Id, error);

                    invalidDomResources.Add(domResource);
                }
            }

            domResourcesToValidate.RemoveAll(x => invalidDomResources.Contains(x));

            // Validate functions
            var domResourcesByFunctionId = domResourcesToValidate
                .GroupBy(x => x.ResourceInternalProperties.Metadata.LinkedFunctionId)
                .ToDictionary(x => x.Key, x => x.ToList());
            var functionDefinitionsById = planApi.CoreHelpers.ProtocolFunctionHelperCache.GetFunctionDefinitions(domResourcesByFunctionId.Keys);

            foreach (var kvp in domResourcesByFunctionId.Where(x => !functionDefinitionsById.ContainsKey(x.Key)).ToList())
            {
                var error = new ResourceConfigurationInvalidFunctionLinkError
                {
                    ErrorMessage = $"No function found with ID '{kvp.Key}'.",
                    FunctionId = kvp.Key,
                };

                AddError(kvp.Value, error);

                domResourcesByFunctionId.Remove(kvp.Key);
            }

            // validate table indexes
            var domResourcesByElementFunction = domResourcesByFunctionId.Values.SelectMany(x => x)
                .GroupBy(x => new ElementFunctionMapping
                {
                    FunctionDefinitionId = x.ResourceInternalProperties.Metadata.LinkedFunctionId,
                    ElementInfo = new DmsElementId(x.ResourceInternalProperties.Metadata.LinkedElementInfo),
                })
                .ToDictionary(x => x.Key, x => x.ToList());

            foreach (var kvp in domResourcesByElementFunction)
            {
                var resourcesRequiringValidation = kvp.Value.ToList();

                // Check for duplicate table indexes
                var resourcesWithSameTableIndex = resourcesRequiringValidation
                    .Where(x => !string.IsNullOrEmpty(x.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex))
                    .GroupBy(x => x.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex)
                    .Where(g => g.Count() > 1)
                    .SelectMany(x => x)
                    .ToList();

                foreach (var resource in resourcesWithSameTableIndex)
                {
                    var elementId = new DmsElementId(resource.ResourceInternalProperties.Metadata.LinkedElementInfo);

                    var error = new ResourceConfigurationDuplicateTableIndexLinkError
                    {
                        ErrorMessage = $"Resource '{resource.ResourceInfo.Name}' has a duplicate table index '{resource.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex}'.",
                        Id = resource.ID.Id,
                        AgentId = elementId.AgentId,
                        ElementId = elementId.ElementId,
                        FunctionId = resource.ResourceInternalProperties.Metadata.LinkedFunctionId,
                        FunctionTableIndex = resource.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex,

                    };

                    AddError(resource.ID.Id, error);

                    resourcesRequiringValidation.Remove(resource);
                }

                var entryPoints = planApi.CoreHelpers.ProtocolFunctionHelperCache.GetElementFunctionEntryPoints(kvp.Key.FunctionDefinitionId, kvp.Key.ElementInfo, forceGet: true, returnAvailableOnly: true);
                foreach (var resource in resourcesRequiringValidation.Where(x => !entryPoints.Any(y => y.IndexValue == x.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex)))
                {
                    var elementId = new DmsElementId(resource.ResourceInternalProperties.Metadata.LinkedElementInfo);

                    var error = new ResourceConfigurationInvalidTableIndexLinkError
                    {
                        ErrorMessage = $"Resource '{resource.ResourceInfo.Name}' has an invalid table index '{resource.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex}'.",
                        Id = resource.ID.Id,
                        AgentId = elementId.AgentId,
                        ElementId = elementId.ElementId,
                        FunctionId = resource.ResourceInternalProperties.Metadata.LinkedFunctionId,
                        FunctionTableIndex = resource.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex,
                    };

                    AddError(resource.ID.Id, error);
                }
            }
        }

        private bool TryValidateVirtualFunctionResourceFunctionDefinition(Guid functionDefinitionId, out string reason)
        {
            reason = String.Empty;

            var functionDefinition = planApi.CoreHelpers.ProtocolFunctionHelperCache.GetFunctionDefinition(functionDefinitionId);

            if (functionDefinition == null)
            {
                reason = $"No function found with ID '{functionDefinitionId}'.";
                return false;
            }

            return true;
        }

        private bool TryValidateVirtualFunctionResourceTableIndex(Guid functionDefinitionId, DmsElementId functionElementId, string tableIndex, out string reason)
        {
            reason = String.Empty;

            var entryPoints = planApi.CoreHelpers.ProtocolFunctionHelperCache.GetElementFunctionEntryPoints(functionDefinitionId, functionElementId, returnAvailableOnly: true);
            if (!entryPoints.Any(x => x.IndexValue == tableIndex))
            {
                reason = $"Invalid table index '{tableIndex}'.";
                return false;
            }

            return true;
        }

        private void AddError(IEnumerable<DomResource> domResources, MediaOpsErrorData error)
        {
            if (domResources == null)
            {
                throw new ArgumentNullException(nameof(domResources));
            }

            if (!domResources.Any())
            {
                return;
            }

            foreach (var domResource in domResources)
            {
                AddError(domResource.ID.Id, error);
            }
        }

        private void AddError(Guid id, MediaOpsErrorData error)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Id cannot be empty.", nameof(id));
            }

            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            if (!traceDataPerItem.TryGetValue(id, out var mediaOpsTraceData))
            {
                mediaOpsTraceData = new MediaOpsTraceData();
                traceDataPerItem.Add(id, mediaOpsTraceData);

                unsuccessfulIds.Add(id);
            }

            mediaOpsTraceData.Add(error);
        }

        private bool SyncName(DomResource domResource, CoreResource coreResource)
        {
            if (String.Equals(domResource.Name, coreResource.Name))
            {
                return false;
            }

            coreResource.Name = domResource.ResourceInfo.Name;
            return true;
        }

        private bool SyncType(DomResource domResource, CoreResource coreResource)
        {
            return typeSyncers[domResource.ResourceInfo.Type.Value].Invoke(domResource, coreResource);
        }

        private bool SyncCapacities(DomResource domResource, CoreResource coreResource)
        {
            bool resourceHasChanges = false;
            var required = GetRequiredResourceCapacities(domResource);
            var removed = coreResource.Capacities.Where(x => !required.Select(y => y.CapacityProfileID).Contains(x.CapacityProfileID)).ToList();

            foreach (var resourceCapacity in removed)
            {
                coreResource.Capacities.Remove(resourceCapacity);

                resourceHasChanges = true;
            }

            foreach (var resourceCapacity in required)
            {
                var capacity = coreResource.Capacities.SingleOrDefault(x => x.CapacityProfileID == resourceCapacity.CapacityProfileID);
                if (capacity == null)
                {
                    coreResource.Capacities.Add(resourceCapacity);
                }
                else if (!HasChangedValue(capacity, resourceCapacity))
                {
                    continue;
                }

                resourceHasChanges = true;
            }

            return resourceHasChanges;

            bool HasChangedValue(MultiResourceCapacity current, MultiResourceCapacity expected)
            {
                var hasChangedValue = false;

                if (!CoreCapacitiesById.TryGetValue(current.CapacityProfileID, out var coreCapacity))
                {
                    return hasChangedValue;
                }

                if (coreCapacity.IsRange())
                {
                    if (!current.Value.MinDecimalQuantity.Equals(expected.Value.MinDecimalQuantity))
                    {
                        current.Value.MinDecimalQuantity = expected.Value.MinDecimalQuantity;
                        hasChangedValue = true;
                    }

                    if (!current.Value.MaxDecimalQuantity.Equals(expected.Value.MaxDecimalQuantity))
                    {
                        current.Value.MaxDecimalQuantity = expected.Value.MaxDecimalQuantity;
                        hasChangedValue = true;
                    }
                }
                else if (!current.Value.MaxDecimalQuantity.Equals(expected.Value.MaxDecimalQuantity))
                {
                    current.Value.MaxDecimalQuantity = expected.Value.MaxDecimalQuantity;
                    hasChangedValue = true;
                }

                return hasChangedValue;
            }
        }

        private List<MultiResourceCapacity> GetRequiredResourceCapacities(DomResource domResource)
        {
            var capacities = new List<MultiResourceCapacity>();
            foreach (var resourceCapacity in domResource.ResourceCapacities)
            {
                if (!Guid.TryParse(resourceCapacity.ProfileParameterID, out Guid profileParameterId))
                {
                    planApi.Logger.LogWarning($"Invalid ProfileParameterID '{resourceCapacity.ProfileParameterID}' for resource '{domResource.ResourceInfo.Name}'. Skipping capacity sync.");
                    continue;
                }

                if (!CoreCapacitiesById.TryGetValue(profileParameterId, out var coreCapacity))
                {
                    continue;
                }

                var capacity = new MultiResourceCapacity
                {
                    CapacityProfileID = coreCapacity.ID,
                };

                if (coreCapacity.IsRange())
                {
                    capacity.Value = new Net.Profiles.CapacityParameterValue
                    {
                        MinDecimalQuantity = (decimal)resourceCapacity.DoubleMinValue,
                        MaxDecimalQuantity = (decimal)resourceCapacity.DoubleMaxValue,
                    };
                }
                else
                {
                    capacity.Value = new Net.Profiles.CapacityParameterValue
                    {
                        MaxDecimalQuantity = (decimal)resourceCapacity.DoubleMaxValue,
                    };
                }

                capacities.Add(capacity);
            }

            return capacities;
        }

        private bool SyncCapabilities(DomResource domResource, CoreResource coreResource)
        {
            bool resourceHasChanges = false;
            var required = GetRequiredResourceCapabilities(domResource);
            var removed = coreResource.Capabilities
                .Where(x =>
                    x.CapabilityProfileID != CoreCapabilities.ResourceType.Id
                    && !required.Select(y => y.CapabilityProfileID).Contains(x.CapabilityProfileID))
                .ToList();

            foreach (var resourceCapability in removed)
            {
                coreResource.Capabilities.Remove(resourceCapability);
                resourceHasChanges = true;
            }

            foreach (var resourceCapability in required)
            {
                var capability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == resourceCapability.CapabilityProfileID);
                if (capability == null)
                {
                    coreResource.Capabilities.Add(resourceCapability);
                }
                else if (!capability.IsTimeDynamic && !capability.Value.Discreets.Equals(resourceCapability.Value.Discreets))
                {
                    capability.Value.Discreets = resourceCapability.Value.Discreets;
                }
                else
                {
                    continue;
                }

                resourceHasChanges = true;
            }

            return resourceHasChanges;
        }

        private List<Net.SRM.Capabilities.ResourceCapability> GetRequiredResourceCapabilities(DomResource domResource)
        {
            var domCapabilities = CapabilitiesHandler.GetExpectedCoreResourceCapabilities(domResource);

            var coreCapabilities = new List<Net.SRM.Capabilities.ResourceCapability>();
            foreach (var configuredCapability in domCapabilities)
            {
                if (!CoreCapabilitiesById.TryGetValue(configuredCapability.ProfileParameterId, out var coreCapability))
                {
                    continue;
                }

                if (coreCapability.IsTimeDependent(out var timeDependentCapabilityLink))
                {
                    if (!CoreTimeDependentCapabilitiesById.TryGetValue(timeDependentCapabilityLink.LinkedParameterId, out var linkedCoreCapability))
                    {
                        continue;
                    }

                    var timeDependentCapability = new Net.SRM.Capabilities.ResourceCapability(linkedCoreCapability.ID)
                    {
                        Value = new Net.Profiles.CapabilityParameterValue(),
                        IsTimeDynamic = true,
                    };

                    coreCapabilities.Add(timeDependentCapability);
                }

                var capability = new Net.SRM.Capabilities.ResourceCapability(coreCapability.ID)
                {
                    Value = new Net.Profiles.CapabilityParameterValue(GetDiscretes(configuredCapability)),
                };

                coreCapabilities.Add(capability);
            }

            return coreCapabilities;
        }

        private List<string> GetDiscretes(IConfiguredCapability configuredCapability)
        {
            if (string.IsNullOrEmpty(configuredCapability.StringValue))
            {
                return new List<string>();
            }

            return configuredCapability.StringValue.Split(';').ToList();
        }

        private bool SyncConcurrency(DomResource domResource, CoreResource coreResource)
        {
            var configuredConcurrency = (int)domResource.ResourceInfo.Concurrency;
            if (configuredConcurrency < 1)
            {
                configuredConcurrency = 1;
            }

            if (coreResource.MaxConcurrency == configuredConcurrency)
            {
                return false;
            }

            coreResource.MaxConcurrency = configuredConcurrency;
            return true;
        }

        private bool SyncPools(DomResource domResource, CoreResource coreResource)
        {
            var poolIds = domResource.ResourceInternalProperties?.PoolIds ?? Enumerable.Empty<Guid>();
            var cachedDomPoolsById = domResource.GetFromCache<DomResourcePool>().ToDictionary(x => x.ID.Id);

            var missingPoolIds = poolIds.Where(x => !cachedDomPoolsById.ContainsKey(x));
            var domPools = planApi.ResourcePools.Read(missingPoolIds).Values.Select(x => x.OriginalInstance).ToList();
            domPools.AddRange(cachedDomPoolsById.Values);

            var corePoolIds = domPools.Select(x => x.ResourcePoolInternalProperties.ResourcePoolId).Where(x => x != Guid.Empty).ToList();

            if (coreResource.PoolGUIDs.ScrambledEquals(corePoolIds))
            {
                return false;
            }

            coreResource.PoolGUIDs.Clear();
            coreResource.PoolGUIDs.AddRange(corePoolIds);

            return true;
        }

        private sealed class ResourceMapping
        {
            private ResourceMapping(DomResource domResource) : this(domResource, new CoreResource { ID = Guid.NewGuid() })
            {
            }

            private ResourceMapping(DomResource domResource, CoreResource coreResource)
            {
                DomResource = domResource ?? throw new ArgumentNullException(nameof(domResource));
                CoreResource = coreResource ?? throw new ArgumentNullException(nameof(coreResource));
            }

            public DomResource DomResource { get; }

            public CoreResource CoreResource { get; }

            public bool NeedsNameValidation =>
                CoreResource == null
                || DomResource.ResourceInfo.Name != CoreResource.Name;

            public static IEnumerable<ResourceMapping> GetMappings(MediaOpsPlanApi planApi, IEnumerable<DomResource> domResources)
            {
                if (planApi == null)
                {
                    throw new ArgumentNullException(nameof(planApi));
                }

                if (domResources == null)
                {
                    throw new ArgumentNullException(nameof(domResources));
                }

                if (!domResources.Any())
                {
                    return [];
                }

                return GetMappingsIterator(planApi, domResources);
            }

            private static IEnumerable<ResourceMapping> GetMappingsIterator(MediaOpsPlanApi planApi, IEnumerable<DomResource> domResources)
            {
                var coreResourceIds = domResources
                    .Where(x => x.ResourceInternalProperties.Resource_Id.HasValue && x.ResourceInternalProperties.Resource_Id.Value != Guid.Empty)
                    .Select(x => x.ResourceInternalProperties.Resource_Id.Value)
                    .Distinct();
                FilterElement<CoreResource> filter(Guid id) => Net.Messages.ResourceExposers.ID.Equal(id);
                var coreResourcesById = planApi.CoreHelpers.ResourceManagerHelper.GetResources(coreResourceIds, filter).ToDictionary(x => x.ID);

                foreach (var domResource in domResources)
                {
                    if (domResource.ResourceInternalProperties.Resource_Id.HasValue
                        && coreResourcesById.TryGetValue(domResource.ResourceInternalProperties.Resource_Id.Value, out var coreResource))
                    {
                        yield return new ResourceMapping(domResource, coreResource);
                        continue;
                    }

                    yield return new ResourceMapping(domResource);
                }
            }
        }

        private struct ElementFunctionMapping
        {
            public Guid FunctionDefinitionId { get; set; }

            public DmsElementId ElementInfo { get; set; }
        }
    }
}
