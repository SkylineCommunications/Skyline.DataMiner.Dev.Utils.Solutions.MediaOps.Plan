namespace RT_MediaOps.Plan.RST.Capabilities
{
	using System.Collections.Generic;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	using SLDataGateway.API.Querying;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class BasicTests : IDisposable
	{
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
		public void BasicCrudActions()
		{
			string name = $"Capability_{Guid.NewGuid()}";
			var capability = new Capability
			{
				Name = name,
				IsMandatory = true,
				IsTimeDependent = false,
			};

			capability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });

			objectCreator.CreateCapability(capability);

			var createdCapability = TestContext.Api.Capabilities.Read(capability.Id);
			Assert.IsNotNull(createdCapability);
			Assert.AreEqual(name, createdCapability.Name);
			Assert.IsTrue(createdCapability.IsMandatory);
			CollectionAssert.AreEquivalent(new List<string> { "Value 1", "Value 2", "Value 3" }, createdCapability.Discretes.ToList());
			Assert.IsFalse(createdCapability.IsTimeDependent);

			TestContext.Api.Capabilities.Delete(capability.Id);
		}

		[TestMethod]
		public void Update_PersistsName_ForSingleCapability()
		{
			var prefix = Guid.NewGuid();
			var capability = new Capability
			{
				Name = $"{prefix}_Capability",
			};
			capability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });

			objectCreator.CreateCapability(capability);

			var persistedCapability = TestContext.Api.Capabilities.Read(capability.Id);
			Assert.IsNotNull(persistedCapability);
			Assert.AreEqual(capability.Name, persistedCapability.Name);

			var updatedName = $"{prefix}_Updated";
			persistedCapability.Name = updatedName; // Regression guard: Name must be copied to the underlying CORE parameter during update.
			TestContext.Api.Capabilities.Update(persistedCapability);

			var updatedCapability = TestContext.Api.Capabilities.Read(capability.Id);
			Assert.IsNotNull(updatedCapability);
			Assert.AreEqual(updatedName, updatedCapability.Name);

			var coreCapability = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(capability.Id)).SingleOrDefault();
			Assert.IsNotNull(coreCapability);
			Assert.AreEqual(updatedName, coreCapability.Name);
		}

		[TestMethod]
		public void Update_PersistsName_ForBulkCapabilities()
		{
			var prefix = Guid.NewGuid();
			var capability1 = new Capability
			{
				Name = $"{prefix}_Capability_1",
			};
			capability1.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });

			var capability2 = new Capability
			{
				Name = $"{prefix}_Capability_2",
			};
			capability2.SetDiscretes(new[] { "Value A", "Value B", "Value C" });

			objectCreator.CreateCapabilities([capability1, capability2]);

			var persistedCapability1 = TestContext.Api.Capabilities.Read(capability1.Id);
			var persistedCapability2 = TestContext.Api.Capabilities.Read(capability2.Id);
			Assert.IsNotNull(persistedCapability1);
			Assert.IsNotNull(persistedCapability2);
			Assert.AreEqual(capability1.Name, persistedCapability1.Name);
			Assert.AreEqual(capability2.Name, persistedCapability2.Name);

			var updatedName1 = $"{prefix}_Updated_1";
			var updatedName2 = $"{prefix}_Updated_2";

			persistedCapability1.Name = updatedName1; // Regression guard: Name must be copied to the underlying CORE parameter during update.
			persistedCapability2.Name = updatedName2;

			TestContext.Api.Capabilities.Update([persistedCapability1, persistedCapability2]);

			var updatedCapability1 = TestContext.Api.Capabilities.Read(capability1.Id);
			var updatedCapability2 = TestContext.Api.Capabilities.Read(capability2.Id);
			Assert.IsNotNull(updatedCapability1);
			Assert.IsNotNull(updatedCapability2);
			Assert.AreEqual(updatedName1, updatedCapability1.Name);
			Assert.AreEqual(updatedName2, updatedCapability2.Name);

			var coreCapability1 = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(capability1.Id)).SingleOrDefault();
			var coreCapability2 = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(capability2.Id)).SingleOrDefault();
			Assert.IsNotNull(coreCapability1);
			Assert.IsNotNull(coreCapability2);
			Assert.AreEqual(updatedName1, coreCapability1.Name);
			Assert.AreEqual(updatedName2, coreCapability2.Name);
		}

		[TestMethod]
		public void TimeDependentCapability()
		{
			string name = $"Capability_{Guid.NewGuid()}";
			string linkedName = $"{name} - Time Dependent";
			var capability = new Capability
			{
				Name = name,
				IsMandatory = true,
				IsTimeDependent = true,
			};

			capability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });

			objectCreator.CreateCapability(capability);

			var mainApiCapability = TestContext.Api.Capabilities.Read(capability.Id);
			Assert.IsNotNull(mainApiCapability);
			Assert.IsFalse(TestContext.Api.Capabilities.Read().Any(x => x.Name.Equals(linkedName))); // Linked capabilities should not be accessible from API

			var mainCoreCapability = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.Name.Equal(name)).SingleOrDefault();
			Assert.IsNotNull(mainCoreCapability);

			var linkedCoreCapability = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.Name.Equal(linkedName)).SingleOrDefault();
			Assert.IsNotNull(linkedCoreCapability);

			Assert.IsTrue(TimeDependentCapabilityLink.TryDeserialize(mainCoreCapability.Remarks, out var linkedResult));
			Assert.IsTrue(linkedResult.IsTimeDependent);
			Assert.AreEqual(linkedCoreCapability.ID, linkedResult.LinkedParameterId);

			TestContext.Api.Capabilities.Delete(capability.Id);

			// Verify whether both parameters were removed
			mainCoreCapability = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.Name.Equal(name)).SingleOrDefault();
			linkedCoreCapability = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.Name.Equal(linkedName)).SingleOrDefault();

			Assert.IsNull(mainCoreCapability);
			Assert.IsNull(linkedCoreCapability);
		}

		[TestMethod]
		public void ChangeRegularCapabilityToTimeDependentThrowsException()
		{
			var prefix = Guid.NewGuid().ToString();

			var capability = new Capability
			{
				Name = $"{prefix}_Capability",
				IsTimeDependent = false,
			};
			capability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
			objectCreator.CreateCapability(capability);

			capability = TestContext.Api.Capabilities.Read(capability.Id);
			capability.IsTimeDependent = true;

			try
			{
				TestContext.Api.Capabilities.Update(capability);
			}
			catch (MediaOpsException ex)
			{
				StringAssert.Contains(ex.Message, "Changing the time dependency of a capability is not allowed.");

				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
				var capabilityConfigurationError = ex.TraceData.ErrorData.OfType<CapabilityError>().SingleOrDefault();
				Assert.IsNotNull(capabilityConfigurationError);

				var capabilityConfigurationInvalidTimeDependencyError = capabilityConfigurationError as CapabilityInvalidTimeDependencyError;
				Assert.IsNotNull(capabilityConfigurationInvalidTimeDependencyError);
				Assert.AreEqual(capability.Id, capabilityConfigurationInvalidTimeDependencyError.Id);
				Assert.AreEqual("Changing the time dependency of a capability is not allowed.", capabilityConfigurationError.ErrorMessage);

				return;
			}

			Assert.Fail("Expected exception was not thrown when changing a regular capability to time-dependent.");
		}

		[TestMethod]
		public void ChangeTimeDependentToRegularCapabilityThrowsException()
		{
			var prefix = Guid.NewGuid().ToString();
			var capability = new Capability
			{
				Name = $"{prefix}_Capability",
				IsTimeDependent = true,
			};
			capability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
			objectCreator.CreateCapability(capability);

			capability = TestContext.Api.Capabilities.Read(capability.Id);
			capability.IsTimeDependent = false;

			try
			{
				TestContext.Api.Capabilities.Update(capability);
			}
			catch (MediaOpsException ex)
			{
				StringAssert.Contains(ex.Message, "Changing the time dependency of a capability is not allowed.");

				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
				var capabilityConfigurationError = ex.TraceData.ErrorData.OfType<CapabilityError>().SingleOrDefault();
				Assert.IsNotNull(capabilityConfigurationError);

				var capabilityConfigurationInvalidTimeDependencyError = capabilityConfigurationError as CapabilityInvalidTimeDependencyError;
				Assert.IsNotNull(capabilityConfigurationInvalidTimeDependencyError);
				Assert.AreEqual(capability.Id, capabilityConfigurationInvalidTimeDependencyError.Id);
				Assert.AreEqual("Changing the time dependency of a capability is not allowed.", capabilityConfigurationError.ErrorMessage);

				return;
			}

			Assert.Fail("Expected exception was not thrown when changing a time-dependent capability to regular.");
		}

		[TestMethod]
		public void DuplicateDiscretes()
		{
			string name = $"Capability_{Guid.NewGuid()}";
			string linkedName = $"{name} - Time Dependent";
			var capability = new Capability
			{
				Name = name,
				IsMandatory = true,
				IsTimeDependent = true,
			};

			string discreteValue = "Value 1";
			var values = Enumerable.Repeat(discreteValue, 10).ToList();
			capability.SetDiscretes(values);

			try
			{
				objectCreator.CreateCapability(capability);
			}
			catch (MediaOpsException ex)
			{
				Assert.AreEqual("The capability defines the following duplicate discretes: Value 1, Value 1, Value 1, Value 1, Value 1, Value 1, Value 1, Value 1, Value 1, Value 1.", ex.Message);
				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

				var duplicateDiscretesError = ex.TraceData.ErrorData.OfType<CapabilityDuplicateDiscretesError>().SingleOrDefault();
				Assert.IsNotNull(duplicateDiscretesError);

				Assert.AreEqual(duplicateDiscretesError.Id, capability.Id);
				CollectionAssert.AreEquivalent(values, duplicateDiscretesError.Discretes);

				var apiCapability = TestContext.Api.Capabilities.Read(capability.Id);
				Assert.IsNull(apiCapability);

				return;
			}

			Assert.Fail("The expected exception was not thrown.");
		}

		[TestMethod]
		public void ReadAllPaged()
		{
			foreach (var page in TestContext.Api.Capabilities.ReadPaged())
			{
				foreach (var capability in page)
				{
					Assert.IsNotNull(capability);
				}
			}
		}

		[TestMethod]
		public void ReadWithEmptyListReturnsEmptyList()
		{
			var capabilities = TestContext.Api.Capabilities.Read(new List<Guid>());
			Assert.IsNotNull(capabilities);
			Assert.AreEqual(0, capabilities.Count());
		}

		[TestMethod]
		public void ReadWithEmptyFilterReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Capability>(idsToRetrieve.Select(x => CapabilityExposers.Id.Equal(x)).ToArray());

			var capabilities = TestContext.Api.Capabilities.Read(emptyFilter);
			Assert.IsNotNull(capabilities);
			Assert.AreEqual(0, capabilities.Count());
		}

		[TestMethod]
		public void CountWithEmptyFilterReturnsZero()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Capability>(idsToRetrieve.Select(x => CapabilityExposers.Id.Equal(x)).ToArray());

			var count = TestContext.Api.Capabilities.Count(emptyFilter);
			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public void ReadWithEmptyQueryReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Capability>(idsToRetrieve.Select(x => CapabilityExposers.Id.Equal(x)).ToArray());
			var queryWithEmptyFilter = emptyFilter.ToQuery();

			var capabilities = TestContext.Api.Capabilities.Read(queryWithEmptyFilter);
			Assert.IsNotNull(capabilities);
			Assert.AreEqual(0, capabilities.Count());
		}

		[TestMethod]
		public void CreateWithNullNameThrowsException()
		{
			var capability = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
			{
				Name = null,
			};

			try
			{
				objectCreator.CreateCapability(capability);
			}
			catch (MediaOpsException ex)
			{
				var invalidNameError = ex.TraceData.ErrorData.OfType<CapabilityInvalidNameError>().SingleOrDefault();
				Assert.IsNotNull(invalidNameError);
				Assert.AreEqual("Name cannot be empty.", invalidNameError.ErrorMessage);
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void CreateWithEmptyNameThrowsException()
		{
			var capability = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
			{
				Name = string.Empty,
			};

			try
			{
				objectCreator.CreateCapability(capability);
			}
			catch (MediaOpsException ex)
			{
				var invalidNameError = ex.TraceData.ErrorData.OfType<CapabilityInvalidNameError>().SingleOrDefault();
				Assert.IsNotNull(invalidNameError);
				Assert.AreEqual("Name cannot be empty.", invalidNameError.ErrorMessage);
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}
	}
}
