namespace RT_MediaOps.Plan.RST
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Utils.Categories.API;
    using Skyline.DataMiner.Utils.Categories.API.Objects;

    internal class ResourceStudioObjectCreator : IDisposable
    {
        private readonly IMediaOpsPlanApi api;
        private readonly CategoriesApi catagoriesApi;

        private readonly HashSet<Guid> createdPoolIds = new HashSet<Guid>();

        private readonly HashSet<Guid> createdResourceIds = new HashSet<Guid>();

        private readonly HashSet<Guid> createdCapabilityIds = new HashSet<Guid>();

        private readonly HashSet<Guid> createdCapacityIds = new HashSet<Guid>();

        private readonly HashSet<Guid> createdConfigurationIds = new HashSet<Guid>();

        private readonly HashSet<Guid> createdPropertyIds = new HashSet<Guid>();

        private readonly HashSet<Guid> createdCategoryIds = new HashSet<Guid>();

        public ResourceStudioObjectCreator(IMediaOpsPlanApi api, CategoriesApi categoriesApi)
        {
            this.api = api ?? throw new ArgumentNullException(nameof(api));
            this.catagoriesApi = categoriesApi ?? throw new ArgumentNullException(nameof(categoriesApi));
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

            try
            {
                CategoriesCleanup();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        private void ResourcesCleanup()
        {
            var resources = api.Resources.Read(createdResourceIds.ToArray());

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
            var pools = api.ResourcePools.Read(createdPoolIds.ToArray());

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
            var capabilities = api.Capabilities.Read(createdCapabilityIds.ToArray());

            api.Capabilities.Delete(capabilities.ToArray());
        }

        private void CapacitiesCleanup()
        {
            var capacities = api.Capacities.Read(createdCapacityIds.ToArray());

            api.Capacities.Delete(capacities.ToArray());
        }

        private void ConfigurationsCleanup()
        {
            var configurations = api.Configurations.Read(createdConfigurationIds.ToArray());

            api.Configurations.Delete(configurations.ToArray());
        }

        private void PropertiesCleanup()
        {
            var properties = api.Properties.Read(createdPropertyIds.ToArray());

            api.Properties.Delete(properties.ToArray());
        }

        private void CategoriesCleanup()
        {
            var categories = catagoriesApi.Categories.Read(createdCategoryIds.ToArray()).Values;
            catagoriesApi.Categories.Delete(categories);
        }

        public void CreateResource(Resource resource)
        {
            api.Resources.Create(resource);
            createdResourceIds.Add(resource.Id);
        }

        public void CreateResources(IEnumerable<Resource> resources)
        {
            try
            {
                api.Resources.Create(resources);

                foreach (var id in resources.Select(x => x.Id))
                {
                    createdResourceIds.Add(id);
                }
            }
            catch (MediaOpsBulkException<Guid> bulkException)
            {
                foreach (var id in bulkException.Result.SuccessfulIds)
                {
                    createdResourceIds.Add(id);
                }

                throw;
            }
        }

        public void CreateResourcePool(ResourcePool resourcePool)
        {
            api.ResourcePools.Create(resourcePool);
            createdPoolIds.Add(resourcePool.Id);
        }

        public void CreateResourcePools(IEnumerable<ResourcePool> resourcePools)
        {
            try
            {
                api.ResourcePools.Create(resourcePools);

                foreach (var id in resourcePools.Select(x => x.Id))
                {
                    createdPoolIds.Add(id);
                }
            }
            catch (MediaOpsBulkException<Guid> bulkException)
            {
                foreach (var id in bulkException.Result.SuccessfulIds)
                {
                    createdPoolIds.Add(id);
                }

                throw;
            }
        }

        public void CreateCapability(Capability capability)
        {
            api.Capabilities.Create(capability);
            createdCapabilityIds.Add(capability.Id);
        }

        public void CreateCapabilities(IEnumerable<Capability> capabilities)
        {
            try
            {
                api.Capabilities.Create(capabilities);

                foreach (var id in capabilities.Select(x => x.Id))
                {
                    createdCapabilityIds.Add(id);
                }
            }
            catch (MediaOpsBulkException<Guid> bulkException)
            {
                foreach (var id in bulkException.Result.SuccessfulIds)
                {
                    createdCapabilityIds.Add(id);
                }

                throw;
            }
        }

        public void CreateCapacity(Capacity capacity)
        {
            api.Capacities.Create(capacity);
            createdCapacityIds.Add(capacity.Id);
        }

        public void CreateCapacities(IEnumerable<Capacity> capacities)
        {
            try
            {
                api.Capacities.Create(capacities);

                foreach (var id in capacities.Select(x => x.Id))
                {
                    createdCapacityIds.Add(id);
                }
            }
            catch (MediaOpsBulkException<Guid> bulkException)
            {
                foreach (var id in bulkException.Result.SuccessfulIds)
                {
                    createdCapacityIds.Add(id);
                }

                throw;
            }
        }

        public void CreateConfiguration(Configuration configuration)
        {
            api.Configurations.Create(configuration);
            createdConfigurationIds.Add(configuration.Id);
        }

        public void CreateConfigurations(IEnumerable<Configuration> configurations)
        {
            try
            {
                api.Configurations.Create(configurations);

                foreach (var id in configurations.Select(x => x.Id))
                {
                    createdConfigurationIds.Add(id);
                }
            }
            catch (MediaOpsBulkException<Guid> bulkException)
            {
                foreach (var id in bulkException.Result.SuccessfulIds)
                {
                    createdConfigurationIds.Add(id);
                }

                throw;
            }
        }

        public void CreateProperty(ResourceProperty property)
        {
            api.Properties.Create(property);
            createdPropertyIds.Add(property.Id);
        }

        public void CreateProperties(IEnumerable<ResourceProperty> properties)
        {
            try
            {
                api.Properties.Create(properties);

                foreach (var id in properties.Select(x => x.Id))
                {
                    createdPropertyIds.Add(id);
                }
            }
            catch (MediaOpsBulkException<Guid> bulkException)
            {
                foreach (var id in bulkException.Result.SuccessfulIds)
                {
                    createdPropertyIds.Add(id);
                }

                throw;
            }
        }

        public Category CreateCategory(Category category)
        {
            category = catagoriesApi.Categories.Create(category);
            createdCategoryIds.Add(category.ID);
            return category;
        }
    }
}
