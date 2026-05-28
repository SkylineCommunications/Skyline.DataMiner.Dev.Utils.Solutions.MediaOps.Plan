namespace RT_MediaOps.Plan.Workflow.SchedulingProperties
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
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

			var mediaOpsProperty = new BooleanProperty
			{
				Name = $"{prefix}_MediaOps",
				Scope = MediaOpsScope,
				SectionName = "General",
			};
			var otherProperty = new BooleanProperty
			{
				Name = $"{prefix}_Other",
				Scope = OtherScope,
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
			var property = new BooleanProperty
			{
				Name = $"{Guid.NewGuid()}_Property",
				Scope = MediaOpsScope,
				SectionName = "General",
			};

			var created = objectCreator.CreateSchedulingProperty(property);

			Assert.AreEqual(MediaOpsScope, created.Scope);
			Assert.IsNotNull(TestContext.Api.SchedulingProperties.Read(created.Id));
		}

		[TestMethod]
		public void CreateSchedulingProperty_WithNonMediaOpsScope_ThrowsInvalidScopeError()
		{
			var property = new BooleanProperty
			{
				Name = $"{Guid.NewGuid()}_Property",
				Scope = OtherScope,
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

			var validProperty = new BooleanProperty
			{
				Name = $"{prefix}_Valid",
				Scope = MediaOpsScope,
				SectionName = "General",
			};
			var invalidProperty = new BooleanProperty
			{
				Name = $"{prefix}_Invalid",
				Scope = OtherScope,
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
			var property = new BooleanProperty
			{
				Name = $"{Guid.NewGuid()}_Property",
				Scope = MediaOpsScope,
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
			var property = new BooleanProperty
			{
				Name = $"{Guid.NewGuid()}_Property",
				Scope = MediaOpsScope,
				SectionName = "General",
			};

			objectCreator.CreateSchedulingProperty(property);

			TestContext.Api.SchedulingProperties.Delete(property, new PropertyDeleteOptions { ForceDelete = false });

			Assert.IsNull(TestContext.Api.Properties.Read(property.Id));
		}

		[TestMethod]
		public void DeleteSchedulingProperty_NonMediaOpsScope_DoesNotDeleteProperty()
		{
			var property = new BooleanProperty
			{
				Name = $"{Guid.NewGuid()}_Property",
				Scope = OtherScope,
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

			var mediaOpsProperty = new BooleanProperty
			{
				Name = $"{prefix}_MediaOps",
				Scope = MediaOpsScope,
				SectionName = "General",
			};
			var otherProperty = new BooleanProperty
			{
				Name = $"{prefix}_Other",
				Scope = OtherScope,
				SectionName = "General",
			};

			objectCreator.CreateProperty(mediaOpsProperty);
			objectCreator.CreateProperty(otherProperty);

			var ids = new[] { mediaOpsProperty.Id, otherProperty.Id };
			var filter = new ORFilterElement<Property>(ids.Select(x => PropertyExposers.Id.Equal(x)).ToArray());

			Assert.AreEqual(2, TestContext.Api.Properties.Count(filter));
			Assert.AreEqual(1, TestContext.Api.SchedulingProperties.Count(filter));
		}
	}
}
