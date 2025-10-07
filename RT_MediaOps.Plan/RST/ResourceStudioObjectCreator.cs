namespace RT_MediaOps.Plan.RST
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.MediaOps.Plan.API;

    internal class ResourceStudioObjectCreator : IDisposable
    {
        private readonly IMediaOpsPlanApi api;

        private readonly HashSet<Guid> createdPoolIds = new HashSet<Guid>();

        public ResourceStudioObjectCreator(IMediaOpsPlanApi api)
        {
            this.api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public void Dispose()
        {
            try
            {
                ResourcePoolsCleanup();
            }
            catch (Exception)
            {
                // Ignore cleanup errors
            }
        }

        private void ResourcePoolsCleanup()
        {
            var pools = api.ResourcePools.Read(createdPoolIds.ToArray()).Values;

            foreach (var pool in pools.Where(p => p.State == ResourcePoolState.Complete))
            {
                try
                {
                    api.ResourcePools.MoveTo(pool.Id, ResourcePoolState.Deprecated);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            api.ResourcePools.Delete(createdPoolIds.ToArray());
        }

        public Guid CreateResourcePool(Skyline.DataMiner.MediaOps.Plan.API.ResourcePool resourcePool)
        {
            var poolId = api.ResourcePools.Create(resourcePool);
            createdPoolIds.Add(poolId);

            return poolId;
        }

        public IEnumerable<Guid> CreateResourcePools(IEnumerable<Skyline.DataMiner.MediaOps.Plan.API.ResourcePool> resourcePools)
        {
            var poolIds = api.ResourcePools.Create(resourcePools);
            foreach (var id in poolIds)
            {
                createdPoolIds.Add(id);
            }
            return poolIds;
        }
    }
}
