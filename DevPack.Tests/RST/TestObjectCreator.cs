namespace RT_MediaOps.Plan.RST
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Utils.Categories.API;
    using Skyline.DataMiner.Utils.Categories.API.Objects;

    internal class TestObjectCreator : IDisposable
    {
        private readonly IntegrationTestContext testContext;

        private readonly HashSet<Guid> createdPoolIds = new HashSet<Guid>();

        private readonly HashSet<Guid> createdResourceIds = new HashSet<Guid>();

        private readonly HashSet<Guid> createdCapabilityIds = new HashSet<Guid>();

        private readonly HashSet<Guid> createdCapacityIds = new HashSet<Guid>();

        private readonly HashSet<Guid> createdConfigurationIds = new HashSet<Guid>();

        private readonly HashSet<Guid> createdPropertyIds = new HashSet<Guid>();

        private readonly HashSet<Guid> createdCategoryIds = new HashSet<Guid>();

        private readonly HashSet<DmsElementId> createdElementIds = new HashSet<DmsElementId>();

        public TestObjectCreator(IntegrationTestContext testContext)
        {
            this.testContext = testContext ?? throw new ArgumentNullException(nameof(testContext));
        }

        private IMediaOpsPlanApi PlanApi => testContext.Api;

        private CategoriesApi CatagoriesApi => testContext.CategoriesApi;

        private IDms Dms => testContext.Dms;

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

            try
            {
                ElementsCleanup();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        private void ResourcesCleanup()
        {
            var resources = PlanApi.Resources.Read(createdResourceIds.ToArray());

            foreach (var resource in resources.Where(r => r.State == ResourceState.Complete))
            {
                try
                {
                    PlanApi.Resources.Deprecate(resource.Id);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            PlanApi.Resources.Delete(createdResourceIds.ToArray());
        }

        private void ResourcePoolsCleanup()
        {
            var pools = PlanApi.ResourcePools.Read(createdPoolIds.ToArray());

            foreach (var pool in pools.Where(p => p.State == ResourcePoolState.Complete))
            {
                try
                {
                    PlanApi.ResourcePools.Deprecate(pool.Id);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            PlanApi.ResourcePools.Delete(createdPoolIds.ToArray());
        }

        private void CapabilitiesCleanup()
        {
            var capabilities = PlanApi.Capabilities.Read(createdCapabilityIds.ToArray());

            PlanApi.Capabilities.Delete(capabilities.ToArray());
        }

        private void CapacitiesCleanup()
        {
            var capacities = PlanApi.Capacities.Read(createdCapacityIds.ToArray());

            PlanApi.Capacities.Delete(capacities.ToArray());
        }

        private void ConfigurationsCleanup()
        {
            var configurations = PlanApi.Configurations.Read(createdConfigurationIds.ToArray());

            PlanApi.Configurations.Delete(configurations.ToArray());
        }

        private void PropertiesCleanup()
        {
            var properties = PlanApi.ResourceProperties.Read(createdPropertyIds.ToArray());

            PlanApi.ResourceProperties.Delete(properties.ToArray());
        }

        private void CategoriesCleanup()
        {
            var categories = CatagoriesApi.Categories.Read(createdCategoryIds.ToArray()).Values;
            CatagoriesApi.Categories.Delete(categories);
        }

        private void ElementsCleanup()
        {
            foreach (var elementId in createdElementIds)
            {
                if (!Dms.ElementExists(elementId))
                {
                    continue;
                }

                var element = Dms.GetElement(elementId);
                element.Delete();
            }
        }

        public T CreateResource<T>(T resource) where T : Resource
        {
            var createdResource = PlanApi.Resources.Create(resource);
            createdResourceIds.Add(createdResource.Id);
            return createdResource;
        }

        public IReadOnlyCollection<Resource> CreateResources(IEnumerable<Resource> resources)
        {
            try
            {
                var createdResources = PlanApi.Resources.Create(resources);

                foreach (var id in resources.Select(x => x.Id))
                {
                    createdResourceIds.Add(id);
                }

                return createdResources;
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

        public ResourcePool CreateResourcePool(ResourcePool resourcePool)
        {
            var createdPool = PlanApi.ResourcePools.Create(resourcePool);
            createdPoolIds.Add(createdPool.Id);
            return createdPool;
        }

        public IReadOnlyCollection<ResourcePool> CreateResourcePools(IEnumerable<ResourcePool> resourcePools)
        {
            try
            {
                var createdPools = PlanApi.ResourcePools.Create(resourcePools);

                foreach (var id in resourcePools.Select(x => x.Id))
                {
                    createdPoolIds.Add(id);
                }

                return createdPools;
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

        public Capability CreateCapability(Capability capability)
        {
            var createdCapability = PlanApi.Capabilities.Create(capability);
            createdCapabilityIds.Add(createdCapability.Id);
            return createdCapability;
        }

        public IReadOnlyCollection<Capability> CreateCapabilities(IEnumerable<Capability> capabilities)
        {
            try
            {
                var createdCapabilities = PlanApi.Capabilities.Create(capabilities);

                foreach (var id in capabilities.Select(x => x.Id))
                {
                    createdCapabilityIds.Add(id);
                }

                return createdCapabilities;
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

        public Capacity CreateCapacity(Capacity capacity)
        {
            var createdCapacity = PlanApi.Capacities.Create(capacity);
            createdCapacityIds.Add(createdCapacity.Id);
            return createdCapacity;
        }

        public IReadOnlyCollection<Capacity> CreateCapacities(IEnumerable<Capacity> capacities)
        {
            try
            {
                var createdCapacities = PlanApi.Capacities.Create(capacities);

                foreach (var id in capacities.Select(x => x.Id))
                {
                    createdCapacityIds.Add(id);
                }

                return createdCapacities;
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

        public Configuration CreateConfiguration(Configuration configuration)
        {
            var createdConfiguration = PlanApi.Configurations.Create(configuration);
            createdConfigurationIds.Add(createdConfiguration.Id);
            return createdConfiguration;
        }

        public IReadOnlyCollection<Configuration> CreateConfigurations(IEnumerable<Configuration> configurations)
        {
            try
            {
                var createdConfigurations = PlanApi.Configurations.Create(configurations);

                foreach (var id in configurations.Select(x => x.Id))
                {
                    createdConfigurationIds.Add(id);
                }

                return createdConfigurations;
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

        public ResourceProperty CreateProperty(ResourceProperty property)
        {
            var createdProperty = PlanApi.ResourceProperties.Create(property);
            createdPropertyIds.Add(createdProperty.Id);
            return createdProperty;
        }

        public IReadOnlyCollection<ResourceProperty> CreateProperties(IEnumerable<ResourceProperty> properties)
        {
            try
            {
                var createdProperties = PlanApi.ResourceProperties.Create(properties);

                foreach (var id in properties.Select(x => x.Id))
                {
                    createdPropertyIds.Add(id);
                }

                return createdProperties;
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
            category = CatagoriesApi.Categories.Create(category);
            createdCategoryIds.Add(category.ID);
            return category;
        }

        public DmsElementId CreateElement(ElementConfiguration configuration)
        {
            var agent = Dms.GetAgents().First();
            var elementId = agent.CreateElement(configuration);

            createdElementIds.Add(elementId);
            return elementId;
        }
    }
}
