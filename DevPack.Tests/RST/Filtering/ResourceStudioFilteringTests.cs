namespace RT_MediaOps.Plan.RST.Filtering
{
    using System;
    using System.Linq;
    using RT_MediaOps.Plan.RegressionTests;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class ResourceStudioFilteringTests
    {
        private static ResourceStudioObjectCreator? objectCreator;
        private static ResourceFilteringSetup? setup;

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            objectCreator = new ResourceStudioObjectCreator(TestContext.Api);
            setup = new ResourceFilteringSetup(objectCreator, TestContext);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            objectCreator?.Dispose();
            objectCreator = null;
            setup = null;
        }

        private static ResourceFilteringSetup Setup => setup ?? throw new InvalidOperationException("Test setup was not initialized.");

        private FilterElement<Resource> ResourceFilter => new ORFilterElement<Resource>(Setup.Resources.Select(x => ResourceExposers.Id.Equal(x.Id)).ToArray());

        private FilterElement<ResourcePool> ResourcePoolFilter => new ORFilterElement<ResourcePool>(Setup.ResourcePools.Select(x => ResourcePoolExposers.Id.Equal(x.Id)).ToArray());

        private FilterElement<Capability> CapabilityFilter => new ORFilterElement<Capability>(Setup.Capabilities.Select(x => CapabilityExposers.Id.Equal(x.Id)).ToArray());

        private FilterElement<Capacity> CapacityFilter => new ORFilterElement<Capacity>(Setup.Capacities.Select(x => CapacityExposers.Id.Equal(x.Id)).ToArray());

        private FilterElement<ResourceProperty> PropertyFilter => new ORFilterElement<ResourceProperty>(Setup.Properties.Select(x => ResourcePropertyExposers.Id.Equal(x.Id)).ToArray());

        private FilterElement<Configuration> ConfigurationFilter => new ORFilterElement<Configuration>(Setup.Configurations.Select(x => ConfigurationExposers.Id.Equal(x.Id)).ToArray());

        private Tuple<int, FilterElement<Resource>>[] ResourceFilterTestCases => new[]
        {
            new Tuple<int, FilterElement<Resource>>(5, ResourceFilter),
            new Tuple<int, FilterElement<Resource>>(3, ResourceFilter.AND(ResourceExposers.Name.Contains("Resource_Draft"))),

            new Tuple<int, FilterElement<Resource>>(3, ResourceFilter.AND(ResourceExposers.Concurrency.GreaterThan(6))),
            new Tuple<int, FilterElement<Resource>>(3, ResourceFilter.AND(ResourceExposers.IsFavorite.Equal(true))),

            new Tuple<int, FilterElement<Resource>>(3, ResourceFilter.AND(ResourceExposers.State.Equal((int)ResourceState.Draft))),
            new Tuple<int, FilterElement<Resource>>(2, ResourceFilter.AND(ResourceExposers.State.Equal((int)ResourceState.Complete))),
            new Tuple<int, FilterElement<Resource>>(0, ResourceFilter.AND(ResourceExposers.State.Equal((int)ResourceState.Deprecated))),

            new Tuple<int, FilterElement<Resource>>(2, ResourceFilter.AND(ResourceExposers.ResourcePoolIds.Contains(Setup.ResourcePool1!.Id))),
            new Tuple<int, FilterElement<Resource>>(3, ResourceFilter.AND(ResourceExposers.ResourcePoolIds.Contains(Setup.ResourcePool2!.Id))),
            new Tuple<int, FilterElement<Resource>>(0, ResourceFilter.AND(ResourceExposers.ResourcePoolIds.Contains(Setup.ResourcePool3!.Id))),

            new Tuple<int, FilterElement<Resource>>(1, ResourceFilter.AND(ResourceExposers.Capabilities.CapabilityId.Equal(Setup.Location!.Id))),
            new Tuple<int, FilterElement<Resource>>(1, ResourceFilter.AND(ResourceExposers.Capabilities.Discretes.Contains("USA"))),
            new Tuple<int, FilterElement<Resource>>(1, ResourceFilter.AND(ResourceExposers.Capabilities.CapabilityId.Equal(Setup.Location.Id)).AND(ResourceExposers.Capabilities.Discretes.Contains("USA"))),

            new Tuple<int, FilterElement<Resource>>(3, ResourceFilter.AND(ResourceExposers.Capacities.CapacityId.Equal(Setup.Frequency!.Id))),
            new Tuple<int, FilterElement<Resource>>(2, ResourceFilter.AND(ResourceExposers.Capacities.CapacityId.Equal(Setup.Bandwidth!.Id))),

            new Tuple<int, FilterElement<Resource>>(4, ResourceFilter.AND(ResourceExposers.Properties.PropertyId.Equal(Setup.Format!.Id))),
            new Tuple<int, FilterElement<Resource>>(4, ResourceFilter.AND(ResourceExposers.Properties.Value.Equal("16:9"))),

            new Tuple<int, FilterElement<Resource>>(2, ResourceFilter.AND(ResourceExposers.Properties.PropertyId.Equal(Setup.Channel!.Id))),
            new Tuple<int, FilterElement<Resource>>(2, ResourceFilter.AND(ResourceExposers.Properties.Value.Equal("VRT"))),

            new Tuple<int, FilterElement<Resource>>(0, ResourceFilter.AND(ResourceExposers.Properties.PropertyId.Equal(Setup.Color!.Id))),

            // TODO: add tests for ElementResources, VirtualFunctionResources and ServiceResources.
        };

        private Tuple<int, FilterElement<Capability>>[] CapabilityFilterTestCases => new[]
        {
            new Tuple<int, FilterElement<Capability>>(3, CapabilityFilter),
            new Tuple<int, FilterElement<Capability>>(1, CapabilityFilter.AND(CapabilityExposers.Name.Contains("Location"))),
            new Tuple<int, FilterElement<Capability>>(1, CapabilityFilter.AND(CapabilityExposers.Name.Equal(Setup.Priority!.Name))),
            new Tuple<int, FilterElement<Capability>>(1, CapabilityFilter.AND(CapabilityExposers.IsMandatory.Equal(true))),
            new Tuple<int, FilterElement<Capability>>(2, CapabilityFilter.AND(CapabilityExposers.IsTimeDependent.Equal(false))),
            new Tuple<int, FilterElement<Capability>>(1, CapabilityFilter.AND(CapabilityExposers.Discretes.Contains("Belgium"))),
            new Tuple<int, FilterElement<Capability>>(1, CapabilityFilter.AND(CapabilityExposers.Discretes.Contains("4K"))),
        };

        private Tuple<int, FilterElement<Capacity>>[] CapacityFilterTestCases => new[]
        {
            new Tuple<int, FilterElement<Capacity>>(3, CapacityFilter),
            new Tuple<int, FilterElement<Capacity>>(1, CapacityFilter.AND(CapacityExposers.Name.Contains("Frequency"))),
            new Tuple<int, FilterElement<Capacity>>(0, CapacityFilter.AND(CapacityExposers.IsMandatory.Equal(true))),
            new Tuple<int, FilterElement<Capacity>>(2, CapacityFilter.AND(CapacityExposers.Units.Contains("Hz"))),
            new Tuple<int, FilterElement<Capacity>>(2, CapacityFilter.AND(CapacityExposers.RangeMin.GreaterThan(0))),
            new Tuple<int, FilterElement<Capacity>>(0, CapacityFilter.AND(CapacityExposers.RangeMin.GreaterThan(1000))),
            new Tuple<int, FilterElement<Capacity>>(2, CapacityFilter.AND(CapacityExposers.RangeMax.LessThanOrEqual(30000))),
            new Tuple<int, FilterElement<Capacity>>(2, CapacityFilter.AND(CapacityExposers.StepSize.LessThan(1m))),
            new Tuple<int, FilterElement<Capacity>>(1, CapacityFilter.AND(CapacityExposers.Decimals.GreaterThan(2))),
            new Tuple<int, FilterElement<Capacity>>(2, CapacityFilter.AND(CapacityExposers.HasRangeMin.Equal(true))),
            new Tuple<int, FilterElement<Capacity>>(2, CapacityFilter.AND(CapacityExposers.HasRangeMax.Equal(true))),
            new Tuple<int, FilterElement<Capacity>>(2, CapacityFilter.AND(CapacityExposers.HasUnits.Equal(true))),
            new Tuple<int, FilterElement<Capacity>>(2, CapacityFilter.AND(CapacityExposers.HasStepSize.Equal(true))),
            new Tuple<int, FilterElement<Capacity>>(2, CapacityFilter.AND(CapacityExposers.HasDecimals.Equal(true))),
            new Tuple<int, FilterElement<Capacity>>(1, CapacityFilter.AND(CapacityExposers.HasRangeMin.NotEqual(true))),
            new Tuple<int, FilterElement<Capacity>>(1, CapacityFilter.AND(CapacityExposers.HasRangeMax.NotEqual(true))),
            new Tuple<int, FilterElement<Capacity>>(1, CapacityFilter.AND(CapacityExposers.HasUnits.NotEqual(true))),
            new Tuple<int, FilterElement<Capacity>>(1, CapacityFilter.AND(CapacityExposers.HasStepSize.NotEqual(true))),
            new Tuple<int, FilterElement<Capacity>>(1, CapacityFilter.AND(CapacityExposers.HasDecimals.NotEqual(true))),
        };

        private Tuple<int, FilterElement<ResourcePool>>[] ResourcePoolFilterTestCases => new[]
        {
            new Tuple<int, FilterElement<ResourcePool>>(5, ResourcePoolFilter),
            new Tuple<int, FilterElement<ResourcePool>>(1, ResourcePoolFilter.AND(ResourcePoolExposers.Name.Contains("ResourcePool_Draft"))),
            new Tuple<int, FilterElement<ResourcePool>>(4, ResourcePoolFilter.AND(ResourcePoolExposers.Name.Contains("ResourcePool_Complete"))),

            new Tuple<int, FilterElement<ResourcePool>>(1, ResourcePoolFilter.AND(ResourcePoolExposers.IconImage.Contains(".jpeg"))),
            new Tuple<int, FilterElement<ResourcePool>>(3, ResourcePoolFilter.AND(ResourcePoolExposers.IconImage.Contains(".png"))),
            new Tuple<int, FilterElement<ResourcePool>>(0, ResourcePoolFilter.AND(ResourcePoolExposers.IconImage.Contains(".gif"))),
            new Tuple<int, FilterElement<ResourcePool>>(1, ResourcePoolFilter.AND(ResourcePoolExposers.HasIconImage.Equal(false))),

            new Tuple<int, FilterElement<ResourcePool>>(1, ResourcePoolFilter.AND(ResourcePoolExposers.State.Equal((int)ResourcePoolState.Draft))),
            new Tuple<int, FilterElement<ResourcePool>>(4, ResourcePoolFilter.AND(ResourcePoolExposers.State.Equal((int)ResourcePoolState.Complete))),
            new Tuple<int, FilterElement<ResourcePool>>(0, ResourcePoolFilter.AND(ResourcePoolExposers.State.Equal((int)ResourcePoolState.Deprecated))),

            new Tuple<int, FilterElement<ResourcePool>>(0, ResourcePoolFilter.AND(ResourcePoolExposers.HasUrl.Equal(true).AND(ResourcePoolExposers.Url.Equal("skyline.be")))),
            new Tuple<int, FilterElement<ResourcePool>>(5, ResourcePoolFilter.AND(ResourcePoolExposers.HasUrl.Equal(false))),

            new Tuple<int, FilterElement<ResourcePool>>(2, ResourcePoolFilter.AND(ResourcePoolExposers.Capabilities.CapabilityId.Equal(Setup.Location!.Id))),
            new Tuple<int, FilterElement<ResourcePool>>(3, ResourcePoolFilter.AND(ResourcePoolExposers.Capabilities.CapabilityId.Equal(Setup.Priority!.Id))),
            new Tuple<int, FilterElement<ResourcePool>>(1, ResourcePoolFilter.AND(ResourcePoolExposers.Capabilities.CapabilityId.Equal(Setup.Resolution!.Id))),

            new Tuple<int, FilterElement<ResourcePool>>(2, ResourcePoolFilter.AND(ResourcePoolExposers.Capabilities.Discretes.Contains("Belgium"))),
            new Tuple<int, FilterElement<ResourcePool>>(2, ResourcePoolFilter.AND(ResourcePoolExposers.Capabilities.Discretes.Contains("Low"))),
            new Tuple<int, FilterElement<ResourcePool>>(1, ResourcePoolFilter.AND(ResourcePoolExposers.Capabilities.Discretes.Contains("4K"))),

            new Tuple<int, FilterElement<ResourcePool>>(2, ResourcePoolFilter.AND(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId.Equal(Setup.ResourcePool1!.Id))),
            new Tuple<int, FilterElement<ResourcePool>>(2, ResourcePoolFilter.AND(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId.Equal(Setup.ResourcePool2!.Id))),
            new Tuple<int, FilterElement<ResourcePool>>(0, ResourcePoolFilter.AND(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId.Equal(Setup.ResourcePool3!.Id))),
            new Tuple<int, FilterElement<ResourcePool>>(0, ResourcePoolFilter.AND(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId.Equal(Setup.ResourcePool4!.Id))),
            new Tuple<int, FilterElement<ResourcePool>>(0, ResourcePoolFilter.AND(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId.Equal(Setup.ResourcePool5!.Id))),

            new Tuple<int, FilterElement<ResourcePool>>(2, ResourcePoolFilter.AND(ResourcePoolExposers.LinkedResourcePools.SelectionType.Equal(ResourceSelectionType.Automatic))),
            new Tuple<int, FilterElement<ResourcePool>>(1, ResourcePoolFilter.AND(ResourcePoolExposers.LinkedResourcePools.SelectionType.Equal(ResourceSelectionType.Manual))),
        };

        private Tuple<int, FilterElement<ResourceProperty>>[] PropertyFilterTestCases => new[]
        {
            new Tuple<int, FilterElement<ResourceProperty>>(3, PropertyFilter),
            new Tuple<int, FilterElement<ResourceProperty>>(1, PropertyFilter.AND(ResourcePropertyExposers.Name.Contains("Format"))),
            new Tuple<int, FilterElement<ResourceProperty>>(1, PropertyFilter.AND(ResourcePropertyExposers.Name.Contains("Channel"))),
            new Tuple<int, FilterElement<ResourceProperty>>(1, PropertyFilter.AND(ResourcePropertyExposers.Name.Contains("Color"))),
            new Tuple<int, FilterElement<ResourceProperty>>(0, PropertyFilter.AND(ResourcePropertyExposers.Name.Contains("Something"))),
            new Tuple<int, FilterElement<ResourceProperty>>(3, PropertyFilter.AND(ResourcePropertyExposers.Name.NotContains("Something"))),
            new Tuple<int, FilterElement<ResourceProperty>>(3, PropertyFilter.AND(ResourcePropertyExposers.Name.NotEqual("Something"))),
        };

        private Tuple<int, FilterElement<Configuration>>[] ConfigurationFilterTestCases => new[]
        {
            new Tuple<int, FilterElement<Configuration>>(4, ConfigurationFilter),
            new Tuple<int, FilterElement<Configuration>>(1, ConfigurationFilter.AND(ConfigurationExposers.Name.Contains("Region"))),
            new Tuple<int, FilterElement<Configuration>>(1, ConfigurationFilter.AND(ConfigurationExposers.IsMandatory.Equal(true))),
            new Tuple<int, FilterElement<Configuration>>(3, ConfigurationFilter.AND(ConfigurationExposers.IsMandatory.Equal(false))),
            new Tuple<int, FilterElement<Configuration>>(0, ConfigurationFilter.AND(DiscreteTextConfigurationExposers.Discretes.Contains("SD"))),
            new Tuple<int, FilterElement<Configuration>>(1, ConfigurationFilter.AND(DiscreteTextConfigurationExposers.Discretes.Contains("720p"))),
            new Tuple<int, FilterElement<Configuration>>(0, ConfigurationFilter.AND(DiscreteNumberConfigurationExposers.Discretes.Contains(20))),
            new Tuple<int, FilterElement<Configuration>>(1, ConfigurationFilter.AND(DiscreteNumberConfigurationExposers.Discretes.Contains(100))),
        };

        [TestMethod]
        public void ReadResourcesWithFilter()
        {
            foreach (var (expectedCount, filter) in ResourceFilterTestCases)
            {
                Assert.AreEqual(expectedCount, TestContext.Api.Resources.Read(filter).Count(), filter.ToString());
            }
        }

        [TestMethod]
        public void CountResourcesWithFilter()
        {
            foreach (var (expectedCount, filter) in ResourceFilterTestCases)
            {
                Assert.AreEqual(expectedCount, TestContext.Api.Resources.Count(filter), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadResourcesPagedWithFilter_DefaultPageSize()
        {
            foreach (var (expectedCount, filter) in ResourceFilterTestCases)
            {
                var pages = TestContext.Api.Resources.ReadPaged(filter);

                Assert.AreEqual(1, pages.Count(), filter.ToString());
                Assert.AreEqual(expectedCount, pages.SelectMany(x => x).Count(), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadResourcesPagedWithFilter_CustomPageSize()
        {
            foreach (var (expectedCount, filter) in ResourceFilterTestCases)
            {
                var pages = TestContext.Api.Resources.ReadPaged(filter, 2);
                Assert.AreEqual(expectedCount, pages.SelectMany(x => x).Count(), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadResourcePoolsWithFilter()
        {
            foreach (var (expectedCount, filter) in ResourcePoolFilterTestCases)
            {
                Assert.AreEqual(expectedCount, TestContext.Api.ResourcePools.Read(filter).Count(), filter.ToString());
            }
        }

        [TestMethod]
        public void CountResourcePoolsWithFilter()
        {
            foreach (var (expectedCount, filter) in ResourcePoolFilterTestCases)
            {
                Assert.AreEqual(expectedCount, TestContext.Api.ResourcePools.Count(filter), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadResourcePoolsPagedWithFilter_DefaultPageSize()
        {
            foreach (var (expectedCount, filter) in ResourcePoolFilterTestCases)
            {
                var pages = TestContext.Api.ResourcePools.ReadPaged(filter);

                Assert.AreEqual(1, pages.Count(), filter.ToString());
                Assert.AreEqual(expectedCount, pages.SelectMany(x => x).Count(), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadResourcePoolsPagedWithFilter_CustomPageSize()
        {
            foreach (var (expectedCount, filter) in ResourcePoolFilterTestCases)
            {
                var pages = TestContext.Api.ResourcePools.ReadPaged(filter, 2);
                Assert.AreEqual(expectedCount, pages.SelectMany(x => x).Count(), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadCapabilitiesWithFilter()
        {
            foreach (var (expectedCount, filter) in CapabilityFilterTestCases)
            {
                Assert.AreEqual(expectedCount, TestContext.Api.Capabilities.Read(filter).Count(), filter.ToString());
            }
        }

        [TestMethod]
        public void CountCapabilitiesWithFilter()
        {
            foreach (var (expectedCount, filter) in CapabilityFilterTestCases)
            {
                Assert.AreEqual(expectedCount, TestContext.Api.Capabilities.Count(filter), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadCapabilitiesPagedWithFilter_DefaultPageSize()
        {
            foreach (var (expectedCount, filter) in CapabilityFilterTestCases)
            {
                var pages = TestContext.Api.Capabilities.ReadPaged(filter);

                Assert.AreEqual(1, pages.Count(), filter.ToString());
                Assert.AreEqual(expectedCount, pages.SelectMany(x => x).Count(), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadCapabilitiesPagedWithFilter_CustomPageSize()
        {
            foreach (var (expectedCount, filter) in CapabilityFilterTestCases)
            {
                var pages = TestContext.Api.Capabilities.ReadPaged(filter, 2);
                Assert.AreEqual(expectedCount, pages.SelectMany(x => x).Count(), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadCapacitiesWithFilter()
        {
            foreach (var (expectedCount, filter) in CapacityFilterTestCases)
            {
                Assert.AreEqual(expectedCount, TestContext.Api.Capacities.Read(filter).Count(), filter.ToString());
            }
        }

        [TestMethod]
        public void CountCapacitiesWithFilter()
        {
            foreach (var (expectedCount, filter) in CapacityFilterTestCases)
            {
                Assert.AreEqual(expectedCount, TestContext.Api.Capacities.Count(filter), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadCapacitiesPagedWithFilter_DefaultPageSize()
        {
            foreach (var (expectedCount, filter) in CapacityFilterTestCases)
            {
                var pages = TestContext.Api.Capacities.ReadPaged(filter);

                Assert.AreEqual(1, pages.Count(), filter.ToString());
                Assert.AreEqual(expectedCount, pages.SelectMany(x => x).Count(), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadCapacitiesPagedWithFilter_CustomPageSize()
        {
            foreach (var (expectedCount, filter) in CapacityFilterTestCases)
            {
                var pages = TestContext.Api.Capacities.ReadPaged(filter, 2);
                Assert.AreEqual(expectedCount, pages.SelectMany(x => x).Count(), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadConfigurationsWithFilter()
        {
            foreach (var (expectedCount, filter) in ConfigurationFilterTestCases)
            {
                Assert.AreEqual(expectedCount, TestContext.Api.Configurations.Read(filter).Count(), filter.ToString());
            }
        }

        [TestMethod]
        public void CountConfigurationsWithFilter()
        {
            foreach (var (expectedCount, filter) in ConfigurationFilterTestCases)
            {
                Assert.AreEqual(expectedCount, TestContext.Api.Configurations.Count(filter), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadConfigurationsPagedWithFilter_DefaultPageSize()
        {
            foreach (var (expectedCount, filter) in ConfigurationFilterTestCases)
            {
                var pages = TestContext.Api.Configurations.ReadPaged(filter);

                Assert.AreEqual(1, pages.Count(), filter.ToString());
                Assert.AreEqual(expectedCount, pages.SelectMany(x => x).Count(), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadConfigurationsPagedWithFilter_CustomPageSize()
        {
            foreach (var (expectedCount, filter) in ConfigurationFilterTestCases)
            {
                var pages = TestContext.Api.Configurations.ReadPaged(filter, 2);
                Assert.AreEqual(expectedCount, pages.SelectMany(x => x).Count(), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadResourcePropertiesWithFilter()
        {
            foreach (var (expectedCount, filter) in PropertyFilterTestCases)
            {
                Assert.AreEqual(expectedCount, TestContext.Api.Properties.Read(filter).Count(), filter.ToString());
            }
        }

        [TestMethod]
        public void CountResourcePropertiesWithFilter()
        {
            foreach (var (expectedCount, filter) in PropertyFilterTestCases)
            {
                Assert.AreEqual(expectedCount, TestContext.Api.Properties.Count(filter), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadResourcePropertiesPagedWithFilter_DefaultPageSize()
        {
            foreach (var (expectedCount, filter) in PropertyFilterTestCases)
            {
                var pages = TestContext.Api.Properties.ReadPaged(filter);

                Assert.AreEqual(1, pages.Count(), filter.ToString());
                Assert.AreEqual(expectedCount, pages.SelectMany(x => x).Count(), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadResourcePropertiesPagedWithFilter_CustomPageSize()
        {
            foreach (var (expectedCount, filter) in PropertyFilterTestCases)
            {
                var pages = TestContext.Api.Properties.ReadPaged(filter, 2);
                Assert.AreEqual(expectedCount, pages.SelectMany(x => x).Count(), filter.ToString());
            }
        }
    }
}