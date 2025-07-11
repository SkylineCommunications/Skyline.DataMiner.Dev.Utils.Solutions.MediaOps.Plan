namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.Core.DataMinerSystem.Common.Selectors;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.MediaOps.Plan.Storage.Core;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.PerformanceIndication;

    using CoreResource = Skyline.DataMiner.Net.Messages.Resource;
    using DomResource = Storage.DOM.SlcResource_Studio.ResourceInstance;

    internal class CoreResourceHandler
    {
        private readonly MediaOpsPlanApi planApi;

        private readonly List<Guid> successfulIds = new List<Guid>();
        private readonly List<Guid> unsuccessfulIds = new List<Guid>();
        private readonly Dictionary<Guid, MediaOpsTraceData> traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();

        private CoreResourceHandler(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

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
            handler.CreateOrUpdate(domResources);

            result = new BulkCreateOrUpdateResult<Guid>(handler.successfulIds, handler.unsuccessfulIds, handler.traceDataPerItem);

            return !result.HasFailures();
        }

        public static BulkDeleteResult<Guid> Delete(MediaOpsPlanApi planApi, IEnumerable<DomResource> domResources)
        {
            var handler = new CoreResourceHandler(planApi);
            handler.Delete(domResources);

            var result = new BulkDeleteResult<Guid>(handler.successfulIds, handler.unsuccessfulIds, handler.traceDataPerItem);
            result.ThrowOnFailure();

            return result;
        }

        public static bool TryDelete(MediaOpsPlanApi planApi, IEnumerable<DomResource> domResources, out BulkDeleteResult<Guid> result)
        {
            var handler = new CoreResourceHandler(planApi);
            handler.Delete(domResources);

            result = new BulkDeleteResult<Guid>(handler.successfulIds, handler.unsuccessfulIds, handler.traceDataPerItem);

            return !result.HasFailures();
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

            var configMapping = new Dictionary<Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type, Action<CoreResource, DomResource>>
            {
                [Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.Unmanaged] = ApplyUnmanagedResourceConfig,
                [Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.Element] = ApplyElementResourceConfig,
                [Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.Service] = ApplyServiceResourceConfig,
                [Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.VirtualFunction] = ApplyVirtualFunctionResourceConfig,
            };

            var resourcesToCreateOrUpdate = new List<CoreResource>();
            foreach (var mapping in resourceMappings)
            {
                var dom = mapping.DomResource;
                var core = mapping.CoreResource ?? BuildCoreResource(dom.ResourceInfo.Type.Value);

                core.Name = dom.ResourceInfo.Name;
                core.MaxConcurrency = (int)dom.ResourceInfo.Concurrency;

                configMapping[dom.ResourceInfo.Type.Value].Invoke(core, dom);

                resourcesToCreateOrUpdate.Add(core);

                domResourcesById.Add(dom.ID.Id, dom);
                domIdByCoreId.Add(core.ID, dom.ID.Id);
            }

            planApi.CoreHelpers.ResourceManagerHelper.TryCreateOrUpdateResourcesInBatches(resourcesToCreateOrUpdate, out var result);

            foreach (var id in unsuccessfulIds)
            {
                if (!domIdByCoreId.TryGetValue(id, out var domId))
                {
                    planApi.Logger.Error(this, $"Failed to find DOM ID for CORE resource ID {id}.");
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
                    planApi.Logger.Error(this, $"Failed to find DOM ID for CORE resource pool ID {id}.");
                    continue;
                }

                domResourcesById[domId].ResourceInternalProperties.Resource_Id = id;

                successfulIds.Add(domId);
            }
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

                if (!domResource.ResourceInternalProperties.Resource_Id.HasValue
                    || domResource.ResourceInternalProperties.Resource_Id.Value == Guid.Empty)
                {
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
                    planApi.Logger.Error(this, $"Failed to find DOM ID for CORE resource ID {id}.");
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
                    planApi.Logger.Error(this, $"Failed to find DOM ID for CORE resource ID {id}.");
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

        private void ApplyUnmanagedResourceConfig(CoreResource coreResource, DomResource domResource)
        {
            SetResourceType(coreResource, "Unlinked Resource");
        }

        private void ApplyElementResourceConfig(CoreResource coreResource, DomResource domResource)
        {
            var elementInfo = new DmsElementId(domResource.ResourceInternalProperties.Metadata.LinkedElementInfo);
            coreResource.DmaID = elementInfo.AgentId;
            coreResource.ElementID = elementInfo.ElementId;

            SetResourceType(coreResource, "Element");
        }

        private void ApplyServiceResourceConfig(CoreResource coreResource, DomResource domResource)
        {
            var serviceLinkProperty = coreResource.Properties.FirstOrDefault(x => x.Name == "Service Link");
            if (serviceLinkProperty == null)
            {
                serviceLinkProperty = new Net.Messages.ResourceManagerProperty("Service Link", string.Empty);
                coreResource.Properties.Add(serviceLinkProperty);
            }

            serviceLinkProperty.Value = domResource.ResourceInternalProperties.Metadata.LinkedServiceInfo;

            SetResourceType(coreResource, "Service");
        }

        private void ApplyVirtualFunctionResourceConfig(CoreResource coreResource, DomResource domResource)
        {
            if (domResource.Status != Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum.Draft)
            {
                return;
            }

            if (coreResource is not Net.ResourceManager.Objects.FunctionResource functionResource)
            {
                return;
            }

            var functionDefinition = planApi.CoreHelpers.ProtocolFunctionHelperCache.GetFunctionDefinition(domResource.ResourceInternalProperties.Metadata.LinkedFunctionId);
            functionResource.FunctionGUID = functionDefinition.GUID;

            var elementInfo = new DmsElementId(domResource.ResourceInternalProperties.Metadata.LinkedElementInfo);
            functionResource.MainDVEDmaID = elementInfo.AgentId;
            functionResource.MainDVEElementID = elementInfo.ElementId;

            if (functionDefinition.EntryPoints.Any())
            {
                functionResource.LinkerTableEntries = new[] { new Tuple<int, string>(functionDefinition.EntryPoints.First().ParameterId, domResource.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex) };
            }

            SetResourceType(coreResource, "Virtual Function");

            throw new NotImplementedException();
        }

        private void SetResourceType(CoreResource resource, string resourceTypeValue)
        {
            // Todo: implement when capability repository is available
            throw new NotImplementedException();
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
                var error = new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.DuplicateName,
                    ErrorMessage = $"Resource '{resource.ResourceInfo.Name}' has a duplicate name.",
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

                var existingResources = coreResources.Where(x => x.ID != resource.ResourceInternalProperties.Resource_Id.Value).ToList();
                if (existingResources.Count == 0)
                {
                    continue;
                }

                planApi.Logger.Information(this, $"Name '{resource.ResourceInfo.Name}' is already in use by CORE resource(s) with ID(s): {string.Join(" ,", existingResources.Select(x => x.ID))}");

                var error = new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.NameExists,
                    ErrorMessage = "Name is already in use.",
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

            if (!domResources.Any())
            {
                return;
            }

            var domResourcesByElementInfo = domResources.ToDictionary(x => new DmsElementId(x.ResourceInternalProperties.Metadata.LinkedElementInfo));
            var elementsByElementInfo = planApi.CoreHelpers.DmsCache.GetElements(domResourcesByElementInfo.Keys);

            foreach (var kvp in domResourcesByElementInfo)
            {
                if (!elementsByElementInfo.TryGetValue(kvp.Key, out var element))
                {
                    var error = new ResourceConfigurationError
                    {
                        ErrorReason = ResourceConfigurationError.Reason.InvalidElementLink,
                        ErrorMessage = $"No element found with ID '{kvp.Value.ResourceInternalProperties.Metadata.LinkedElementInfo}'.",
                    };
                    AddError(kvp.Value.ID.Id, error);

                    continue;
                }

                if (element.FunctionSettings.IsFunctionElement)
                {
                    var error = new ResourceConfigurationError
                    {
                        ErrorReason = ResourceConfigurationError.Reason.InvalidElementLink,
                        ErrorMessage = $"Element '{element.Name}' is a function element and cannot be linked to a resource.",
                    };
                    AddError(kvp.Value.ID.Id, error);
                }
            }
        }

        private void ValidateServiceResources(IEnumerable<DomResource> domResources)
        {
            if (domResources == null)
            {
                throw new ArgumentNullException(nameof(domResources));
            }

            if (!domResources.Any())
            {
                return;
            }

            var domResourcesByServiceInfo = domResources.ToDictionary(x => new DmsServiceId(x.ResourceInternalProperties.Metadata.LinkedServiceInfo));
            var servicesByServiceInfo = planApi.CoreHelpers.DmsCache.GetServices(domResourcesByServiceInfo.Keys);

            foreach (var kvp in domResourcesByServiceInfo.Where(x => !servicesByServiceInfo.ContainsKey(x.Key)))
            {
                var error = new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.InvalidServiceLink,
                    ErrorMessage = $"No service found with ID '{kvp.Value.ResourceInternalProperties.Metadata.LinkedServiceInfo}'.",
                };
                AddError(kvp.Value.ID.Id, error);
            }
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
            var domResourcesByElementInfo = domResources
                .GroupBy(x => new DmsElementId(x.ResourceInternalProperties.Metadata.LinkedElementInfo))
                .ToDictionary(x => x.Key, x => x.ToList());
            var elementsByElementInfo = planApi.CoreHelpers.DmsCache.GetElements(domResourcesByElementInfo.Keys);

            foreach (var kvp in domResourcesByElementInfo.ToList())
            {
                if (!elementsByElementInfo.TryGetValue(kvp.Key, out var element))
                {
                    var error = new ResourceConfigurationError
                    {
                        ErrorReason = ResourceConfigurationError.Reason.InvalidElementLink,
                        ErrorMessage = $"No element found with ID '{kvp.Key.Value}'.",
                    };
                    AddError(kvp.Value, error);

                    domResourcesByElementInfo.Remove(kvp.Key);
                    continue;
                }

                if (element.FunctionSettings.IsFunctionElement)
                {
                    var error = new ResourceConfigurationError
                    {
                        ErrorReason = ResourceConfigurationError.Reason.InvalidElementLink,
                        ErrorMessage = $"Element '{element.Name}' is a function element and cannot be linked to a resource.",
                    };
                    AddError(kvp.Value, error);

                    domResourcesByElementInfo.Remove(kvp.Key);
                }
            }

            // Validate functions
            var domResourcesByFunctionId = domResourcesByElementInfo.Values.SelectMany(x => x)
                .GroupBy(x => x.ResourceInternalProperties.Metadata.LinkedFunctionId)
                .ToDictionary(x => x.Key, x => x.ToList());
            var functionDefinitionsById = planApi.CoreHelpers.ProtocolFunctionHelperCache.GetFunctionDefinitions(domResourcesByFunctionId.Keys);

            foreach (var kvp in domResourcesByFunctionId.Where(x => !functionDefinitionsById.ContainsKey(x.Key)).ToList())
            {
                var error = new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.InvalidFunctionLink,
                    ErrorMessage = $"No function found with ID '{kvp.Key}'.",
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
                    var error = new ResourceConfigurationError
                    {
                        ErrorReason = ResourceConfigurationError.Reason.DuplicateTableIndexLink,
                        ErrorMessage = $"Resource '{resource.ResourceInfo.Name}' has a duplicate table index '{resource.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex}'.",
                    };
                    AddError(resource.ID.Id, error);

                    resourcesRequiringValidation.Remove(resource);
                }

                var entryPoints = planApi.CoreHelpers.ProtocolFunctionHelperCache.GetElementFunctionEntryPoints(kvp.Key.FunctionDefinitionId, kvp.Key.ElementInfo, returnAvailableOnly: true);
                foreach (var resource in resourcesRequiringValidation.Where(x => !entryPoints.Any(y => y.IndexValue == x.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex)))
                {
                    var error = new ResourceConfigurationError
                    {
                        ErrorReason = ResourceConfigurationError.Reason.InvalidTableIndexLink,
                        ErrorMessage = $"Resource '{resource.ResourceInfo.Name}' has an invalid table index '{resource.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex}'.",
                    };
                    AddError(resource.ID.Id, error);
                }
            }
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

        private class ResourceMapping
        {
            private ResourceMapping(DomResource domResource)
            {
                DomResource = domResource ?? throw new ArgumentNullException(nameof(domResource));
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
