namespace RT_MediaOps.Plan.RST.Filtering
{
    using System;
    using System.Linq;
    using RT_MediaOps.Plan.RegressionTests;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.Profiles;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

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

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
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

        private Tuple<Resource[], FilterElement<Resource>>[] ResourceFilterTestCases => new[]
        {
            new Tuple<Resource[], FilterElement<Resource>>(Setup.Resources!, ResourceFilter),
            new Tuple<Resource[], FilterElement<Resource>>([Setup.DraftResource1!, Setup.DraftResource2!, Setup.DraftResource3!], ResourceFilter.AND(ResourceExposers.Name.Contains("Resource_Draft"))),

            new Tuple<Resource[], FilterElement<Resource>>([Setup.DraftResource2!, Setup.DraftResource3!, Setup.CompleteResource5!], ResourceFilter.AND(ResourceExposers.Concurrency.GreaterThan(6))),
            new Tuple<Resource[], FilterElement<Resource>>([Setup.DraftResource1!, Setup.DraftResource3!, Setup.CompleteResource4!, Setup.ServiceResource1!, Setup.VirtualFunctionResource1!], ResourceFilter.AND(ResourceExposers.IsFavorite.Equal(true))),

            new Tuple<Resource[], FilterElement<Resource>>([Setup.DraftResource1!, Setup.DraftResource2!, Setup.DraftResource3!, Setup.ElementResource1!, Setup.ServiceResource1!, Setup.VirtualFunctionResource1!], ResourceFilter.AND(ResourceExposers.State.Equal((int)ResourceState.Draft))),
            new Tuple<Resource[], FilterElement<Resource>>([Setup.CompleteResource4!, Setup.CompleteResource5!], ResourceFilter.AND(ResourceExposers.State.Equal((int)ResourceState.Complete))),
            new Tuple<Resource[], FilterElement<Resource>>([], ResourceFilter.AND(ResourceExposers.State.Equal((int)ResourceState.Deprecated))),

            new Tuple<Resource[], FilterElement<Resource>>([Setup.DraftResource1!, Setup.DraftResource2!], ResourceFilter.AND(ResourceExposers.ResourcePoolIds.Contains(Setup.ResourcePool1!.Id))),
            new Tuple<Resource[], FilterElement<Resource>>([Setup.DraftResource3!, Setup.CompleteResource4!, Setup.CompleteResource5!, Setup.VirtualFunctionResource1!], ResourceFilter.AND(ResourceExposers.ResourcePoolIds.Contains(Setup.ResourcePool2!.Id))),
            new Tuple<Resource[], FilterElement<Resource>>([], ResourceFilter.AND(ResourceExposers.ResourcePoolIds.Contains(Setup.ResourcePool3!.Id))),

            new Tuple<Resource[], FilterElement<Resource>>([Setup.DraftResource1!], ResourceFilter.AND(ResourceExposers.Capabilities.CapabilityId.Equal(Setup.Location!.Id))),
            new Tuple<Resource[], FilterElement<Resource>>([Setup.DraftResource1!], ResourceFilter.AND(ResourceExposers.Capabilities.Discretes.Contains("USA"))),
            new Tuple<Resource[], FilterElement<Resource>>([Setup.DraftResource1!], ResourceFilter.AND(ResourceExposers.Capabilities.CapabilityId.Equal(Setup.Location.Id)).AND(ResourceExposers.Capabilities.Discretes.Contains("USA"))),

            new Tuple<Resource[], FilterElement<Resource>>([Setup.VirtualFunctionResource1!, Setup.DraftResource3!, Setup.CompleteResource4!], ResourceFilter.AND(ResourceExposers.Capabilities.CapabilityId.Equal(Setup.Resolution!.Id))),
            new Tuple<Resource[], FilterElement<Resource>>([Setup.VirtualFunctionResource1!], ResourceFilter.AND(ResourceExposers.Capabilities.Discretes.Contains("720p"))),

            new Tuple<Resource[], FilterElement<Resource>>([Setup.DraftResource1!, Setup.DraftResource2!, Setup.CompleteResource4!], ResourceFilter.AND(ResourceExposers.Capacities.CapacityId.Equal(Setup.Frequency!.Id))),
            new Tuple<Resource[], FilterElement<Resource>>([Setup.DraftResource1!, Setup.DraftResource2!], ResourceFilter.AND(ResourceExposers.Capacities.CapacityId.Equal(Setup.Bandwidth!.Id))),

            new Tuple<Resource[], FilterElement<Resource>>([Setup.DraftResource2!, Setup.DraftResource3!, Setup.CompleteResource4!, Setup.CompleteResource5!], ResourceFilter.AND(ResourceExposers.Properties.PropertyId.Equal(Setup.Format!.Id))),
            new Tuple<Resource[], FilterElement<Resource>>([Setup.DraftResource2!, Setup.DraftResource3!, Setup.CompleteResource4!, Setup.CompleteResource5!], ResourceFilter.AND(ResourceExposers.Properties.Value.Equal("16:9"))),

            new Tuple<Resource[], FilterElement<Resource>>([Setup.DraftResource2!, Setup.CompleteResource5!], ResourceFilter.AND(ResourceExposers.Properties.PropertyId.Equal(Setup.Channel!.Id))),
            new Tuple<Resource[], FilterElement<Resource>>([Setup.DraftResource2!, Setup.CompleteResource5!], ResourceFilter.AND(ResourceExposers.Properties.Value.Equal("VRT"))),

            new Tuple<Resource[], FilterElement<Resource>>([], ResourceFilter.AND(ResourceExposers.Properties.PropertyId.Equal(Setup.Color!.Id))),

            new Tuple<Resource[], FilterElement<Resource>>([Setup.ElementResource1!], ResourceFilter.AND(ElementResourceExposers.AgentId.Equal(100))),
            new Tuple<Resource[], FilterElement<Resource>>([Setup.ElementResource1!], ResourceFilter.AND(ElementResourceExposers.ElementId.Equal(200))),

            new Tuple<Resource[], FilterElement<Resource>>([Setup.ServiceResource1!], ResourceFilter.AND(ServiceResourceExposers.AgentId.Equal(100))),
            new Tuple<Resource[], FilterElement<Resource>>([Setup.ServiceResource1!], ResourceFilter.AND(ServiceResourceExposers.ServiceId.Equal(20))),

            new Tuple<Resource[], FilterElement<Resource>>([Setup.VirtualFunctionResource1!], ResourceFilter.AND(VirtualFunctionResourceExposers.AgentId.Equal(100))),
            new Tuple<Resource[], FilterElement<Resource>>([Setup.VirtualFunctionResource1!], ResourceFilter.AND(VirtualFunctionResourceExposers.ElementId.Equal(200))),
            new Tuple<Resource[], FilterElement<Resource>>([Setup.VirtualFunctionResource1!], ResourceFilter.AND(VirtualFunctionResourceExposers.FunctionId.Equal(Setup.VirtualFunctionResource1!.FunctionId))),
            new Tuple<Resource[], FilterElement<Resource>>([Setup.VirtualFunctionResource1!], ResourceFilter.AND(VirtualFunctionResourceExposers.FunctionTableIndex.Equal("VF_Table_1"))),
        };

