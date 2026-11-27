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
		public void ReadPartialResourcesWithQuery_ReturnsSameResourcesInSameOrder()
		{
			foreach (var (expectedObjects, query) in ResourceQueryTestCases.Concat(ResourceLimitedQueryTestCases))
			{
				var partialResources = TestContext.Api.Resources.ReadPartial(SelectResourceIdAndName(query)).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), partialResources.Select(x => x.GetValue(ResourceExposers.Id)).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), partialResources.Select(x => x.GetValue(ResourceExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedResourcesWithQuery_DefaultPageSize_ReturnsSameResourcesInOnePage()
		{
			foreach (var (expectedObjects, query) in ResourceQueryTestCases.Concat(ResourceLimitedQueryTestCases))
			{
				var pages = TestContext.Api.Resources.ReadPartialPaged(SelectResourceIdAndName(query)).ToList();

				if (expectedObjects.Length == 0)
				{
					Assert.IsTrue(pages.SelectMany(x => x).Count() == 0, $"Query: {query}");
					continue;
				}

				Assert.AreEqual(1, pages.Count, $"Query: {query}");
				Assert.IsFalse(pages[0].HasNextPage, $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages[0].Select(x => x.GetValue(ResourceExposers.Id)).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), pages[0].Select(x => x.GetValue(ResourceExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedResourcesWithQuery_CustomPageSize_SplitsResourcesOverPages()
		{
			foreach (var (expectedObjects, query) in ResourceQueryTestCases.Concat(ResourceLimitedQueryTestCases))
			{
				var pages = TestContext.Api.Resources.ReadPartialPaged(SelectResourceIdAndName(query), 2).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages.SelectMany(x => x).Select(x => x.GetValue(ResourceExposers.Id)).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(Enumerable.Range(0, pages.Count).ToList(), pages.Select(x => x.PageNumber).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(pages.Select((_, i) => i < pages.Count - 1).ToList(), pages.Select(x => x.HasNextPage).ToList(), $"Query: {query}");
				Assert.IsTrue(pages.All(x => x.Count <= 2), $"Query: {query}");
			}

			Assert.IsTrue(ResourceQueryTestCases.Any(x => x.Item1.Length > 2), "No query returns more than one page.");
		}

		[TestMethod]
		public void ReadPartialPagedResourcesWithFilter_ReturnsAllResources()
		{
			var partialQuery = new PartialQuery<Resource>(ResourceFilter.ToQuery())
				.Include(ResourceExposers.Id)
				.Include(ResourceExposers.Name)
				.Include(ResourceExposers.Capabilities.CapabilityId);

			var pages = TestContext.Api.Resources.ReadPartialPaged(partialQuery).ToList();
			var resourcesById = TestContext.Api.Resources.Read(ResourceFilter).ToDictionary(x => x.Id);

			Assert.AreEqual(1, pages.Count);
			CollectionAssert.AreEquivalent(resourcesById.Keys.ToList(), pages[0].Select(x => x.GetValue(ResourceExposers.Id)).ToList());

			foreach (var partialResource in pages[0])
			{
				var resource = resourcesById[partialResource.GetValue(ResourceExposers.Id)];
				Assert.AreEqual(resource.Name, partialResource.GetValue(ResourceExposers.Name));
				CollectionAssert.AreEqual(resource.Capabilities.Select(x => x.Id).ToList(), partialResource.GetValues(ResourceExposers.Capabilities.CapabilityId).ToList(), resource.Name);
			}
		}

		[TestMethod]
		public void ReadPartialResourcesWithFilter_ReturnsSameValuesAsFullRead()
		{
			var partialQuery = new PartialQuery<Resource>(ResourceFilter.ToQuery())
				.Include(ResourceExposers.Id)
				.Include(ResourceExposers.Name)
				.Include(ResourceExposers.IsFavorite)
				.Include(ResourceExposers.Concurrency)
				.Include(ResourceExposers.State)
				.Include(ResourceExposers.ResourcePoolIds)
				.Include(ResourceExposers.Type)
				.Include(ResourceExposers.Capabilities.CapabilityId)
				.Include(ResourceExposers.Capabilities.Discretes)
				.Include(ResourceExposers.Capacities.CapacityId)
				.Include(ResourceExposers.Properties.PropertyId)
				.Include(ResourceExposers.Properties.Value)
				.Include(ElementResourceExposers.AgentId)
				.Include(ElementResourceExposers.ElementId)
				.Include(ServiceResourceExposers.AgentId)
				.Include(ServiceResourceExposers.ServiceId)
				.Include(VirtualFunctionResourceExposers.AgentId)
				.Include(VirtualFunctionResourceExposers.ElementId)
				.Include(VirtualFunctionResourceExposers.FunctionId)
				.Include(VirtualFunctionResourceExposers.FunctionTableIndex);

			var partialResourcesById = TestContext.Api.Resources.ReadPartial(partialQuery).ToDictionary(x => x.GetValue(ResourceExposers.Id));
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

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.Resources.ReadPartial(new PartialQuery<Resource>(ResourceFilter.ToQuery()).Include(unknownField)));
		}

		private static IPartialQuery<Resource> SelectResourceIdAndName(IQuery<Resource> query)
		{
			return new PartialQuery<Resource>(query).Include(ResourceExposers.Id).Include(ResourceExposers.Name);
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
				var partialPools = TestContext.Api.ResourcePools.ReadPartial(SelectResourcePoolIdAndName(query)).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), partialPools.Select(x => x.GetValue(ResourcePoolExposers.Id)).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), partialPools.Select(x => x.GetValue(ResourcePoolExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedResourcePoolsWithQuery_DefaultPageSize_ReturnsSamePoolsInOnePage()
		{
			foreach (var (expectedObjects, query) in ResourcePoolQueryTestCases.Concat(ResourcePoolLimitedQueryTestCases))
			{
				var pages = TestContext.Api.ResourcePools.ReadPartialPaged(SelectResourcePoolIdAndName(query)).ToList();

				Assert.AreEqual(1, pages.Count, $"Query: {query}");
				Assert.IsFalse(pages[0].HasNextPage, $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages[0].Select(x => x.GetValue(ResourcePoolExposers.Id)).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), pages[0].Select(x => x.GetValue(ResourcePoolExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedResourcePoolsWithQuery_CustomPageSize_SplitsPoolsOverPages()
		{
			foreach (var (expectedObjects, query) in ResourcePoolQueryTestCases.Concat(ResourcePoolLimitedQueryTestCases))
			{
				var pages = TestContext.Api.ResourcePools.ReadPartialPaged(SelectResourcePoolIdAndName(query), 2).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages.SelectMany(x => x).Select(x => x.GetValue(ResourcePoolExposers.Id)).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(Enumerable.Range(0, pages.Count).ToList(), pages.Select(x => x.PageNumber).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(pages.Select((_, i) => i < pages.Count - 1).ToList(), pages.Select(x => x.HasNextPage).ToList(), $"Query: {query}");
				Assert.IsTrue(pages.All(x => x.Count <= 2), $"Query: {query}");
			}

			Assert.IsTrue(ResourcePoolQueryTestCases.Any(x => x.Item1.Length > 2), "No query returns more than one page.");
		}

		[TestMethod]
		public void ReadPartialPagedResourcePoolsWithFilter_ReturnsAllPools()
		{
			var partialQuery = new PartialQuery<ResourcePool>(ResourcePoolFilter.ToQuery())
				.Include(ResourcePoolExposers.Id)
				.Include(ResourcePoolExposers.Name)
				.Include(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId);

			var pages = TestContext.Api.ResourcePools.ReadPartialPaged(partialQuery).ToList();
			var poolsById = TestContext.Api.ResourcePools.Read(ResourcePoolFilter).ToDictionary(x => x.Id);

			Assert.AreEqual(1, pages.Count);
			CollectionAssert.AreEquivalent(poolsById.Keys.ToList(), pages[0].Select(x => x.GetValue(ResourcePoolExposers.Id)).ToList());

			foreach (var partialPool in pages[0])
			{
				var pool = poolsById[partialPool.GetValue(ResourcePoolExposers.Id)];
				Assert.AreEqual(pool.Name, partialPool.GetValue(ResourcePoolExposers.Name));
				CollectionAssert.AreEqual(pool.LinkedResourcePools.Select(x => x.LinkedResourcePoolId).ToList(), partialPool.GetValues(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId).ToList(), pool.Name);
			}
		}

		[TestMethod]
		public void ReadPartialResourcePoolsWithFilter_ReturnsSameValuesAsFullRead()
		{
			var partialQuery = new PartialQuery<ResourcePool>(ResourcePoolFilter.ToQuery())
				.Include(ResourcePoolExposers.Id)
				.Include(ResourcePoolExposers.Name)
				.Include(ResourcePoolExposers.State)
				.Include(ResourcePoolExposers.IconImage)
				.Include(ResourcePoolExposers.HasIconImage)
				.Include(ResourcePoolExposers.Url)
				.Include(ResourcePoolExposers.HasUrl)
				.Include(ResourcePoolExposers.CategoryId)
				.Include(ResourcePoolExposers.HasCategory)
				.Include(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId)
				.Include(ResourcePoolExposers.LinkedResourcePools.SelectionType)
				.Include(ResourcePoolExposers.Capabilities.CapabilityId)
				.Include(ResourcePoolExposers.Capabilities.Discretes);

			var partialPoolsById = TestContext.Api.ResourcePools.ReadPartial(partialQuery).ToDictionary(x => x.GetValue(ResourcePoolExposers.Id));
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

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.ResourcePools.ReadPartial(new PartialQuery<ResourcePool>(ResourcePoolFilter.ToQuery()).Include(unknownField)));
		}

		private static IPartialQuery<ResourcePool> SelectResourcePoolIdAndName(IQuery<ResourcePool> query)
		{
			return new PartialQuery<ResourcePool>(query).Include(ResourcePoolExposers.Id).Include(ResourcePoolExposers.Name);
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

		// Partial reads of resource properties.
		[TestMethod]
		public void ReadPartialResourcePropertiesWithQuery_ReturnsSamePropertiesInSameOrder()
		{
			foreach (var (expectedObjects, query) in ResourcePropertyQueryTestCases)
			{
				var partialProperties = TestContext.Api.ResourceProperties.ReadPartial(SelectResourcePropertyIdAndName(query)).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), partialProperties.Select(x => x.GetValue(ResourcePropertyExposers.Id)).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), partialProperties.Select(x => x.GetValue(ResourcePropertyExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedResourcePropertiesWithQuery_DefaultPageSize_ReturnsSamePropertiesInOnePage()
		{
			foreach (var (expectedObjects, query) in ResourcePropertyQueryTestCases)
			{
				var pages = TestContext.Api.ResourceProperties.ReadPartialPaged(SelectResourcePropertyIdAndName(query)).ToList();

				Assert.AreEqual(1, pages.Count, $"Query: {query}");
				Assert.IsFalse(pages[0].HasNextPage, $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages[0].Select(x => x.GetValue(ResourcePropertyExposers.Id)).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), pages[0].Select(x => x.GetValue(ResourcePropertyExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedResourcePropertiesWithQuery_CustomPageSize_SplitsPropertiesOverPages()
		{
			foreach (var (expectedObjects, query) in ResourcePropertyQueryTestCases)
			{
				var pages = TestContext.Api.ResourceProperties.ReadPartialPaged(SelectResourcePropertyIdAndName(query), 2).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages.SelectMany(x => x).Select(x => x.GetValue(ResourcePropertyExposers.Id)).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(Enumerable.Range(0, pages.Count).ToList(), pages.Select(x => x.PageNumber).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(pages.Select((_, i) => i < pages.Count - 1).ToList(), pages.Select(x => x.HasNextPage).ToList(), $"Query: {query}");
				Assert.IsTrue(pages.All(x => x.Count <= 2), $"Query: {query}");
			}

			Assert.IsTrue(ResourcePropertyQueryTestCases.Any(x => x.Item1.Length > 2), "No query returns more than one page.");
		}

		[TestMethod]
		public void ReadPartialPagedResourcePropertiesWithFilter_ReturnsAllProperties()
		{
			var pages = TestContext.Api.ResourceProperties.ReadPartialPaged(SelectResourcePropertyIdAndName(PropertyFilter.ToQuery())).ToList();
			var propertiesById = TestContext.Api.ResourceProperties.Read(PropertyFilter).ToDictionary(x => x.Id);

			Assert.AreEqual(1, pages.Count);
			CollectionAssert.AreEquivalent(propertiesById.Keys.ToList(), pages[0].Select(x => x.GetValue(ResourcePropertyExposers.Id)).ToList());

			foreach (var partialProperty in pages[0])
			{
				Assert.AreEqual(propertiesById[partialProperty.GetValue(ResourcePropertyExposers.Id)].Name, partialProperty.GetValue(ResourcePropertyExposers.Name));
			}
		}

		[TestMethod]
		public void ReadPartialResourcePropertiesWithFilter_ReturnsSameValuesAsFullRead()
		{
			var partialQuery = SelectResourcePropertyIdAndName(PropertyFilter.ToQuery());

			var partialPropertiesById = TestContext.Api.ResourceProperties.ReadPartial(partialQuery).ToDictionary(x => x.GetValue(ResourcePropertyExposers.Id));
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

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.ResourceProperties.ReadPartial(new PartialQuery<ResourceProperty>(PropertyFilter.ToQuery()).Include(unknownField)));
		}

		private static IPartialQuery<ResourceProperty> SelectResourcePropertyIdAndName(IQuery<ResourceProperty> query)
		{
			return new PartialQuery<ResourceProperty>(query).Include(ResourcePropertyExposers.Id).Include(ResourcePropertyExposers.Name);
		}
	}
}
