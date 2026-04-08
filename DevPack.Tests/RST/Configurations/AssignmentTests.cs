namespace RT_MediaOps.Plan.RST.Configurations
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class AssignmentTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public AssignmentTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void RemovingUsedTextDiscreteValueThrowsException()
		{
			var prefix = Guid.NewGuid();

			var configuration = new DiscreteTextConfiguration
			{
				Name = $"{prefix}_Configuration",
			}
			.SetDiscretes(new[]
			{
				new TextDiscreet("1", "Value 1"),
				new TextDiscreet("2", "Value 2"),
				new TextDiscreet("3", "Value 3"),
			});
			objectCreator.CreateConfiguration(configuration);

			var resourcePool1 = new ResourcePool()
			{
				Name = $"{prefix}_ResourcePool1",
			};
			resourcePool1.OrchestrationSettings
				.AddConfiguration(new DiscreteTextConfigurationSetting(configuration.Id)
				{
					Value = configuration.Discretes.First(d => d.Value == "1"),
				})
				.AddOrchestrationEvent(new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails("script 1")
						.AddConfiguration(new DiscreteTextConfigurationSetting(configuration.Id)
						{
							Value = configuration.Discretes.First(d => d.Value == "2"),
						}),
				});

			var resourcePool2 = new ResourcePool()
			{
				Name = $"{prefix}_ResourcePool2",
			};
			resourcePool2.OrchestrationSettings
				.AddConfiguration(new DiscreteTextConfigurationSetting(configuration.Id)
				{
					Value = configuration.Discretes.First(d => d.Value == "2"),
				})
				.AddOrchestrationEvent(new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails("script 1")
						.AddConfiguration(new DiscreteTextConfigurationSetting(configuration.Id)
						{
							Value = configuration.Discretes.First(d => d.Value == "1"),
						}),
				});

			objectCreator.CreateResourcePools([resourcePool1, resourcePool2]);

			configuration = TestContext.Api.Configurations.Read(configuration.Id) as DiscreteTextConfiguration;
			Assert.IsNotNull(configuration);

			configuration.RemoveDiscrete(configuration.Discretes.First(d => d.Value == "2"));

			MediaOpsException? expectedException = null;
			try
			{
				TestContext.Api.Configurations.Update(configuration);
			}
			catch (MediaOpsException ex)
			{
				expectedException = ex;
			}

			if (expectedException == null)
			{
				Assert.Fail("Expected exception was not thrown.");
			}

			Assert.AreEqual(1, expectedException.TraceData.ErrorData.Count);
			var configurationTextDiscreteValueInUseError = expectedException.TraceData.ErrorData.OfType<ConfigurationTextDiscreteValueInUseError>().FirstOrDefault();
			Assert.IsNotNull(configurationTextDiscreteValueInUseError);

			var configurationTextDiscreteValueInUseByResourcePoolError = configurationTextDiscreteValueInUseError as ConfigurationTextDiscreteValueInUseByResourcePoolsError;
			Assert.IsNotNull(configurationTextDiscreteValueInUseByResourcePoolError);
			Assert.AreEqual("2", configurationTextDiscreteValueInUseByResourcePoolError.DiscreteValue.Value);
			Assert.AreEqual("Value 2", configurationTextDiscreteValueInUseByResourcePoolError.DiscreteValue.DisplayName);
			Assert.AreEqual(2, configurationTextDiscreteValueInUseByResourcePoolError.ResourcePoolIds.Count);
			Assert.IsTrue(configurationTextDiscreteValueInUseByResourcePoolError.ResourcePoolIds.Contains(resourcePool1.Id));
			Assert.IsTrue(configurationTextDiscreteValueInUseByResourcePoolError.ResourcePoolIds.Contains(resourcePool2.Id));
		}

		[TestMethod]
		public void RemovingUsedNumberDiscreteValueThrowsException()
		{
			var prefix = Guid.NewGuid();

			var configuration = new DiscreteNumberConfiguration
			{
				Name = $"{prefix}_Configuration",
			}
			.SetDiscretes(new[]
			{
				new NumberDiscreet(1, "Value 1"),
				new NumberDiscreet(2, "Value 2"),
				new NumberDiscreet(3, "Value 3"),
			});
			objectCreator.CreateConfiguration(configuration);

			var resourcePool1 = new ResourcePool()
			{
				Name = $"{prefix}_ResourcePool1",
			};
			resourcePool1.OrchestrationSettings
				.AddConfiguration(new DiscreteNumberConfigurationSetting(configuration.Id)
				{
					Value = configuration.Discretes.First(d => d.Value == 1),
				})
				.AddOrchestrationEvent(new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails("script 1")
						.AddConfiguration(new DiscreteNumberConfigurationSetting(configuration.Id)
						{
							Value = configuration.Discretes.First(d => d.Value == 2),
						}),
				});

			var resourcePool2 = new ResourcePool()
			{
				Name = $"{prefix}_ResourcePool2",
			};
			resourcePool2.OrchestrationSettings
				.AddConfiguration(new DiscreteNumberConfigurationSetting(configuration.Id)
				{
					Value = configuration.Discretes.First(d => d.Value == 2),
				})
				.AddOrchestrationEvent(new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails("script 1")
						.AddConfiguration(new DiscreteNumberConfigurationSetting(configuration.Id)
						{
							Value = configuration.Discretes.First(d => d.Value == 1),
						}),
				});

			objectCreator.CreateResourcePools([resourcePool1, resourcePool2]);

			configuration = TestContext.Api.Configurations.Read(configuration.Id) as DiscreteNumberConfiguration;
			Assert.IsNotNull(configuration);

			configuration.RemoveDiscrete(configuration.Discretes.First(d => d.Value == 2));

			MediaOpsException? expectedException = null;
			try
			{
				TestContext.Api.Configurations.Update(configuration);
			}
			catch (MediaOpsException ex)
			{
				expectedException = ex;
			}

			if (expectedException == null)
			{
				Assert.Fail("Expected exception was not thrown.");
			}

			Assert.AreEqual(1, expectedException.TraceData.ErrorData.Count);
			var configurationNumberDiscreteValueInUseError = expectedException.TraceData.ErrorData.OfType<ConfigurationNumberDiscreteValueInUseError>().FirstOrDefault();
			Assert.IsNotNull(configurationNumberDiscreteValueInUseError);

			var configurationNumberDiscreteValueInUseByResourcePoolError = configurationNumberDiscreteValueInUseError as ConfigurationNumberDiscreteValueInUseByResourcePoolsError;
			Assert.IsNotNull(configurationNumberDiscreteValueInUseByResourcePoolError);
			Assert.AreEqual(2, configurationNumberDiscreteValueInUseByResourcePoolError.DiscreteValue.Value);
			Assert.AreEqual("Value 2", configurationNumberDiscreteValueInUseByResourcePoolError.DiscreteValue.DisplayName);
			Assert.AreEqual(2, configurationNumberDiscreteValueInUseByResourcePoolError.ResourcePoolIds.Count);
			Assert.IsTrue(configurationNumberDiscreteValueInUseByResourcePoolError.ResourcePoolIds.Contains(resourcePool1.Id));
			Assert.IsTrue(configurationNumberDiscreteValueInUseByResourcePoolError.ResourcePoolIds.Contains(resourcePool2.Id));
		}
	}
}