        private Tuple<Capability[], FilterElement<Capability>>[] CapabilityFilterTestCases => new[]
        {
            new Tuple<Capability[], FilterElement<Capability>>(Setup.Capabilities, CapabilityFilter),
            new Tuple<Capability[], FilterElement<Capability>>([Setup.Location!], CapabilityFilter.AND(CapabilityExposers.Name.Contains("Location"))),
            new Tuple<Capability[], FilterElement<Capability>>([Setup.Priority!], CapabilityFilter.AND(CapabilityExposers.Name.Equal(Setup.Priority!.Name))),
            new Tuple<Capability[], FilterElement<Capability>>([Setup.Location!], CapabilityFilter.AND(CapabilityExposers.IsMandatory.Equal(true))),
            new Tuple<Capability[], FilterElement<Capability>>([Setup.Resolution!, Setup.Priority!], CapabilityFilter.AND(CapabilityExposers.IsTimeDependent.Equal(false))),
            new Tuple<Capability[], FilterElement<Capability>>([Setup.Location!], CapabilityFilter.AND(CapabilityExposers.Discretes.Contains("Belgium"))),
            new Tuple<Capability[], FilterElement<Capability>>([Setup.Resolution!], CapabilityFilter.AND(CapabilityExposers.Discretes.Contains("4K"))),
        };

