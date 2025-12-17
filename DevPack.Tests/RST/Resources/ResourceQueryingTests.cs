namespace RT_MediaOps.Plan.RST.Resources
{
    using System;
    using System.Linq;
    using RT_MediaOps.Plan.RegressionTests;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class ResourceQueryingTests : IDisposable
    {
        private readonly ResourceStudioObjectCreator objectCreator;
        private readonly ResourceFilteringSetup setup;

        public ResourceQueryingTests()
        {
            objectCreator = new ResourceStudioObjectCreator(TestContext.Api);
            setup = new ResourceFilteringSetup(objectCreator, TestContext);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
        }

        [TestMethod]
        public void ReadResourcesWithFilter()
        {
            var resourceFilter = new ORFilterElement<Resource>(setup.Resources.Select(x => ResourceExposers.Id.Equal(x.Id)).ToArray());
            Assert.AreEqual(5, TestContext.Api.Resources.Read(resourceFilter).Count());

            Assert.AreEqual(3, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.Name.Contains("Resource_Draft"))).Count());

            Assert.AreEqual(3, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.Concurrency.GreaterThan(6))).Count());

            Assert.AreEqual(3, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.IsFavorite.Equal(true))).Count());

            Assert.AreEqual(3, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.State.Equal((int)ResourceState.Draft))).Count());
            Assert.AreEqual(2, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.State.Equal((int)ResourceState.Complete))).Count());
            Assert.AreEqual(0, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.State.Equal((int)ResourceState.Deprecated))).Count());

            Assert.AreEqual(2, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.AssignedResourcePoolIds.Contains(setup.ResourcePool1.Id))).Count());
            Assert.AreEqual(3, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.AssignedResourcePoolIds.Contains(setup.ResourcePool2.Id))).Count());
            Assert.AreEqual(0, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.AssignedResourcePoolIds.Contains(setup.ResourcePool3.Id))).Count());

            Assert.AreEqual(1, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.Capabilities.Id.Equal(setup.Location.Id))).Count());
            Assert.AreEqual(1, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.Capabilities.Discretes.Contains("USA"))).Count());
            Assert.AreEqual(1, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.Capabilities.Id.Equal(setup.Location.Id)).AND(ResourceExposers.Capabilities.Discretes.Contains("USA"))).Count());

            Assert.AreEqual(3, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.Capacities.Id.Equal(setup.Frequency.Id))).Count());
            Assert.AreEqual(2, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.Capacities.Id.Equal(setup.Bandwidth.Id))).Count());

            Assert.AreEqual(4, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.Properties.Id.Equal(setup.Format.Id))).Count());
            Assert.AreEqual(4, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.Properties.Value.Equal("16:9"))).Count());

            Assert.AreEqual(2, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.Properties.Id.Equal(setup.Channel.Id))).Count());
            Assert.AreEqual(2, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.Properties.Value.Equal("VRT"))).Count());

            Assert.AreEqual(0, TestContext.Api.Resources.Read(resourceFilter.AND(ResourceExposers.Properties.Id.Equal(setup.Color.Id))).Count());
        }

        [TestMethod]
        public void ReadResourceWithQuery()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void ReadResourcesPagedWithFilter()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void ReadResourcesPagedWithQuery()
        {
            Assert.Fail();
        }
    }

    internal sealed class ResourceFilteringSetup
    {
        private readonly ResourceStudioObjectCreator objectCreator;
        private readonly IntegrationTestContext TestContext;

        public ResourceFilteringSetup(ResourceStudioObjectCreator objectCreator, IntegrationTestContext TestContext)
        {
            this.objectCreator = objectCreator;
            this.TestContext = TestContext;

            CreateCapabilities();
            CreateCapacities();
            CreateProperties();
            CreateResourcePools();

            CreateDraftResources();
            CreateCompleteResources();
        }

        public Resource[] Resources => new Resource[]
        {
            DraftResource1!,
            DraftResource2!,
            DraftResource3!,
            CompleteResource4!,
            CompleteResource5!,
        };

        public UnmanagedResource? DraftResource1 { get; private set; }

        public UnmanagedResource? DraftResource2 { get; private set; }

        public UnmanagedResource? DraftResource3 { get; private set; }

        public UnmanagedResource? CompleteResource4 { get; private set; }

        public UnmanagedResource? CompleteResource5 { get; private set; }

        public ResourcePool? ResourcePool1 { get; private set; } // Contains Resource 1 & 2

        public ResourcePool? ResourcePool2 { get; private set; } // Contains Resource 3, 4, and 5

        public ResourcePool? ResourcePool3 { get; private set; } // Contains no resources

        public Capability? Location { get; private set; }

        public Capability? Priority { get; private set; }

        public Capability? Resolution { get; private set; }

        public NumberCapacity? Frequency { get; private set; }

        public RangeCapacity? Bandwidth { get; private set; }

        public ResourceProperty? Channel { get; private set; }

        public ResourceProperty? Color { get; private set; }

        public ResourceProperty? Format { get; private set; }

        private void CreateCapabilities()
        {
            Location = new Capability
            {
                Name = $"Location_{Guid.NewGuid()}",
                IsMandatory = true,
                IsTimeDependent = true,
            };

            Location.SetDiscretes(["USA", "Mozambique", "Belgium"]);

            Priority = new Capability
            {
                Name = $"Priority_{Guid.NewGuid()}",
            };

            Priority.SetDiscretes(["Low", "Medium", "High"]);

            Resolution = new Capability
            {
                Name = $"Resolution_{Guid.NewGuid()}",
            };

            Resolution.SetDiscretes(["720p", "1080p", "4K"]);

            var capabilities = new Capability[]
            {
                    Location,
                    Priority,
                    Resolution,
            };

            foreach (var capability in capabilities)
            {
                objectCreator.CreateCapability(capability);
            }

            Location = TestContext.Api.Capabilities.Read(Location.Id);
            Priority = TestContext.Api.Capabilities.Read(Priority.Id);
            Resolution = TestContext.Api.Capabilities.Read(Resolution.Id);
        }

        private void CreateDraftResources()
        {
            DraftResource1 = new UnmanagedResource
            {
                Name = $"Resource_Draft_1_{Guid.NewGuid()}",
                IsFavorite = true,
                Concurrency = 5,
            };

            var locationResource1 = new ResourceCapabilitySettings(Location);
            locationResource1.SetDiscretes(["USA", "Belgium"]);

            var priorityResource1 = new ResourceCapabilitySettings(Priority);
            priorityResource1.SetDiscretes(["Low"]);

            var frequencyCapacity1 = new ResourceNumberCapacitySettings(Frequency)
            {
                Value = 20
            };

            var bandwidthCapacity1 = new ResourceRangeCapacitySettings(Bandwidth)
            {
                MinValue = 5000,
                MaxValue = 7500
            };

            DraftResource1.AddCapability(locationResource1);
            DraftResource1.AddCapability(priorityResource1);
            DraftResource1.AddCapacity(frequencyCapacity1);
            DraftResource1.AddCapacity(bandwidthCapacity1);
            DraftResource1.AssignToPool(ResourcePool1);

            DraftResource2 = new UnmanagedResource
            {
                Name = $"Resource_Draft_2_{Guid.NewGuid()}",
                IsFavorite = false,
                Concurrency = 10,
            };

            var channelProperty2 = new ResourcePropertySettings(Channel)
            {
                Value = "VRT"
            };

            var formatProperty2 = new ResourcePropertySettings(Format)
            {
                Value = "16:9"
            };

            var frequencyCapacity2 = new ResourceNumberCapacitySettings(Frequency)
            {
                Value = 20
            };

            var bandwidthCapacity2 = new ResourceRangeCapacitySettings(Bandwidth)
            {
                MinValue = 5000,
                MaxValue = 7500
            };

            DraftResource2.AddProperty(channelProperty2);
            DraftResource2.AddProperty(formatProperty2);
            DraftResource2.AddCapacity(frequencyCapacity2);
            DraftResource2.AddCapacity(bandwidthCapacity2);
            DraftResource2.AssignToPool(ResourcePool1);

            DraftResource3 = new UnmanagedResource
            {
                Name = $"Resource_Draft_3_{Guid.NewGuid()}",
                IsFavorite = true,
                Concurrency = 15,
            };

            var resolutionResource3 = new ResourceCapabilitySettings(Resolution);
            resolutionResource3.SetDiscretes(["1080p", "4K"]);

            var formatProperty3 = new ResourcePropertySettings(Format)
            {
                Value = "16:9"
            };

            DraftResource3.AddCapability(resolutionResource3);
            DraftResource3.AddProperty(formatProperty3);
            DraftResource3.AssignToPool(ResourcePool2);

            var resourcesToCreate = new Resource[]
            {
                    DraftResource1,
                    DraftResource2,
                    DraftResource3,
            };

            foreach (var resource in resourcesToCreate)
            {
                objectCreator.CreateResource(resource);
            }

            DraftResource1 = (UnmanagedResource)TestContext.Api.Resources.Read(DraftResource1.Id);
            DraftResource2 = (UnmanagedResource)TestContext.Api.Resources.Read(DraftResource2.Id);
            DraftResource3 = (UnmanagedResource)TestContext.Api.Resources.Read(DraftResource3.Id);
        }

        private void CreateCompleteResources()
        {
            CompleteResource4 = new UnmanagedResource
            {
                Name = $"Resource_Complete_4_{Guid.NewGuid()}",
                IsFavorite = true,
                Concurrency = 5,
            };

            var resolutionResource4 = new ResourceCapabilitySettings(Resolution);
            resolutionResource4.SetDiscretes(["1080p", "4K"]);

            var formatProperty4 = new ResourcePropertySettings(Format)
            {
                Value = "16:9"
            };

            var frequencyCapacity4 = new ResourceNumberCapacitySettings(Frequency)
            {
                Value = 20
            };

            CompleteResource4.AddCapability(resolutionResource4);
            CompleteResource4.AddProperty(formatProperty4);
            CompleteResource4.AddCapacity(frequencyCapacity4);
            CompleteResource4.AssignToPool(ResourcePool2);

            CompleteResource5 = new UnmanagedResource
            {
                Name = $"Resource_Complete_5_{Guid.NewGuid()}",
                IsFavorite = false,
                Concurrency = 10,
            };

            var channelProperty5 = new ResourcePropertySettings(Channel)
            {
                Value = "VRT"
            };

            var formatProperty5 = new ResourcePropertySettings(Format)
            {
                Value = "16:9"
            };

            CompleteResource5.AddProperty(channelProperty5);
            CompleteResource5.AddProperty(formatProperty5);
            CompleteResource5.AssignToPool(ResourcePool2);

            var resourcesToCreate = new Resource[]
            {
                    CompleteResource4,
                    CompleteResource5,
            };

            foreach (var resource in resourcesToCreate)
            {
                objectCreator.CreateResource(resource);
                TestContext.Api.Resources.MoveTo(resource, ResourceState.Complete);
            }

            CompleteResource4 = (UnmanagedResource)TestContext.Api.Resources.Read(CompleteResource4.Id);
            CompleteResource5 = (UnmanagedResource)TestContext.Api.Resources.Read(CompleteResource5.Id);
        }

        private void CreateCapacities()
        {
            Frequency = new NumberCapacity
            {
                Name = $"Frequency _{Guid.NewGuid()}",
                Units = "MHz",
                RangeMin = 1,
                RangeMax = 25,
                StepSize = 0.0001m,
                Decimals = 4,
            };

            Bandwidth = new RangeCapacity
            {
                Name = $"Band_{Guid.NewGuid()}",
                Units = "Hz",
                RangeMin = 1000,
                RangeMax = 30000,
                StepSize = 0.01m,
                Decimals = 2,
            };

            var capacities = new Capacity[]
            {
                Frequency,
                Bandwidth,
            };

            foreach (var capacity in capacities)
            {
                objectCreator.CreateCapacity(capacity);
            }

            Frequency = (NumberCapacity)TestContext.Api.Capacities.Read(Frequency.Id);
            Bandwidth = (RangeCapacity)TestContext.Api.Capacities.Read(Bandwidth.Id);
        }

        private void CreateProperties()
        {
            Channel = new ResourceProperty
            {
                Name = $"Channel_{Guid.NewGuid()}",
            };

            Color = new ResourceProperty
            {
                Name = $"Color_{Guid.NewGuid()}",
            };

            Format = new ResourceProperty
            {
                Name = $"Format_{Guid.NewGuid()}",
            };

            var properties = new ResourceProperty[]
            {
                Channel,
                Color,
                Format,
            };

            foreach (var property in properties)
            {
                objectCreator.CreateProperty(property);
            }

            Channel = TestContext.Api.Properties.Read(Channel.Id);
            Color = TestContext.Api.Properties.Read(Color.Id);
            Format = TestContext.Api.Properties.Read(Format.Id);
        }

        private void CreateResourcePools()
        {
            ResourcePool1 = new ResourcePool
            {
                Name = $"ResourcePool_1_{Guid.NewGuid()}",
                IconImage = "icon_1.png",
            };

            ResourcePool2 = new ResourcePool
            {
                Name = $"ResourcePool_2_{Guid.NewGuid()}",
                IconImage = "icon_2.png",
            };

            ResourcePool3 = new ResourcePool
            {
                Name = $"ResourcePool_3_{Guid.NewGuid()}",
                IconImage = "icon_3.png",
            };

            var resourcePools = new ResourcePool[]
            {
                ResourcePool1,
                ResourcePool2,
                ResourcePool3,
            };

            foreach (var pool in resourcePools)
            {
                objectCreator.CreateResourcePool(pool);
                TestContext.Api.ResourcePools.MoveTo(pool, ResourcePoolState.Complete);
            }

            ResourcePool1 = TestContext.Api.ResourcePools.Read(ResourcePool1.Id);
            ResourcePool2 = TestContext.Api.ResourcePools.Read(ResourcePool2.Id);
            ResourcePool3 = TestContext.Api.ResourcePools.Read(ResourcePool3.Id);
        }
    }
}