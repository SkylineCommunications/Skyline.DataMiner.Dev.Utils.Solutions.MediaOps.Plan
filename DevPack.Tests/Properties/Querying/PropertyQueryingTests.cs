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