        private Tuple<Capacity[], FilterElement<Capacity>>[] CapacityFilterTestCases => new[]
        {
            new Tuple<Capacity[], FilterElement<Capacity>>(Setup.Capacities!, CapacityFilter),
            new Tuple<Capacity[], FilterElement<Capacity>>([Setup.Frequency!], CapacityFilter.AND(CapacityExposers.Name.Contains("Frequency"))),
            new Tuple<Capacity[], FilterElement<Capacity>>([], CapacityFilter.AND(CapacityExposers.IsMandatory.Equal(true))),
            new Tuple<Capacity[], FilterElement<Capacity>>([Setup.Frequency!, Setup.Bandwidth!], CapacityFilter.AND(CapacityExposers.Units.Contains("Hz"))),
            new Tuple<Capacity[], FilterElement<Capacity>>([Setup.Frequency!, Setup.Bandwidth!], CapacityFilter.AND(CapacityExposers.RangeMin.GreaterThan(0))),
            new Tuple<Capacity[], FilterElement<Capacity>>([], CapacityFilter.AND(CapacityExposers.RangeMin.GreaterThan(1000))),
            new Tuple<Capacity[], FilterElement<Capacity>>([Setup.Frequency!, Setup.Bandwidth!], CapacityFilter.AND(CapacityExposers.RangeMax.LessThanOrEqual(30000))),
            new Tuple<Capacity[], FilterElement<Capacity>>([Setup.Frequency!, Setup.Bandwidth!], CapacityFilter.AND(CapacityExposers.StepSize.LessThan(1m))),
            new Tuple<Capacity[], FilterElement<Capacity>>([Setup.Frequency!], CapacityFilter.AND(CapacityExposers.Decimals.GreaterThan(2))),
            new Tuple<Capacity[], FilterElement<Capacity>>([Setup.Frequency!, Setup.Bandwidth!], CapacityFilter.AND(CapacityExposers.HasRangeMin.Equal(true))),
            new Tuple<Capacity[], FilterElement<Capacity>>([Setup.Frequency!, Setup.Bandwidth!], CapacityFilter.AND(CapacityExposers.HasRangeMax.Equal(true))),
            new Tuple<Capacity[], FilterElement<Capacity>>([Setup.Frequency!, Setup.Bandwidth!], CapacityFilter.AND(CapacityExposers.HasUnits.Equal(true))),
            new Tuple<Capacity[], FilterElement<Capacity>>([Setup.Frequency!, Setup.Bandwidth!], CapacityFilter.AND(CapacityExposers.HasStepSize.Equal(true))),
            new Tuple<Capacity[], FilterElement<Capacity>>([Setup.Frequency!, Setup.Bandwidth!], CapacityFilter.AND(CapacityExposers.HasDecimals.Equal(true))),
            new Tuple<Capacity[], FilterElement<Capacity>>([Setup.Reach!], CapacityFilter.AND(CapacityExposers.HasRangeMin.NotEqual(true))),
            new Tuple<Capacity[], FilterElement<Capacity>>([Setup.Reach!], CapacityFilter.AND(CapacityExposers.HasRangeMax.NotEqual(true))),
            new Tuple<Capacity[], FilterElement<Capacity>>([Setup.Reach!], CapacityFilter.AND(CapacityExposers.HasUnits.NotEqual(true))),
            new Tuple<Capacity[], FilterElement<Capacity>>([Setup.Reach!], CapacityFilter.AND(CapacityExposers.HasStepSize.NotEqual(true))),
            new Tuple<Capacity[], FilterElement<Capacity>>([Setup.Reach!], CapacityFilter.AND(CapacityExposers.HasDecimals.NotEqual(true))),
        };

