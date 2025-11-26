namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    internal partial class ResourcePoolsRepository : DomRepository<ResourcePool>, IResourcePoolsRepository
    {
        public Guid Create(ResourcePool apiObject)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public IEnumerable<Guid> Create(IEnumerable<ResourcePool> apiObjects)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<ResourcePool> apiObjects)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Deprecate(ResourcePool resourcePool, ResourcePoolDeprecateOptions options)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Delete(params ResourcePool[] apiObjects)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Delete(params Guid[] apiObjectIds)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Delete(ResourcePool resourcePool, ResourcePoolDeleteOptions options)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void MoveTo(ResourcePool resourcePool, ResourcePoolState desiredState)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void MoveTo(Guid resourcePoolId, ResourcePoolState desiredState)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Update(ResourcePool apiObject)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Update(IEnumerable<ResourcePool> apiObjects)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }
    }
}
