namespace RT_MediaOps.Plan.RST
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

    internal class ResourceStudioObjectCreator : IDisposable
    {
        private readonly IMediaOpsPlanApi api;

        private readonly HashSet<Guid> createdPoolIds = new HashSet<Guid>();

        private readonly HashSet<Guid> createdResourceIds = new HashSet<Guid>();

        private readonly HashSet<Guid> createdCapabilityIds = new HashSet<Guid>();

        private readonly HashSet<Guid> createdCapacityIds = new HashSet<Guid>();

        private readonly HashSet<Guid> createdConfigurationIds = new HashSet<Guid>();

        private readonly HashSet<Guid> createdPropertyIds = new HashSet<Guid>();

        public ResourceStudioObjectCreator(IMediaOpsPlanApi api)
        {
            this.api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public void Dispose()
        {
            try
            {
                ResourcesCleanup();
            }
            catch
            {
                // Ignore cleanup errors
            }

            try
            {
                ResourcePoolsCleanup();
            }
            catch
            {
                // Ignore cleanup errors
            }

            try
            {
                CapabilitiesCleanup();
            }
            catch
            {
                // Ignore cleanup errors
            }

            try
            {
                CapacitiesCleanup();
            }
            catch
            {
                // Ignore cleanup errors
            }

            try
            {
                ConfigurationsCleanup();
            }
            catch
            {
                // Ignore cleanup errors
            }

            try
            {
                PropertiesCleanup();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        private void ResourcesCleanup()
        {
            var resources = api.Resources.Read(createdResourceIds.ToArray()).Values;

            foreach (var resource in resources.Where(r => r.State == ResourceState.Complete))
            {
                try
                {
                    api.Resources.MoveTo(resource.Id, ResourceState.Deprecated);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            api.Resources.Delete(createdResourceIds.ToArray());
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

        private void CapabilitiesCleanup()
        {
            var capabilities = api.Capabilities.Read(createdCapabilityIds.ToArray()).Values;

            api.Capabilities.Delete(capabilities.ToArray());
        }

        private void CapacitiesCleanup()
        {
            var capacities = api.Capacities.Read(createdCapacityIds.ToArray()).Values;

            api.Capacities.Delete(capacities.ToArray());
        }

        private void ConfigurationsCleanup()
        {
            var configurations = api.Configurations.Read(createdConfigurationIds.ToArray()).Values;

            api.Configurations.Delete(configurations.ToArray());
        }

        private void PropertiesCleanup()
        {
            var properties = api.Properties.Read(createdPropertyIds.ToArray()).Values;

            api.Properties.Delete(properties.ToArray());
        }

        public Guid CreateResource(Resource resource)
        {
            var resourceId = api.Resources.Create(resource);
            createdResourceIds.Add(resourceId);

            return resourceId;
        }

        public IEnumerable<Guid> CreateResources(IEnumerable<Resource> resources)
        {
            var resourceIds = api.Resources.Create(resources);
            foreach (var id in resourceIds)
            {
                createdResourceIds.Add(id);
            }

            return resourceIds;
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

        public Guid CreateCapability(Capability capability)
        {
            var capabilityId = api.Capabilities.Create(capability);
            createdCapabilityIds.Add(capabilityId);

            return capabilityId;
        }

        public IEnumerable<Guid> CreateCapabilities(IEnumerable<Capability> capabilities)
        {
            var capabilityIds = api.Capabilities.Create(capabilities);
            foreach (var id in capabilityIds)
            {
                createdCapabilityIds.Add(id);
            }

            return capabilityIds;
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

        public Guid CreateConfiguration(Configuration configuration)
        {
            var configurationId = api.Configurations.Create(configuration);
            createdConfigurationIds.Add(configurationId);

            return configurationId;
        }

        public IEnumerable<Guid> CreateConfigurations(IEnumerable<Configuration> configurations)
        {
            var configurationIds = api.Configurations.Create(configurations);
            foreach (var id in configurationIds)
            {
                createdConfigurationIds.Add(id);
            }

            return configurationIds;
        }

        public Guid CreateProperty(ResourceProperty property)
        {
            var propertyId = api.Properties.Create(property);
            createdPropertyIds.Add(propertyId);

            return propertyId;
        }

        public IEnumerable<Guid> CreateProperties(IEnumerable<ResourceProperty> properties)
        {
            var propertyIds = api.Properties.Create(properties);
            foreach (var id in propertyIds)
            {
                createdPropertyIds.Add(id);
            }

            return propertyIds;
        }
    }
}