        private Tuple<ResourcePool[], FilterElement<ResourcePool>>[] ResourcePoolFilterTestCases => new[]
        {
            new Tuple<ResourcePool[], FilterElement<ResourcePool>>(Setup.ResourcePools!, ResourcePoolFilter),
            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([Setup.ResourcePool1!], ResourcePoolFilter.AND(ResourcePoolExposers.Name.Contains("ResourcePool_Draft"))),
            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([Setup.ResourcePool2!, Setup.ResourcePool3!, Setup.ResourcePool4!, Setup.ResourcePool5!], ResourcePoolFilter.AND(ResourcePoolExposers.Name.Contains("ResourcePool_Complete"))),

            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([Setup.ResourcePool5!], ResourcePoolFilter.AND(ResourcePoolExposers.IconImage.Contains(".jpeg"))),
            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([Setup.ResourcePool1!, Setup.ResourcePool2!, Setup.ResourcePool3!], ResourcePoolFilter.AND(ResourcePoolExposers.IconImage.Contains(".png"))),
            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([], ResourcePoolFilter.AND(ResourcePoolExposers.IconImage.Contains(".gif"))),
            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([Setup.ResourcePool4!], ResourcePoolFilter.AND(ResourcePoolExposers.HasIconImage.Equal(false))),

            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([Setup.ResourcePool1!], ResourcePoolFilter.AND(ResourcePoolExposers.State.Equal((int)ResourcePoolState.Draft))),
            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([Setup.ResourcePool2!, Setup.ResourcePool3!, Setup.ResourcePool4!, Setup.ResourcePool5!], ResourcePoolFilter.AND(ResourcePoolExposers.State.Equal((int)ResourcePoolState.Complete))),
            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([], ResourcePoolFilter.AND(ResourcePoolExposers.State.Equal((int)ResourcePoolState.Deprecated))),

            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([], ResourcePoolFilter.AND(ResourcePoolExposers.HasUrl.Equal(true).AND(ResourcePoolExposers.Url.Equal("skyline.be")))),
            new Tuple<ResourcePool[], FilterElement<ResourcePool>>(Setup.ResourcePools!, ResourcePoolFilter.AND(ResourcePoolExposers.HasUrl.Equal(false))),

            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([Setup.ResourcePool1!, Setup.ResourcePool2!], ResourcePoolFilter.AND(ResourcePoolExposers.Capabilities.CapabilityId.Equal(Setup.Location!.Id))),
            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([Setup.ResourcePool1!, Setup.ResourcePool2!, Setup.ResourcePool3!], ResourcePoolFilter.AND(ResourcePoolExposers.Capabilities.CapabilityId.Equal(Setup.Priority!.Id))),
            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([Setup.ResourcePool3!], ResourcePoolFilter.AND(ResourcePoolExposers.Capabilities.CapabilityId.Equal(Setup.Resolution!.Id))),

            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([Setup.ResourcePool1!, Setup.ResourcePool2!], ResourcePoolFilter.AND(ResourcePoolExposers.Capabilities.Discretes.Contains("Belgium"))),
            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([Setup.ResourcePool1!, Setup.ResourcePool2!], ResourcePoolFilter.AND(ResourcePoolExposers.Capabilities.Discretes.Contains("Low"))),
            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([Setup.ResourcePool3!], ResourcePoolFilter.AND(ResourcePoolExposers.Capabilities.Discretes.Contains("4K"))),

            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([Setup.ResourcePool4!, Setup.ResourcePool5!], ResourcePoolFilter.AND(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId.Equal(Setup.ResourcePool1!.Id))),
            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([Setup.ResourcePool4!, Setup.ResourcePool5!], ResourcePoolFilter.AND(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId.Equal(Setup.ResourcePool2!.Id))),
            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([], ResourcePoolFilter.AND(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId.Equal(Setup.ResourcePool3!.Id))),
            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([], ResourcePoolFilter.AND(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId.Equal(Setup.ResourcePool4!.Id))),
            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([], ResourcePoolFilter.AND(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId.Equal(Setup.ResourcePool5!.Id))),

            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([Setup.ResourcePool4!, Setup.ResourcePool5!], ResourcePoolFilter.AND(ResourcePoolExposers.LinkedResourcePools.SelectionType.Equal(ResourceSelectionType.Automatic))),
            new Tuple<ResourcePool[], FilterElement<ResourcePool>>([Setup.ResourcePool5!], ResourcePoolFilter.AND(ResourcePoolExposers.LinkedResourcePools.SelectionType.Equal(ResourceSelectionType.Manual))),
        };

