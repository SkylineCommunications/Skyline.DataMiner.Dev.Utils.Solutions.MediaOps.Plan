namespace RT_MediaOps.Plan.RST.Querying
{
	using System;
	using System.Collections.Generic;
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
		/// Gets the expected objects, in the expected order, mapped to the applied query with a limit.
		/// </summary>
		private Tuple<Capability[], IQuery<Capability>>[] CapabilityLimitedQueryTestCases => new[]
		{
			new Tuple<Capability[], IQuery<Capability>>(
				[Setup.Location!],
				CapabilityFilter.ToQuery().OrderBy(CapabilityExposers.Name).Limit(1)),
			new Tuple<Capability[], IQuery<Capability>>(
				[Setup.Location!, Setup.Priority!],
				CapabilityFilter.ToQuery().OrderBy(CapabilityExposers.Name).Limit(2)),
			new Tuple<Capability[], IQuery<Capability>>(
				[Setup.Resolution!, Setup.Priority!],
				CapabilityFilter.ToQuery().OrderByDescending(CapabilityExposers.Name).Limit(2)),
			new Tuple<Capability[], IQuery<Capability>>(
				[Setup.Location!, Setup.Priority!, Setup.Resolution!],
				CapabilityFilter.ToQuery().OrderBy(CapabilityExposers.Name).Limit(10)),
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
		/// Gets the expected objects, in the expected order, mapped to the applied query with a limit.
		/// </summary>
		private Tuple<Capacity[], IQuery<Capacity>>[] CapacityLimitedQueryTestCases => new[]
		{
			new Tuple<Capacity[], IQuery<Capacity>>(
				[Setup.Bandwidth!],
				CapacityFilter.ToQuery().OrderBy(CapacityExposers.Name).Limit(1)),
			new Tuple<Capacity[], IQuery<Capacity>>(
				[Setup.Bandwidth!, Setup.Frequency!],
				CapacityFilter.ToQuery().OrderBy(CapacityExposers.Name).Limit(2)),
			new Tuple<Capacity[], IQuery<Capacity>>(
				[Setup.Reach!, Setup.Frequency!],
				CapacityFilter.ToQuery().OrderByDescending(CapacityExposers.Name).Limit(2)),
			new Tuple<Capacity[], IQuery<Capacity>>(
				[Setup.Bandwidth!, Setup.Frequency!, Setup.Reach!],
				CapacityFilter.ToQuery().OrderBy(CapacityExposers.Name).Limit(10)),
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

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query with a limit.
		/// </summary>
		private Tuple<Configuration[], IQuery<Configuration>>[] ConfigurationLimitedQueryTestCases => new[]
		{
			new Tuple<Configuration[], IQuery<Configuration>>(
				[Setup.Distance!],
				ConfigurationFilter.ToQuery().OrderBy(ConfigurationExposers.Name).Limit(1)),
			new Tuple<Configuration[], IQuery<Configuration>>(
				[Setup.Distance!, Setup.PriorityConfig!],
				ConfigurationFilter.ToQuery().OrderBy(ConfigurationExposers.Name).Limit(2)),
			new Tuple<Configuration[], IQuery<Configuration>>(
				[Setup.ResolutionConfig!, Setup.Region!],
				ConfigurationFilter.ToQuery().OrderByDescending(ConfigurationExposers.Name).Limit(2)),
			new Tuple<Configuration[], IQuery<Configuration>>(
				[Setup.Distance!, Setup.PriorityConfig!, Setup.Region!, Setup.ResolutionConfig!],
				ConfigurationFilter.ToQuery().OrderBy(ConfigurationExposers.Name).Limit(10)),
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
		public void ReadPartialResourcesWithQuery_ReturnsSameResourcesInSameOrder()
		{
			foreach (var (expectedObjects, query) in ResourceQueryTestCases.Concat(ResourceLimitedQueryTestCases))
			{
				var partialResources = TestContext.Api.Resources.ReadPartial(query.WithFields(ResourceExposers.Name)).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), partialResources.Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), partialResources.Select(x => x.GetValue(ResourceExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedResourcesWithQuery_DefaultPageSize_ReturnsSameResourcesInOnePage()
		{
			foreach (var (expectedObjects, query) in ResourceQueryTestCases.Concat(ResourceLimitedQueryTestCases))
			{
				var pages = TestContext.Api.Resources.ReadPartialPaged(query.WithFields(ResourceExposers.Name)).ToList();

				if (expectedObjects.Length == 0)
				{
					Assert.IsTrue(pages.SelectMany(x => x).Count() == 0, $"Query: {query}");
					continue;
				}

				Assert.AreEqual(1, pages.Count, $"Query: {query}");
				Assert.IsFalse(pages[0].HasNextPage, $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages[0].Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), pages[0].Select(x => x.GetValue(ResourceExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedResourcesWithQuery_CustomPageSize_SplitsResourcesOverPages()
		{
			var repository = (ResourcesRepository)TestContext.Api.Resources;

			foreach (var (expectedObjects, query) in ResourceQueryTestCases.Concat(ResourceLimitedQueryTestCases))
			{
				var pages = repository.ReadPartialPaged(query.WithFields(ResourceExposers.Name), 2).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages.SelectMany(x => x).Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(Enumerable.Range(0, pages.Count).ToList(), pages.Select(x => x.PageNumber).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(pages.Select((_, i) => i < pages.Count - 1).ToList(), pages.Select(x => x.HasNextPage).ToList(), $"Query: {query}");
				Assert.IsTrue(pages.All(x => x.Count <= 2), $"Query: {query}");
			}

			Assert.IsTrue(ResourceQueryTestCases.Any(x => x.Item1.Length > 2), "No query returns more than one page.");
		}

		[TestMethod]
		public void ReadPartialPagedResourcesWithFilter_ReturnsAllResources()
		{
			var pages = TestContext.Api.Resources.ReadPartialPaged(ResourceFilter.WithFields(ResourceExposers.Name, ResourceExposers.Capabilities.CapabilityId)).ToList();
			var resourcesById = TestContext.Api.Resources.Read(ResourceFilter).ToDictionary(x => x.Id);

			Assert.AreEqual(1, pages.Count);
			CollectionAssert.AreEquivalent(resourcesById.Keys.ToList(), pages[0].Select(x => x.Id).ToList());

			foreach (var partialResource in pages[0])
			{
				var resource = resourcesById[partialResource.Id];
				Assert.AreEqual(resource.Name, partialResource.GetValue(ResourceExposers.Name));
				CollectionAssert.AreEqual(resource.Capabilities.Select(x => x.Id).ToList(), partialResource.GetValues(ResourceExposers.Capabilities.CapabilityId).ToList(), resource.Name);
			}
		}

		[TestMethod]
		public void ReadPartialResourcesWithFilter_ReturnsSameValuesAsFullRead()
		{
			var partialFilter = ResourceFilter.WithFields(
				ResourceExposers.Id,
				ResourceExposers.Name,
				ResourceExposers.IsFavorite,
				ResourceExposers.Concurrency,
				ResourceExposers.State,
				ResourceExposers.ResourcePoolIds,
				ResourceExposers.Type,
				ResourceExposers.Capabilities.CapabilityId,
				ResourceExposers.Capabilities.Discretes,
				ResourceExposers.Capacities.CapacityId,
				ResourceExposers.Properties.PropertyId,
				ResourceExposers.Properties.Value,
				ElementResourceExposers.AgentId,
				ElementResourceExposers.ElementId,
				ServiceResourceExposers.AgentId,
				ServiceResourceExposers.ServiceId,
				VirtualFunctionResourceExposers.AgentId,
				VirtualFunctionResourceExposers.ElementId,
				VirtualFunctionResourceExposers.FunctionId,
				VirtualFunctionResourceExposers.FunctionTableIndex);

			var partialResourcesById = TestContext.Api.Resources.ReadPartial(partialFilter).ToDictionary(x => x.Id);
			var resources = TestContext.Api.Resources.Read(ResourceFilter).ToList();

			Assert.AreEqual(resources.Count, partialResourcesById.Count);

			foreach (var resource in resources)
			{
				var partialResource = partialResourcesById[resource.Id];

				Assert.AreEqual(resource.Id, partialResource.GetValue(ResourceExposers.Id));
				Assert.AreEqual(resource.Name, partialResource.GetValue(ResourceExposers.Name));
				Assert.AreEqual(resource.IsFavorite, partialResource.GetValue(ResourceExposers.IsFavorite));
				Assert.AreEqual(resource.Concurrency, partialResource.GetValue(ResourceExposers.Concurrency));
				Assert.AreEqual(resource.State, partialResource.GetValue(ResourceExposers.State));
				Assert.AreEqual(resource.GetType(), partialResource.GetValue(ResourceExposers.Type));
				CollectionAssert.AreEqual(resource.ResourcePoolIds.ToList(), partialResource.GetValues(ResourceExposers.ResourcePoolIds).ToList(), resource.Name);
				CollectionAssert.AreEqual(resource.Capabilities.Select(x => x.Id).ToList(), partialResource.GetValues(ResourceExposers.Capabilities.CapabilityId).ToList(), resource.Name);
				CollectionAssert.AreEqual(resource.Capabilities.SelectMany(x => x.Discretes).ToList(), partialResource.GetValues(ResourceExposers.Capabilities.Discretes).ToList(), resource.Name);
				CollectionAssert.AreEqual(resource.Capacities.Select(x => x.Id).ToList(), partialResource.GetValues(ResourceExposers.Capacities.CapacityId).ToList(), resource.Name);
				CollectionAssert.AreEqual(resource.Properties.Select(x => x.Id).ToList(), partialResource.GetValues(ResourceExposers.Properties.PropertyId).ToList(), resource.Name);
				CollectionAssert.AreEqual(resource.Properties.Select(x => x.Value).Where(x => x != null).ToList(), partialResource.GetValues(ResourceExposers.Properties.Value).ToList(), resource.Name);

				var element = resource as ElementResource;
				var service = resource as ServiceResource;
				var virtualFunction = resource as VirtualFunctionResource;

				Assert.AreEqual(element?.AgentId ?? -1, partialResource.GetValue(ElementResourceExposers.AgentId), resource.Name);
				Assert.AreEqual(element?.ElementId ?? -1, partialResource.GetValue(ElementResourceExposers.ElementId), resource.Name);
				Assert.AreEqual(service?.AgentId ?? -1, partialResource.GetValue(ServiceResourceExposers.AgentId), resource.Name);
				Assert.AreEqual(service?.ServiceId ?? -1, partialResource.GetValue(ServiceResourceExposers.ServiceId), resource.Name);
				Assert.AreEqual(virtualFunction?.AgentId ?? -1, partialResource.GetValue(VirtualFunctionResourceExposers.AgentId), resource.Name);
				Assert.AreEqual(virtualFunction?.ElementId ?? -1, partialResource.GetValue(VirtualFunctionResourceExposers.ElementId), resource.Name);
				Assert.AreEqual(virtualFunction?.FunctionId ?? Guid.Empty, partialResource.GetValue(VirtualFunctionResourceExposers.FunctionId), resource.Name);
				Assert.AreEqual(virtualFunction?.FunctionTableIndex, partialResource.GetValue(VirtualFunctionResourceExposers.FunctionTableIndex), resource.Name);
			}

			// The setup must cover the multi-section, external and type specific fields, otherwise the comparison above proves little.
			Assert.IsTrue(resources.Any(x => x.Capabilities.Count > 1 && x.Capabilities.Sum(c => c.Discretes.Count) > 1), "No resource with multiple capabilities.");
			Assert.IsTrue(resources.Any(x => x.Capacities.OfType<NumberCapacitySetting>().Any() && x.Capacities.OfType<RangeCapacitySetting>().Any()), "No resource with number and range capacities.");
			Assert.IsTrue(resources.Any(x => x.Properties.Count > 1), "No resource with multiple properties.");
			Assert.IsTrue(resources.Any(x => x.ResourcePoolIds.Any()), "No resource in a pool.");
			Assert.IsTrue(resources.Select(x => x.State).Distinct().Count() > 1, "No resources in different states.");
			CollectionAssert.IsSubsetOf(
				new[] { typeof(UnmanagedResource), typeof(ElementResource), typeof(ServiceResource), typeof(VirtualFunctionResource) },
				resources.Select(x => x.GetType()).Distinct().ToList(),
				"Not every resource type is covered.");
		}

		[TestMethod]
		public void ReadPartialResources_UnknownField_ThrowsBeforeReading()
		{
			var unknownField = new Exposer<Resource, string>(x => x.Name, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.Resources.ReadPartial(ResourceFilter.WithFields(unknownField)));
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
		public void ReadPartialResourcePoolsWithQuery_ReturnsSamePoolsInSameOrder()
		{
			foreach (var (expectedObjects, query) in ResourcePoolQueryTestCases.Concat(ResourcePoolLimitedQueryTestCases))
			{
				var partialPools = TestContext.Api.ResourcePools.ReadPartial(query.WithFields(ResourcePoolExposers.Name)).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), partialPools.Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), partialPools.Select(x => x.GetValue(ResourcePoolExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedResourcePoolsWithQuery_DefaultPageSize_ReturnsSamePoolsInOnePage()
		{
			foreach (var (expectedObjects, query) in ResourcePoolQueryTestCases.Concat(ResourcePoolLimitedQueryTestCases))
			{
				var pages = TestContext.Api.ResourcePools.ReadPartialPaged(query.WithFields(ResourcePoolExposers.Name)).ToList();

				Assert.AreEqual(1, pages.Count, $"Query: {query}");
				Assert.IsFalse(pages[0].HasNextPage, $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages[0].Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), pages[0].Select(x => x.GetValue(ResourcePoolExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedResourcePoolsWithQuery_CustomPageSize_SplitsPoolsOverPages()
		{
			var repository = (ResourcePoolsRepository)TestContext.Api.ResourcePools;

			foreach (var (expectedObjects, query) in ResourcePoolQueryTestCases.Concat(ResourcePoolLimitedQueryTestCases))
			{
				var pages = repository.ReadPartialPaged(query.WithFields(ResourcePoolExposers.Name), 2).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages.SelectMany(x => x).Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(Enumerable.Range(0, pages.Count).ToList(), pages.Select(x => x.PageNumber).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(pages.Select((_, i) => i < pages.Count - 1).ToList(), pages.Select(x => x.HasNextPage).ToList(), $"Query: {query}");
				Assert.IsTrue(pages.All(x => x.Count <= 2), $"Query: {query}");
			}

			Assert.IsTrue(ResourcePoolQueryTestCases.Any(x => x.Item1.Length > 2), "No query returns more than one page.");
		}

		[TestMethod]
		public void ReadPartialPagedResourcePoolsWithFilter_ReturnsAllPools()
		{
			var pages = TestContext.Api.ResourcePools.ReadPartialPaged(ResourcePoolFilter.WithFields(ResourcePoolExposers.Name, ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId)).ToList();
			var poolsById = TestContext.Api.ResourcePools.Read(ResourcePoolFilter).ToDictionary(x => x.Id);

			Assert.AreEqual(1, pages.Count);
			CollectionAssert.AreEquivalent(poolsById.Keys.ToList(), pages[0].Select(x => x.Id).ToList());

			foreach (var partialPool in pages[0])
			{
				var pool = poolsById[partialPool.Id];
				Assert.AreEqual(pool.Name, partialPool.GetValue(ResourcePoolExposers.Name));
				CollectionAssert.AreEqual(pool.LinkedResourcePools.Select(x => x.LinkedResourcePoolId).ToList(), partialPool.GetValues(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId).ToList(), pool.Name);
			}
		}

		[TestMethod]
		public void ReadPartialResourcePoolsWithFilter_ReturnsSameValuesAsFullRead()
		{
			var partialFilter = ResourcePoolFilter.WithFields(
				ResourcePoolExposers.Id,
				ResourcePoolExposers.Name,
				ResourcePoolExposers.State,
				ResourcePoolExposers.IconImage,
				ResourcePoolExposers.HasIconImage,
				ResourcePoolExposers.Url,
				ResourcePoolExposers.HasUrl,
				ResourcePoolExposers.CategoryId,
				ResourcePoolExposers.HasCategory,
				ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId,
				ResourcePoolExposers.LinkedResourcePools.SelectionType,
				ResourcePoolExposers.Capabilities.CapabilityId,
				ResourcePoolExposers.Capabilities.Discretes);

			var partialPoolsById = TestContext.Api.ResourcePools.ReadPartial(partialFilter).ToDictionary(x => x.Id);
			var pools = TestContext.Api.ResourcePools.Read(ResourcePoolFilter).ToList();

			Assert.AreEqual(pools.Count, partialPoolsById.Count);

			foreach (var pool in pools)
			{
				var partialPool = partialPoolsById[pool.Id];

				Assert.AreEqual(pool.Id, partialPool.GetValue(ResourcePoolExposers.Id));
				Assert.AreEqual(pool.Name, partialPool.GetValue(ResourcePoolExposers.Name));
				Assert.AreEqual(pool.State, partialPool.GetValue(ResourcePoolExposers.State));
				Assert.AreEqual(pool.IconImage, partialPool.GetValue(ResourcePoolExposers.IconImage));
				Assert.AreEqual(pool.IconImage != null, partialPool.GetValue(ResourcePoolExposers.HasIconImage));
				Assert.AreEqual(pool.Url, partialPool.GetValue(ResourcePoolExposers.Url));
				Assert.AreEqual(pool.Url != null, partialPool.GetValue(ResourcePoolExposers.HasUrl));
				Assert.AreEqual(pool.CategoryId, partialPool.GetValue(ResourcePoolExposers.CategoryId));
				Assert.AreEqual(pool.CategoryId != null, partialPool.GetValue(ResourcePoolExposers.HasCategory));

				CollectionAssert.AreEqual(pool.LinkedResourcePools.Select(x => x.LinkedResourcePoolId).ToList(), partialPool.GetValues(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId).ToList(), pool.Name);
				CollectionAssert.AreEqual(pool.LinkedResourcePools.Select(x => x.SelectionType).ToList(), partialPool.GetValues(ResourcePoolExposers.LinkedResourcePools.SelectionType).ToList(), pool.Name);
				CollectionAssert.AreEqual(pool.Capabilities.Select(x => x.Id).ToList(), partialPool.GetValues(ResourcePoolExposers.Capabilities.CapabilityId).ToList(), pool.Name);
				CollectionAssert.AreEqual(pool.Capabilities.SelectMany(x => x.Discretes).ToList(), partialPool.GetValues(ResourcePoolExposers.Capabilities.Discretes).ToList(), pool.Name);
			}

			// The setup must cover the multi-section fields, otherwise the comparison above proves little.
			Assert.IsTrue(pools.Any(x => x.LinkedResourcePools.Count > 1 && x.LinkedResourcePools.Select(l => l.SelectionType).Distinct().Count() > 1), "No pool with multiple links of different selection types.");
			Assert.IsTrue(pools.Any(x => x.Capabilities.Count > 1 && x.Capabilities.Any(c => c.Discretes.Count > 1)), "No pool with multiple capabilities with multiple discretes.");
			Assert.IsTrue(pools.Any(x => x.IconImage != null) && pools.Any(x => x.IconImage == null), "No pools with and without an icon.");
			Assert.IsTrue(pools.Any(x => x.State == ResourcePoolState.Draft) && pools.Any(x => x.State == ResourcePoolState.Complete), "No pools in different states.");
		}

		[TestMethod]
		public void ReadPartialResourcePools_UnknownField_ThrowsBeforeReading()
		{
			var unknownField = new Exposer<ResourcePool, string>(x => x.Name, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.ResourcePools.ReadPartial(ResourcePoolFilter.WithFields(unknownField)));
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
		public void ReadPartialCapabilitiesWithQuery_ReturnsSameCapabilitiesInSameOrder()
		{
			foreach (var (expectedObjects, query) in CapabilityQueryTestCases.Concat(CapabilityLimitedQueryTestCases))
			{
				var partialObjects = TestContext.Api.Capabilities.ReadPartial(query.WithFields(CapabilityExposers.Name)).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), partialObjects.Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), partialObjects.Select(x => x.GetValue(CapabilityExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedCapabilitiesWithQuery_DefaultPageSize_ReturnsSameCapabilitiesInOnePage()
		{
			foreach (var (expectedObjects, query) in CapabilityQueryTestCases.Concat(CapabilityLimitedQueryTestCases))
			{
				var pages = TestContext.Api.Capabilities.ReadPartialPaged(query.WithFields(CapabilityExposers.Name)).ToList();

				Assert.AreEqual(1, pages.Count, $"Query: {query}");
				Assert.IsFalse(pages[0].HasNextPage, $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages[0].Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), pages[0].Select(x => x.GetValue(CapabilityExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedCapabilitiesWithQuery_CustomPageSize_SplitsCapabilitiesOverPages()
		{
			var repository = (CapabilitiesRepository)TestContext.Api.Capabilities;

			foreach (var (expectedObjects, query) in CapabilityQueryTestCases.Concat(CapabilityLimitedQueryTestCases))
			{
				var pages = repository.ReadPartialPaged(query.WithFields(CapabilityExposers.Name), 2).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages.SelectMany(x => x).Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(Enumerable.Range(0, pages.Count).ToList(), pages.Select(x => x.PageNumber).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(pages.Select((_, i) => i < pages.Count - 1).ToList(), pages.Select(x => x.HasNextPage).ToList(), $"Query: {query}");
				Assert.IsTrue(pages.All(x => x.Count <= 2), $"Query: {query}");
			}

			Assert.IsTrue(CapabilityQueryTestCases.Any(x => x.Item1.Length > 2), "No query returns more than one page.");
		}

		[TestMethod]
		public void ReadPartialPagedCapabilitiesWithFilter_ReturnsAllCapabilities()
		{
			var pages = TestContext.Api.Capabilities.ReadPartialPaged(CapabilityFilter.WithFields(CapabilityExposers.Name)).ToList();
			var objectsById = TestContext.Api.Capabilities.Read(CapabilityFilter).ToDictionary(x => x.Id);

			Assert.AreEqual(1, pages.Count);
			Assert.IsFalse(pages[0].HasNextPage);
			CollectionAssert.AreEquivalent(objectsById.Keys.ToList(), pages[0].Select(x => x.Id).ToList());

			foreach (var partialObject in pages[0])
			{
				Assert.AreEqual(objectsById[partialObject.Id].Name, partialObject.GetValue(CapabilityExposers.Name));
			}
		}

		[TestMethod]
		public void ReadPartialCapabilities_CustomExposer_IsEvaluatedOnFullObject()
		{
			// The objects are read in full, so any exposer of the type can be projected, also one that is unknown to the filter translator.
			var customField = new Exposer<Capability, string>(x => x.Name, "Unknown");

			var partialObjectsById = TestContext.Api.Capabilities.ReadPartial(CapabilityFilter.WithFields(customField)).ToDictionary(x => x.Id);

			Assert.AreEqual(Setup.Capabilities.Length, partialObjectsById.Count);

			foreach (var expected in Setup.Capabilities)
			{
				Assert.AreEqual(expected.Name, partialObjectsById[expected.Id].GetValue(customField));
			}
		}

		[TestMethod]
		public void ReadPartialCapabilitiesWithFilter_ReturnsSameValuesAsFullRead()
		{
			var partialFilter = CapabilityFilter.WithFields(
				CapabilityExposers.Id,
				CapabilityExposers.Name,
				CapabilityExposers.IsMandatory,
				CapabilityExposers.IsTimeDependent,
				CapabilityExposers.Discretes);

			var partialObjectsById = TestContext.Api.Capabilities.ReadPartial(partialFilter).ToDictionary(x => x.Id);
			var capabilities = TestContext.Api.Capabilities.Read(CapabilityFilter).ToList();

			Assert.AreEqual(capabilities.Count, partialObjectsById.Count);

			foreach (var capability in capabilities)
			{
				var partialObject = partialObjectsById[capability.Id];

				Assert.AreEqual(capability.Id, partialObject.GetValue(CapabilityExposers.Id));
				Assert.AreEqual(capability.Name, partialObject.GetValue(CapabilityExposers.Name));
				Assert.AreEqual(capability.IsMandatory, partialObject.GetValue(CapabilityExposers.IsMandatory));
				Assert.AreEqual(capability.IsTimeDependent, partialObject.GetValue(CapabilityExposers.IsTimeDependent));
				CollectionAssert.AreEqual(capability.Discretes.ToList(), partialObject.GetValues(CapabilityExposers.Discretes).ToList(), capability.Name);
			}

			Assert.IsTrue(capabilities.Any(x => x.IsMandatory) && capabilities.Any(x => !x.IsMandatory), "The setup doesn't cover both mandatory values.");
			Assert.IsTrue(capabilities.Any(x => x.IsTimeDependent) && capabilities.Any(x => !x.IsTimeDependent), "The setup doesn't cover both time dependent values.");
			Assert.IsTrue(capabilities.Any(x => x.Discretes.Count > 1), "No capability with multiple discretes.");
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
		public void ReadPartialCapacitiesWithQuery_ReturnsSameCapacitiesInSameOrder()
		{
			foreach (var (expectedObjects, query) in CapacityQueryTestCases.Concat(CapacityLimitedQueryTestCases))
			{
				var partialObjects = TestContext.Api.Capacities.ReadPartial(query.WithFields(CapacityExposers.Name)).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), partialObjects.Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), partialObjects.Select(x => x.GetValue(CapacityExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedCapacitiesWithQuery_DefaultPageSize_ReturnsSameCapacitiesInOnePage()
		{
			foreach (var (expectedObjects, query) in CapacityQueryTestCases.Concat(CapacityLimitedQueryTestCases))
			{
				var pages = TestContext.Api.Capacities.ReadPartialPaged(query.WithFields(CapacityExposers.Name)).ToList();

				Assert.AreEqual(1, pages.Count, $"Query: {query}");
				Assert.IsFalse(pages[0].HasNextPage, $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages[0].Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), pages[0].Select(x => x.GetValue(CapacityExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedCapacitiesWithQuery_CustomPageSize_SplitsCapacitiesOverPages()
		{
			var repository = (CapacitiesRepository)TestContext.Api.Capacities;

			foreach (var (expectedObjects, query) in CapacityQueryTestCases.Concat(CapacityLimitedQueryTestCases))
			{
				var pages = repository.ReadPartialPaged(query.WithFields(CapacityExposers.Name), 2).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages.SelectMany(x => x).Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(Enumerable.Range(0, pages.Count).ToList(), pages.Select(x => x.PageNumber).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(pages.Select((_, i) => i < pages.Count - 1).ToList(), pages.Select(x => x.HasNextPage).ToList(), $"Query: {query}");
				Assert.IsTrue(pages.All(x => x.Count <= 2), $"Query: {query}");
			}

			Assert.IsTrue(CapacityQueryTestCases.Any(x => x.Item1.Length > 2), "No query returns more than one page.");
		}

		[TestMethod]
		public void ReadPartialPagedCapacitiesWithFilter_ReturnsAllCapacities()
		{
			var pages = TestContext.Api.Capacities.ReadPartialPaged(CapacityFilter.WithFields(CapacityExposers.Name)).ToList();
			var objectsById = TestContext.Api.Capacities.Read(CapacityFilter).ToDictionary(x => x.Id);

			Assert.AreEqual(1, pages.Count);
			Assert.IsFalse(pages[0].HasNextPage);
			CollectionAssert.AreEquivalent(objectsById.Keys.ToList(), pages[0].Select(x => x.Id).ToList());

			foreach (var partialObject in pages[0])
			{
				Assert.AreEqual(objectsById[partialObject.Id].Name, partialObject.GetValue(CapacityExposers.Name));
			}
		}

		[TestMethod]
		public void ReadPartialCapacities_CustomExposer_IsEvaluatedOnFullObject()
		{
			// The objects are read in full, so any exposer of the type can be projected, also one that is unknown to the filter translator.
			var customField = new Exposer<Capacity, string>(x => x.Name, "Unknown");

			var partialObjectsById = TestContext.Api.Capacities.ReadPartial(CapacityFilter.WithFields(customField)).ToDictionary(x => x.Id);

			Assert.AreEqual(Setup.Capacities.Length, partialObjectsById.Count);

			foreach (var expected in Setup.Capacities)
			{
				Assert.AreEqual(expected.Name, partialObjectsById[expected.Id].GetValue(customField));
			}
		}

		[TestMethod]
		public void ReadPartialCapacitiesWithFilter_ReturnsSameValuesAsFullRead()
		{
			var partialFilter = CapacityFilter.WithFields(
				CapacityExposers.Id,
				CapacityExposers.IsMandatory,
				CapacityExposers.Name,
				CapacityExposers.Units,
				CapacityExposers.HasUnits,
				CapacityExposers.RangeMin,
				CapacityExposers.HasRangeMin,
				CapacityExposers.RangeMax,
				CapacityExposers.HasRangeMax,
				CapacityExposers.StepSize,
				CapacityExposers.HasStepSize,
				CapacityExposers.Decimals,
				CapacityExposers.HasDecimals);

			var partialObjectsById = TestContext.Api.Capacities.ReadPartial(partialFilter).ToDictionary(x => x.Id);
			var capacities = TestContext.Api.Capacities.Read(CapacityFilter).ToList();

			Assert.AreEqual(capacities.Count, partialObjectsById.Count);

			foreach (var capacity in capacities)
			{
				var partialObject = partialObjectsById[capacity.Id];

				Assert.AreEqual(capacity.Id, partialObject.GetValue(CapacityExposers.Id));
				Assert.AreEqual(capacity.IsMandatory, partialObject.GetValue(CapacityExposers.IsMandatory));
				Assert.AreEqual(capacity.Name, partialObject.GetValue(CapacityExposers.Name));
				Assert.AreEqual(capacity.Units, partialObject.GetValue(CapacityExposers.Units));
				Assert.AreEqual(!String.IsNullOrEmpty(capacity.Units), partialObject.GetValue(CapacityExposers.HasUnits), capacity.Name);
				Assert.AreEqual(capacity.RangeMin.GetValueOrDefault(), partialObject.GetValue(CapacityExposers.RangeMin), capacity.Name);
				Assert.AreEqual(capacity.RangeMin.HasValue, partialObject.GetValue(CapacityExposers.HasRangeMin), capacity.Name);
				Assert.AreEqual(capacity.RangeMax.GetValueOrDefault(), partialObject.GetValue(CapacityExposers.RangeMax), capacity.Name);
				Assert.AreEqual(capacity.RangeMax.HasValue, partialObject.GetValue(CapacityExposers.HasRangeMax), capacity.Name);
				Assert.AreEqual(capacity.StepSize.GetValueOrDefault(), partialObject.GetValue(CapacityExposers.StepSize), capacity.Name);
				Assert.AreEqual(capacity.StepSize.HasValue, partialObject.GetValue(CapacityExposers.HasStepSize), capacity.Name);
				Assert.AreEqual(capacity.Decimals.GetValueOrDefault(), partialObject.GetValue(CapacityExposers.Decimals), capacity.Name);
				Assert.AreEqual(capacity.Decimals.HasValue, partialObject.GetValue(CapacityExposers.HasDecimals), capacity.Name);
			}

			// Reach has no units, range, step size or decimals, the other capacities have all of them.
			Assert.IsTrue(capacities.Any(x => x.RangeMin.HasValue && x.RangeMax.HasValue && x.StepSize.HasValue && x.Decimals.HasValue && !String.IsNullOrEmpty(x.Units)), "No capacity with all optional values.");
			Assert.IsTrue(capacities.Any(x => !x.RangeMin.HasValue && !x.RangeMax.HasValue && !x.StepSize.HasValue && !x.Decimals.HasValue && String.IsNullOrEmpty(x.Units)), "No capacity without optional values.");
		}

		[TestMethod]
		public void ReadPartialCapacities_HasFields_MatchTheFilterOnTheSameField()
		{
			var hasFields = new[]
			{
				CapacityExposers.HasUnits,
				CapacityExposers.HasRangeMin,
				CapacityExposers.HasRangeMax,
				CapacityExposers.HasStepSize,
				CapacityExposers.HasDecimals,
			};

			var partialObjects = TestContext.Api.Capacities.ReadPartial(CapacityFilter.WithFields(hasFields)).ToList();

			foreach (var hasField in hasFields)
			{
				var filteredIds = TestContext.Api.Capacities.Read(CapacityFilter.AND(hasField.Equal(true))).Select(x => x.Id).ToList();

				CollectionAssert.AreEquivalent(filteredIds, partialObjects.Where(x => x.GetValue(hasField)).Select(x => x.Id).ToList(), hasField.fieldName);
				Assert.IsTrue(filteredIds.Count > 0 && filteredIds.Count < partialObjects.Count, $"The setup doesn't cover both values of {hasField.fieldName}.");
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

		// Partial reads of resource properties.
		[TestMethod]
		public void ReadPartialResourcePropertiesWithQuery_ReturnsSamePropertiesInSameOrder()
		{
			foreach (var (expectedObjects, query) in ResourcePropertyQueryTestCases)
			{
				var partialProperties = TestContext.Api.ResourceProperties.ReadPartial(query.WithFields(ResourcePropertyExposers.Name)).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), partialProperties.Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), partialProperties.Select(x => x.GetValue(ResourcePropertyExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedResourcePropertiesWithQuery_DefaultPageSize_ReturnsSamePropertiesInOnePage()
		{
			foreach (var (expectedObjects, query) in ResourcePropertyQueryTestCases)
			{
				var pages = TestContext.Api.ResourceProperties.ReadPartialPaged(query.WithFields(ResourcePropertyExposers.Name)).ToList();

				Assert.AreEqual(1, pages.Count, $"Query: {query}");
				Assert.IsFalse(pages[0].HasNextPage, $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages[0].Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), pages[0].Select(x => x.GetValue(ResourcePropertyExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedResourcePropertiesWithQuery_CustomPageSize_SplitsPropertiesOverPages()
		{
			var repository = (ResourcePropertiesRepository)TestContext.Api.ResourceProperties;

			foreach (var (expectedObjects, query) in ResourcePropertyQueryTestCases)
			{
				var pages = repository.ReadPartialPaged(query.WithFields(ResourcePropertyExposers.Name), 2).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages.SelectMany(x => x).Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(Enumerable.Range(0, pages.Count).ToList(), pages.Select(x => x.PageNumber).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(pages.Select((_, i) => i < pages.Count - 1).ToList(), pages.Select(x => x.HasNextPage).ToList(), $"Query: {query}");
				Assert.IsTrue(pages.All(x => x.Count <= 2), $"Query: {query}");
			}

			Assert.IsTrue(ResourcePropertyQueryTestCases.Any(x => x.Item1.Length > 2), "No query returns more than one page.");
		}

		[TestMethod]
		public void ReadPartialPagedResourcePropertiesWithFilter_ReturnsAllProperties()
		{
			var pages = TestContext.Api.ResourceProperties.ReadPartialPaged(PropertyFilter.WithFields(ResourcePropertyExposers.Name)).ToList();
			var propertiesById = TestContext.Api.ResourceProperties.Read(PropertyFilter).ToDictionary(x => x.Id);

			Assert.AreEqual(1, pages.Count);
			CollectionAssert.AreEquivalent(propertiesById.Keys.ToList(), pages[0].Select(x => x.Id).ToList());

			foreach (var partialProperty in pages[0])
			{
				Assert.AreEqual(propertiesById[partialProperty.Id].Name, partialProperty.GetValue(ResourcePropertyExposers.Name));
			}
		}

		[TestMethod]
		public void ReadPartialResourcePropertiesWithFilter_ReturnsSameValuesAsFullRead()
		{
			var partialFilter = PropertyFilter.WithFields(ResourcePropertyExposers.Id, ResourcePropertyExposers.Name);

			var partialPropertiesById = TestContext.Api.ResourceProperties.ReadPartial(partialFilter).ToDictionary(x => x.Id);
			var properties = TestContext.Api.ResourceProperties.Read(PropertyFilter).ToList();

			Assert.AreEqual(properties.Count, partialPropertiesById.Count);
			Assert.IsTrue(properties.Count > 1, "The setup must contain multiple resource properties.");

			foreach (var property in properties)
			{
				var partialProperty = partialPropertiesById[property.Id];

				Assert.AreEqual(property.Id, partialProperty.GetValue(ResourcePropertyExposers.Id));
				Assert.AreEqual(property.Name, partialProperty.GetValue(ResourcePropertyExposers.Name));
			}
		}

		[TestMethod]
		public void ReadPartialResourceProperties_UnknownField_ThrowsBeforeReading()
		{
			var unknownField = new Exposer<ResourceProperty, string>(x => x.Name, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.ResourceProperties.ReadPartial(PropertyFilter.WithFields(unknownField)));
		}

		[TestMethod]
		public void ReadPartialConfigurationsWithQuery_ReturnsSameConfigurationsInSameOrder()
		{
			foreach (var (expectedObjects, query) in ConfigurationQueryTestCases.Concat(ConfigurationLimitedQueryTestCases))
			{
				var partialObjects = TestContext.Api.Configurations.ReadPartial(query.WithFields(ConfigurationExposers.Name)).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), partialObjects.Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), partialObjects.Select(x => x.GetValue(ConfigurationExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedConfigurationsWithQuery_DefaultPageSize_ReturnsSameConfigurationsInOnePage()
		{
			foreach (var (expectedObjects, query) in ConfigurationQueryTestCases.Concat(ConfigurationLimitedQueryTestCases))
			{
				var pages = TestContext.Api.Configurations.ReadPartialPaged(query.WithFields(ConfigurationExposers.Name)).ToList();

				Assert.AreEqual(1, pages.Count, $"Query: {query}");
				Assert.IsFalse(pages[0].HasNextPage, $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages[0].Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), pages[0].Select(x => x.GetValue(ConfigurationExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedConfigurationsWithQuery_CustomPageSize_SplitsConfigurationsOverPages()
		{
			var repository = (ConfigurationsRepository)TestContext.Api.Configurations;

			foreach (var (expectedObjects, query) in ConfigurationQueryTestCases.Concat(ConfigurationLimitedQueryTestCases))
			{
				var pages = repository.ReadPartialPaged(query.WithFields(ConfigurationExposers.Name), 2).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages.SelectMany(x => x).Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(Enumerable.Range(0, pages.Count).ToList(), pages.Select(x => x.PageNumber).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(pages.Select((_, i) => i < pages.Count - 1).ToList(), pages.Select(x => x.HasNextPage).ToList(), $"Query: {query}");
				Assert.IsTrue(pages.All(x => x.Count <= 2), $"Query: {query}");
			}

			Assert.IsTrue(ConfigurationQueryTestCases.Any(x => x.Item1.Length > 2), "No query returns more than one page.");
		}

		[TestMethod]
		public void ReadPartialPagedConfigurationsWithFilter_ReturnsAllConfigurations()
		{
			var pages = TestContext.Api.Configurations.ReadPartialPaged(ConfigurationFilter.WithFields(ConfigurationExposers.Name)).ToList();
			var objectsById = TestContext.Api.Configurations.Read(ConfigurationFilter).ToDictionary(x => x.Id);

			Assert.AreEqual(1, pages.Count);
			Assert.IsFalse(pages[0].HasNextPage);
			CollectionAssert.AreEquivalent(objectsById.Keys.ToList(), pages[0].Select(x => x.Id).ToList());

			foreach (var partialObject in pages[0])
			{
				Assert.AreEqual(objectsById[partialObject.Id].Name, partialObject.GetValue(ConfigurationExposers.Name));
			}
		}

		[TestMethod]
		public void ReadPartialConfigurations_CustomExposer_IsEvaluatedOnFullObject()
		{
			// The objects are read in full, so any exposer of the type can be projected, also one that is unknown to the filter translator.
			var customField = new Exposer<Configuration, string>(x => x.Name, "Unknown");

			var partialObjectsById = TestContext.Api.Configurations.ReadPartial(ConfigurationFilter.WithFields(customField)).ToDictionary(x => x.Id);

			Assert.AreEqual(Setup.Configurations.Length, partialObjectsById.Count);

			foreach (var expected in Setup.Configurations)
			{
				Assert.AreEqual(expected.Name, partialObjectsById[expected.Id].GetValue(customField));
			}
		}

		[TestMethod]
		public void ReadPartialConfigurationsWithFilter_ReturnsSameValuesAsFullRead()
		{
			var partialFilter = ConfigurationFilter.WithFields(
				ConfigurationExposers.Id,
				ConfigurationExposers.IsMandatory,
				ConfigurationExposers.Name,
				DiscreteTextConfigurationExposers.Discretes,
				DiscreteNumberConfigurationExposers.Discretes);

			var partialObjectsById = TestContext.Api.Configurations.ReadPartial(partialFilter).ToDictionary(x => x.Id);
			var configurations = TestContext.Api.Configurations.Read(ConfigurationFilter).ToList();

			Assert.AreEqual(configurations.Count, partialObjectsById.Count);

			foreach (var configuration in configurations)
			{
				var partialObject = partialObjectsById[configuration.Id];
				var textDiscretes = configuration is DiscreteTextConfiguration textConfiguration ? textConfiguration.Discretes.Select(x => x.Value).ToList() : new List<string>();
				var numberDiscretes = configuration is DiscreteNumberConfiguration numberConfiguration ? numberConfiguration.Discretes.Select(x => x.Value).ToList() : new List<decimal>();

				Assert.AreEqual(configuration.Id, partialObject.GetValue(ConfigurationExposers.Id));
				Assert.AreEqual(configuration.IsMandatory, partialObject.GetValue(ConfigurationExposers.IsMandatory));
				Assert.AreEqual(configuration.Name, partialObject.GetValue(ConfigurationExposers.Name));
				CollectionAssert.AreEqual(textDiscretes, partialObject.GetValues(DiscreteTextConfigurationExposers.Discretes).ToList(), configuration.Name);
				CollectionAssert.AreEqual(numberDiscretes, partialObject.GetValues(DiscreteNumberConfigurationExposers.Discretes).ToList(), configuration.Name);
			}

			Assert.IsTrue(configurations.Any(x => x.IsMandatory) && configurations.Any(x => !x.IsMandatory), "The setup doesn't cover both mandatory values.");
			Assert.IsTrue(configurations.OfType<DiscreteTextConfiguration>().Any(x => x.Discretes.Count > 1), "No discrete text configuration with multiple discretes.");
			Assert.IsTrue(configurations.OfType<DiscreteNumberConfiguration>().Any(x => x.Discretes.Count > 1), "No discrete number configuration with multiple discretes.");
			Assert.IsTrue(configurations.Any(x => !(x is DiscreteTextConfiguration) && !(x is DiscreteNumberConfiguration)), "No configuration without discretes.");
		}
	}
}
