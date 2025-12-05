namespace RT_MediaOps.Plan.RST.Configurations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class BasicDiscreteTextConfigurationTests : IDisposable
    {
        private readonly ResourceStudioObjectCreator objectCreator;

        public BasicDiscreteTextConfigurationTests()
        {
            objectCreator = new ResourceStudioObjectCreator(TestContext.Api);
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

            configuration.AddDiscrete("Low", "low_value");
            configuration.AddDiscrete("Medium", "medium_value");
            configuration.AddDiscrete("High", "high_value");

            var returnedId = objectCreator.CreateConfiguration(configuration);
            Assert.AreEqual(configurationId, returnedId);

            var returnedConfiguration = TestContext.Api.Configurations.Read(configurationId);
            Assert.IsNotNull(returnedConfiguration);
            Assert.AreEqual(name, returnedConfiguration.Name);
            Assert.AreEqual(false, returnedConfiguration.IsMandatory);
            Assert.IsInstanceOfType(returnedConfiguration, typeof(DiscreteTextConfiguration));

            var discreteTextConfig = (DiscreteTextConfiguration)returnedConfiguration;
            Assert.AreEqual(3, discreteTextConfig.Discretes.Count);
            Assert.IsTrue(discreteTextConfig.Discretes.ContainsKey("Low"));
            Assert.AreEqual("low_value", discreteTextConfig.Discretes["Low"]);

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
            discreteTextConfig.AddDiscrete("Critical", "critical_value");
            TestContext.Api.Configurations.Update(discreteTextConfig);

            returnedConfiguration = TestContext.Api.Configurations.Read(configurationId);
            Assert.IsNotNull(returnedConfiguration);
            Assert.AreEqual(updatedName, returnedConfiguration.Name);

            discreteTextConfig = (DiscreteTextConfiguration)returnedConfiguration;
            Assert.AreEqual(4, discreteTextConfig.Discretes.Count);
            Assert.IsTrue(discreteTextConfig.Discretes.ContainsKey("Critical"));

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
            configuration1.AddDiscrete("Value1", "value_1");

            var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration2",
            };
            configuration2.AddDiscrete("Value2", "value_2");

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

            var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration1",
            };
            configuration1.AddDiscrete("Value1", "value_1");

            var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration2",
            };
            configuration2.AddDiscrete("Value2", "value_2");

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

            var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration()
            {
                Name = $"{configurationId}_Configuration",
            };
            configuration1.AddDiscrete("Value1", "value_1");

            var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration()
            {
                Name = $"{configurationId}_Configuration",
            };
            configuration2.AddDiscrete("Value2", "value_2");

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

            var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration()
            {
                Name = $"{configurationId}_Configuration",
            };
            configuration1.AddDiscrete("Value1", "value_1");

            var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration()
            {
                Name = $"{configurationId}_Configuration",
            };
            configuration2.AddDiscrete("Value2", "value_2");

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

            var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration()
            {
                Name = $"{configurationId}_Configuration_1",
            };
            configuration1.AddDiscrete("Value1", "value_1");

            var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration()
            {
                Name = $"{configurationId}_Configuration_2",
            };
            configuration2.AddDiscrete("Value2", "value_2");

            var id1 = objectCreator.CreateConfiguration(configuration1);
            var id2 = objectCreator.CreateConfiguration(configuration2);

            var toUpdate = TestContext.Api.Configurations.Read(id2);
            toUpdate.Name = configuration1.Name;

            try
            {
                TestContext.Api.Configurations.Update(toUpdate);
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
        public void DiscreteValuesValidation()
        {
            // Create
            var configurationId = Guid.NewGuid();

            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration",
                IsMandatory = true,
            };

            configuration.AddDiscrete("Low", "low_value");
            configuration.AddDiscrete("Medium", "medium_value");
            configuration.AddDiscrete("High", "high_value");
            configuration.DefaultValue = "Medium";

            objectCreator.CreateConfiguration(configuration);

            configuration = TestContext.Api.Configurations.Read(configurationId) as DiscreteTextConfiguration;
            Assert.IsNotNull(configuration);
            Assert.AreEqual(true, configuration.IsMandatory);
            Assert.AreEqual(3, configuration.Discretes.Count);
            Assert.AreEqual("Medium", configuration.DefaultValue);
            Assert.AreEqual("low_value", configuration.Discretes["Low"]);
            Assert.AreEqual("medium_value", configuration.Discretes["Medium"]);
            Assert.AreEqual("high_value", configuration.Discretes["High"]);

            var coreConfiguration = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
            Assert.IsNotNull(coreConfiguration);
            Assert.AreEqual(false, coreConfiguration.IsOptional);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Configuration, coreConfiguration.Categories);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Discrete, coreConfiguration.Type);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.String, coreConfiguration.InterpreteType.Type);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Other, coreConfiguration.InterpreteType.RawType);
            Assert.AreEqual(3, coreConfiguration.Discretes.Count);

            // Update
            configuration.AddDiscrete("Critical", "critical_value");
            configuration.DefaultValue = "Critical";

            TestContext.Api.Configurations.Update(configuration);

            configuration = TestContext.Api.Configurations.Read(configurationId) as DiscreteTextConfiguration;
            Assert.IsNotNull(configuration);
            Assert.AreEqual(true, configuration.IsMandatory);
            Assert.AreEqual(4, configuration.Discretes.Count);
            Assert.AreEqual("Critical", configuration.DefaultValue);
            Assert.AreEqual("critical_value", configuration.Discretes["Critical"]);

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

            configuration.AddDiscrete("Low", "low_value");
            configuration.AddDiscrete("Medium", "medium_value");
            configuration.AddDiscrete("High", "high_value");

            objectCreator.CreateConfiguration(configuration);

            configuration = (DiscreteTextConfiguration)TestContext.Api.Configurations.Read(configurationId);
            Assert.AreEqual(3, configuration.Discretes.Count);

            configuration.RemoveDiscrete("Medium");
            TestContext.Api.Configurations.Update(configuration);

            configuration = (DiscreteTextConfiguration)TestContext.Api.Configurations.Read(configurationId);
            Assert.AreEqual(2, configuration.Discretes.Count);
            Assert.IsFalse(configuration.Discretes.ContainsKey("Medium"));
            Assert.IsTrue(configuration.Discretes.ContainsKey("Low"));
            Assert.IsTrue(configuration.Discretes.ContainsKey("High"));
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
                var configurationConfigurationError = ex.TraceData.ErrorData.OfType<ConfigurationConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(configurationConfigurationError);

                Assert.AreEqual(ConfigurationConfigurationError.Reason.InvalidDiscretes, configurationConfigurationError.ErrorReason);
                Assert.AreEqual("A discreet configuration should have at least one discreet option defined", configurationConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }
    }
}
