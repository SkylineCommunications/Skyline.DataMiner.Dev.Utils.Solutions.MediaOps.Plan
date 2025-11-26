namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.InterApp.Messages;

    internal partial class ResourcesRepository : DomRepository<Resource>, IResourcesRepository
    {
        public ElementResource ConvertToElementResource(Resource resource, ResourceElementLinkConfiguration configuration)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public ElementResource ConvertToElementResource(Guid resourceId, ResourceElementLinkConfiguration configuration)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public ServiceResource ConvertToServiceResource(Resource resource, ResourceServiceLinkConfiguration configuration)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public ServiceResource ConvertToServiceResource(Guid resourceId, ResourceServiceLinkConfiguration configuration)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public UnmanagedResource ConvertToUnmanagedResource(Resource resource)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public UnmanagedResource ConvertToUnmanagedResource(Guid resourceId)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public VirtualFunctionResource ConvertToVirtualFunctionResource(Resource resource, ResourceVirtualFunctionLinkConfiguration configuration)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public VirtualFunctionResource ConvertToVirtualFunctionResource(Guid resourceId, ResourceVirtualFunctionLinkConfiguration configuration)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public Guid Create(Resource apiObject)
        {
            var request = new CreateResourceRequest
            {
                Resources = new[] { apiObject },
            };

            var response = PlanApi.SendMessage<CreateResourceResponse>(request);
            return response.ResourceIds.First();
        }

        public IEnumerable<Guid> Create(IEnumerable<Resource> apiObjects)
        {
            var request = new CreateResourceRequest
            {
                Resources = apiObjects.ToArray(),
            };

            var response = PlanApi.SendMessage<CreateResourceResponse>(request);
            return response.ResourceIds;
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<Resource> apiObjects)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Deprecate(IEnumerable<Resource> resources)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Delete(params Resource[] apiObjects)
        {
            var request = new DeleteResourceRequest
            {
                ResourceIds = apiObjects.Select(x => x.Id).ToArray()
            };

            PlanApi.SendMessage<DeleteResourceResponse>(request);
        }

        public void Delete(params Guid[] apiObjectIds)
        {
            var request = new DeleteResourceRequest
            {
                ResourceIds = apiObjectIds
            };

            PlanApi.SendMessage<DeleteResourceResponse>(request);
        }

        public void MoveTo(Resource resource, ResourceState desiredState)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void MoveTo(Guid resourceId, ResourceState desiredState)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Update(Resource apiObject)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Update(IEnumerable<Resource> apiObjects)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }
    }
}
