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

        private readonly HashSet<Guid> createdCapacityIds = new HashSet<Guid>();

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

            try
            {
                CapacitiesCleanup();
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

        private void CapacitiesCleanup()
        {
            var capacities = api.Capacities.Read(createdCapacityIds.ToArray()).Values;

            api.Capacities.Delete(capacities.ToArray());
        }

        public Guid CreateResourcePool(ResourcePool resourcePool)
        {
            var poolId = api.ResourcePools.Create(resourcePool);
            createdPoolIds.Add(poolId);

            return poolId;
        }

        public IEnumerable<Guid> CreateResourcePools(IEnumerable<ResourcePool> resourcePools)
        {
            var poolIds = api.ResourcePools.Create(resourcePools);
            foreach (var id in poolIds)
            {
                createdPoolIds.Add(id);
            }

            return poolIds;
        }

        public Guid CreateCapacity(Capacity capacity)
        {
            var capacityId = api.Capacities.Create(capacity);
            createdCapacityIds.Add(capacityId);

            return capacityId;
        }

        public IEnumerable<Guid> CreateCapacities(IEnumerable<Capacity> capacities)
        {
            var capacityIds = api.Capacities.Create(capacities);
            foreach (var id in capacityIds)
            {
                createdCapacityIds.Add(id);
            }

            return capacityIds;
        }
    }
}