        private Tuple<ResourceProperty[], FilterElement<ResourceProperty>>[] PropertyFilterTestCases => new[]
        {
            new Tuple<ResourceProperty[], FilterElement<ResourceProperty>>(Setup.Properties!, PropertyFilter),
            new Tuple<ResourceProperty[], FilterElement<ResourceProperty>>([Setup.Format!], PropertyFilter.AND(ResourcePropertyExposers.Name.Contains("Format"))),
            new Tuple<ResourceProperty[], FilterElement<ResourceProperty>>([Setup.Channel!], PropertyFilter.AND(ResourcePropertyExposers.Name.Contains("Channel"))),
            new Tuple<ResourceProperty[], FilterElement<ResourceProperty>>([Setup.Color!], PropertyFilter.AND(ResourcePropertyExposers.Name.Contains("Color"))),
            new Tuple<ResourceProperty[], FilterElement<ResourceProperty>>([], PropertyFilter.AND(ResourcePropertyExposers.Name.Contains("Something"))),
            new Tuple<ResourceProperty[], FilterElement<ResourceProperty>>(Setup.Properties!, PropertyFilter.AND(ResourcePropertyExposers.Name.NotContains("Something"))),
            new Tuple<ResourceProperty[], FilterElement<ResourceProperty>>(Setup.Properties!, PropertyFilter.AND(ResourcePropertyExposers.Name.NotEqual("Something"))),
        };

        private Tuple<Configuration[], FilterElement<Configuration>>[] ConfigurationFilterTestCases => new[]
        {
            new Tuple<Configuration[], FilterElement<Configuration>>(Setup.Configurations!, ConfigurationFilter),
            new Tuple<Configuration[], FilterElement<Configuration>>([Setup.Region!], ConfigurationFilter.AND(ConfigurationExposers.Name.Contains("Region"))),
            new Tuple<Configuration[], FilterElement<Configuration>>([Setup.Distance!], ConfigurationFilter.AND(ConfigurationExposers.IsMandatory.Equal(true))),
            new Tuple<Configuration[], FilterElement<Configuration>>([Setup.Region!, Setup.ResolutionConfig!, Setup.PriorityConfig!], ConfigurationFilter.AND(ConfigurationExposers.IsMandatory.Equal(false))),
            new Tuple<Configuration[], FilterElement<Configuration>>([], ConfigurationFilter.AND(DiscreteTextConfigurationExposers.Discretes.Contains("SD"))),
            new Tuple<Configuration[], FilterElement<Configuration>>([Setup.ResolutionConfig!], ConfigurationFilter.AND(DiscreteTextConfigurationExposers.Discretes.Contains("720p"))),
            new Tuple<Configuration[], FilterElement<Configuration>>([], ConfigurationFilter.AND(DiscreteNumberConfigurationExposers.Discretes.Contains(20))),
            new Tuple<Configuration[], FilterElement<Configuration>>([Setup.PriorityConfig!], ConfigurationFilter.AND(DiscreteNumberConfigurationExposers.Discretes.Contains(100))),
        };

