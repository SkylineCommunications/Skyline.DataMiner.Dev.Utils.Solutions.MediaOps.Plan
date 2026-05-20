namespace RT_MediaOps.Plan.RST.Configurations
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	using SLDataGateway.API.Querying;

	[TestClass]
	[TestCategory("IntegrationTest")]
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
		public void ReadWithEmptyListReturnsEmptyList()
		{
			var configurations = TestContext.Api.Configurations.Read(new List<Guid>());
			Assert.IsNotNull(configurations);
			Assert.AreEqual(0, configurations.Count());
		}

		[TestMethod]
		public void Update_PersistsName_ForSingleConfiguration()
		{
			var prefix = Guid.NewGuid();
			var configuration = new TextConfiguration
			{
				Name = $"{prefix}_Configuration",
			};

			objectCreator.CreateConfiguration(configuration);

			var persistedConfiguration = TestContext.Api.Configurations.Read(configuration.Id);
			Assert.IsNotNull(persistedConfiguration);
			Assert.AreEqual(configuration.Name, persistedConfiguration.Name);

			var updatedName = $"{prefix}_Updated";
			persistedConfiguration.Name = updatedName; // Regression guard: Name must be copied to the underlying CORE parameter during update.
			TestContext.Api.Configurations.Update(persistedConfiguration);

			var updatedConfiguration = TestContext.Api.Configurations.Read(configuration.Id);
			Assert.IsNotNull(updatedConfiguration);
			Assert.AreEqual(updatedName, updatedConfiguration.Name);

			var coreConfiguration = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configuration.Id)).SingleOrDefault();
			Assert.IsNotNull(coreConfiguration);
			Assert.AreEqual(updatedName, coreConfiguration.Name);
		}

		[TestMethod]
		public void Update_PersistsName_ForBulkConfigurations()
		{
			var prefix = Guid.NewGuid();
			var configuration1 = new TextConfiguration
			{
				Name = $"{prefix}_Configuration_1",
			};

			var configuration2 = new NumberConfiguration
			{
				Name = $"{prefix}_Configuration_2",
			};

			objectCreator.CreateConfigurations([configuration1, configuration2]);

			var persistedConfiguration1 = TestContext.Api.Configurations.Read(configuration1.Id);
			var persistedConfiguration2 = TestContext.Api.Configurations.Read(configuration2.Id);
			Assert.IsNotNull(persistedConfiguration1);
			Assert.IsNotNull(persistedConfiguration2);
			Assert.AreEqual(configuration1.Name, persistedConfiguration1.Name);
			Assert.AreEqual(configuration2.Name, persistedConfiguration2.Name);

			var updatedName1 = $"{prefix}_Updated_1";
			var updatedName2 = $"{prefix}_Updated_2";

			persistedConfiguration1.Name = updatedName1; // Regression guard: Name must be copied to the underlying CORE parameter during update.
			persistedConfiguration2.Name = updatedName2;

			TestContext.Api.Configurations.Update([persistedConfiguration1, persistedConfiguration2]);

			var updatedConfiguration1 = TestContext.Api.Configurations.Read(configuration1.Id);
			var updatedConfiguration2 = TestContext.Api.Configurations.Read(configuration2.Id);
			Assert.IsNotNull(updatedConfiguration1);
			Assert.IsNotNull(updatedConfiguration2);
			Assert.AreEqual(updatedName1, updatedConfiguration1.Name);
			Assert.AreEqual(updatedName2, updatedConfiguration2.Name);

			var coreConfiguration1 = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configuration1.Id)).SingleOrDefault();
			var coreConfiguration2 = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configuration2.Id)).SingleOrDefault();
			Assert.IsNotNull(coreConfiguration1);
			Assert.IsNotNull(coreConfiguration2);
			Assert.AreEqual(updatedName1, coreConfiguration1.Name);
			Assert.AreEqual(updatedName2, coreConfiguration2.Name);
		}

		[TestMethod]
		public void ReadWithEmptyFilterReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Configuration>(idsToRetrieve.Select(x => Skyline.DataMiner.Solutions.MediaOps.Plan.API.ConfigurationExposers.Id.Equal(x)).ToArray());

			var configurations = TestContext.Api.Configurations.Read(emptyFilter);
			Assert.IsNotNull(configurations);
			Assert.AreEqual(0, configurations.Count());
		}

		[TestMethod]
		public void CountWithEmptyFilterReturnsZero()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Configuration>(idsToRetrieve.Select(x => Skyline.DataMiner.Solutions.MediaOps.Plan.API.ConfigurationExposers.Id.Equal(x)).ToArray());

			var count = TestContext.Api.Configurations.Count(emptyFilter);
			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public void ReadWithEmptyQueryReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Configuration>(idsToRetrieve.Select(x => Skyline.DataMiner.Solutions.MediaOps.Plan.API.ConfigurationExposers.Id.Equal(x)).ToArray());
			var queryWithEmptyFilter = emptyFilter.ToQuery();

			var configurations = TestContext.Api.Configurations.Read(queryWithEmptyFilter);
			Assert.IsNotNull(configurations);
			Assert.AreEqual(0, configurations.Count());
		}

		[TestMethod]
		public void DeleteTextConfiguration_PartOfResourcePoolConfigurations_ThrowsException()
		{
			var prefix = Guid.NewGuid();
			var textConfiguration = new TextConfiguration
			{
				Name = $"{prefix}_TextConfiguration",
			};

			objectCreator.CreateConfigurations([textConfiguration]);

			var resourcePool = new ResourcePool
			{
				Name = $"{prefix}_ResourcePool",
			};

			resourcePool.OrchestrationSettings.SetConfigurations([new TextConfigurationSetting(textConfiguration)]);
			objectCreator.CreateResourcePool(resourcePool);

			try
			{
				TestContext.Api.Configurations.Delete(textConfiguration);
			}
			catch (MediaOpsException ex)
			{
				var errorMessage = $"Configuration '{textConfiguration.Name}' is in use by 1 resource pool(s).";
				Assert.AreEqual(errorMessage, ex.Message);

				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

				var configurationInUseError = ex.TraceData.ErrorData.Single() as ConfigurationInUseByResourcePoolsError;
				Assert.IsNotNull(configurationInUseError);
				Assert.AreEqual(errorMessage, configurationInUseError.ErrorMessage);
				Assert.IsNotNull(configurationInUseError.ResourcePoolIds);
				Assert.AreEqual(1, configurationInUseError.ResourcePoolIds.Count());
				Assert.AreEqual(resourcePool.Id, configurationInUseError.ResourcePoolIds.Single());

				return;
			}

			Assert.Fail();
		}

		[TestMethod]
		public void DeleteNumberConfiguration_PartOfResourcePoolConfigurations_ThrowsException()
		{
			var prefix = Guid.NewGuid();
			var numberConfiguration = new NumberConfiguration
			{
				Name = $"{prefix}_NumberConfiguration",
			};

			objectCreator.CreateConfigurations([numberConfiguration]);

			var resourcePool = new ResourcePool
			{
				Name = $"{prefix}_ResourcePool",
			};

			resourcePool.OrchestrationSettings.SetConfigurations([new NumberConfigurationSetting(numberConfiguration)]);
			objectCreator.CreateResourcePool(resourcePool);

			try
			{
				TestContext.Api.Configurations.Delete(numberConfiguration);
			}
			catch (MediaOpsException ex)
			{
				var errorMessage = $"Configuration '{numberConfiguration.Name}' is in use by 1 resource pool(s).";
				Assert.AreEqual(errorMessage, ex.Message);

				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

				var configurationInUseError = ex.TraceData.ErrorData.Single() as ConfigurationInUseByResourcePoolsError;
				Assert.IsNotNull(configurationInUseError);
				Assert.AreEqual(errorMessage, configurationInUseError.ErrorMessage);
				Assert.IsNotNull(configurationInUseError.ResourcePoolIds);
				Assert.AreEqual(1, configurationInUseError.ResourcePoolIds.Count());
				Assert.AreEqual(resourcePool.Id, configurationInUseError.ResourcePoolIds.Single());

				return;
			}

			Assert.Fail();
		}

		[TestMethod]
		public void DeleteDiscreteTextConfiguration_PartOfResourcePoolConfigurations_ThrowsException()
		{
			var prefix = Guid.NewGuid();
			var discreteTextConfiguration = new DiscreteTextConfiguration
			{
				Name = $"{prefix}_DiscreteTextConfiguration",
			}.SetDiscretes([new TextDiscreet("A", "A")]);

			objectCreator.CreateConfigurations([discreteTextConfiguration]);

			var resourcePool = new ResourcePool
			{
				Name = $"{prefix}_ResourcePool",
			};

			resourcePool.OrchestrationSettings.SetConfigurations([new DiscreteTextConfigurationSetting(discreteTextConfiguration)]);
			objectCreator.CreateResourcePool(resourcePool);

			try
			{
				TestContext.Api.Configurations.Delete(discreteTextConfiguration);
			}
			catch (MediaOpsException ex)
			{
				var errorMessage = $"Configuration '{discreteTextConfiguration.Name}' is in use by 1 resource pool(s).";
				Assert.AreEqual(errorMessage, ex.Message);

				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

				var configurationInUseError = ex.TraceData.ErrorData.Single() as ConfigurationInUseByResourcePoolsError;
				Assert.IsNotNull(configurationInUseError);
				Assert.AreEqual(errorMessage, configurationInUseError.ErrorMessage);
				Assert.IsNotNull(configurationInUseError.ResourcePoolIds);
				Assert.AreEqual(1, configurationInUseError.ResourcePoolIds.Count());
				Assert.AreEqual(resourcePool.Id, configurationInUseError.ResourcePoolIds.Single());

				return;
			}

			Assert.Fail();
		}

		[TestMethod]
		public void DeleteDiscreteNumberConfiguration_PartOfResourcePoolConfigurations_ThrowsException()
		{
			var prefix = Guid.NewGuid();
			var discreteNumberConfiguration = new DiscreteNumberConfiguration
			{
				Name = $"{prefix}_DiscreteNumberConfiguration",
			}.SetDiscretes([new NumberDiscreet(1, "A")]);

			objectCreator.CreateConfigurations([discreteNumberConfiguration]);

			var resourcePool = new ResourcePool
			{
				Name = $"{prefix}_ResourcePool",
			};

			resourcePool.OrchestrationSettings.SetConfigurations([new DiscreteNumberConfigurationSetting(discreteNumberConfiguration)]);
			objectCreator.CreateResourcePool(resourcePool);

			try
			{
				TestContext.Api.Configurations.Delete(discreteNumberConfiguration);
			}
			catch (MediaOpsException ex)
			{
				var errorMessage = $"Configuration '{discreteNumberConfiguration.Name}' is in use by 1 resource pool(s).";
				Assert.AreEqual(errorMessage, ex.Message);

				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

				var configurationInUseError = ex.TraceData.ErrorData.Single() as ConfigurationInUseByResourcePoolsError;
				Assert.IsNotNull(configurationInUseError);
				Assert.AreEqual(errorMessage, configurationInUseError.ErrorMessage);
				Assert.IsNotNull(configurationInUseError.ResourcePoolIds);
				Assert.AreEqual(1, configurationInUseError.ResourcePoolIds.Count());
				Assert.AreEqual(resourcePool.Id, configurationInUseError.ResourcePoolIds.Single());

				return;
			}

			Assert.Fail();
		}

		[TestMethod]
		public void DeleteTextConfiguration_PartOfResourcePoolOrchestrationEvents_ThrowsException()
		{
			var prefix = Guid.NewGuid();
			var textConfiguration = new TextConfiguration
			{
				Name = $"{prefix}_TextConfiguration",
			};

			objectCreator.CreateConfigurations([textConfiguration]);

			var resourcePool = new ResourcePool
			{
				Name = $"{prefix}_ResourcePool",
			};

			resourcePool.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
			{
				new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails("SomeScript").AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "HelloWorld" }),
				},
			});
			objectCreator.CreateResourcePool(resourcePool);

			try
			{
				TestContext.Api.Configurations.Delete(textConfiguration);
			}
			catch (MediaOpsException ex)
			{
				var errorMessage = $"Configuration '{textConfiguration.Name}' is in use by 1 resource pool(s).";
				Assert.AreEqual(errorMessage, ex.Message);

				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

				var configurationInUseError = ex.TraceData.ErrorData.Single() as ConfigurationInUseByResourcePoolsError;
				Assert.IsNotNull(configurationInUseError);
				Assert.AreEqual(errorMessage, configurationInUseError.ErrorMessage);
				Assert.IsNotNull(configurationInUseError.ResourcePoolIds);
				Assert.AreEqual(1, configurationInUseError.ResourcePoolIds.Count());
				Assert.AreEqual(resourcePool.Id, configurationInUseError.ResourcePoolIds.Single());

				return;
			}

			Assert.Fail();
		}

		[TestMethod]
		public void DeleteNumberConfiguration_PartOfResourcePoolOrchestrationEvents_ThrowsException()
		{
			var prefix = Guid.NewGuid();
			var numberConfiguration = new NumberConfiguration
			{
				Name = $"{prefix}_NumberConfiguration",
			};

			objectCreator.CreateConfigurations([numberConfiguration]);

			var resourcePool = new ResourcePool
			{
				Name = $"{prefix}_ResourcePool",
			};

			resourcePool.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
			{
				new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails("SomeScript").AddConfiguration(new NumberConfigurationSetting(numberConfiguration) { Value = 100 }),
				},
			});
			objectCreator.CreateResourcePool(resourcePool);

			try
			{
				TestContext.Api.Configurations.Delete(numberConfiguration);
			}
			catch (MediaOpsException ex)
			{
				var errorMessage = $"Configuration '{numberConfiguration.Name}' is in use by 1 resource pool(s).";
				Assert.AreEqual(errorMessage, ex.Message);

				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

				var configurationInUseError = ex.TraceData.ErrorData.Single() as ConfigurationInUseByResourcePoolsError;
				Assert.IsNotNull(configurationInUseError);
				Assert.AreEqual(errorMessage, configurationInUseError.ErrorMessage);
				Assert.IsNotNull(configurationInUseError.ResourcePoolIds);
				Assert.AreEqual(1, configurationInUseError.ResourcePoolIds.Count());
				Assert.AreEqual(resourcePool.Id, configurationInUseError.ResourcePoolIds.Single());

				return;
			}

			Assert.Fail();
		}

		[TestMethod]
		public void DeleteDiscreteTextConfiguration_PartOfResourcePoolOrchestrationEvents_ThrowsException()
		{
			var prefix = Guid.NewGuid();
			var discreteTextConfiguration = new DiscreteTextConfiguration
			{
				Name = $"{prefix}_DiscreteTextConfiguration",
			}.SetDiscretes([new TextDiscreet("A", "A")]);

			objectCreator.CreateConfigurations([discreteTextConfiguration]);

			var resourcePool = new ResourcePool
			{
				Name = $"{prefix}_ResourcePool",
			};

			resourcePool.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
			{
				new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails("SomeScript").AddConfiguration(new DiscreteTextConfigurationSetting(discreteTextConfiguration) { Value = discreteTextConfiguration.Discretes.First() }),
				},
			});
			objectCreator.CreateResourcePool(resourcePool);

			try
			{
				TestContext.Api.Configurations.Delete(discreteTextConfiguration);
			}
			catch (MediaOpsException ex)
			{
				var errorMessage = $"Configuration '{discreteTextConfiguration.Name}' is in use by 1 resource pool(s).";
				Assert.AreEqual(errorMessage, ex.Message);

				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

				var configurationInUseError = ex.TraceData.ErrorData.Single() as ConfigurationInUseByResourcePoolsError;
				Assert.IsNotNull(configurationInUseError);
				Assert.AreEqual(errorMessage, configurationInUseError.ErrorMessage);
				Assert.IsNotNull(configurationInUseError.ResourcePoolIds);
				Assert.AreEqual(1, configurationInUseError.ResourcePoolIds.Count());
				Assert.AreEqual(resourcePool.Id, configurationInUseError.ResourcePoolIds.Single());

				return;
			}

			Assert.Fail();
		}

		[TestMethod]
		public void DeleteDiscreteNumberConfiguration_PartOfResourcePoolOrchestrationEvents_ThrowsException()
		{
			var prefix = Guid.NewGuid();
			var discreteNumberConfiguration = new DiscreteNumberConfiguration
			{
				Name = $"{prefix}_DiscreteNumberConfiguration",
			}.SetDiscretes([new NumberDiscreet(1, "A")]);

			objectCreator.CreateConfigurations([discreteNumberConfiguration]);

			var resourcePool = new ResourcePool
			{
				Name = $"{prefix}_ResourcePool",
			};

			resourcePool.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
			{
				new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails("SomeScript").AddConfiguration(new DiscreteNumberConfigurationSetting(discreteNumberConfiguration) { Value = discreteNumberConfiguration.Discretes.First() }),
				},
			});
			objectCreator.CreateResourcePool(resourcePool);

			try
			{
				TestContext.Api.Configurations.Delete(discreteNumberConfiguration);
			}
			catch (MediaOpsException ex)
			{
				var errorMessage = $"Configuration '{discreteNumberConfiguration.Name}' is in use by 1 resource pool(s).";
				Assert.AreEqual(errorMessage, ex.Message);

				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

				var configurationInUseError = ex.TraceData.ErrorData.Single() as ConfigurationInUseByResourcePoolsError;
				Assert.IsNotNull(configurationInUseError);
				Assert.AreEqual(errorMessage, configurationInUseError.ErrorMessage);
				Assert.IsNotNull(configurationInUseError.ResourcePoolIds);
				Assert.AreEqual(1, configurationInUseError.ResourcePoolIds.Count());
				Assert.AreEqual(resourcePool.Id, configurationInUseError.ResourcePoolIds.Single());

				return;
			}

			Assert.Fail();
		}
	}
}
