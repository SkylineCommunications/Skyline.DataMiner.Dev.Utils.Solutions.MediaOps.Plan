namespace RT_MediaOps.Plan.RST.Configurations
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class BasicDiscreteTextConfigurationTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public BasicDiscreteTextConfigurationTests()
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
			// Create
			var configurationId = Guid.NewGuid();
			var name = $"{configurationId}_Configuration";

			var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration(configurationId)
			{
				Name = name,
			};

			configuration.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("low_value", "Low"));
			configuration.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("medium_value", "Medium"));
			configuration.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("high_value", "High"));

			objectCreator.CreateConfiguration(configuration);

			var returnedConfiguration = TestContext.Api.Configurations.Read(configurationId);
			Assert.IsNotNull(returnedConfiguration);
			Assert.AreEqual(name, returnedConfiguration.Name);
			Assert.AreEqual(false, returnedConfiguration.IsMandatory);
			Assert.IsInstanceOfType(returnedConfiguration, typeof(Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration));

			var discreteTextConfig = (Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration)returnedConfiguration;
			Assert.AreEqual(3, discreteTextConfig.Discretes.Count);
			Assert.IsTrue(discreteTextConfig.Discretes.Any(x => x.DisplayName == "Low"));
			Assert.AreEqual("low_value", discreteTextConfig.Discretes.First(x => x.DisplayName == "Low").Value);

			var coreConfiguration = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
			Assert.IsNotNull(coreConfiguration);
			Assert.AreEqual(name, coreConfiguration.Name);
			Assert.AreEqual(true, coreConfiguration.IsOptional);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Configuration, coreConfiguration.Categories);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Discrete, coreConfiguration.Type);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.String, coreConfiguration.InterpreteType.Type);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Other, coreConfiguration.InterpreteType.RawType);
			Assert.AreEqual(3, coreConfiguration.Discretes.Count);

			// Update
			var updatedName = name + "_Updated";
			discreteTextConfig.Name = updatedName;
			discreteTextConfig.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("critical_value", "Critical"));
			TestContext.Api.Configurations.Update(discreteTextConfig);

			returnedConfiguration = TestContext.Api.Configurations.Read(configurationId);
			Assert.IsNotNull(returnedConfiguration);
			Assert.AreEqual(updatedName, returnedConfiguration.Name);

			discreteTextConfig = (Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration)returnedConfiguration;
			Assert.AreEqual(4, discreteTextConfig.Discretes.Count);
			Assert.IsTrue(discreteTextConfig.Discretes.Any(x => x.DisplayName == "Critical"));

			coreConfiguration = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
			Assert.IsNotNull(coreConfiguration);
			Assert.AreEqual(updatedName, coreConfiguration.Name);
			Assert.AreEqual(4, coreConfiguration.Discretes.Count);

			// Delete
			TestContext.Api.Configurations.Delete(returnedConfiguration);
			returnedConfiguration = TestContext.Api.Configurations.Read(configurationId);
			Assert.IsNull(returnedConfiguration);

			coreConfiguration = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
			Assert.IsNull(coreConfiguration);
		}

		[TestMethod]
		public void CreateWithExistingIdThrowsException()
		{
			var configurationId = Guid.NewGuid();

			var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration(configurationId)
			{
				Name = $"{configurationId}_Configuration1",
			};
			configuration1.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("value_1", "Value1"));

			var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration(configurationId)
			{
				Name = $"{configurationId}_Configuration2",
			};
			configuration2.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("value_2", "Value2"));

			objectCreator.CreateConfiguration(configuration1);
			try
			{
				objectCreator.CreateConfiguration(configuration2);
			}
			catch (MediaOpsException ex)
			{
				StringAssert.Contains(ex.Message, "ID is already in use.");

				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
				var configurationConfigurationError = ex.TraceData.ErrorData.OfType<ConfigurationError>().SingleOrDefault();
				Assert.IsNotNull(configurationConfigurationError);

				var configurationConfigurationIdInUseError = configurationConfigurationError as ConfigurationIdInUseError;
				Assert.IsNotNull(configurationConfigurationIdInUseError);
				Assert.AreEqual("ID is already in use.", configurationConfigurationError.ErrorMessage);

				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void CreateWithSameIdInBulkThrowsException()
		{
			var configurationId = Guid.NewGuid();

			var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration(configurationId)
			{
				Name = $"{configurationId}_Configuration1",
			};
			configuration1.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("value_1", "Value1"));

			var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration(configurationId)
			{
				Name = $"{configurationId}_Configuration2",
			};
			configuration2.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("value_2", "Value2"));

			try
			{
				objectCreator.CreateConfigurations(new[] { configuration1, configuration2 });
			}
			catch (MediaOpsBulkException<Guid> ex)
			{
				if (!ex.Result.TraceDataPerItem.TryGetValue(configurationId, out var traceData))
				{
					Assert.Fail("No trace data found for the failed ID");
				}

				Assert.AreEqual(2, traceData.ErrorData.Count);
				var configurationConfigurationErrors = traceData.ErrorData.OfType<ConfigurationError>().ToList();
				Assert.AreEqual(2, configurationConfigurationErrors.Count());

				var errorMessages = new List<string>
				{
				   $"Configuration '{configuration1.Name}' has a duplicate ID.",
				   $"Configuration '{configuration2.Name}' has a duplicate ID.",
				};

				foreach (var error in configurationConfigurationErrors)
				{
					var configurationConfigurationDuplicateIdError = error as ConfigurationDuplicateIdError;
					Assert.IsNotNull(configurationConfigurationDuplicateIdError);
					Assert.IsTrue(errorMessages.Contains(error.ErrorMessage));

					errorMessages.Remove(error.ErrorMessage);
				}

				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void CreateWithExistingNameThrowsException()
		{
			var configurationId = Guid.NewGuid();

			var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration()
			{
				Name = $"{configurationId}_Configuration",
			};
			configuration1.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("value_1", "Value1"));

			var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration()
			{
				Name = $"{configurationId}_Configuration",
			};
			configuration2.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("value_2", "Value2"));

			objectCreator.CreateConfiguration(configuration1);
			try
			{
				objectCreator.CreateConfiguration(configuration2);
			}
			catch (MediaOpsException ex)
			{
				StringAssert.Contains(ex.Message, "Name is already in use.");

				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
				var configurationConfigurationError = ex.TraceData.ErrorData.OfType<ConfigurationError>().SingleOrDefault();
				Assert.IsNotNull(configurationConfigurationError);

				var configurationConfigurationNameExistsError = configurationConfigurationError as ConfigurationNameExistsError;
				Assert.IsNotNull(configurationConfigurationNameExistsError);
				Assert.AreEqual("Name is already in use.", configurationConfigurationError.ErrorMessage);

				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void CreateWithSameNameInBulkThrowsException()
		{
			var configurationId = Guid.NewGuid();

			var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration()
			{
				Name = $"{configurationId}_Configuration",
			};
			configuration1.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("value_1", "Value1"));

			var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration()
			{
				Name = $"{configurationId}_Configuration",
			};
			configuration2.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("value_2", "Value2"));

			try
			{
				objectCreator.CreateConfigurations(new[] { configuration1, configuration2 });
			}
			catch (MediaOpsBulkException<Guid> ex)
			{
				Assert.AreEqual(2, ex.Result.TraceDataPerItem.Count);

				foreach (var traceData in ex.Result.TraceDataPerItem.Values)
				{
					Assert.AreEqual(1, traceData.ErrorData.Count);
					var configurationConfigurationError = traceData.ErrorData.OfType<ConfigurationError>().SingleOrDefault();
					Assert.IsNotNull(configurationConfigurationError);

					var configurationConfigurationDuplicateNameError = configurationConfigurationError as ConfigurationDuplicateNameError;
					Assert.IsNotNull(configurationConfigurationDuplicateNameError);
					Assert.AreEqual($"Configuration '{configuration1.Name}' has a duplicate name.", configurationConfigurationError.ErrorMessage);
				}

				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void UpdateToSameNameThrowsException()
		{
			var configurationId = Guid.NewGuid();

			var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration()
			{
				Name = $"{configurationId}_Configuration_1",
			};
			configuration1.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("value_1", "Value1"));

			var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration()
			{
				Name = $"{configurationId}_Configuration_2",
			};
			configuration2.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("value_2", "Value2"));

			objectCreator.CreateConfiguration(configuration1);
			objectCreator.CreateConfiguration(configuration2);

			var toUpdate = TestContext.Api.Configurations.Read(configuration2.Id);
			toUpdate.Name = configuration1.Name;

			try
			{
				TestContext.Api.Configurations.Update(toUpdate);
			}
			catch (MediaOpsException ex)
			{
				StringAssert.Contains(ex.Message, "Name is already in use.");

				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
				var configurationConfigurationError = ex.TraceData.ErrorData.OfType<ConfigurationError>().SingleOrDefault();
				Assert.IsNotNull(configurationConfigurationError);

				var configurationConfigurationNameExistsError = configurationConfigurationError as ConfigurationNameExistsError;
				Assert.IsNotNull(configurationConfigurationNameExistsError);
				Assert.AreEqual("Name is already in use.", configurationConfigurationError.ErrorMessage);

				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void DiscreteValuesValidation()
		{
			// Create
			var configurationId = Guid.NewGuid();

			var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration(configurationId)
			{
				Name = $"{configurationId}_Configuration",
				IsMandatory = true,
			};

			configuration.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("low_value", "Low"));
			configuration.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("medium_value", "Medium"));
			configuration.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("high_value", "High"));
			configuration.DefaultValue = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("medium_value", "Medium");

			objectCreator.CreateConfiguration(configuration);

			configuration = TestContext.Api.Configurations.Read(configurationId) as Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration;
			Assert.IsNotNull(configuration);
			Assert.AreEqual(true, configuration.IsMandatory);
			Assert.AreEqual(3, configuration.Discretes.Count);
			Assert.AreEqual("Medium", configuration.DefaultValue.DisplayName);
			Assert.AreEqual("low_value", configuration.Discretes.First(x => x.DisplayName == "Low").Value);
			Assert.AreEqual("medium_value", configuration.Discretes.First(x => x.DisplayName == "Medium").Value);
			Assert.AreEqual("high_value", configuration.Discretes.First(x => x.DisplayName == "High").Value);

			var coreConfiguration = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
			Assert.IsNotNull(coreConfiguration);
			Assert.AreEqual(false, coreConfiguration.IsOptional);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Configuration, coreConfiguration.Categories);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Discrete, coreConfiguration.Type);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.String, coreConfiguration.InterpreteType.Type);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Other, coreConfiguration.InterpreteType.RawType);
			Assert.AreEqual(3, coreConfiguration.Discretes.Count);

			// Update
			configuration.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("critical_value", "Critical"));
			configuration.DefaultValue = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("critical_value", "Critical");

			TestContext.Api.Configurations.Update(configuration);

			configuration = TestContext.Api.Configurations.Read(configurationId) as Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration;
			Assert.IsNotNull(configuration);
			Assert.AreEqual(true, configuration.IsMandatory);
			Assert.AreEqual(4, configuration.Discretes.Count);
			Assert.AreEqual("Critical", configuration.DefaultValue.DisplayName);
			Assert.AreEqual("critical_value", configuration.Discretes.First(x => x.DisplayName == "Critical").Value);

			coreConfiguration = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
			Assert.IsNotNull(coreConfiguration);
			Assert.AreEqual(false, coreConfiguration.IsOptional);
			Assert.AreEqual(4, coreConfiguration.Discretes.Count);
		}

		[TestMethod]
		public void RemoveDiscreteValue()
		{
			var configurationId = Guid.NewGuid();

			var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration(configurationId)
			{
				Name = $"{configurationId}_Configuration",
			};

			configuration.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("low_value", "Low"));
			configuration.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("medium_value", "Medium"));
			configuration.AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("high_value", "High"));

			objectCreator.CreateConfiguration(configuration);

			configuration = (Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration)TestContext.Api.Configurations.Read(configurationId);
			Assert.AreEqual(3, configuration.Discretes.Count);

			configuration.RemoveDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("medium_value", "Medium"));
			TestContext.Api.Configurations.Update(configuration);

			configuration = (Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration)TestContext.Api.Configurations.Read(configurationId);
			Assert.AreEqual(2, configuration.Discretes.Count);
			Assert.IsFalse(configuration.Discretes.Contains(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("medium_value", "Medium")));
			Assert.IsTrue(configuration.Discretes.Contains(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("low_value", "Low")));
			Assert.IsTrue(configuration.Discretes.Contains(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("high_value", "High")));
		}

		[TestMethod]
		public void CreateWithNoDiscretesThrowsException()
		{
			var configurationId = Guid.NewGuid();

			var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration(configurationId)
			{
				Name = $"{configurationId}_Configuration",
			};

			try
			{
				objectCreator.CreateConfiguration(configuration);
			}
			catch (MediaOpsException ex)
			{
				StringAssert.Contains(ex.Message, "A discreet configuration should have at least one discreet option defined");

				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
				var configurationConfigurationError = ex.TraceData.ErrorData.OfType<ConfigurationError>().SingleOrDefault();
				Assert.IsNotNull(configurationConfigurationError);

				var configurationConfigurationInvalidDiscretesError = configurationConfigurationError as ConfigurationInvalidDiscretesError;
				Assert.IsNotNull(configurationConfigurationInvalidDiscretesError);
				Assert.AreEqual("A discreet configuration should have at least one discreet option defined", configurationConfigurationError.ErrorMessage);

				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void CreateWithNullNameThrowsException()
		{
			var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration()
			{
				Name = null,
			};

			try
			{
				objectCreator.CreateConfiguration(configuration);
			}
			catch (MediaOpsException ex)
			{
				var invalidNameError = ex.TraceData.ErrorData.OfType<ConfigurationInvalidNameError>().SingleOrDefault();
				Assert.IsNotNull(invalidNameError);
				Assert.AreEqual("Name cannot be empty.", invalidNameError.ErrorMessage);
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void CreateWithEmptyNameThrowsException()
		{
			var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration()
			{
				Name = string.Empty,
			};

			try
			{
				objectCreator.CreateConfiguration(configuration);
			}
			catch (MediaOpsException ex)
			{
				var invalidNameError = ex.TraceData.ErrorData.OfType<ConfigurationInvalidNameError>().SingleOrDefault();
				Assert.IsNotNull(invalidNameError);
				Assert.AreEqual("Name cannot be empty.", invalidNameError.ErrorMessage);
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}
	}
}