        [TestMethod]
        public void ReadResourcesWithFilter()
        {
            foreach (var (expectedObjects, filter) in ResourceFilterTestCases)
            {
                var expectedObjectIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
                var actualObjectIds = TestContext.Api.Resources.Read(filter).Select(x => x.Id).OrderBy(x => x).ToList();

                Assert.IsTrue(expectedObjectIds.SequenceEqual(actualObjectIds), filter.ToString());
            }
        }

        [TestMethod]
        public void CountResourcesWithFilter()
        {
            foreach (var (expectedObjects, filter) in ResourceFilterTestCases)
            {
                Assert.AreEqual(expectedObjects.Length, TestContext.Api.Resources.Count(filter), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadResourcesPagedWithFilter_DefaultPageSize()
        {
            foreach (var (expectedObjects, filter) in ResourceFilterTestCases)
            {
                var expectedObjectIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
                var pages = TestContext.Api.Resources.ReadPaged(filter).ToList();
                var actualObjectIds = pages.SelectMany(x => x).Select(x => x.Id).OrderBy(x => x).ToList();

                Assert.IsTrue(expectedObjectIds.SequenceEqual(actualObjectIds), filter.ToString());
                Assert.AreEqual(1, pages.Count(), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadResourcesPagedWithFilter_CustomPageSize()
        {
            foreach (var (expectedObjects, filter) in ResourceFilterTestCases)
            {
                var expectedObjectIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
                var pages = TestContext.Api.Resources.ReadPaged(filter, 2).ToList();
                var actualObjectIds = pages.SelectMany(x => x).Select(x => x.Id).OrderBy(x => x).ToList();

                Assert.IsTrue(expectedObjectIds.SequenceEqual(actualObjectIds), filter.ToString());

                foreach (var page in pages)
                {
                    Assert.IsTrue(page.Count() <= 2, filter.ToString());
                }
            }
        }

        [TestMethod]
        public void ReadResourcePoolsWithFilter()
        {
            foreach (var (expectedObjects, filter) in ResourcePoolFilterTestCases)
            {
                var expectedIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
                var actualIds = TestContext.Api.ResourcePools.Read(filter).Select(x => x.Id).OrderBy(x => x).ToList();

                Assert.IsTrue(expectedIds.SequenceEqual(actualIds), filter.ToString());
            }
        }

        [TestMethod]
        public void CountResourcePoolsWithFilter()
        {
            foreach (var (expectedObjects, filter) in ResourcePoolFilterTestCases)
            {
                Assert.AreEqual(expectedObjects.Length, TestContext.Api.ResourcePools.Count(filter), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadResourcePoolsPagedWithFilter_DefaultPageSize()
        {
            foreach (var (expectedObjects, filter) in ResourcePoolFilterTestCases)
            {
                var expectedIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
                var pages = TestContext.Api.ResourcePools.ReadPaged(filter).ToList();
                var actualIds = pages.SelectMany(x => x).Select(x => x.Id).OrderBy(x => x).ToList();

                Assert.AreEqual(1, pages.Count(), filter.ToString());
                Assert.IsTrue(expectedIds.SequenceEqual(actualIds), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadResourcePoolsPagedWithFilter_CustomPageSize()
        {
            foreach (var (expectedObjects, filter) in ResourcePoolFilterTestCases)
            {
                var expectedIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
                var pages = TestContext.Api.ResourcePools.ReadPaged(filter, 2).ToList();
                var actualIds = pages.SelectMany(x => x).Select(x => x.Id).OrderBy(x => x).ToList();

                Assert.IsTrue(expectedIds.SequenceEqual(actualIds), filter.ToString());

                foreach (var page in pages)
                {
                    Assert.IsTrue(page.Count() <= 2, filter.ToString());
                }
            }
        }

        [TestMethod]
        public void ReadCapabilitiesWithFilter()
        {
            foreach (var (expectedObjects, filter) in CapabilityFilterTestCases)
            {
                var expectedObjectIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
                var actualObjectIds = TestContext.Api.Capabilities.Read(filter).Select(x => x.Id).OrderBy(x => x).ToList();

                Assert.IsTrue(expectedObjectIds.SequenceEqual(actualObjectIds), filter.ToString());
            }
        }

        [TestMethod]
        public void CountCapabilitiesWithFilter()
        {
            foreach (var (expectedObjects, filter) in CapabilityFilterTestCases)
            {
                Assert.AreEqual(expectedObjects.Length, TestContext.Api.Capabilities.Count(filter), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadCapabilitiesPagedWithFilter_DefaultPageSize()
        {
            foreach (var (expectedObjects, filter) in CapabilityFilterTestCases)
            {
                var expectedObjectIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
                var pages = TestContext.Api.Capabilities.ReadPaged(filter).ToList();
                var actualObjectIds = pages.SelectMany(x => x).Select(x => x.Id).OrderBy(x => x).ToList();

                Assert.AreEqual(1, pages.Count(), filter.ToString());
                Assert.IsTrue(expectedObjectIds.SequenceEqual(actualObjectIds), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadCapabilitiesPagedWithFilter_CustomPageSize()
        {
            foreach (var (expectedObjects, filter) in CapabilityFilterTestCases)
            {
                var expectedObjectIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
                var pages = TestContext.Api.Capabilities.ReadPaged(filter, 2).ToList();
                var actualObjectIds = pages.SelectMany(x => x).Select(x => x.Id).OrderBy(x => x).ToList();

                Assert.IsTrue(expectedObjectIds.SequenceEqual(actualObjectIds), filter.ToString());

                foreach (var page in pages)
                {
                    Assert.IsTrue(page.Count() <= 2, filter.ToString());
                }
            }
        }

        [TestMethod]
        public void ReadCapacitiesWithFilter()
        {
            foreach (var (expectedObjects, filter) in CapacityFilterTestCases)
            {
                var expectedIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
                var actualIds = TestContext.Api.Capacities.Read(filter).Select(x => x.Id).OrderBy(x => x).ToList();

                Assert.IsTrue(expectedIds.SequenceEqual(actualIds), filter.ToString());
            }
        }

        [TestMethod]
        public void CountCapacitiesWithFilter()
        {
            foreach (var (expectedObjects, filter) in CapacityFilterTestCases)
            {
                Assert.AreEqual(expectedObjects.Length, TestContext.Api.Capacities.Count(filter), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadCapacitiesPagedWithFilter_DefaultPageSize()
        {
            foreach (var (expectedObjects, filter) in CapacityFilterTestCases)
            {
                var expectedIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
                var pages = TestContext.Api.Capacities.ReadPaged(filter).ToList();
                var actualIds = pages.SelectMany(x => x).Select(x => x.Id).OrderBy(x => x).ToList();

                Assert.AreEqual(1, pages.Count(), filter.ToString());
                Assert.IsTrue(expectedIds.SequenceEqual(actualIds), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadCapacitiesPagedWithFilter_CustomPageSize()
        {
            foreach (var (expectedObjects, filter) in CapacityFilterTestCases)
            {
                var expectedIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
                var pages = TestContext.Api.Capacities.ReadPaged(filter, 2).ToList();
                var actualIds = pages.SelectMany(x => x).Select(x => x.Id).OrderBy(x => x).ToList();

                Assert.IsTrue(expectedIds.SequenceEqual(actualIds), filter.ToString());

                foreach (var page in pages)
                {
                    Assert.IsTrue(page.Count() <= 2, filter.ToString());
                }
            }
        }

        [TestMethod]
        public void ReadConfigurationsWithFilter()
        {
            foreach (var (expectedObjects, filter) in ConfigurationFilterTestCases)
            {
                var expectedIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
                var actualIds = TestContext.Api.Configurations.Read(filter).Select(x => x.Id).OrderBy(x => x).ToList();

                Assert.IsTrue(expectedIds.SequenceEqual(actualIds), filter.ToString());
            }
        }

        [TestMethod]
        public void CountConfigurationsWithFilter()
        {
            foreach (var (expectedObjects, filter) in ConfigurationFilterTestCases)
            {
                Assert.AreEqual(expectedObjects.Length, TestContext.Api.Configurations.Count(filter), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadConfigurationsPagedWithFilter_DefaultPageSize()
        {
            foreach (var (expectedObjects, filter) in ConfigurationFilterTestCases)
            {
                var expectedIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
                var pages = TestContext.Api.Configurations.ReadPaged(filter).ToList();
                var actualIds = pages.SelectMany(x => x).Select(x => x.Id).OrderBy(x => x).ToList();

                Assert.AreEqual(1, pages.Count(), filter.ToString());
                Assert.IsTrue(expectedIds.SequenceEqual(actualIds), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadConfigurationsPagedWithFilter_CustomPageSize()
        {
            foreach (var (expectedObjects, filter) in ConfigurationFilterTestCases)
            {
                var expectedIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
                var pages = TestContext.Api.Configurations.ReadPaged(filter, 2).ToList();
                var actualIds = pages.SelectMany(x => x).Select(x => x.Id).OrderBy(x => x).ToList();

                Assert.IsTrue(expectedIds.SequenceEqual(actualIds), filter.ToString());

                foreach (var page in pages)
                {
                    Assert.IsTrue(page.Count() <= 2, filter.ToString());
                }
            }
        }

        [TestMethod]
        public void ReadResourcePropertiesWithFilter()
        {
            foreach (var (expectedObjects, filter) in PropertyFilterTestCases)
            {
                var expectedIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
                var actualIds = TestContext.Api.Properties.Read(filter).Select(x => x.Id).OrderBy(x => x).ToList();

                Assert.IsTrue(expectedIds.SequenceEqual(actualIds), filter.ToString());
            }
        }

        [TestMethod]
        public void CountResourcePropertiesWithFilter()
        {
            foreach (var (expectedObjects, filter) in PropertyFilterTestCases)
            {
                Assert.AreEqual(expectedObjects.Length, TestContext.Api.Properties.Count(filter), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadResourcePropertiesPagedWithFilter_DefaultPageSize()
        {
            foreach (var (expectedObjects, filter) in PropertyFilterTestCases)
            {
                var expectedIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
                var pages = TestContext.Api.Properties.ReadPaged(filter).ToList();
                var actualIds = pages.SelectMany(x => x).Select(x => x.Id).OrderBy(x => x).ToList();

                Assert.AreEqual(1, pages.Count(), filter.ToString());
                Assert.IsTrue(expectedIds.SequenceEqual(actualIds), filter.ToString());
            }
        }

        [TestMethod]
        public void ReadResourcePropertiesPagedWithFilter_CustomPageSize()
        {
            foreach (var (expectedObjects, filter) in PropertyFilterTestCases)
            {
                var expectedIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
                var pages = TestContext.Api.Properties.ReadPaged(filter, 2).ToList();
                var actualIds = pages.SelectMany(x => x).Select(x => x.Id).OrderBy(x => x).ToList();

                Assert.IsTrue(expectedIds.SequenceEqual(actualIds), filter.ToString());

                foreach (var page in pages)
                {
                    Assert.IsTrue(page.Count() <= 2, filter.ToString());
                }
            }
        }
    }
}