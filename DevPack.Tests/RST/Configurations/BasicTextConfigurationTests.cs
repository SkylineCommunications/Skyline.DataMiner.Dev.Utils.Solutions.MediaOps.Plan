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
	public sealed class BasicTextConfigurationTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public BasicTextConfigurationTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void TestDefaultValues()
		{
			var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration();
			Assert.IsNull(configuration.DefaultValue);
			Assert.IsNull(configuration.Name);
			Assert.AreNotEqual(Guid.Empty, configuration.Id);
		}

		[TestMethod]
		public void BasicCrudActions()
		{
			// Create
			var configurationId = Guid.NewGuid();
			var name = $"{configurationId}_Configuration";

			var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration(configurationId)
			{
				Name = name,
			};

			objectCreator.CreateConfiguration(configuration);

			var returnedConfiguration = TestContext.Api.Configurations.Read(configurationId);
			Assert.IsNotNull(returnedConfiguration);
			Assert.AreEqual(name, returnedConfiguration.Name);
			Assert.AreEqual(false, returnedConfiguration.IsMandatory);

			var coreConfiguration = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
			Assert.IsNotNull(coreConfiguration);
			Assert.AreEqual(name, coreConfiguration.Name);
			Assert.AreEqual(true, coreConfiguration.IsOptional);

			Assert.IsNull(coreConfiguration.Remarks);
			Assert.IsNull(coreConfiguration.DefaultValue);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Configuration, coreConfiguration.Categories);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Text, coreConfiguration.Type);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Undefined, coreConfiguration.InterpreteType.RawType);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Undefined, coreConfiguration.InterpreteType.Type);

			Assert.IsNull(coreConfiguration.Units);
			Assert.AreEqual(double.NaN, coreConfiguration.RangeMin);
			Assert.AreEqual(double.NaN, coreConfiguration.RangeMax);
			Assert.AreEqual(double.NaN, coreConfiguration.Stepsize);
			Assert.AreEqual(int.MaxValue, coreConfiguration.Decimals);

			// Update
			var updatedName = name + "_Updated";
			returnedConfiguration.Name = updatedName;
			TestContext.Api.Configurations.Update(returnedConfiguration);
			returnedConfiguration = TestContext.Api.Configurations.Read(configurationId);
			Assert.IsNotNull(returnedConfiguration);
			Assert.AreEqual(updatedName, returnedConfiguration.Name);

			coreConfiguration = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
			Assert.IsNotNull(coreConfiguration);
			Assert.AreEqual(updatedName, coreConfiguration.Name);
			Assert.AreEqual(true, coreConfiguration.IsOptional);

			Assert.IsNull(coreConfiguration.Remarks);
			Assert.IsNull(coreConfiguration.DefaultValue);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Configuration, coreConfiguration.Categories);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Text, coreConfiguration.Type);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Undefined, coreConfiguration.InterpreteType.RawType);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Undefined, coreConfiguration.InterpreteType.Type);

			Assert.IsNull(coreConfiguration.Units);
			Assert.AreEqual(double.NaN, coreConfiguration.RangeMin);
			Assert.AreEqual(double.NaN, coreConfiguration.RangeMax);
			Assert.AreEqual(double.NaN, coreConfiguration.Stepsize);
			Assert.AreEqual(int.MaxValue, coreConfiguration.Decimals);

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

			var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration(configurationId)
			{
				Name = $"{configurationId}_Configuration1",
			};

			var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration(configurationId)
			{
				Name = $"{configurationId}_Configuration2",
			};

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

			var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration(configurationId)
			{
				Name = $"{configurationId}_Configuration1",
			};

			var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration(configurationId)
			{
				Name = $"{configurationId}_Configuration2",
			};

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

			var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration()
			{
				Name = $"{configurationId}_Configuration",
			};

			var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration()
			{
				Name = $"{configurationId}_Configuration",
			};

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

			var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration()
			{
				Name = $"{configurationId}_Configuration",
			};

			var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration()
			{
				Name = $"{configurationId}_Configuration",
			};

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

			var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration()
			{
				Name = $"{configurationId}_Configuration_1",
			};

			var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration()
			{
				Name = $"{configurationId}_Configuration_2",
			};

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
		public void DefaultValueValidation()
		{
			// Create
			var configurationId = Guid.NewGuid();

			var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration(configurationId)
			{
				Name = $"{configurationId}_Configuration",
				IsMandatory = true,
				DefaultValue = "DefaultText",
			};

			objectCreator.CreateConfiguration(configuration);

			configuration = TestContext.Api.Configurations.Read(configurationId) as Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration;
			Assert.IsNotNull(configuration);
			Assert.AreEqual(true, configuration.IsMandatory);
			Assert.AreEqual("DefaultText", configuration.DefaultValue);

			var coreConfiguration = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
			Assert.IsNotNull(coreConfiguration);
			Assert.AreEqual(false, coreConfiguration.IsOptional);
			Assert.IsNull(coreConfiguration.Remarks);
			Assert.IsNotNull(coreConfiguration.DefaultValue);
			Assert.AreEqual("DefaultText", coreConfiguration.DefaultValue.StringValue);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Configuration, coreConfiguration.Categories);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Text, coreConfiguration.Type);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Undefined, coreConfiguration.InterpreteType.RawType);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Undefined, coreConfiguration.InterpreteType.Type);

			// Update
			configuration.DefaultValue = "UpdatedDefaultText";

			TestContext.Api.Configurations.Update(configuration);

			configuration = TestContext.Api.Configurations.Read(configurationId) as Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration;
			Assert.IsNotNull(configuration);
			Assert.AreEqual(true, configuration.IsMandatory);
			Assert.AreEqual("UpdatedDefaultText", configuration.DefaultValue);

			coreConfiguration = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
			Assert.IsNotNull(coreConfiguration);
			Assert.AreEqual(false, coreConfiguration.IsOptional);
			Assert.IsNull(coreConfiguration.Remarks);
			Assert.IsNotNull(coreConfiguration.DefaultValue);
			Assert.AreEqual("UpdatedDefaultText", coreConfiguration.DefaultValue.StringValue);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Configuration, coreConfiguration.Categories);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Text, coreConfiguration.Type);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Undefined, coreConfiguration.InterpreteType.RawType);
			Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Undefined, coreConfiguration.InterpreteType.Type);

			// Update
			configuration.DefaultValue = String.Concat(Enumerable.Repeat("foobar", 50));

			try
			{
				TestContext.Api.Configurations.Update(configuration);
			}
			catch (MediaOpsException ex)
			{
				StringAssert.Contains(ex.Message, "The default value of the text configuration exceeds 150 characters");

				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
				var configurationConfigurationError = ex.TraceData.ErrorData.OfType<ConfigurationError>().SingleOrDefault();
				Assert.IsNotNull(configurationConfigurationError);

				var configurationConfigurationInvalidDefaultValueError = configurationConfigurationError as ConfigurationInvalidDefaultValueError;
				Assert.IsNotNull(configurationConfigurationInvalidDefaultValueError);
				Assert.AreEqual("The default value of the text configuration exceeds 150 characters", configurationConfigurationError.ErrorMessage);

				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void CreateWithNullNameThrowsException()
		{
			var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration()
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
			var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration()
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
