namespace RT_MediaOps.Plan.RST.Querying
{
    using System;
    using System.Linq;
    using RT_MediaOps.Plan.RegressionTests;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class ResourceQueryingTests : IDisposable
    {
        private readonly ResourceStudioObjectCreator objectCreator;
        private readonly ResourceQueryingSetup setup;

        public ResourceQueryingTests()
        {
            objectCreator = new ResourceStudioObjectCreator(TestContext.Api);
            setup = new ResourceQueryingSetup(objectCreator, TestContext);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
        }

        private FilterElement<Resource> ResourceFilter => new ORFilterElement<Resource>(setup.Resources.Select(x => ResourceExposers.Id.Equal(x.Id)).ToArray());

        private FilterElement<ResourcePool> ResourcePoolFilter => new ORFilterElement<ResourcePool>(setup.ResourcePools.Select(x => ResourcePoolExposers.Id.Equal(x.Id)).ToArray());

        private Tuple<int, FilterElement<Resource>>[] ResourceFilterTestCases => new[]
        {
            new Tuple<int, FilterElement<Resource>>(5, ResourceFilter),
            new Tuple<int, FilterElement<Resource>>(3, ResourceFilter.AND(ResourceExposers.Name.Contains("Resource_Draft"))),

            new Tuple<int, FilterElement<Resource>>(3, ResourceFilter.AND(ResourceExposers.Concurrency.GreaterThan(6))),
            new Tuple<int, FilterElement<Resource>>(3, ResourceFilter.AND(ResourceExposers.IsFavorite.Equal(true))),

            new Tuple<int, FilterElement<Resource>>(3, ResourceFilter.AND(ResourceExposers.State.Equal((int)ResourceState.Draft))),
            new Tuple<int, FilterElement<Resource>>(2, ResourceFilter.AND(ResourceExposers.State.Equal((int)ResourceState.Complete))),
            new Tuple<int, FilterElement<Resource>>(0, ResourceFilter.AND(ResourceExposers.State.Equal((int)ResourceState.Deprecated))),

            new Tuple<int, FilterElement<Resource>>(2, ResourceFilter.AND(ResourceExposers.ResourcePoolIds.Contains(setup.ResourcePool1!.Id))),
            new Tuple<int, FilterElement<Resource>>(3, ResourceFilter.AND(ResourceExposers.ResourcePoolIds.Contains(setup.ResourcePool2!.Id))),
            new Tuple<int, FilterElement<Resource>>(0, ResourceFilter.AND(ResourceExposers.ResourcePoolIds.Contains(setup.ResourcePool3!.Id))),

            new Tuple<int, FilterElement<Resource>>(1, ResourceFilter.AND(ResourceExposers.Capabilities.CapabilityId.Equal(setup.Location!.Id))),
            new Tuple<int, FilterElement<Resource>>(1, ResourceFilter.AND(ResourceExposers.Capabilities.Discretes.Contains("USA"))),
            new Tuple<int, FilterElement<Resource>>(1, ResourceFilter.AND(ResourceExposers.Capabilities.CapabilityId.Equal(setup.Location.Id)).AND(ResourceExposers.Capabilities.Discretes.Contains("USA"))),

            new Tuple<int, FilterElement<Resource>>(3, ResourceFilter.AND(ResourceExposers.Capacities.CapacityId.Equal(setup.Frequency!.Id))),
            new Tuple<int, FilterElement<Resource>>(2, ResourceFilter.AND(ResourceExposers.Capacities.CapacityId.Equal(setup.Bandwidth!.Id))),

            new Tuple<int, FilterElement<Resource>>(4, ResourceFilter.AND(ResourceExposers.Properties.PropertyId.Equal(setup.Format!.Id))),
            new Tuple<int, FilterElement<Resource>>(4, ResourceFilter.AND(ResourceExposers.Properties.Value.Equal("16:9"))),

            new Tuple<int, FilterElement<Resource>>(2, ResourceFilter.AND(ResourceExposers.Properties.PropertyId.Equal(setup.Channel!.Id))),
            new Tuple<int, FilterElement<Resource>>(2, ResourceFilter.AND(ResourceExposers.Properties.Value.Equal("VRT"))),

            new Tuple<int, FilterElement<Resource>>(0, ResourceFilter.AND(ResourceExposers.Properties.PropertyId.Equal(setup.Color!.Id))),
        };

        private Tuple<int, FilterElement<ResourcePool>>[] ResourcePoolFilterTestCases => new[]
{
            new Tuple<int, FilterElement<ResourcePool>>(5, ResourcePoolFilter),
            new Tuple<int, FilterElement<ResourcePool>>(1, ResourcePoolFilter.AND(ResourcePoolExposers.Name.Contains("ResourcePool_Draft"))),
            new Tuple<int, FilterElement<ResourcePool>>(4, ResourcePoolFilter.AND(ResourcePoolExposers.Name.Contains("ResourcePool_Complete"))),

            new Tuple<int, FilterElement<ResourcePool>>(2, ResourcePoolFilter.AND(ResourcePoolExposers.IconImage.Contains(".jpeg"))),
            new Tuple<int, FilterElement<ResourcePool>>(3, ResourcePoolFilter.AND(ResourcePoolExposers.IconImage.Contains(".png"))),
            new Tuple<int, FilterElement<ResourcePool>>(0, ResourcePoolFilter.AND(ResourcePoolExposers.IconImage.Contains(".gif"))),

            new Tuple<int, FilterElement<ResourcePool>>(1, ResourcePoolFilter.AND(ResourcePoolExposers.State.Equal((int)ResourcePoolState.Draft))),
            new Tuple<int, FilterElement<ResourcePool>>(4, ResourcePoolFilter.AND(ResourcePoolExposers.State.Equal((int)ResourcePoolState.Complete))),
            new Tuple<int, FilterElement<ResourcePool>>(0, ResourcePoolFilter.AND(ResourcePoolExposers.State.Equal((int)ResourcePoolState.Deprecated))),

            new Tuple<int, FilterElement<ResourcePool>>(0, ResourcePoolFilter.AND(ResourcePoolExposers.Url.Contains("skyline.be"))),
            new Tuple<int, FilterElement<ResourcePool>>(5, ResourcePoolFilter.AND(ResourcePoolExposers.Url.Equal(String.Empty))),

            new Tuple<int, FilterElement<ResourcePool>>(2, ResourcePoolFilter.AND(ResourcePoolExposers.Capabilities.CapabilityId.Equal(setup.Location!.Id))),
            new Tuple<int, FilterElement<ResourcePool>>(3, ResourcePoolFilter.AND(ResourcePoolExposers.Capabilities.CapabilityId.Equal(setup.Priority!.Id))),
            new Tuple<int, FilterElement<ResourcePool>>(1, ResourcePoolFilter.AND(ResourcePoolExposers.Capabilities.CapabilityId.Equal(setup.Resolution!.Id))),

            new Tuple<int, FilterElement<ResourcePool>>(2, ResourcePoolFilter.AND(ResourcePoolExposers.Capabilities.Discretes.Contains("Belgium"))),
            new Tuple<int, FilterElement<ResourcePool>>(2, ResourcePoolFilter.AND(ResourcePoolExposers.Capabilities.Discretes.Contains("Low"))),
            new Tuple<int, FilterElement<ResourcePool>>(1, ResourcePoolFilter.AND(ResourcePoolExposers.Capabilities.Discretes.Contains("4K"))),

            new Tuple<int, FilterElement<ResourcePool>>(2, ResourcePoolFilter.AND(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId.Equal(setup.ResourcePool1))),
        };

        [TestMethod]
        public void ReadResourcesWithFilter()
        {
            foreach (var (expectedCount, filter) in ResourceFilterTestCases)
            {
                Assert.AreEqual(expectedCount, TestContext.Api.Resources.Read(filter).Count());
            }
        }

        [TestMethod]
        public void CountResourcesWithFilter()
        {
            foreach (var (expectedCount, filter) in ResourceFilterTestCases)
            {
                Assert.AreEqual(expectedCount, TestContext.Api.Resources.Count(filter));
            }
        }

        [TestMethod]
        public void ReadResourcesPagedWithFilter_DefaultPageSize()
        {
            foreach (var (expectedCount, filter) in ResourceFilterTestCases)
            {
                var pages = TestContext.Api.Resources.ReadPaged(filter);

                Assert.AreEqual(1, pages.Count());
                Assert.AreEqual(expectedCount, pages.SelectMany(x => x).Count());
            }
        }

        [TestMethod]
        public void ReadResourcesPagedWithFilter_CustomPageSize()
        {
            foreach (var (expectedCount, filter) in ResourceFilterTestCases)
            {
                int expectedAmountOfPages = expectedCount / 2 + 1;
                var pages = TestContext.Api.Resources.ReadPaged(filter, 2);

                Assert.AreEqual(expectedAmountOfPages, pages.Count());
                Assert.AreEqual(expectedCount, pages.SelectMany(x => x).Count());
            }
        }

        [TestMethod]
        public void ReadResourcePoolsWithFilter()
        {
            foreach (var (expectedCount, filter) in ResourcePoolFilterTestCases)
            {
                Assert.AreEqual(expectedCount, TestContext.Api.ResourcePools.Read(filter).Count());
            }
        }

        [TestMethod]
        public void CountResourcePoolsWithFilter()
        {
            foreach (var (expectedCount, filter) in ResourcePoolFilterTestCases)
            {
                Assert.AreEqual(expectedCount, TestContext.Api.ResourcePools.Count(filter));
            }
        }

        [TestMethod]
        public void ReadResourcePoolsPagedWithFilter_DefaultPageSize()
        {
            foreach (var (expectedCount, filter) in ResourcePoolFilterTestCases)
            {
                var pages = TestContext.Api.ResourcePools.ReadPaged(filter);

                Assert.AreEqual(1, pages.Count());
                Assert.AreEqual(expectedCount, pages.SelectMany(x => x).Count());
            }
        }

        [TestMethod]
        public void ReadResourcePoolsPagedWithFilter_CustomPageSize()
        {
            foreach (var (expectedCount, filter) in ResourcePoolFilterTestCases)
            {
                int expectedAmountOfPages = expectedCount / 2 + 1; // If page 
                var pages = TestContext.Api.ResourcePools.ReadPaged(filter, 2);

                Assert.AreEqual(expectedAmountOfPages, pages.Count());
                Assert.AreEqual(expectedCount, pages.SelectMany(x => x).Count());
            }
        }
    }
}