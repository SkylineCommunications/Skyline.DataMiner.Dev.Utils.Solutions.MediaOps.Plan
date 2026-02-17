namespace RT_MediaOps.Plan.RST.Configurations
{
    using System;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class BasicNumberConfigurationTests : IDisposable
    {
        private readonly TestObjectCreator objectCreator;

        public BasicNumberConfigurationTests()
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

            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration(configurationId)
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
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Number, coreConfiguration.Type);
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
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Number, coreConfiguration.Type);
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

            var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration1",
            };

            var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration(configurationId)
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

            var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration1",
            };

            var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration(configurationId)
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
                   $"Configuration '{configuration2.Name}' has a duplicate ID."
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

            var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration()
            {
                Name = $"{configurationId}_Configuration",
            };

            var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration()
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

            var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration()
            {
                Name = $"{configurationId}_Configuration",
            };

            var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration()
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

            var configuration1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration()
            {
                Name = $"{configurationId}_Configuration_1",
            };

            var configuration2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration()
            {
                Name = $"{configurationId}_Configuration_2",
            };

            objectCreator.CreateConfiguration(configuration1);
            objectCreator.CreateConfiguration(configuration2);

            var toUpdate = TestContext.Api.Configurations.Read(configuration2.ID);
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
        public void RangeValidation()
        {
            // Create
            var configurationId = Guid.NewGuid();

            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration",
                IsMandatory = true,
                Units = "MHz",
                RangeMin = 10,
                RangeMax = 100,
                StepSize = 5,
                Decimals = 2
            };

            objectCreator.CreateConfiguration(configuration);

            configuration = TestContext.Api.Configurations.Read(configurationId) as Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration;
            Assert.IsNotNull(configuration);
            Assert.AreEqual(true, configuration.IsMandatory);

            Assert.AreEqual("MHz", configuration.Units);
            Assert.AreEqual(10, configuration.RangeMin);
            Assert.AreEqual(100, configuration.RangeMax);
            Assert.AreEqual(5, configuration.StepSize);
            Assert.AreEqual(2, configuration.Decimals);

            var coreConfiguration = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
            Assert.IsNotNull(coreConfiguration);
            Assert.AreEqual(false, coreConfiguration.IsOptional);

            Assert.IsNull(coreConfiguration.Remarks);
            Assert.IsNull(coreConfiguration.DefaultValue);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Configuration, coreConfiguration.Categories);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Number, coreConfiguration.Type);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Undefined, coreConfiguration.InterpreteType.RawType);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Undefined, coreConfiguration.InterpreteType.Type);

            Assert.AreEqual("MHz", coreConfiguration.Units);
            Assert.AreEqual(10, coreConfiguration.RangeMin);
            Assert.AreEqual(100, coreConfiguration.RangeMax);
            Assert.AreEqual(5, coreConfiguration.Stepsize);
            Assert.AreEqual(2, coreConfiguration.Decimals);

            // Update
            configuration.Units = "kHz";
            configuration.RangeMin = 20;
            configuration.RangeMax = 200;
            configuration.StepSize = 10;
            configuration.Decimals = 1;

            TestContext.Api.Configurations.Update(configuration);

            configuration = TestContext.Api.Configurations.Read(configurationId) as Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration;
            Assert.IsNotNull(configuration);
            Assert.AreEqual(true, configuration.IsMandatory);

            Assert.AreEqual("kHz", configuration.Units);
            Assert.AreEqual(20, configuration.RangeMin);
            Assert.AreEqual(200, configuration.RangeMax);
            Assert.AreEqual(10, configuration.StepSize);
            Assert.AreEqual(1, configuration.Decimals);

            coreConfiguration = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(configurationId)).SingleOrDefault();
            Assert.IsNotNull(coreConfiguration);
            Assert.AreEqual(false, coreConfiguration.IsOptional);

            Assert.IsNull(coreConfiguration.Remarks);
            Assert.IsNull(coreConfiguration.DefaultValue);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Configuration, coreConfiguration.Categories);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Number, coreConfiguration.Type);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Undefined, coreConfiguration.InterpreteType.RawType);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Undefined, coreConfiguration.InterpreteType.Type);

            Assert.AreEqual("kHz", coreConfiguration.Units);
            Assert.AreEqual(20, coreConfiguration.RangeMin);
            Assert.AreEqual(200, coreConfiguration.RangeMax);
            Assert.AreEqual(10, coreConfiguration.Stepsize);
            Assert.AreEqual(1, coreConfiguration.Decimals);
        }

        [TestMethod]
        public void RangeValidationThrowsException()
        {
            var configurationId = Guid.NewGuid();

            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration",
                IsMandatory = true,
                Units = "MHz",
                RangeMin = 10.123m,
                RangeMax = 1.123m,
                StepSize = 5.123m,
                Decimals = 2
            };

            try
            {
                objectCreator.CreateConfiguration(configuration);
            }
            catch (MediaOpsException ex)
            {
                Assert.AreEqual(4, ex.TraceData.ErrorData.Count);
                var configurationConfigurationErrors = ex.TraceData.ErrorData.OfType<ConfigurationError>();
                Assert.AreEqual(4, configurationConfigurationErrors.Count());

                var configurationConfigurationInvalidRangeError = configurationConfigurationErrors.OfType<ConfigurationInvalidRangeError>().SingleOrDefault();
                Assert.IsNotNull(configurationConfigurationInvalidRangeError);
                Assert.AreEqual("RangeMax must be greater than RangeMin.", configurationConfigurationInvalidRangeError.ErrorMessage);
                Assert.AreEqual(10.123m, configurationConfigurationInvalidRangeError.RangeMin);
                Assert.AreEqual(1.123m, configurationConfigurationInvalidRangeError.RangeMax);

                var configurationConfigurationInvalidRangeMinError = configurationConfigurationErrors.OfType<ConfigurationInvalidRangeMinError>().SingleOrDefault();
                Assert.IsNotNull(configurationConfigurationInvalidRangeMinError);
                Assert.AreEqual("RangeMin has more decimal places than allowed by Decimals (2).", configurationConfigurationInvalidRangeMinError.ErrorMessage);
                Assert.AreEqual(10.123m, configurationConfigurationInvalidRangeMinError.RangeMin);

                var configurationConfigurationInvalidRangeMaxError = configurationConfigurationErrors.OfType<ConfigurationInvalidRangeMaxError>().SingleOrDefault();
                Assert.IsNotNull(configurationConfigurationInvalidRangeMaxError);
                Assert.AreEqual("RangeMax has more decimal places than allowed by Decimals (2).", configurationConfigurationInvalidRangeMaxError.ErrorMessage);
                Assert.AreEqual(1.123m, configurationConfigurationInvalidRangeMaxError.RangeMax);

                var configurationConfigurationInvalidStepSizeError = configurationConfigurationErrors.OfType<ConfigurationInvalidStepSizeError>().SingleOrDefault();
                Assert.IsNotNull(configurationConfigurationInvalidStepSizeError);
                Assert.AreEqual("StepSize has more decimal places than allowed by Decimals (2).", configurationConfigurationInvalidStepSizeError.ErrorMessage);
                Assert.AreEqual(5.123m, configurationConfigurationInvalidStepSizeError.StepSize);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void DecimalsValidationThrowsException()
        {
            var configurationId = Guid.NewGuid();

            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration",
                IsMandatory = true,
                Units = "MHz",
                RangeMin = 10,
                RangeMax = 100,
                StepSize = 5,
                Decimals = -3
            };

            try
            {
                objectCreator.CreateConfiguration(configuration);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "Decimals must be between 0 and 15.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var configurationConfigurationError = ex.TraceData.ErrorData.OfType<ConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(configurationConfigurationError);

                var configurationConfigurationInvalidDecimalsError = configurationConfigurationError as ConfigurationInvalidDecimalsError;
                Assert.IsNotNull(configurationConfigurationInvalidDecimalsError);
                Assert.AreEqual("Decimals must be between 0 and 15.", configurationConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void StepSizeValidationThrowsException()
        {
            var configurationId = Guid.NewGuid();

            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration",
                IsMandatory = true,
                Units = "MHz",
                RangeMin = 10,
                RangeMax = 100,
                StepSize = -5,
                Decimals = 3
            };

            try
            {
                objectCreator.CreateConfiguration(configuration);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "StepSize must be greater than 0.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var configurationConfigurationError = ex.TraceData.ErrorData.OfType<ConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(configurationConfigurationError);

                var configurationConfigurationInvalidStepSizeError = configurationConfigurationError as ConfigurationInvalidStepSizeError;
                Assert.IsNotNull(configurationConfigurationInvalidStepSizeError);
                Assert.AreEqual("StepSize must be greater than 0.", configurationConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CreateWithNullNameThrowsException()
        {
            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration()
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
            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration()
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
