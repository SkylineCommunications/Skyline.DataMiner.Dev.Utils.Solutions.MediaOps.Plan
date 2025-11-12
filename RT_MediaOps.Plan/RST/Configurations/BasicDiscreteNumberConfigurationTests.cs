namespace RT_MediaOps.Plan.RST.Configurations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using RT_MediaOps.Plan.RegressionTests;
    using Skyline.DataMiner.MediaOps.Plan.API;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public class BasicDiscreteNumberConfigurationTests : IDisposable
    {
        private readonly IntegrationTestContext testContext;
        private readonly ResourceStudioObjectCreator objectCreator;

        public BasicDiscreteNumberConfigurationTests()
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
        public void BasicCrudActions()
        {
            // Create
            var configurationId = Guid.NewGuid();
            var name = $"{configurationId}_Configuration";

            var configuration = new Skyline.DataMiner.MediaOps.Plan.API.DiscreteNumberConfiguration(configurationId)
            {
                Name = name,
            };

            configuration.AddDiscrete("Low", 10);
            configuration.AddDiscrete("Medium", 50);
            configuration.AddDiscrete("High", 100);

            var returnedId = objectCreator.CreateConfiguration(configuration);
            Assert.AreEqual(configurationId, returnedId);

            var returnedConfiguration = testContext.Api.Configurations.Read(configurationId);
            Assert.IsNotNull(returnedConfiguration);
            Assert.AreEqual(name, returnedConfiguration.Name);
            Assert.AreEqual(false, returnedConfiguration.IsMandatory);
            Assert.IsInstanceOfType(returnedConfiguration, typeof(DiscreteNumberConfiguration));

            var discreteNumberConfig = (DiscreteNumberConfiguration)returnedConfiguration;
            Assert.AreEqual(3, discreteNumberConfig.Discretes.Count);
            Assert.IsTrue(discreteNumberConfig.Discretes.ContainsKey("Low"));
            Assert.AreEqual(10, discreteNumberConfig.Discretes["Low"]);

            var coreConfiguration = testContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
            Assert.IsNotNull(coreConfiguration);
            Assert.AreEqual(name, coreConfiguration.Name);
            Assert.AreEqual(true, coreConfiguration.IsOptional);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Configuration, coreConfiguration.Categories);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Discrete, coreConfiguration.Type);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Double, coreConfiguration.InterpreteType.Type);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.NumericText, coreConfiguration.InterpreteType.RawType);
            Assert.AreEqual(3, coreConfiguration.Discretes.Count);

            // Update
            var updatedName = name + "_Updated";
            discreteNumberConfig.Name = updatedName;
            discreteNumberConfig.AddDiscrete("Critical", 200);
            testContext.Api.Configurations.Update(discreteNumberConfig);

            returnedConfiguration = testContext.Api.Configurations.Read(configurationId);
            Assert.IsNotNull(returnedConfiguration);
            Assert.AreEqual(updatedName, returnedConfiguration.Name);

            discreteNumberConfig = (DiscreteNumberConfiguration)returnedConfiguration;
            Assert.AreEqual(4, discreteNumberConfig.Discretes.Count);
            Assert.IsTrue(discreteNumberConfig.Discretes.ContainsKey("Critical"));

            coreConfiguration = testContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
            Assert.IsNotNull(coreConfiguration);
            Assert.AreEqual(updatedName, coreConfiguration.Name);
            Assert.AreEqual(4, coreConfiguration.Discretes.Count);

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

            var configuration1 = new Skyline.DataMiner.MediaOps.Plan.API.DiscreteNumberConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration1",
            };
            configuration1.AddDiscrete("Value1", 1);

            var configuration2 = new Skyline.DataMiner.MediaOps.Plan.API.DiscreteNumberConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration2",
            };
            configuration2.AddDiscrete("Value2", 2);

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

            var configuration1 = new Skyline.DataMiner.MediaOps.Plan.API.DiscreteNumberConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration1",
            };
            configuration1.AddDiscrete("Value1", 1);

            var configuration2 = new Skyline.DataMiner.MediaOps.Plan.API.DiscreteNumberConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration2",
            };
            configuration2.AddDiscrete("Value2", 2);

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

            var configuration1 = new Skyline.DataMiner.MediaOps.Plan.API.DiscreteNumberConfiguration()
            {
                Name = $"{configurationId}_Configuration",
            };
            configuration1.AddDiscrete("Value1", 1);

            var configuration2 = new Skyline.DataMiner.MediaOps.Plan.API.DiscreteNumberConfiguration()
            {
                Name = $"{configurationId}_Configuration",
            };
            configuration2.AddDiscrete("Value2", 2);

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

            var configuration1 = new Skyline.DataMiner.MediaOps.Plan.API.DiscreteNumberConfiguration()
            {
                Name = $"{configurationId}_Configuration",
            };
            configuration1.AddDiscrete("Value1", 1);

            var configuration2 = new Skyline.DataMiner.MediaOps.Plan.API.DiscreteNumberConfiguration()
            {
                Name = $"{configurationId}_Configuration",
            };
            configuration2.AddDiscrete("Value2", 2);

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

            var configuration1 = new Skyline.DataMiner.MediaOps.Plan.API.DiscreteNumberConfiguration()
            {
                Name = $"{configurationId}_Configuration_1",
            };
            configuration1.AddDiscrete("Value1", 1);

            var configuration2 = new Skyline.DataMiner.MediaOps.Plan.API.DiscreteNumberConfiguration()
            {
                Name = $"{configurationId}_Configuration_2",
            };
            configuration2.AddDiscrete("Value2", 2);

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
        public void DiscreteValuesValidation()
        {
            // Create
            var configurationId = Guid.NewGuid();

            var configuration = new Skyline.DataMiner.MediaOps.Plan.API.DiscreteNumberConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration",
                IsMandatory = true,
            };

            configuration.AddDiscrete("Low", 10);
            configuration.AddDiscrete("Medium", 50);
            configuration.AddDiscrete("High", 100);
            configuration.DefaultValue = "Medium";

            objectCreator.CreateConfiguration(configuration);

            configuration = testContext.Api.Configurations.Read(configurationId) as DiscreteNumberConfiguration;
            Assert.IsNotNull(configuration);
            Assert.AreEqual(true, configuration.IsMandatory);
            Assert.AreEqual(3, configuration.Discretes.Count);
            Assert.AreEqual("Medium", configuration.DefaultValue);
            Assert.AreEqual(10, configuration.Discretes["Low"]);
            Assert.AreEqual(50, configuration.Discretes["Medium"]);
            Assert.AreEqual(100, configuration.Discretes["High"]);

            var coreConfiguration = testContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
            Assert.IsNotNull(coreConfiguration);
            Assert.AreEqual(false, coreConfiguration.IsOptional);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Configuration, coreConfiguration.Categories);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Discrete, coreConfiguration.Type);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Double, coreConfiguration.InterpreteType.Type);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.NumericText, coreConfiguration.InterpreteType.RawType);
            Assert.AreEqual(3, coreConfiguration.Discretes.Count);

            // Update
            configuration.AddDiscrete("Critical", 200);
            configuration.DefaultValue = "Critical";

            testContext.Api.Configurations.Update(configuration);

            configuration = testContext.Api.Configurations.Read(configurationId) as DiscreteNumberConfiguration;
            Assert.IsNotNull(configuration);
            Assert.AreEqual(true, configuration.IsMandatory);
            Assert.AreEqual(4, configuration.Discretes.Count);
            Assert.AreEqual("Critical", configuration.DefaultValue);
            Assert.AreEqual(200, configuration.Discretes["Critical"]);

            coreConfiguration = testContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
            Assert.IsNotNull(coreConfiguration);
            Assert.AreEqual(false, coreConfiguration.IsOptional);
            Assert.AreEqual(4, coreConfiguration.Discretes.Count);
        }

        [TestMethod]
        public void RemoveDiscreteValue()
        {
            var configurationId = Guid.NewGuid();

            var configuration = new Skyline.DataMiner.MediaOps.Plan.API.DiscreteNumberConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration",
            };

            configuration.AddDiscrete("Low", 10);
            configuration.AddDiscrete("Medium", 50);
            configuration.AddDiscrete("High", 100);

            objectCreator.CreateConfiguration(configuration);

            configuration = testContext.Api.Configurations.Read(configurationId) as DiscreteNumberConfiguration;
            Assert.AreEqual(3, configuration.Discretes.Count);

            configuration.RemoveDiscrete("Medium");
            testContext.Api.Configurations.Update(configuration);

            configuration = testContext.Api.Configurations.Read(configurationId) as DiscreteNumberConfiguration;
            Assert.AreEqual(2, configuration.Discretes.Count);
            Assert.IsFalse(configuration.Discretes.ContainsKey("Medium"));
            Assert.IsTrue(configuration.Discretes.ContainsKey("Low"));
            Assert.IsTrue(configuration.Discretes.ContainsKey("High"));
        }

        [TestMethod]
        public void CreateWithNoDiscretesThrowsException()
        {
            var configurationId = Guid.NewGuid();

            var configuration = new Skyline.DataMiner.MediaOps.Plan.API.DiscreteNumberConfiguration(configurationId)
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
