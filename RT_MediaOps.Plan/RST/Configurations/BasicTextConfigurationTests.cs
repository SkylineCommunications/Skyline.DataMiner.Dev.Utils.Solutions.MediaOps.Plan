namespace RT_MediaOps.Plan.RST.Configurations
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using RT_MediaOps.Plan.RegressionTests;
    using Skyline.DataMiner.MediaOps.Plan.API;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public class BasicTextConfigurationTests : IDisposable
    {
        private readonly IntegrationTestContext testContext;
        private readonly ResourceStudioObjectCreator objectCreator;

        public BasicTextConfigurationTests()
        {
            testContext = new IntegrationTestContext();
            objectCreator = new ResourceStudioObjectCreator(testContext.Api);
        }

        public void Dispose()
        {
            objectCreator.Dispose();
            testContext.Dispose();
        }

        [TestMethod]
        public void TestDefaultValues()
        {
            var configuration = new TextConfiguration();
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

            var configuration = new TextConfiguration(configurationId)
            {
                Name = name,
            };

            var returnedId = objectCreator.CreateConfiguration(configuration);
            Assert.AreEqual(configurationId, returnedId);

            var returnedConfiguration = testContext.Api.Configurations.Read(configurationId);
            Assert.IsNotNull(returnedConfiguration);
            Assert.AreEqual(name, returnedConfiguration.Name);
            Assert.AreEqual(false, returnedConfiguration.IsMandatory);

            var coreConfiguration = testContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
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
            testContext.Api.Configurations.Update(returnedConfiguration);
            returnedConfiguration = testContext.Api.Configurations.Read(configurationId);
            Assert.IsNotNull(returnedConfiguration);
            Assert.AreEqual(updatedName, returnedConfiguration.Name);

            coreConfiguration = testContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
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
            testContext.Api.Configurations.Delete(returnedConfiguration);
            returnedConfiguration = testContext.Api.Configurations.Read(configurationId);
            Assert.IsNull(returnedConfiguration);

            coreConfiguration = testContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
            Assert.IsNull(coreConfiguration);
        }

        [TestMethod]
        public void CreateWithExistingIdThrowsException()
        {
            var configurationId = Guid.NewGuid();

            var configuration1 = new Skyline.DataMiner.MediaOps.Plan.API.TextConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration1",
            };

            var configuration2 = new Skyline.DataMiner.MediaOps.Plan.API.TextConfiguration(configurationId)
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
                var configurationConfigurationError = ex.TraceData.ErrorData.OfType<ConfigurationConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(configurationConfigurationError);

                Assert.AreEqual(ConfigurationConfigurationError.Reason.IdInUse, configurationConfigurationError.ErrorReason);
                Assert.AreEqual("ID is already in use.", configurationConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CreateWithSameIdInBulkThrowsException()
        {
            var configurationId = Guid.NewGuid();

            var configuration1 = new Skyline.DataMiner.MediaOps.Plan.API.TextConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration1",
            };

            var configuration2 = new Skyline.DataMiner.MediaOps.Plan.API.TextConfiguration(configurationId)
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
                var configurationConfigurationErrors = traceData.ErrorData.OfType<ConfigurationConfigurationError>().ToList();
                Assert.AreEqual(2, configurationConfigurationErrors.Count());

                var errorMessages = new List<string>
                {
                   $"Configuration '{configuration1.Name}' has a duplicate ID.",
                   $"Configuration '{configuration2.Name}' has a duplicate ID."
                };

                foreach (var error in configurationConfigurationErrors)
                {
                    Assert.AreEqual(ConfigurationConfigurationError.Reason.DuplicateId, error.ErrorReason);
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

            var configuration1 = new Skyline.DataMiner.MediaOps.Plan.API.TextConfiguration()
            {
                Name = $"{configurationId}_Configuration",
            };

            var configuration2 = new Skyline.DataMiner.MediaOps.Plan.API.TextConfiguration()
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
                var configurationConfigurationError = ex.TraceData.ErrorData.OfType<ConfigurationConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(configurationConfigurationError);

                Assert.AreEqual(ConfigurationConfigurationError.Reason.NameExists, configurationConfigurationError.ErrorReason);
                Assert.AreEqual("Name is already in use.", configurationConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CreateWithSameNameInBulkThrowsException()
        {
            var configurationId = Guid.NewGuid();

            var configuration1 = new Skyline.DataMiner.MediaOps.Plan.API.TextConfiguration()
            {
                Name = $"{configurationId}_Configuration",
            };

            var configuration2 = new Skyline.DataMiner.MediaOps.Plan.API.TextConfiguration()
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
                    var configurationConfigurationError = traceData.ErrorData.OfType<ConfigurationConfigurationError>().SingleOrDefault();
                    Assert.IsNotNull(configurationConfigurationError);

                    Assert.AreEqual(ConfigurationConfigurationError.Reason.DuplicateName, configurationConfigurationError.ErrorReason);
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

            var configuration1 = new Skyline.DataMiner.MediaOps.Plan.API.TextConfiguration()
            {
                Name = $"{configurationId}_Configuration_1",
            };

            var configuration2 = new Skyline.DataMiner.MediaOps.Plan.API.TextConfiguration()
            {
                Name = $"{configurationId}_Configuration_2",
            };

            var id1 = objectCreator.CreateConfiguration(configuration1);
            var id2 = objectCreator.CreateConfiguration(configuration2);

            var toUpdate = testContext.Api.Configurations.Read(id2);
            toUpdate.Name = configuration1.Name;

            try
            {
                testContext.Api.Configurations.Update(toUpdate);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "Name is already in use.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var configurationConfigurationError = ex.TraceData.ErrorData.OfType<ConfigurationConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(configurationConfigurationError);

                Assert.AreEqual(ConfigurationConfigurationError.Reason.NameExists, configurationConfigurationError.ErrorReason);
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

            var configuration = new Skyline.DataMiner.MediaOps.Plan.API.TextConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration",
                IsMandatory = true,
                DefaultValue = "DefaultText",
            };

            objectCreator.CreateConfiguration(configuration);

            configuration = testContext.Api.Configurations.Read(configurationId) as TextConfiguration;
            Assert.IsNotNull(configuration);
            Assert.AreEqual(true, configuration.IsMandatory);
            Assert.AreEqual("DefaultText", configuration.DefaultValue);

            var coreConfiguration = testContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
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

            testContext.Api.Configurations.Update(configuration);

            configuration = testContext.Api.Configurations.Read(configurationId) as TextConfiguration;
            Assert.IsNotNull(configuration);
            Assert.AreEqual(true, configuration.IsMandatory);
            Assert.AreEqual("UpdatedDefaultText", configuration.DefaultValue);

            coreConfiguration = testContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
            Assert.IsNotNull(coreConfiguration);
            Assert.AreEqual(false, coreConfiguration.IsOptional);
            Assert.IsNull(coreConfiguration.Remarks);
            Assert.IsNotNull(coreConfiguration.DefaultValue);
            Assert.AreEqual("UpdatedDefaultText", coreConfiguration.DefaultValue?.StringValue);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Configuration, coreConfiguration.Categories);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Text, coreConfiguration.Type);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Undefined, coreConfiguration.InterpreteType.RawType);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Undefined, coreConfiguration.InterpreteType.Type);

            // Update
            configuration.DefaultValue = String.Concat(Enumerable.Repeat("foobar", 50));

            try
            {
                testContext.Api.Configurations.Update(configuration);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "The default value of the text configuration exceeds 150 characters");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var configurationConfigurationError = ex.TraceData.ErrorData.OfType<ConfigurationConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(configurationConfigurationError);

                Assert.AreEqual(ConfigurationConfigurationError.Reason.InvalidDefaultValue, configurationConfigurationError.ErrorReason);
                Assert.AreEqual("The default value of the text configuration exceeds 150 characters", configurationConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }
    }
}
