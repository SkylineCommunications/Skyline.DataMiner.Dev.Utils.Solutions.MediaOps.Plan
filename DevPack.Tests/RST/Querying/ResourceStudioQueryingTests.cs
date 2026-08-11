namespace RT_MediaOps.Plan.RST.Querying
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Querying;
	using RT_MediaOps.Plan.RegressionTests;
	using RT_MediaOps.Plan.RST.Filtering;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class ResourceStudioQueryingTests
	{
		private static TestObjectCreator? objectCreator;
		private static ResourceFilteringSetup? setup;

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		private static ResourceFilteringSetup Setup => setup ?? throw new InvalidOperationException("Test setup was not initialized.");

		[ClassInitialize]
		public static void ClassInitialize(TestContext context)
		{
			objectCreator = new TestObjectCreator(TestContext);
			setup = new ResourceFilteringSetup(objectCreator, TestContext);
		}

		[ClassCleanup(ClassCleanupBehavior.EndOfClass)]
		public static void ClassCleanup()
		{
			objectCreator?.Dispose();
			objectCreator = null;
			setup = null;
		}

		private FilterElement<Resource> ResourceFilter => new ORFilterElement<Resource>(Setup.Resources.Select(x => ResourceExposers.Id.Equal(x.Id)).ToArray());

		private FilterElement<ResourcePool> ResourcePoolFilter => new ORFilterElement<ResourcePool>(Setup.ResourcePools.Select(x => ResourcePoolExposers.Id.Equal(x.Id)).ToArray());

		private FilterElement<Capability> CapabilityFilter => new ORFilterElement<Capability>(Setup.Capabilities.Select(x => CapabilityExposers.Id.Equal(x.Id)).ToArray());

		private FilterElement<Capacity> CapacityFilter => new ORFilterElement<Capacity>(Setup.Capacities.Select(x => CapacityExposers.Id.Equal(x.Id)).ToArray());

		private FilterElement<ResourceProperty> PropertyFilter => new ORFilterElement<ResourceProperty>(Setup.Properties.Select(x => ResourcePropertyExposers.Id.Equal(x.Id)).ToArray());

		private FilterElement<Configuration> ConfigurationFilter => new ORFilterElement<Configuration>(Setup.Configurations.Select(x => ConfigurationExposers.Id.Equal(x.Id)).ToArray());

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query.
		/// </summary>
		private Tuple<Resource[], IQuery<Resource>>[] ResourceQueryTestCases => new[]
		{
			new Tuple<Resource[], IQuery<Resource>>(
				[
					Setup.ElementResource1!,
					Setup.CompleteResource4!,
					Setup.CompleteResource5!,
					Setup.DraftResource1!,
					Setup.DraftResource2!,
					Setup.DraftResource3!,
					Setup.ServiceResource1!,
					Setup.VirtualFunctionResource1!,
				],
				ResourceFilter.ToQuery().OrderBy(ResourceExposers.Name)),
			new Tuple<Resource[], IQuery<Resource>>(
				[
					Setup.VirtualFunctionResource1!,
					Setup.ServiceResource1!,
					Setup.DraftResource3!,
					Setup.DraftResource2!,
					Setup.DraftResource1!,
					Setup.CompleteResource5!,
					Setup.CompleteResource4!,
					Setup.ElementResource1!,
				],
				ResourceFilter.ToQuery().OrderByDescending(ResourceExposers.Name)),

			new Tuple<Resource[], IQuery<Resource>>(
				[Setup.DraftResource1!, Setup.DraftResource2!, Setup.DraftResource3!],
				ResourceFilter.AND(ResourceExposers.Name.Contains("Resource_Draft")).ToQuery().OrderBy(ResourceExposers.Concurrency)),
			new Tuple<Resource[], IQuery<Resource>>(
				[Setup.DraftResource3!, Setup.DraftResource2!, Setup.DraftResource1!],
				ResourceFilter.AND(ResourceExposers.Name.Contains("Resource_Draft")).ToQuery().OrderByDescending(ResourceExposers.Concurrency)),

			new Tuple<Resource[], IQuery<Resource>>(
				[],
				ResourceFilter.AND(ResourceExposers.Name.Contains("Unknown")).ToQuery().OrderBy(ResourceExposers.Name)),
		};

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query with a limit.
		/// </summary>
		private Tuple<Resource[], IQuery<Resource>>[] ResourceLimitedQueryTestCases => new[]
		{
			new Tuple<Resource[], IQuery<Resource>>(
				[Setup.DraftResource1!],
				ResourceFilter.AND(ResourceExposers.Name.Contains("Resource_Draft")).ToQuery().OrderBy(ResourceExposers.Name).Limit(1)),
			new Tuple<Resource[], IQuery<Resource>>(
				[Setup.DraftResource1!, Setup.DraftResource2!],
				ResourceFilter.AND(ResourceExposers.Name.Contains("Resource_Draft")).ToQuery().OrderBy(ResourceExposers.Name).Limit(2)),
			new Tuple<Resource[], IQuery<Resource>>(
				[Setup.DraftResource1!, Setup.DraftResource2!, Setup.DraftResource3!],
				ResourceFilter.AND(ResourceExposers.Name.Contains("Resource_Draft")).ToQuery().OrderBy(ResourceExposers.Name).Limit(10)),
		};

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query.
		/// </summary>
		private Tuple<ResourcePool[], IQuery<ResourcePool>>[] ResourcePoolQueryTestCases => new[]
		{
			new Tuple<ResourcePool[], IQuery<ResourcePool>>(
				[Setup.ResourcePool2!, Setup.ResourcePool3!, Setup.ResourcePool4!, Setup.ResourcePool5!, Setup.ResourcePool1!],
				ResourcePoolFilter.ToQuery().OrderBy(ResourcePoolExposers.Name)),
			new Tuple<ResourcePool[], IQuery<ResourcePool>>(
				[Setup.ResourcePool1!, Setup.ResourcePool5!, Setup.ResourcePool4!, Setup.ResourcePool3!, Setup.ResourcePool2!],
				ResourcePoolFilter.ToQuery().OrderByDescending(ResourcePoolExposers.Name)),
		};

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query with a limit.
		/// </summary>
		private Tuple<ResourcePool[], IQuery<ResourcePool>>[] ResourcePoolLimitedQueryTestCases => new[]
		{
			new Tuple<ResourcePool[], IQuery<ResourcePool>>(
				[Setup.ResourcePool2!, Setup.ResourcePool3!],
				ResourcePoolFilter.AND(ResourcePoolExposers.Name.Contains("ResourcePool_Complete")).ToQuery().OrderBy(ResourcePoolExposers.Name).Limit(2)),
		};

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query.
		/// </summary>
		private Tuple<Capability[], IQuery<Capability>>[] CapabilityQueryTestCases => new[]
		{
			new Tuple<Capability[], IQuery<Capability>>(
				[Setup.Location!, Setup.Priority!, Setup.Resolution!],
				CapabilityFilter.ToQuery().OrderBy(CapabilityExposers.Name)),
			new Tuple<Capability[], IQuery<Capability>>(
				[Setup.Resolution!, Setup.Priority!, Setup.Location!],
				CapabilityFilter.ToQuery().OrderByDescending(CapabilityExposers.Name)),
		};

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query.
		/// </summary>
		private Tuple<Capacity[], IQuery<Capacity>>[] CapacityQueryTestCases => new[]
		{
			new Tuple<Capacity[], IQuery<Capacity>>(
				[Setup.Bandwidth!, Setup.Frequency!, Setup.Reach!],
				CapacityFilter.ToQuery().OrderBy(CapacityExposers.Name)),
			new Tuple<Capacity[], IQuery<Capacity>>(
				[Setup.Reach!, Setup.Frequency!, Setup.Bandwidth!],
				CapacityFilter.ToQuery().OrderByDescending(CapacityExposers.Name)),
		};

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query.
		/// </summary>
		private Tuple<ResourceProperty[], IQuery<ResourceProperty>>[] ResourcePropertyQueryTestCases => new[]
		{
			new Tuple<ResourceProperty[], IQuery<ResourceProperty>>(
				[Setup.Channel!, Setup.Color!, Setup.Format!],
				PropertyFilter.ToQuery().OrderBy(ResourcePropertyExposers.Name)),
			new Tuple<ResourceProperty[], IQuery<ResourceProperty>>(
				[Setup.Format!, Setup.Color!, Setup.Channel!],
				PropertyFilter.ToQuery().OrderByDescending(ResourcePropertyExposers.Name)),
		};

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query.
		/// </summary>
		private Tuple<Configuration[], IQuery<Configuration>>[] ConfigurationQueryTestCases => new[]
		{
			new Tuple<Configuration[], IQuery<Configuration>>(
				[Setup.Distance!, Setup.PriorityConfig!, Setup.Region!, Setup.ResolutionConfig!],
				ConfigurationFilter.ToQuery().OrderBy(ConfigurationExposers.Name)),
			new Tuple<Configuration[], IQuery<Configuration>>(
				[Setup.ResolutionConfig!, Setup.Region!, Setup.PriorityConfig!, Setup.Distance!],
				ConfigurationFilter.ToQuery().OrderByDescending(ConfigurationExposers.Name)),
		};

		[TestMethod]
		public void ReadResourcesWithQuery()
		{
			foreach (var (expectedObjects, query) in ResourceQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.Resources, expectedObjects, query);
			}
		}

		[TestMethod]
		public void CountResourcesWithQuery()
		{
			foreach (var (expectedObjects, query) in ResourceQueryTestCases)
			{
				QueryAssert.Count(TestContext.Api.Resources, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadResourcesPagedWithQuery_DefaultPageSize()
		{
			foreach (var (expectedObjects, query) in ResourceQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.Resources, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadResourcesPagedWithQuery_CustomPageSize()
		{
			foreach (var (expectedObjects, query) in ResourceQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.Resources, expectedObjects, query, 2);
			}
		}

		[TestMethod]
		public void ReadResourcesWithLimitedQuery()
		{
			foreach (var (expectedObjects, query) in ResourceLimitedQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.Resources, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadResourcesWithUnsupportedOrderByThrowsException()
		{
			var query = ResourceFilter.ToQuery().OrderBy(ResourceExposers.ResourcePoolIds);

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.Resources.Read(query).ToList());
		}

		[TestMethod]
		public void ReadResourcePoolsWithQuery()
		{
			foreach (var (expectedObjects, query) in ResourcePoolQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.ResourcePools, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadResourcePoolsPagedWithQuery_CustomPageSize()
		{
			foreach (var (expectedObjects, query) in ResourcePoolQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.ResourcePools, expectedObjects, query, 2);
			}
		}

		[TestMethod]
		public void CountResourcePoolsWithQuery()
		{
			foreach (var (expectedObjects, query) in ResourcePoolQueryTestCases)
			{
				QueryAssert.Count(TestContext.Api.ResourcePools, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadResourcePoolsWithLimitedQuery()
		{
			foreach (var (expectedObjects, query) in ResourcePoolLimitedQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.ResourcePools, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadCapabilitiesWithQuery()
		{
			foreach (var (expectedObjects, query) in CapabilityQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.Capabilities, expectedObjects, query);
			}
		}

		[TestMethod]
		public void CountCapabilitiesWithQuery()
		{
			foreach (var (expectedObjects, query) in CapabilityQueryTestCases)
			{
				QueryAssert.Count(TestContext.Api.Capabilities, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadCapabilitiesPagedWithQuery_CustomPageSize()
		{
			foreach (var (expectedObjects, query) in CapabilityQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.Capabilities, expectedObjects, query, 2);
			}
		}

		[TestMethod]
		public void ReadCapacitiesWithQuery()
		{
			foreach (var (expectedObjects, query) in CapacityQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.Capacities, expectedObjects, query);
			}
		}

		[TestMethod]
		public void CountCapacitiesWithQuery()
		{
			foreach (var (expectedObjects, query) in CapacityQueryTestCases)
			{
				QueryAssert.Count(TestContext.Api.Capacities, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadCapacitiesPagedWithQuery_CustomPageSize()
		{
			foreach (var (expectedObjects, query) in CapacityQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.Capacities, expectedObjects, query, 2);
			}
		}

		[TestMethod]
		public void ReadResourcePropertiesWithQuery()
		{
			foreach (var (expectedObjects, query) in ResourcePropertyQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.ResourceProperties, expectedObjects, query);
			}
		}

		[TestMethod]
		public void CountResourcePropertiesWithQuery()
		{
			foreach (var (expectedObjects, query) in ResourcePropertyQueryTestCases)
			{
				QueryAssert.Count(TestContext.Api.ResourceProperties, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadResourcePropertiesPagedWithQuery_CustomPageSize()
		{
			foreach (var (expectedObjects, query) in ResourcePropertyQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.ResourceProperties, expectedObjects, query, 2);
			}
		}

		[TestMethod]
		public void ReadConfigurationsWithQuery()
		{
			foreach (var (expectedObjects, query) in ConfigurationQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.Configurations, expectedObjects, query);
			}
		}

		[TestMethod]
		public void CountConfigurationsWithQuery()
		{
			foreach (var (expectedObjects, query) in ConfigurationQueryTestCases)
			{
				QueryAssert.Count(TestContext.Api.Configurations, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadConfigurationsPagedWithQuery_CustomPageSize()
		{
			foreach (var (expectedObjects, query) in ConfigurationQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.Configurations, expectedObjects, query, 2);
			}
		}
	}
}
