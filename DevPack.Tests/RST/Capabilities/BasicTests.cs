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
		public void UpdateUnmodifiedCapability()
		{
			var capability = new Capability
			{
				Name = $"{Guid.NewGuid()}_Capability",
			}
			.SetDiscretes(["Value 1", "Value 2"]);
			capability = objectCreator.CreateCapability(capability);

			var originalCapability = TestContext.Api.Capabilities.Read(capability.Id);
			var updatedCapability = TestContext.Api.Capabilities.Update(originalCapability);

			Assert.AreEqual(originalCapability, updatedCapability);
		}

		[TestMethod]
		public void BulkUpdateWithChangedAndUnchangedCapabilityReturnsTwoCapabilities()
		{
			var prefix = Guid.NewGuid().ToString();

			var changedCapability = new Capability
			{
				Name = $"{prefix}_Changed",
			}
			.SetDiscretes(["Value 1", "Value 2"]);
			var unchangedCapability = new Capability
			{
				Name = $"{prefix}_Unchanged",
			}
			.SetDiscretes(["Value 1", "Value 2"]);

			changedCapability = objectCreator.CreateCapability(changedCapability);
			unchangedCapability = objectCreator.CreateCapability(unchangedCapability);

			var changedToUpdate = TestContext.Api.Capabilities.Read(changedCapability.Id);
			var unchangedToUpdate = TestContext.Api.Capabilities.Read(unchangedCapability.Id);

			changedToUpdate.Name = $"{prefix}_Changed_Updated";

			var updatedCapabilities = TestContext.Api.Capabilities.Update(new[] { changedToUpdate, unchangedToUpdate });

			Assert.AreEqual(2, updatedCapabilities.Count);
			Assert.IsTrue(updatedCapabilities.Any(x => x.Id == changedCapability.Id));
			Assert.IsTrue(updatedCapabilities.Any(x => x.Id == unchangedCapability.Id));

			var changedAfterUpdate = TestContext.Api.Capabilities.Read(changedCapability.Id);
			var unchangedAfterUpdate = TestContext.Api.Capabilities.Read(unchangedCapability.Id);

			Assert.AreEqual(changedToUpdate.Name, changedAfterUpdate.Name);
			Assert.AreEqual(unchangedCapability.Name, unchangedAfterUpdate.Name);
		}

		[TestMethod]
		public void BulkUpdateWithChangedInvalidAndUnchangedCapabilityReturnsTwoSuccessfulIds()
		{
			var prefix = Guid.NewGuid().ToString();

			var changedCapability = new Capability
			{
				Name = $"{prefix}_Changed",
			}
			.SetDiscretes(["Value 1", "Value 2"]);
			var invalidCapability = new Capability
			{
				Name = $"{prefix}_Invalid",
			}
			.SetDiscretes(["Value 1", "Value 2"]);
			var unchangedCapability = new Capability
			{
				Name = $"{prefix}_Unchanged",
			}
			.SetDiscretes(["Value 1", "Value 2"]);

			changedCapability = objectCreator.CreateCapability(changedCapability);
			invalidCapability = objectCreator.CreateCapability(invalidCapability);
			unchangedCapability = objectCreator.CreateCapability(unchangedCapability);

			var changedToUpdate = TestContext.Api.Capabilities.Read(changedCapability.Id);
			var invalidToUpdate = TestContext.Api.Capabilities.Read(invalidCapability.Id);
			var unchangedToUpdate = TestContext.Api.Capabilities.Read(unchangedCapability.Id);

			changedToUpdate.Name = $"{prefix}_Changed_Updated";
			invalidToUpdate.Name = string.Empty;

			var ex = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => TestContext.Api.Capabilities.Update(new[] { changedToUpdate, invalidToUpdate, unchangedToUpdate }));

			Assert.AreEqual(2, ex.Result.SuccessfulIds.Count);
			Assert.IsTrue(ex.Result.SuccessfulIds.Contains(changedCapability.Id));
			Assert.IsTrue(ex.Result.SuccessfulIds.Contains(unchangedCapability.Id));
			Assert.AreEqual(1, ex.Result.UnsuccessfulIds.Count);
			Assert.IsTrue(ex.Result.UnsuccessfulIds.Contains(invalidCapability.Id));

			var changedAfterUpdate = TestContext.Api.Capabilities.Read(changedCapability.Id);
			var invalidAfterUpdate = TestContext.Api.Capabilities.Read(invalidCapability.Id);
			var unchangedAfterUpdate = TestContext.Api.Capabilities.Read(unchangedCapability.Id);

			Assert.AreEqual(changedToUpdate.Name, changedAfterUpdate.Name);
			Assert.AreEqual(invalidCapability.Name, invalidAfterUpdate.Name);
			Assert.AreEqual(unchangedCapability.Name, unchangedAfterUpdate.Name);
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
