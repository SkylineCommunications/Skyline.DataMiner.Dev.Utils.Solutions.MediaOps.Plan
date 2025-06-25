namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    public interface IResourcePoolsRepository : ICrudRepository<ResourcePool>
    {
        void MoveTo(ResourcePool resourcePool, ResourcePoolState desiredState);

        void MoveTo(Guid resourcePoolId, ResourcePoolState desiredState);
    }
}
