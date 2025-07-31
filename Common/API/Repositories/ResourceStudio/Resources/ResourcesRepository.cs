namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal class ResourcesRepository : RepositoryBase<Resource>, IResourcesRepository
    {
        public ResourcesRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        public ElementResource ConvertToElementResource(Resource resource, ResourceElementLinkConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        public ElementResource ConvertToElementResource(Guid resourceId, ResourceElementLinkConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        public ServiceResource ConvertToServiceResource(Resource resource, ResourceServiceLinkConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        public ServiceResource ConvertToServiceResource(Guid resourceId, ResourceServiceLinkConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        public UnmanagedResource ConvertToUnmanagedResource(Resource resource)
        {
            throw new NotImplementedException();
        }

        public UnmanagedResource ConvertToUnmanagedResource(Guid resourceId)
        {
            throw new NotImplementedException();
        }

        public VirtualFunctionResource ConvertToVirtualFunctionResource(Resource resource, ResourceVirtualFunctionLinkConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        public VirtualFunctionResource ConvertToVirtualFunctionResource(Guid resourceId, ResourceVirtualFunctionLinkConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        public Guid Create(Resource apiObject)
        {
            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            PlanApi.Logger.LogInformation($"Creating new Resource {apiObject.Name}...");

            return ActivityHelper.Track(nameof(ResourcePoolsRepository), nameof(Create), act =>
            {
                if (!apiObject.IsNew)
                {
                    throw new MediaOpsException("Not possible to use method Create for existing resources. Use CreateOrUpdate or Update instead.");
                }

                if (!DomResourceHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var resourceId = result.SuccessfulIds.First();
                act.AddTag("ResourceId", resourceId);

                return resourceId;
            });
        }

        public IEnumerable<Guid> Create(IEnumerable<Resource> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            return ActivityHelper.Track(nameof(ResourcesRepository), nameof(Create), act =>
            {
                var existingResources = apiObjects.Where(x => !x.IsNew);
                if (existingResources.Any())
                {
                    throw new MediaOpsException("Not possible to use method Create for existing resources. Use CreateOrUpdate or Update instead.");
                }

                if (!DomResourceHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var resourceIds = result.SuccessfulIds;
                act.AddTag("ResourceIds", String.Join(", ", resourceIds));

                return resourceIds;
            });
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<Resource> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            return ActivityHelper.Track(nameof(ResourcesRepository), nameof(CreateOrUpdate), act =>
            {
                if (!DomResourceHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var resourceIds = result.SuccessfulIds;
                act.AddTag("Created Resources", String.Join(", ", resourceIds));
                act.AddTag("Created Resources Count", resourceIds.Count);

                return resourceIds;
            });
        }

        public void Delete(params Resource[] apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            Delete(apiObjects.Select(x => x.Id).ToArray());
        }

        public void Delete(params Guid[] apiObjectIds)
        {
            ActivityHelper.Track(nameof(ResourcesRepository), nameof(Delete), act =>
            {
                var resourcesToDelete = Read(apiObjectIds).Values;

                if (!DomResourceHandler.TryDelete(PlanApi, resourcesToDelete, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var resourceIds = result.SuccessfulIds;
                act.AddTag("Removed Resources", String.Join(", ", resourceIds));
                act.AddTag("Removed Resources Count", resourceIds.Count);
            });
        }

        public void MoveTo(Resource resource, ResourceState desiredState)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            MoveTo(resource.Id, desiredState);
        }

        public void MoveTo(Guid resourceId, ResourceState desiredState)
        {
            PlanApi.Logger.LogInformation("Moving Resource {resourceId} to {desiredState}...", resourceId, desiredState);

            if (resourceId == Guid.Empty)
            {
                throw new ArgumentException("Resource ID cannot be empty.", nameof(resourceId));
            }

            var resource = Read(resourceId);
            if (resource == null)
            {
                throw new MediaOpsException($"Unable to find resource with ID {resourceId}");
            }

            ActivityHelper.Track(nameof(ResourcesRepository), nameof(MoveTo), act =>
            {
                act.AddTag("ResourceId", resourceId);
                act.AddTag("DesiredState", desiredState);

                var actionMethods = new Dictionary<ResourceState, Action<Resource>>
                {
                    [ResourceState.Complete] = HandleMoveToCompleteAction,
                    [ResourceState.Deprecated] = HandleMoveToDeprecatedAction,
                };

                if (!actionMethods.TryGetValue(desiredState, out var action))
                {
                    throw new MediaOpsException($"Move to state '{desiredState}' is not supported.");
                }

                action(resource);
            });
        }

        private void HandleMoveToCompleteAction(Resource resource)
        {
            if (resource.State == ResourceState.Complete)
            {
                PlanApi.Logger.LogInformation("Resource {resource.Id} is already in Complete state. No action taken.", resource.Id);
                return;
            }

            if (resource.State != ResourceState.Draft)
            {
                throw new MediaOpsException("A resource can only be completed from Draft State");
            }

            ActivityHelper.Track(nameof(ResourcesRepository), nameof(HandleMoveToCompleteAction), act =>
            {
                act.AddTag("ResourceId", resource.Id);
                act.AddTag("ResourceName", resource.Name);

                DomResourceHandler.TransitionToComplete(PlanApi, resource);
            });
        }

        private void HandleMoveToDeprecatedAction(Resource resource)
        {
            if (resource.State == ResourceState.Deprecated)
            {
                PlanApi.Logger.LogInformation("Resource {resource.Id} is already in Deprecated state. No action taken.", resource.Id);
                return;
            }

            if (resource.State != ResourceState.Complete)
            {
                throw new MediaOpsException("A resource can only be deprecated from Complete State");
            }

            ActivityHelper.Track(nameof(ResourcesRepository), nameof(HandleMoveToDeprecatedAction), act =>
            {
                act.AddTag("ResourceId", resource.Id);
                act.AddTag("ResourceName", resource.Name);

                DomResourceHandler.TransitionToDeprecated(PlanApi, resource);
            });
        }

        public Resource Read(Guid id)
        {
            PlanApi.Logger.LogInformation($"Reading Resource with ID: {id}...");

            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return ActivityHelper.Track<Resource>(nameof(ResourcesRepository), nameof(Read), act =>
            {
                act.AddTag("ResourceId", id);
                var filter = DomInstanceExposers.DomDefinitionId.Equal(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Definitions.Resource.Id)
                        .AND(DomInstanceExposers.Id.Equal(id));
                var domResource = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResources(filter)
                    .FirstOrDefault();

                if (domResource == null)
                {
                    act.AddTag("Hit", false);
                    return null;
                }

                act.AddTag("Hit", true);

                return Resource.InstantiateResources([domResource]).First();
            });
        }

        public IDictionary<Guid, Resource> Read(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            return ActivityHelper.Track(nameof(ResourcesRepository), nameof(Read), act =>
            {
                act.AddTag("ResourceIds", String.Join(", ", ids));
                act.AddTag("ResourceIds Count", ids.Count());

                var resources = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResources(ids);
                return Resource.InstantiateResources(resources).ToDictionary(x => x.Id);
            });
        }

        public IEnumerable<Resource> Read(FilterElement<Resource> filter)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Resource> ReadAll()
        {
            return ActivityHelper.Track(nameof(ResourcesRepository), nameof(ReadAll), act =>
            {
                var filter = DomInstanceExposers.DomDefinitionId.Equal(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Definitions.Resource.Id);
                return Resource.InstantiateResources(PlanApi.DomHelpers.SlcResourceStudioHelper.GetResources(filter));
            });
        }

        public IEnumerable<IEnumerable<Resource>> ReadAllPaged()
        {
            throw new NotImplementedException();
        }

        public bool TryConvertToElementResource(Resource resource, ResourceElementLinkConfiguration configuration, out ElementResource elementResource)
        {
            throw new NotImplementedException();
        }

        public bool TryConvertToElementResource(Guid resourceId, ResourceElementLinkConfiguration configuration, out ElementResource elementResource)
        {
            throw new NotImplementedException();
        }

        public bool TryConvertToServiceResource(Resource resource, ResourceServiceLinkConfiguration configuration, out ServiceResource serviceResource)
        {
            throw new NotImplementedException();
        }

        public bool TryConvertToServiceResource(Guid resourceId, ResourceServiceLinkConfiguration configuration, out ServiceResource serviceResource)
        {
            throw new NotImplementedException();
        }

        public bool TryConvertToUnmanagedResource(Resource resource, out UnmanagedResource unmanagedResource)
        {
            throw new NotImplementedException();
        }

        public bool TryConvertToUnmanagedResource(Guid guid, out UnmanagedResource unmanagedResource)
        {
            throw new NotImplementedException();
        }

        public bool TryConvertToVirtualFunctionResource(Resource resource, ResourceVirtualFunctionLinkConfiguration configuration, out VirtualFunctionResource virtualFunctionResource)
        {
            throw new NotImplementedException();
        }

        public bool TryConvertToVirtualFunctionResource(Guid resourceId, ResourceVirtualFunctionLinkConfiguration configuration, out VirtualFunctionResource virtualFunctionResource)
        {
            throw new NotImplementedException();
        }

        public void Update(Resource apiObject)
        {
            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            PlanApi.Logger.LogInformation($"Updating existing Resource {apiObject.Name}...");

            ActivityHelper.Track(nameof(ResourcesRepository), nameof(Update), act =>
            {
                if (apiObject.IsNew)
                {
                    throw new MediaOpsException("Not possible to use method Update for new resources. Use Create or CreateOrUpdate instead.");
                }

                if (!DomResourceHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var resourceId = result.SuccessfulIds.First();
                act.AddTag("ResourceId", resourceId);
            });
        }

        public void Update(IEnumerable<Resource> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(ResourcesRepository), nameof(Update), act =>
            {
                var newResources = apiObjects.Where(x => x.IsNew);
                if (newResources.Any())
                {
                    throw new MediaOpsException("Not possible to use method Update for new resources. Use Create or CreateOrUpdate instead.");
                }

                if (!DomResourceHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var resourceIds = result.SuccessfulIds;
                act.AddTag("ResourceIds", String.Join(", ", resourceIds));
            });
        }
    }
}
