namespace RT_MediaOps.Plan.Properties.Querying
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Querying;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class PropertyQueryingTests
	{
		private static TestObjectCreator? objectCreator;
		private static PropertyQueryingSetup? setup;

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		private static PropertyQueryingSetup Setup => setup ?? throw new InvalidOperationException("Test setup was not initialized.");

		[ClassInitialize]
		public static void ClassInitialize(TestContext context)
		{
			objectCreator = new TestObjectCreator(TestContext);
			setup = new PropertyQueryingSetup(objectCreator);
		}

		[ClassCleanup(ClassCleanupBehavior.EndOfClass)]
		public static void ClassCleanup()
		{
			objectCreator?.Dispose();
			objectCreator = null;
			setup = null;
		}

		private FilterElement<Property> PropertyFilter => new ORFilterElement<Property>(Setup.Properties.Select(x => PropertyExposers.Id.Equal(x.Id)).ToArray());

		private FilterElement<PropertySettingCollection> PropertySettingCollectionFilter => new ORFilterElement<PropertySettingCollection>(Setup.PropertySettingCollections.Select(x => PropertySettingCollectionExposers.Id.Equal(x.Id)).ToArray());

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query.
		/// </summary>
		private Tuple<Property[], IQuery<Property>>[] PropertyQueryTestCases => new[]
		{
			new Tuple<Property[], IQuery<Property>>(
				[Setup.GlobalPropertyA!, Setup.SchedulingPropertyB!, Setup.GlobalPropertyC!, Setup.SchedulingPropertyD!],
				PropertyFilter.ToQuery().OrderBy(PropertyExposers.Name)),
			new Tuple<Property[], IQuery<Property>>(
				[Setup.SchedulingPropertyD!, Setup.GlobalPropertyC!, Setup.SchedulingPropertyB!, Setup.GlobalPropertyA!],
				PropertyFilter.ToQuery().OrderByDescending(PropertyExposers.Name)),

			new Tuple<Property[], IQuery<Property>>(
				[Setup.SchedulingPropertyD!, Setup.GlobalPropertyC!, Setup.SchedulingPropertyB!, Setup.GlobalPropertyA!],
				PropertyFilter.ToQuery().OrderByDescending(PropertyExposers.Order)),

			new Tuple<Property[], IQuery<Property>>(
				[Setup.GlobalPropertyA!, Setup.SchedulingPropertyB!],
				PropertyFilter.AND(PropertyExposers.SectionName.Equal("General")).ToQuery().OrderBy(PropertyExposers.Order)),
			new Tuple<Property[], IQuery<Property>>(
				[],
				PropertyFilter.AND(PropertyExposers.SectionName.Equal($"Unknown_{Setup.Prefix}")).ToQuery().OrderBy(PropertyExposers.Name)),
		};

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query with a limit.
		/// </summary>
		private Tuple<Property[], IQuery<Property>>[] PropertyLimitedQueryTestCases => new[]
		{
			new Tuple<Property[], IQuery<Property>>(
				[Setup.GlobalPropertyA!, Setup.SchedulingPropertyB!],
				PropertyFilter.ToQuery().OrderBy(PropertyExposers.Name).Limit(2)),
			new Tuple<Property[], IQuery<Property>>(
				[Setup.SchedulingPropertyD!, Setup.GlobalPropertyC!, Setup.SchedulingPropertyB!],
				PropertyFilter.ToQuery().OrderByDescending(PropertyExposers.Order).Limit(3)),
			new Tuple<Property[], IQuery<Property>>(
				[Setup.GlobalPropertyA!, Setup.SchedulingPropertyB!, Setup.GlobalPropertyC!, Setup.SchedulingPropertyD!],
				PropertyFilter.ToQuery().OrderBy(PropertyExposers.Order).Limit(10)),
		};

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query. Only the properties that are
		/// used in a scheduling context are expected to be returned.
		/// </summary>
		private Tuple<Property[], IQuery<Property>>[] SchedulingPropertyQueryTestCases => new[]
		{
			new Tuple<Property[], IQuery<Property>>(
				[Setup.SchedulingPropertyB!, Setup.SchedulingPropertyD!],
				PropertyFilter.ToQuery().OrderBy(PropertyExposers.Name)),
			new Tuple<Property[], IQuery<Property>>(
				[Setup.SchedulingPropertyD!, Setup.SchedulingPropertyB!],
				PropertyFilter.ToQuery().OrderByDescending(PropertyExposers.Name)),

			new Tuple<Property[], IQuery<Property>>(
				[Setup.SchedulingPropertyD!, Setup.SchedulingPropertyB!],
				PropertyFilter.ToQuery().OrderByDescending(PropertyExposers.Order)),

			new Tuple<Property[], IQuery<Property>>(
				[Setup.SchedulingPropertyB!],
				PropertyFilter.AND(PropertyExposers.SectionName.Equal("General")).ToQuery().OrderBy(PropertyExposers.Order)),
			new Tuple<Property[], IQuery<Property>>(
				[],
				PropertyFilter.AND(PropertyExposers.SectionName.Equal($"Unknown_{Setup.Prefix}")).ToQuery().OrderBy(PropertyExposers.Name)),
		};

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query.
		/// </summary>
		private Tuple<PropertySettingCollection[], IQuery<PropertySettingCollection>>[] PropertySettingCollectionQueryTestCases => new[]
		{
			new Tuple<PropertySettingCollection[], IQuery<PropertySettingCollection>>(
				[Setup.PropertySettingCollection1!, Setup.PropertySettingCollection2!, Setup.PropertySettingCollection3!],
				PropertySettingCollectionFilter.ToQuery().OrderBy(PropertySettingCollectionExposers.LinkedObjectId)),
			new Tuple<PropertySettingCollection[], IQuery<PropertySettingCollection>>(
				[Setup.PropertySettingCollection3!, Setup.PropertySettingCollection2!, Setup.PropertySettingCollection1!],
				PropertySettingCollectionFilter.ToQuery().OrderByDescending(PropertySettingCollectionExposers.LinkedObjectId)),

			new Tuple<PropertySettingCollection[], IQuery<PropertySettingCollection>>(
				[Setup.PropertySettingCollection1!, Setup.PropertySettingCollection2!],
				PropertySettingCollectionFilter.AND(PropertySettingCollectionExposers.PropertySettings.PropertyId.Equal(Setup.GlobalPropertyA!.Id)).ToQuery().OrderBy(PropertySettingCollectionExposers.LinkedObjectId)),
		};

		[TestMethod]
		public void ReadPropertiesWithQuery()
		{
			foreach (var (expectedObjects, query) in PropertyQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.Properties, expectedObjects, query);
			}
		}

		[TestMethod]
		public void CountPropertiesWithQuery()
		{
			foreach (var (expectedObjects, query) in PropertyQueryTestCases)
			{
				QueryAssert.Count(TestContext.Api.Properties, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadPropertiesPagedWithQuery_DefaultPageSize()
		{
			foreach (var (expectedObjects, query) in PropertyQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.Properties, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadPropertiesPagedWithQuery_CustomPageSize()
		{
			foreach (var (expectedObjects, query) in PropertyQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.Properties, expectedObjects, query, 2);
			}
		}

		[TestMethod]
		public void ReadPropertiesWithLimitedQuery()
		{
			var query = PropertyFilter.ToQuery().OrderBy(PropertyExposers.Name).Limit(2);

			QueryAssert.Read(TestContext.Api.Properties, [Setup.GlobalPropertyA!, Setup.SchedulingPropertyB!], query);
		}

		[TestMethod]
		public void ReadPartialPropertiesWithQuery_ReturnsSamePropertiesInSameOrder()
		{
			foreach (var (expectedObjects, query) in PropertyQueryTestCases.Concat(PropertyLimitedQueryTestCases))
			{
				var partialProperties = TestContext.Api.Properties.ReadPartial(query.WithFields(PropertyExposers.Name)).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), partialProperties.Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), partialProperties.Select(x => x.GetValue(PropertyExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedPropertiesWithQuery_DefaultPageSize_ReturnsSamePropertiesInOnePage()
		{
			foreach (var (expectedObjects, query) in PropertyQueryTestCases.Concat(PropertyLimitedQueryTestCases))
			{
				var pages = TestContext.Api.Properties.ReadPartialPaged(query.WithFields(PropertyExposers.Order)).ToList();

				Assert.AreEqual(1, pages.Count, $"Query: {query}");
				Assert.IsFalse(pages[0].HasNextPage, $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages[0].Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Order).ToList(), pages[0].Select(x => x.GetValue(PropertyExposers.Order)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedPropertiesWithQuery_CustomPageSize_SplitsPropertiesOverPages()
		{
			var repository = (PropertiesRepository)TestContext.Api.Properties;

			foreach (var (expectedObjects, query) in PropertyQueryTestCases.Concat(PropertyLimitedQueryTestCases))
			{
				var pages = repository.ReadPartialPaged(query.WithFields(PropertyExposers.Name), 2).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages.SelectMany(x => x).Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(Enumerable.Range(0, pages.Count).ToList(), pages.Select(x => x.PageNumber).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(pages.Select((_, i) => i < pages.Count - 1).ToList(), pages.Select(x => x.HasNextPage).ToList(), $"Query: {query}");
				Assert.IsTrue(pages.All(x => x.Count <= 2), $"Query: {query}");
			}

			Assert.IsTrue(PropertyQueryTestCases.Any(x => x.Item1.Length > 2), "No query returns more than one page.");
		}

		[TestMethod]
		public void ReadPartialPagedPropertiesWithFilter_ReturnsAllProperties()
		{
			var pages = TestContext.Api.Properties.ReadPartialPaged(PropertyFilter.WithFields(PropertyExposers.Scope, PropertyExposers.SectionName)).ToList();
			var propertiesById = TestContext.Api.Properties.Read(PropertyFilter).ToDictionary(x => x.Id);

			Assert.AreEqual(1, pages.Count);
			CollectionAssert.AreEquivalent(propertiesById.Keys.ToList(), pages[0].Select(x => x.Id).ToList());

			foreach (var partialProperty in pages[0])
			{
				var property = propertiesById[partialProperty.Id];
				Assert.AreEqual(property.Scope, partialProperty.GetValue(PropertyExposers.Scope));
				Assert.AreEqual(property.SectionName, partialProperty.GetValue(PropertyExposers.SectionName));
			}
		}

		[TestMethod]
		public void ReadPartialPropertiesWithFilter_EveryPropertyType_ReturnsSameValuesAsFullRead()
		{
			var otherTypes = new Property[]
			{
				objectCreator!.CreateProperty(new BooleanProperty(new PropertyData { Scope = "global" }) { Name = $"Boolean_{Setup.Prefix}", SectionName = "Types", Order = 5, DefaultValue = true }),
				objectCreator.CreateProperty(new DiscreteProperty(new PropertyData { Scope = "global" }) { Name = $"Discrete_{Setup.Prefix}", SectionName = "Types", Order = 6 }.SetDiscretes(["One", "Two"])),
				objectCreator.CreateProperty(new FileProperty(new PropertyData { Scope = "global" }) { Name = $"File_{Setup.Prefix}", SectionName = "Types", Order = 7 }),
			};

			var ids = Setup.Properties.Concat(otherTypes).Select(x => x.Id).ToList();
			var filter = new ORFilterElement<Property>(ids.Select(x => PropertyExposers.Id.Equal(x)).ToArray());
			var partialFilter = filter.WithFields(
				PropertyExposers.Id,
				PropertyExposers.Name,
				PropertyExposers.Scope,
				PropertyExposers.SectionName,
				PropertyExposers.Order);

			var partialPropertiesById = TestContext.Api.Properties.ReadPartial(partialFilter).ToDictionary(x => x.Id);
			var properties = TestContext.Api.Properties.Read(filter).ToList();

			Assert.AreEqual(ids.Count, properties.Count);
			Assert.IsTrue(properties.Any(x => x.IsBooleanProperty(out _)) && properties.Any(x => x.IsDiscreteProperty(out _)) && properties.Any(x => x.IsFileProperty(out _)) && properties.Any(x => x.IsStringProperty(out _)));
			Assert.AreEqual(properties.Count, partialPropertiesById.Count);

			foreach (var property in properties)
			{
				var partialProperty = partialPropertiesById[property.Id];

				Assert.AreEqual(property.Id, partialProperty.GetValue(PropertyExposers.Id));
				Assert.AreEqual(property.Name, partialProperty.GetValue(PropertyExposers.Name));
				Assert.AreEqual(property.Scope, partialProperty.GetValue(PropertyExposers.Scope));
				Assert.AreEqual(property.SectionName, partialProperty.GetValue(PropertyExposers.SectionName));
				Assert.AreEqual(property.Order, partialProperty.GetValue(PropertyExposers.Order));
			}
		}

		[TestMethod]
		public void ReadPartialProperties_OnlyId_ReturnsIds()
		{
			var partialProperties = TestContext.Api.Properties.ReadPartial(PropertyFilter.WithFields(PropertyExposers.Id)).ToList();

			CollectionAssert.AreEquivalent(Setup.Properties.Select(x => x.Id).ToList(), partialProperties.Select(x => x.GetValue(PropertyExposers.Id)).ToList());
		}

		[TestMethod]
		public void ReadPartialProperties_UnknownField_ThrowsBeforeReading()
		{
			var unknownField = new Exposer<Property, string>(x => x.Name, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.Properties.ReadPartial(PropertyFilter.WithFields(unknownField)));
		}

		[TestMethod]
		public void ReadSchedulingPropertiesWithQuery()
		{
			foreach (var (expectedObjects, query) in SchedulingPropertyQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.SchedulingProperties, expectedObjects, query);
			}
		}

		[TestMethod]
		public void CountSchedulingPropertiesWithQuery()
		{
			foreach (var (expectedObjects, query) in SchedulingPropertyQueryTestCases)
			{
				QueryAssert.Count(TestContext.Api.SchedulingProperties, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadSchedulingPropertiesPagedWithQuery_DefaultPageSize()
		{
			foreach (var (expectedObjects, query) in SchedulingPropertyQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.SchedulingProperties, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadSchedulingPropertiesPagedWithQuery_CustomPageSize()
		{
			foreach (var (expectedObjects, query) in SchedulingPropertyQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.SchedulingProperties, expectedObjects, query, 1);
			}
		}

		[TestMethod]
		public void ReadSchedulingPropertiesWithLimitedQuery()
		{
			var query = PropertyFilter.ToQuery().OrderBy(PropertyExposers.Name).Limit(1);

			QueryAssert.Read(TestContext.Api.SchedulingProperties, [Setup.SchedulingPropertyB!], query);
		}

		[TestMethod]
		public void ReadPropertySettingCollectionsWithQuery()
		{
			foreach (var (expectedObjects, query) in PropertySettingCollectionQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.PropertySettingCollections, expectedObjects, query);
			}
		}

		[TestMethod]
		public void CountPropertySettingCollectionsWithQuery()
		{
			foreach (var (expectedObjects, query) in PropertySettingCollectionQueryTestCases)
			{
				QueryAssert.Count(TestContext.Api.PropertySettingCollections, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadPropertySettingCollectionsPagedWithQuery_CustomPageSize()
		{
			foreach (var (expectedObjects, query) in PropertySettingCollectionQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.PropertySettingCollections, expectedObjects, query, 2);
			}
		}

		[TestMethod]
		public void ReadPropertySettingCollectionsWithLimitedQuery()
		{
			var query = PropertySettingCollectionFilter.ToQuery().OrderBy(PropertySettingCollectionExposers.LinkedObjectId).Limit(2);

			QueryAssert.Read(TestContext.Api.PropertySettingCollections, [Setup.PropertySettingCollection1!, Setup.PropertySettingCollection2!], query);
		}

		[TestMethod]
		public void ReadPropertySettingCollectionsWithUnsupportedOrderByThrowsException()
		{
			var query = PropertySettingCollectionFilter.ToQuery().OrderBy(PropertySettingCollectionExposers.PropertySettings.PropertyId);

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.PropertySettingCollections.Read(query).ToList());
		}
	}
}
