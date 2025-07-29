namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.MediaOps.Plan.API.Handlers;
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

            return ActivityHelper.Track(nameof(Create), act =>
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

            return ActivityHelper.Track(nameof(Create), act =>
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

            return ActivityHelper.Track(nameof(CreateOrUpdate), act =>
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

            ActivityHelper.Track(nameof(Delete), act =>
            {
                if (!DomResourceHandler.TryDelete(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var resourceIds = result.SuccessfulIds;
                act.AddTag("Removed Resources", String.Join(", ", resourceIds));
                act.AddTag("Removed Resources Count", resourceIds.Count);
            });
        }

        public void Delete(params Guid[] apiObjectIds)
        {
            var resourcesToDelete = Read(apiObjectIds).Values;
            Delete(resourcesToDelete.ToArray());
        }

        public void MoveTo(Resource resource, ResourceState desiredState)
        {
            throw new NotImplementedException();
        }

        public void MoveTo(Guid resourceId, ResourceState desiredState)
        {
            throw new NotImplementedException();
        }

        public Resource Read(Guid id)
        {
            PlanApi.Logger.LogInformation($"Reading Resource with ID: {id}...");

            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return ActivityHelper.Track<Resource>(nameof(Read), act =>
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

            return ActivityHelper.Track(nameof(Read), act =>
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
            return ActivityHelper.Track(nameof(Read), act =>
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
            throw new NotImplementedException();
        }

        public void Update(IEnumerable<Resource> apiObjects)
        {
            throw new NotImplementedException();
        }
    }
}
