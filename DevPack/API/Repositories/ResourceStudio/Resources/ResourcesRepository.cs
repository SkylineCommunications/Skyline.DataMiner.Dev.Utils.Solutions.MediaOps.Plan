namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    internal partial class ResourcesRepository : DomRepository<Resource>, IResourcesRepository
    {
        public ElementResource ConvertToElementResource(Resource resource, ResourceElementLinkConfiguration configuration)
        {
            if (resource.State != ResourceState.Draft)
            {
                throw new MediaOpsException($"Resource {resource.Name} is not in Draft state. Cannot convert to ElementResource.");
            }

            if (resource is ElementResource elementResource)
            {
                return elementResource;
            }

            DomResourceHandler.ConvertToElementResource(PlanApi, resource, configuration);
            return (ElementResource)Read(resource.Id);
        }

        public ElementResource ConvertToElementResource(Guid resourceId, ResourceElementLinkConfiguration configuration)
        {
            var resource = Read(resourceId) ?? throw new ResourceNotFoundException(resourceId);
            return ConvertToElementResource(resource, configuration);
        }

        public ServiceResource ConvertToServiceResource(Resource resource, ResourceServiceLinkConfiguration configuration)
        {
            if (resource.State != ResourceState.Draft)
            {
                throw new MediaOpsException($"Resource {resource.Name} is not in Draft state. Cannot convert to ElementResource.");
            }

            if (resource is ServiceResource serviceResource)
            {
                return serviceResource;
            }

            DomResourceHandler.ConvertToServiceResource(PlanApi, resource, configuration);
            return (ServiceResource)Read(resource.Id);
        }

        public ServiceResource ConvertToServiceResource(Guid resourceId, ResourceServiceLinkConfiguration configuration)
        {
            var resource = Read(resourceId) ?? throw new ResourceNotFoundException(resourceId);
            return ConvertToServiceResource(resource, configuration);
        }

        public UnmanagedResource ConvertToUnmanagedResource(Resource resource)
        {
            if (resource.State != ResourceState.Draft)
            {
                throw new MediaOpsException($"Resource {resource.Name} is not in Draft state. Cannot convert to ElementResource.");
            }

            if (resource is UnmanagedResource unmanagedResource)
            {
                return unmanagedResource;
            }

            DomResourceHandler.ConvertToUnmanagedResource(PlanApi, resource);
            return (UnmanagedResource)Read(resource.Id);
        }

        public UnmanagedResource ConvertToUnmanagedResource(Guid resourceId)
        {
            var resource = Read(resourceId) ?? throw new ResourceNotFoundException(resourceId);
            return ConvertToUnmanagedResource(resource);
        }

        public VirtualFunctionResource ConvertToVirtualFunctionResource(Resource resource, ResourceVirtualFunctionLinkConfiguration configuration)
        {
            if (resource.State != ResourceState.Draft)
            {
                throw new MediaOpsException($"Resource {resource.Name} is not in Draft state. Cannot convert to ElementResource.");
            }

            if (resource is VirtualFunctionResource virtualFunctionResource)
            {
                return virtualFunctionResource;
            }

            DomResourceHandler.ConvertToVirtualFunctionResource(PlanApi, resource, configuration);
            return (VirtualFunctionResource)Read(resource.Id);
        }

        public VirtualFunctionResource ConvertToVirtualFunctionResource(Guid resourceId, ResourceVirtualFunctionLinkConfiguration configuration)
        {
            var resource = Read(resourceId) ?? throw new ResourceNotFoundException(resourceId);
            return ConvertToVirtualFunctionResource(resource, configuration);
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
                    throw new InvalidOperationException("Not possible to use method Create for existing resources. Use CreateOrUpdate or Update instead.");
                }

                if (!DomResourceHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var resourceId = result.SuccessfulIds.First();
                act?.AddTag("ResourceId", resourceId);

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
                    throw new InvalidOperationException("Not possible to use method Create for existing resources. Use CreateOrUpdate or Update instead.");
                }

                if (!DomResourceHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var resourceIds = result.SuccessfulIds;
                act?.AddTag("ResourceIds", String.Join(", ", resourceIds));

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
                act?.AddTag("Created or Updated Resources", String.Join(", ", resourceIds));
                act?.AddTag("Created or Updated Resources Count", resourceIds.Count);

                return resourceIds;
            });
        }

        public void Deprecate(IEnumerable<Resource> resources)
        {
            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            ActivityHelper.Track(nameof(ResourcesRepository), nameof(Deprecate), act =>
            {
                if (!DomResourceHandler.TryDeprecate(PlanApi, resources, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var resourceIds = result.SuccessfulIds;
                act?.AddTag("Deprecated Resources", String.Join(", ", resourceIds));
                act?.AddTag("Deprecated Resources Count", resourceIds.Count);
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
            if (apiObjectIds == null)
            {
                throw new ArgumentNullException(nameof(apiObjectIds));
            }

            PlanApi.Logger.LogInformation("Deleting Resources {resourceIds}...", String.Join(", ", apiObjectIds));

            var resourcesToDelete = Read(apiObjectIds).Values;

            ActivityHelper.Track(nameof(ResourcesRepository), nameof(Delete), act =>
            {
                if (!DomResourceHandler.TryDelete(PlanApi, resourcesToDelete, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var resourceIds = result.SuccessfulIds;
                act?.AddTag("Removed Resources", String.Join(", ", resourceIds));
                act?.AddTag("Removed Resources Count", resourceIds.Count);
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
                act?.AddTag("ResourceId", resourceId);
                act?.AddTag("DesiredState", desiredState);

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

            ActivityHelper.Track(nameof(DomResourceHandler), nameof(DomResourceHandler.TransitionToComplete), act =>
            {
                act?.AddTag("ResourceId", resource.Id);
                act?.AddTag("ResourceName", resource.Name);

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
                act?.AddTag("ResourceId", resource.Id);
                act?.AddTag("ResourceName", resource.Name);

                if (!DomResourceHandler.TryDeprecate(PlanApi, [resource], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[resource.Id]);
                }
            });
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
                if (!apiObject.HasChanges)
                {
                    act?.AddTag("NoChanges", true);
                    return;
                }

                if (apiObject.IsNew)
                {
                    throw new InvalidOperationException("Not possible to use method Update for new resources. Use Create or CreateOrUpdate instead.");
                }

                if (!DomResourceHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var resourceId = result.SuccessfulIds.First();
                act?.AddTag("ResourceId", resourceId);
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
                act?.AddTag("ResourceIds", String.Join(", ", resourceIds));
            });
        }
    }
}
