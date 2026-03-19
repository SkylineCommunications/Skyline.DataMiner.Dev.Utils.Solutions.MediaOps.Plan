namespace RT_MediaOps.Plan.RST.Configurations
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

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
