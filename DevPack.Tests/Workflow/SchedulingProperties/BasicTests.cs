namespace RT_MediaOps.Plan.Workflow.SchedulingProperties
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class BasicTests : IDisposable
	{
		private const string MediaOpsScope = "MediaOps";
		private const string OtherScope = "other";

		private readonly TestObjectCreator objectCreator;

		public BasicTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void PropertiesRepoCreates_MediaOpsAndOtherScope_SchedulingRepoOnlyReturnsMediaOps()
		{
			var prefix = Guid.NewGuid();

			var mediaOpsProperty = new BooleanProperty(new PropertyData
			{
				Scope = MediaOpsScope,
			})
			{
				Name = $"{prefix}_MediaOps",
				SectionName = "General",
			};
			var otherProperty = new BooleanProperty(new PropertyData
			{
				Scope = OtherScope,
			})
			{
				Name = $"{prefix}_Other",
				SectionName = "General",
			};

			objectCreator.CreateProperty(mediaOpsProperty);
			objectCreator.CreateProperty(otherProperty);

			// PropertiesRepository must return both
			var allFromProperties = TestContext.Api.Properties
				.Read(new[] { mediaOpsProperty.Id, otherProperty.Id })
				.ToList();
			Assert.AreEqual(2, allFromProperties.Count);
			Assert.IsTrue(allFromProperties.Any(p => p.Id == mediaOpsProperty.Id));
			Assert.IsTrue(allFromProperties.Any(p => p.Id == otherProperty.Id));

			// SchedulingPropertiesRepository must return only the MediaOps-scoped one
			var allFromScheduling = TestContext.Api.SchedulingProperties
				.Read(new[] { mediaOpsProperty.Id, otherProperty.Id })
				.ToList();
			Assert.AreEqual(1, allFromScheduling.Count);
			Assert.AreEqual(mediaOpsProperty.Id, allFromScheduling[0].Id);
			Assert.AreEqual(MediaOpsScope, allFromScheduling[0].Scope);

			// Reading the non-MediaOps property by Id via SchedulingPropertiesRepository returns null
			Assert.IsNull(TestContext.Api.SchedulingProperties.Read(otherProperty.Id));
			Assert.IsNotNull(TestContext.Api.SchedulingProperties.Read(mediaOpsProperty.Id));
		}

		[TestMethod]
		public void CreateSchedulingProperty_WithoutScope_AssignsMediaOpsScope()
		{
			var property = new BooleanProperty
			{
				Name = $"{Guid.NewGuid()}_Property",
				SectionName = "General",
			};

			var created = objectCreator.CreateSchedulingProperty(property);

			Assert.AreEqual(MediaOpsScope, created.Scope);

			var readBack = TestContext.Api.SchedulingProperties.Read(created.Id);
			Assert.IsNotNull(readBack);
			Assert.AreEqual(MediaOpsScope, readBack.Scope);
		}

		[TestMethod]
		public void CreateSchedulingProperty_WithExplicitMediaOpsScope_Succeeds()
		{
			var property = new BooleanProperty(new PropertyData
			{
				Scope = MediaOpsScope,
			})
			{
				Name = $"{Guid.NewGuid()}_Property",
				SectionName = "General",
			};

			var created = objectCreator.CreateSchedulingProperty(property);

			Assert.AreEqual(MediaOpsScope, created.Scope);
			Assert.IsNotNull(TestContext.Api.SchedulingProperties.Read(created.Id));
		}

		[TestMethod]
		public void CreateSchedulingProperty_WithNonMediaOpsScope_ThrowsInvalidScopeError()
		{
			var property = new BooleanProperty(new PropertyData
			{
				Scope = OtherScope,
			})
			{
				Name = $"{Guid.NewGuid()}_Property",
				SectionName = "General",
			};

			try
			{
				objectCreator.CreateSchedulingProperty(property);
			}
			catch (MediaOpsException ex)
			{
				var invalidScopeError = ex.TraceData.ErrorData.OfType<SchedulingPropertyInvalidScopeError>().SingleOrDefault();
				Assert.IsNotNull(invalidScopeError);
				Assert.AreEqual(property.Id, invalidScopeError.Id);
				Assert.AreEqual(OtherScope, invalidScopeError.Scope);

				// Property must not have been persisted
				Assert.IsNull(TestContext.Api.Properties.Read(property.Id));
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void CreateSchedulingProperties_BulkWithMixedScopes_OnlyMediaOpsScopedFails()
		{
			var prefix = Guid.NewGuid();

			var validProperty = new BooleanProperty(new PropertyData
			{
				Scope = MediaOpsScope,
			})
			{
				Name = $"{prefix}_Valid",
				SectionName = "General",
			};
			var invalidProperty = new BooleanProperty(new PropertyData
			{
				Scope = OtherScope,
			})
			{
				Name = $"{prefix}_Invalid",
				SectionName = "General",
			};

			try
			{
				objectCreator.CreateSchedulingProperties(new Property[] { validProperty, invalidProperty });
			}
			catch (MediaOpsBulkException<Guid> ex)
			{
				Assert.IsTrue(ex.Result.SuccessfulIds.Contains(validProperty.Id));
				Assert.IsTrue(ex.Result.UnsuccessfulIds.Contains(invalidProperty.Id));

				Assert.IsTrue(ex.Result.TraceDataPerItem.TryGetValue(invalidProperty.Id, out var traceData));
				var invalidScopeError = traceData.ErrorData.OfType<SchedulingPropertyInvalidScopeError>().SingleOrDefault();
				Assert.IsNotNull(invalidScopeError);
				Assert.AreEqual(invalidProperty.Id, invalidScopeError.Id);
				Assert.AreEqual(OtherScope, invalidScopeError.Scope);

				// Valid property was persisted, invalid one was not
				Assert.IsNotNull(TestContext.Api.SchedulingProperties.Read(validProperty.Id));
				Assert.IsNull(TestContext.Api.Properties.Read(invalidProperty.Id));
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void DeleteSchedulingProperty_MediaOpsScope_DeletesProperty()
		{
			var property = new BooleanProperty(new PropertyData
			{
				Scope = MediaOpsScope,
			})
			{
				Name = $"{Guid.NewGuid()}_Property",
				SectionName = "General",
			};

			objectCreator.CreateSchedulingProperty(property);
			Assert.IsNotNull(TestContext.Api.SchedulingProperties.Read(property.Id));

			TestContext.Api.SchedulingProperties.Delete(property.Id);

			Assert.IsNull(TestContext.Api.SchedulingProperties.Read(property.Id));
			Assert.IsNull(TestContext.Api.Properties.Read(property.Id));
		}

		[TestMethod]
		public void DeleteSchedulingProperty_MediaOpsScope_WithOptions_DeletesProperty()
		{
			var property = new BooleanProperty(new PropertyData
			{
				Scope = MediaOpsScope,
			})
			{
				Name = $"{Guid.NewGuid()}_Property",
				SectionName = "General",
			};

			objectCreator.CreateSchedulingProperty(property);

			TestContext.Api.SchedulingProperties.Delete(property, new PropertyDeleteOptions { ForceDelete = false });

			Assert.IsNull(TestContext.Api.Properties.Read(property.Id));
		}

		[TestMethod]
		public void DeleteSchedulingProperty_NonMediaOpsScope_DoesNotDeleteProperty()
		{
			var property = new BooleanProperty(new PropertyData
			{
				Scope = OtherScope,
			})
			{
				Name = $"{Guid.NewGuid()}_Property",
				SectionName = "General",
			};

			objectCreator.CreateProperty(property);

			// SchedulingPropertiesRepository scopes its reads to MediaOps, so the property is invisible to it.
			// As a result, Delete-by-id is a no-op (target is not found) and the property must remain.
			TestContext.Api.SchedulingProperties.Delete(property.Id);

			Assert.IsNotNull(TestContext.Api.Properties.Read(property.Id));
		}

		[TestMethod]
		public void CountSchedulingProperties_OnlyCountsMediaOpsScopedProperties()
		{
			var prefix = Guid.NewGuid();

			var mediaOpsProperty = new BooleanProperty(new PropertyData
			{
				Scope = MediaOpsScope,
			})
			{
				Name = $"{prefix}_MediaOps",
				SectionName = "General",
			};
			var otherProperty = new BooleanProperty(new PropertyData
			{
				Scope = OtherScope,
			})
			{
				Name = $"{prefix}_Other",
				SectionName = "General",
			};

			objectCreator.CreateProperty(mediaOpsProperty);
			objectCreator.CreateProperty(otherProperty);

			var ids = new[] { mediaOpsProperty.Id, otherProperty.Id };
			var filter = new ORFilterElement<Property>(ids.Select(x => PropertyExposers.Id.Equal(x)).ToArray());

			Assert.AreEqual(2, TestContext.Api.Properties.Count(filter));
			Assert.AreEqual(1, TestContext.Api.SchedulingProperties.Count(filter));
		}

		[TestMethod]
		public void ReadPartialSchedulingPropertiesWithQuery_ReturnsSamePropertiesInSameOrder()
		{
			var setup = CreatePartialReadProperties();

			foreach (var (expectedObjects, query) in setup.QueryTestCases.Concat(setup.LimitedQueryTestCases))
			{
				var partialProperties = TestContext.Api.SchedulingProperties.ReadPartial(query.WithFields(PropertyExposers.Name)).ToList();
				var fullRead = TestContext.Api.SchedulingProperties.Read(query).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), fullRead.Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), partialProperties.Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(fullRead.Select(x => x.Name).ToList(), partialProperties.Select(x => x.GetValue(PropertyExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedSchedulingPropertiesWithQuery_DefaultPageSize_ReturnsSamePropertiesInOnePage()
		{
			var setup = CreatePartialReadProperties();

			foreach (var (expectedObjects, query) in setup.QueryTestCases.Concat(setup.LimitedQueryTestCases))
			{
				var pages = TestContext.Api.SchedulingProperties.ReadPartialPaged(query.WithFields(PropertyExposers.Name)).ToList();

				Assert.AreEqual(1, pages.Count, $"Query: {query}");
				Assert.IsFalse(pages[0].HasNextPage, $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages[0].Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), pages[0].Select(x => x.GetValue(PropertyExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedSchedulingPropertiesWithQuery_CustomPageSize_SplitsPropertiesOverPages()
		{
			var setup = CreatePartialReadProperties();
			var repository = (SchedulingPropertiesRepository)TestContext.Api.SchedulingProperties;

			foreach (var (expectedObjects, query) in setup.QueryTestCases.Concat(setup.LimitedQueryTestCases))
			{
				var pages = repository.ReadPartialPaged(query.WithFields(PropertyExposers.Name), 2).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages.SelectMany(x => x).Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(Enumerable.Range(0, pages.Count).ToList(), pages.Select(x => x.PageNumber).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(pages.Select((_, i) => i < pages.Count - 1).ToList(), pages.Select(x => x.HasNextPage).ToList(), $"Query: {query}");
				Assert.IsTrue(pages.All(x => x.Count <= 2), $"Query: {query}");
			}

			Assert.IsTrue(setup.QueryTestCases.Any(x => x.Item1.Length > 2), "No query returns more than one page.");
		}

		[TestMethod]
		public void ReadPartialPagedSchedulingPropertiesWithFilter_ReturnsAllScopedProperties()
		{
			var setup = CreatePartialReadProperties();

			var pages = TestContext.Api.SchedulingProperties.ReadPartialPaged(setup.Filter.WithFields(PropertyExposers.Name, PropertyExposers.Scope)).ToList();
			var propertiesById = TestContext.Api.SchedulingProperties.Read(setup.Filter).ToDictionary(x => x.Id);

			Assert.AreEqual(1, pages.Count);
			CollectionAssert.AreEquivalent(setup.ScopedProperties.Select(x => x.Id).ToList(), pages[0].Select(x => x.Id).ToList());
			CollectionAssert.AreEquivalent(propertiesById.Keys.ToList(), pages[0].Select(x => x.Id).ToList());

			foreach (var partialProperty in pages[0])
			{
				var property = propertiesById[partialProperty.Id];
				Assert.AreEqual(property.Name, partialProperty.GetValue(PropertyExposers.Name));
				Assert.AreEqual(property.Scope, partialProperty.GetValue(PropertyExposers.Scope));
			}
		}

		[TestMethod]
		public void ReadPartialSchedulingPropertiesWithFilter_ReturnsSameValuesAsFullRead()
		{
			var setup = CreatePartialReadProperties();

			var partialFilter = setup.Filter.WithFields(
				PropertyExposers.Id,
				PropertyExposers.Name,
				PropertyExposers.Scope,
				PropertyExposers.SectionName,
				PropertyExposers.Order);

			var partialPropertiesById = TestContext.Api.SchedulingProperties.ReadPartial(partialFilter).ToDictionary(x => x.Id);
			var properties = TestContext.Api.SchedulingProperties.Read(setup.Filter).ToList();

			Assert.AreEqual(setup.ScopedProperties.Length, properties.Count);
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
		public void ReadPartialSchedulingProperties_OnlyId_ReturnsIds()
		{
			var setup = CreatePartialReadProperties();

			var partialProperties = TestContext.Api.SchedulingProperties.ReadPartial(setup.Filter.WithFields(PropertyExposers.Id)).ToList();

			CollectionAssert.AreEquivalent(setup.ScopedProperties.Select(x => x.Id).ToList(), partialProperties.Select(x => x.GetValue(PropertyExposers.Id)).ToList());
		}

		[TestMethod]
		public void ReadPartialSchedulingProperties_UnknownField_ThrowsBeforeReading()
		{
			var unknownField = new Exposer<Property, string>(x => x.Name, "Unknown");
			var filter = PropertyExposers.Name.Contains(Guid.NewGuid().ToString());

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.SchedulingProperties.ReadPartial(filter.WithFields(unknownField)));
			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.SchedulingProperties.ReadPartialPaged(filter.WithFields(unknownField)));
		}

		/// <summary>
		/// Creates three properties in the scheduling scope of different types and one property in another scope that the
		/// scheduling properties repository must not return.
		/// </summary>
		private PartialReadSetup CreatePartialReadProperties()
		{
			var prefix = Guid.NewGuid().ToString();

			var stringProperty = objectCreator.CreateSchedulingProperty(new StringProperty
			{
				Name = $"{prefix}_A",
				SectionName = "SectionB",
				Order = 3,
			});
			var booleanProperty = objectCreator.CreateSchedulingProperty(new BooleanProperty
			{
				Name = $"{prefix}_B",
				SectionName = "SectionA",
				Order = 1,
			});
			var fileProperty = objectCreator.CreateSchedulingProperty(new FileProperty
			{
				Name = $"{prefix}_C",
				SectionName = "SectionA",
				Order = 2,
			});
			objectCreator.CreateProperty(new BooleanProperty(new PropertyData { Scope = OtherScope })
			{
				Name = $"{prefix}_D",
				SectionName = "SectionA",
				Order = 0,
			});

			return new PartialReadSetup(PropertyExposers.Name.Contains(prefix), stringProperty, booleanProperty, fileProperty);
		}

		private sealed class PartialReadSetup
		{
			public PartialReadSetup(FilterElement<Property> filter, Property stringProperty, Property booleanProperty, Property fileProperty)
			{
				Filter = filter;
				ScopedProperties = [stringProperty, booleanProperty, fileProperty];

				QueryTestCases =
				[
					Tuple.Create<Property[], IQuery<Property>>([booleanProperty, fileProperty, stringProperty], filter.ToQuery().OrderBy(PropertyExposers.Order)),
					Tuple.Create<Property[], IQuery<Property>>([fileProperty, booleanProperty, stringProperty], filter.ToQuery().OrderByDescending(PropertyExposers.Name)),
					Tuple.Create<Property[], IQuery<Property>>([stringProperty, booleanProperty, fileProperty], filter.ToQuery().OrderBy(PropertyExposers.Name)),
					Tuple.Create<Property[], IQuery<Property>>([booleanProperty, fileProperty], filter.AND(PropertyExposers.SectionName.Equal("SectionA")).ToQuery().OrderBy(PropertyExposers.Order)),
					Tuple.Create<Property[], IQuery<Property>>([], filter.AND(PropertyExposers.Name.Equal("Unknown")).ToQuery().OrderBy(PropertyExposers.Order)),
				];

				LimitedQueryTestCases =
				[
					Tuple.Create<Property[], IQuery<Property>>([booleanProperty], filter.ToQuery().OrderBy(PropertyExposers.Order).Limit(1)),
					Tuple.Create<Property[], IQuery<Property>>([booleanProperty, fileProperty], filter.ToQuery().OrderBy(PropertyExposers.Order).Limit(2)),
					Tuple.Create<Property[], IQuery<Property>>([fileProperty, booleanProperty], filter.ToQuery().OrderByDescending(PropertyExposers.Name).Limit(2)),
					Tuple.Create<Property[], IQuery<Property>>([stringProperty, booleanProperty, fileProperty], filter.ToQuery().OrderBy(PropertyExposers.Name).Limit(10)),
				];
			}

			public FilterElement<Property> Filter { get; }

			public Property[] ScopedProperties { get; }

			public Tuple<Property[], IQuery<Property>>[] QueryTestCases { get; }

			public Tuple<Property[], IQuery<Property>>[] LimitedQueryTestCases { get; }
		}
	}
}