namespace RT_MediaOps.Plan.RST.Capacities
{
    using System;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class BasicTests : IDisposable
    {
        private readonly ResourceStudioObjectCreator objectCreator;

        public BasicTests()
        {
            objectCreator = new ResourceStudioObjectCreator(TestContext.Api);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
        }

        [TestMethod]
        public void BasicNumberCapacityCrudActions()
        {
            // Create
            var capacityId = Guid.NewGuid();
            var name = $"{capacityId}_Capacity";

            var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity(capacityId)
            {
                Name = name,
            };

            objectCreator.CreateCapacity(capacity);

            var returnedCapacity = TestContext.Api.Capacities.Read(capacityId);
            Assert.IsNotNull(returnedCapacity);
            Assert.AreEqual(typeof(Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity), returnedCapacity.GetType());
            Assert.AreEqual(name, returnedCapacity.Name);
            Assert.AreEqual(false, returnedCapacity.IsMandatory);

            var coreCapacity = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(capacityId)).SingleOrDefault();
            Assert.IsNotNull(coreCapacity);
            Assert.AreEqual(name, coreCapacity.Name);
            Assert.AreEqual(true, coreCapacity.IsOptional);

            Assert.IsNull(coreCapacity.Remarks);
            Assert.IsNull(coreCapacity.DefaultValue);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Capacity, coreCapacity.Categories);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Number, coreCapacity.Type);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Undefined, coreCapacity.InterpreteType.RawType);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Undefined, coreCapacity.InterpreteType.Type);

            Assert.AreEqual(String.Empty, coreCapacity.Units); // Verified that this is in line with the current solution
            Assert.AreEqual(double.NaN, coreCapacity.RangeMin);
            Assert.AreEqual(double.NaN, coreCapacity.RangeMax);
            Assert.AreEqual(double.NaN, coreCapacity.Stepsize);
            Assert.AreEqual(int.MaxValue, coreCapacity.Decimals);

            // Update
            var updatedName = name + "_Updated";
            returnedCapacity.Name = updatedName;
            TestContext.Api.Capacities.Update(returnedCapacity);
            returnedCapacity = TestContext.Api.Capacities.Read(capacityId);
            Assert.IsNotNull(returnedCapacity);
            Assert.AreEqual(updatedName, returnedCapacity.Name);

            coreCapacity = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(capacityId)).SingleOrDefault();
            Assert.IsNotNull(coreCapacity);
            Assert.AreEqual(updatedName, coreCapacity.Name);
            Assert.AreEqual(true, coreCapacity.IsOptional);

            Assert.IsNull(coreCapacity.Remarks);
            Assert.IsNull(coreCapacity.DefaultValue);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Capacity, coreCapacity.Categories);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Number, coreCapacity.Type);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Undefined, coreCapacity.InterpreteType.RawType);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Undefined, coreCapacity.InterpreteType.Type);

            Assert.AreEqual(String.Empty, coreCapacity.Units); // Verified that this is in line with the current solution
            Assert.AreEqual(double.NaN, coreCapacity.RangeMin);
            Assert.AreEqual(double.NaN, coreCapacity.RangeMax);
            Assert.AreEqual(double.NaN, coreCapacity.Stepsize);
            Assert.AreEqual(int.MaxValue, coreCapacity.Decimals);

            // Delete
            TestContext.Api.Capacities.Delete(returnedCapacity);
            returnedCapacity = TestContext.Api.Capacities.Read(capacityId);
            Assert.IsNull(returnedCapacity);

            coreCapacity = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(capacityId)).SingleOrDefault();
            Assert.IsNull(coreCapacity);
        }

        [TestMethod]
        public void BasicRangeCapacityCrudActions()
        {
            // Create
            var capacityId = Guid.NewGuid();
            var name = $"{capacityId}_Capacity";

            var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacity(capacityId)
            {
                Name = name,
            };

            objectCreator.CreateCapacity(capacity);

            var returnedCapacity = TestContext.Api.Capacities.Read(capacityId);
            Assert.IsNotNull(returnedCapacity);
            Assert.AreEqual(typeof(Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacity), returnedCapacity.GetType());
            Assert.AreEqual(name, returnedCapacity.Name);
            Assert.AreEqual(false, returnedCapacity.IsMandatory);

            var coreCapacity = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(capacityId)).SingleOrDefault();
            Assert.IsNotNull(coreCapacity);
            Assert.AreEqual(name, coreCapacity.Name);
            Assert.AreEqual(true, coreCapacity.IsOptional);

            Assert.IsNull(coreCapacity.Remarks);
            Assert.IsNull(coreCapacity.DefaultValue);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Capacity, coreCapacity.Categories);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Range, coreCapacity.Type);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Undefined, coreCapacity.InterpreteType.RawType);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Undefined, coreCapacity.InterpreteType.Type);

            Assert.AreEqual(String.Empty, coreCapacity.Units); // Verified that this is in line with the current solution
            Assert.AreEqual(double.NaN, coreCapacity.RangeMin);
            Assert.AreEqual(double.NaN, coreCapacity.RangeMax);
            Assert.AreEqual(double.NaN, coreCapacity.Stepsize);
            Assert.AreEqual(int.MaxValue, coreCapacity.Decimals);

            // Update
            var updatedName = name + "_Updated";
            returnedCapacity.Name = updatedName;
            TestContext.Api.Capacities.Update(returnedCapacity);
            returnedCapacity = TestContext.Api.Capacities.Read(capacityId);
            Assert.IsNotNull(returnedCapacity);
            Assert.AreEqual(updatedName, returnedCapacity.Name);

            coreCapacity = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(capacityId)).SingleOrDefault();
            Assert.IsNotNull(coreCapacity);
            Assert.AreEqual(updatedName, coreCapacity.Name);
            Assert.AreEqual(true, coreCapacity.IsOptional);

            Assert.IsNull(coreCapacity.Remarks);
            Assert.IsNull(coreCapacity.DefaultValue);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Capacity, coreCapacity.Categories);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Range, coreCapacity.Type);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Undefined, coreCapacity.InterpreteType.RawType);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Undefined, coreCapacity.InterpreteType.Type);

            Assert.AreEqual(String.Empty, coreCapacity.Units);
            Assert.AreEqual(double.NaN, coreCapacity.RangeMin);
            Assert.AreEqual(double.NaN, coreCapacity.RangeMax);
            Assert.AreEqual(double.NaN, coreCapacity.Stepsize);
            Assert.AreEqual(int.MaxValue, coreCapacity.Decimals);

            // Delete
            TestContext.Api.Capacities.Delete(returnedCapacity);
            returnedCapacity = TestContext.Api.Capacities.Read(capacityId);
            Assert.IsNull(returnedCapacity);

            coreCapacity = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(capacityId)).SingleOrDefault();
            Assert.IsNull(coreCapacity);
        }

        [TestMethod]
        public void CreateWithExistingIdThrowsException()
        {
            var capacityId = Guid.NewGuid();

            var capacity1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity(capacityId)
            {
                Name = $"{capacityId}_Capacity1",
            };

            var capacity2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity(capacityId)
            {
                Name = $"{capacityId}_Capacity2",
            };

            objectCreator.CreateCapacity(capacity1);
            try
            {
                objectCreator.CreateCapacity(capacity2);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "ID is already in use.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var capacityConfigurationError = ex.TraceData.ErrorData.OfType<CapacityError>().SingleOrDefault();
                Assert.IsNotNull(capacityConfigurationError);

                var capacityConfigurationIdInUseError = capacityConfigurationError as CapacityIdInUseError;
                Assert.IsNotNull(capacityConfigurationIdInUseError);
                Assert.AreEqual(capacityId, capacityConfigurationIdInUseError.Id);
                Assert.AreEqual("ID is already in use.", capacityConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CreateWithSameIdInBulkThrowsException()
        {
            var capacityId = Guid.NewGuid();

            var capacity1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity(capacityId)
            {
                Name = $"{capacityId}_Capacity1",
            };

            var capacity2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity(capacityId)
            {
                Name = $"{capacityId}_Capacity2",
            };

            try
            {
                objectCreator.CreateCapacities(new[] { capacity1, capacity2 });
            }
            catch (MediaOpsBulkException<Guid> ex)
            {
                if (!ex.Result.TraceDataPerItem.TryGetValue(capacityId, out var traceData))
                {
                    Assert.Fail("No trace data found for the failed ID");
                }

                Assert.AreEqual(2, traceData.ErrorData.Count);
                var capacityConfigurationErrors = traceData.ErrorData.OfType<CapacityError>().ToList();
                Assert.AreEqual(2, capacityConfigurationErrors.Count());

                var errorMessages = new List<string>
                {
                   $"Capacity '{capacity1.Name}' has a duplicate ID.",
                   $"Capacity '{capacity2.Name}' has a duplicate ID."
                };

                foreach (var error in capacityConfigurationErrors)
                {
                    var capacityConfigurationDuplicateIdError = error as CapacityDuplicateIdError;
                    Assert.IsNotNull(capacityConfigurationDuplicateIdError);
                    Assert.AreEqual(capacityId, capacityConfigurationDuplicateIdError.Id);
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
            var capacityId = Guid.NewGuid();

            var capacity1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{capacityId}_Capacity",
            };

            var capacity2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{capacityId}_Capacity",
            };

            objectCreator.CreateCapacity(capacity1);
            try
            {
                objectCreator.CreateCapacity(capacity2);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "Name is already in use.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var capacityConfigurationError = ex.TraceData.ErrorData.OfType<CapacityError>().SingleOrDefault();
                Assert.IsNotNull(capacityConfigurationError);

                var capacityConfigurationNameExistsError = capacityConfigurationError as CapacityNameExistsError;
                Assert.IsNotNull(capacityConfigurationNameExistsError);
                Assert.AreEqual(capacity2.Id, capacityConfigurationNameExistsError.Id);
                Assert.AreEqual(capacity2.Name, capacityConfigurationNameExistsError.Name);
                Assert.AreEqual("Name is already in use.", capacityConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CreateWithSameNameInBulkThrowsException()
        {
            var capacityId = Guid.NewGuid();

            var capacity1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{capacityId}_Capacity",
            };

            var capacity2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{capacityId}_Capacity",
            };

            try
            {
                objectCreator.CreateCapacities(new[] { capacity1, capacity2 });
            }
            catch (MediaOpsBulkException<Guid> ex)
            {
                Assert.AreEqual(2, ex.Result.TraceDataPerItem.Count);

                foreach (var traceData in ex.Result.TraceDataPerItem.Values)
                {
                    Assert.AreEqual(1, traceData.ErrorData.Count);
                    var capacityConfigurationError = traceData.ErrorData.OfType<CapacityError>().SingleOrDefault();
                    Assert.IsNotNull(capacityConfigurationError);

                    var capacityConfigurationDuplicateNameError = capacityConfigurationError as CapacityDuplicateNameError;
                    Assert.IsNotNull(capacityConfigurationDuplicateNameError);
                    Assert.AreEqual($"Capacity '{capacity1.Name}' has a duplicate name.", capacityConfigurationError.ErrorMessage);
                }

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void UpdateToSameNameThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capacity1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity_1",
            };

            var capacity2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity_2",
            };

            objectCreator.CreateCapacity(capacity1);
            objectCreator.CreateCapacity(capacity2);

            var toUpdate = TestContext.Api.Capacities.Read(capacity2.Id);
            toUpdate.Name = capacity1.Name;

            try
            {
                TestContext.Api.Capacities.Update(toUpdate);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "Name is already in use.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var capacityConfigurationError = ex.TraceData.ErrorData.OfType<CapacityError>().SingleOrDefault();
                Assert.IsNotNull(capacityConfigurationError);

                var capacityConfigurationNameExistsError = capacityConfigurationError as CapacityNameExistsError;
                Assert.IsNotNull(capacityConfigurationNameExistsError);
                Assert.AreEqual(toUpdate.Id, capacityConfigurationNameExistsError.Id);
                Assert.AreEqual(toUpdate.Name, capacityConfigurationNameExistsError.Name);
                Assert.AreEqual("Name is already in use.", capacityConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void RangeValidation()
        {
            // Create
            var capacityId = Guid.NewGuid();

            var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity(capacityId)
            {
                Name = $"{capacityId}_Capacity",
                IsMandatory = true,
                Units = "MHz",
                RangeMin = 10,
                RangeMax = 100,
                StepSize = 5,
                Decimals = 2
            };

            objectCreator.CreateCapacity(capacity);

            capacity = TestContext.Api.Capacities.Read(capacityId) as Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity;
            Assert.IsNotNull(capacity);
            Assert.AreEqual(true, capacity.IsMandatory);

            Assert.AreEqual("MHz", capacity.Units);
            Assert.AreEqual(10, capacity.RangeMin);
            Assert.AreEqual(100, capacity.RangeMax);
            Assert.AreEqual(5, capacity.StepSize);
            Assert.AreEqual(2, capacity.Decimals);

            var coreCapacity = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(capacityId)).SingleOrDefault();
            Assert.IsNotNull(coreCapacity);
            Assert.AreEqual(false, coreCapacity.IsOptional);

            Assert.IsNull(coreCapacity.Remarks);
            Assert.IsNull(coreCapacity.DefaultValue);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Capacity, coreCapacity.Categories);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Number, coreCapacity.Type);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Undefined, coreCapacity.InterpreteType.RawType);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Undefined, coreCapacity.InterpreteType.Type);

            Assert.AreEqual("MHz", coreCapacity.Units);
            Assert.AreEqual(10, coreCapacity.RangeMin);
            Assert.AreEqual(100, coreCapacity.RangeMax);
            Assert.AreEqual(5, coreCapacity.Stepsize);
            Assert.AreEqual(2, coreCapacity.Decimals);

            // Update
            capacity.Units = "kHz";
            capacity.RangeMin = 20;
            capacity.RangeMax = 200;
            capacity.StepSize = 10;
            capacity.Decimals = 1;

            TestContext.Api.Capacities.Update(capacity);

            capacity = TestContext.Api.Capacities.Read(capacityId) as Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity;
            Assert.IsNotNull(capacity);
            Assert.AreEqual(true, capacity.IsMandatory);

            Assert.AreEqual("kHz", capacity.Units);
            Assert.AreEqual(20, capacity.RangeMin);
            Assert.AreEqual(200, capacity.RangeMax);
            Assert.AreEqual(10, capacity.StepSize);
            Assert.AreEqual(1, capacity.Decimals);

            coreCapacity = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.ID.Equal(capacityId)).SingleOrDefault();
            Assert.IsNotNull(coreCapacity);
            Assert.AreEqual(false, coreCapacity.IsOptional);

            Assert.IsNull(coreCapacity.Remarks);
            Assert.IsNull(coreCapacity.DefaultValue);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Capacity, coreCapacity.Categories);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Number, coreCapacity.Type);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Undefined, coreCapacity.InterpreteType.RawType);
            Assert.AreEqual(Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Undefined, coreCapacity.InterpreteType.Type);

            Assert.AreEqual("kHz", coreCapacity.Units);
            Assert.AreEqual(20, coreCapacity.RangeMin);
            Assert.AreEqual(200, coreCapacity.RangeMax);
            Assert.AreEqual(10, coreCapacity.Stepsize);
            Assert.AreEqual(1, coreCapacity.Decimals);
        }

        [TestMethod]
        public void RangeValidationThrowsException()
        {
            var capacityId = Guid.NewGuid();

            var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity(capacityId)
            {
                Name = $"{capacityId}_Capacity",
                IsMandatory = true,
                Units = "MHz",
                RangeMin = 10.123m,
                RangeMax = 1.123m,
                StepSize = 5.123m,
                Decimals = 2
            };

            try
            {
                objectCreator.CreateCapacity(capacity);
            }
            catch (MediaOpsException ex)
            {
                Assert.AreEqual(4, ex.TraceData.ErrorData.Count);
                var capacityConfigurationErrors = ex.TraceData.ErrorData.OfType<CapacityError>();
                Assert.AreEqual(4, capacityConfigurationErrors.Count());

                var capacityConfigurationInvalidRangeError = capacityConfigurationErrors.OfType<CapacityInvalidRangeError>().SingleOrDefault();
                Assert.IsNotNull(capacityConfigurationInvalidRangeError);
                Assert.AreEqual(capacityId, capacityConfigurationInvalidRangeError.Id);
                Assert.AreEqual("RangeMax must be greater than RangeMin.", capacityConfigurationInvalidRangeError.ErrorMessage);
                Assert.AreEqual(10.123m, capacityConfigurationInvalidRangeError.RangeMin);
                Assert.AreEqual(1.123m, capacityConfigurationInvalidRangeError.RangeMax);

                var capacityConfigurationInvalidRangeMinError = capacityConfigurationErrors.OfType<CapacityInvalidRangeMinError>().SingleOrDefault();
                Assert.IsNotNull(capacityConfigurationInvalidRangeMinError);
                Assert.AreEqual(capacityId, capacityConfigurationInvalidRangeMinError.Id);
                Assert.AreEqual("RangeMin has more decimal places than allowed by Decimals (2).", capacityConfigurationInvalidRangeMinError.ErrorMessage);
                Assert.AreEqual(10.123m, capacityConfigurationInvalidRangeMinError.RangeMin);

                var capacityConfigurationInvalidRangeMaxError = capacityConfigurationErrors.OfType<CapacityInvalidRangeMaxError>().SingleOrDefault();
                Assert.IsNotNull(capacityConfigurationInvalidRangeMaxError);
                Assert.AreEqual(capacityId, capacityConfigurationInvalidRangeMaxError.Id);
                Assert.AreEqual("RangeMax has more decimal places than allowed by Decimals (2).", capacityConfigurationInvalidRangeMaxError.ErrorMessage);
                Assert.AreEqual(1.123m, capacityConfigurationInvalidRangeMaxError.RangeMax);

                var capacityConfigurationInvalidStepSizeError = capacityConfigurationErrors.OfType<CapacityInvalidStepSizeError>().SingleOrDefault();
                Assert.IsNotNull(capacityConfigurationInvalidStepSizeError);
                Assert.AreEqual(capacityId, capacityConfigurationInvalidStepSizeError.Id);
                Assert.AreEqual("StepSize has more decimal places than allowed by Decimals (2).", capacityConfigurationInvalidStepSizeError.ErrorMessage);
                Assert.AreEqual(5.123m, capacityConfigurationInvalidStepSizeError.StepSize);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void DecimalsValidationThrowsException()
        {
            var capacityId = Guid.NewGuid();

            var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity(capacityId)
            {
                Name = $"{capacityId}_Capacity",
                IsMandatory = true,
                Units = "MHz",
                RangeMin = 10,
                RangeMax = 100,
                StepSize = 5,
                Decimals = -3
            };

            try
            {
                objectCreator.CreateCapacity(capacity);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "Decimals must be between 0 and 15.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var capacityConfigurationError = ex.TraceData.ErrorData.OfType<CapacityError>().SingleOrDefault();
                Assert.IsNotNull(capacityConfigurationError);

                var capacityConfigurationInvalidDecimalsError = capacityConfigurationError as CapacityInvalidDecimalsError;
                Assert.IsNotNull(capacityConfigurationInvalidDecimalsError);
                Assert.AreEqual(capacityId, capacityConfigurationInvalidDecimalsError.Id);
                Assert.AreEqual("Decimals must be between 0 and 15.", capacityConfigurationError.ErrorMessage);
                Assert.AreEqual(-3, capacityConfigurationInvalidDecimalsError.Decimals);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void StepSizeValidationThrowsException()
        {
            var capacityId = Guid.NewGuid();

            var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity(capacityId)
            {
                Name = $"{capacityId}_Capacity",
                IsMandatory = true,
                Units = "MHz",
                RangeMin = 10,
                RangeMax = 100,
                StepSize = -5,
                Decimals = 3
            };

            try
            {
                objectCreator.CreateCapacity(capacity);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "StepSize must be greater than 0.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var capacityConfigurationError = ex.TraceData.ErrorData.OfType<CapacityError>().SingleOrDefault();
                Assert.IsNotNull(capacityConfigurationError);

                var capacityConfigurationInvalidStepSizeError = capacityConfigurationError as CapacityInvalidStepSizeError;
                Assert.IsNotNull(capacityConfigurationInvalidStepSizeError);
                Assert.AreEqual(capacityId, capacityConfigurationInvalidStepSizeError.Id);
                Assert.AreEqual("StepSize must be greater than 0.", capacityConfigurationError.ErrorMessage);
                Assert.AreEqual(-5m, capacityConfigurationInvalidStepSizeError.StepSize);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void ReadWithEmptyListReturnsEmptyList()
        {
            var capacities = TestContext.Api.Capacities.Read(new List<Guid>());
            Assert.IsNotNull(capacities);
            Assert.AreEqual(0, capacities.Count());
        }
    }
}
