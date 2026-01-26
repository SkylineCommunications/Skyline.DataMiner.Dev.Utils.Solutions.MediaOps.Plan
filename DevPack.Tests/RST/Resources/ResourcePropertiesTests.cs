namespace RT_MediaOps.Plan.RST.Resources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class ResourcePropertiesTests : IDisposable
    {
        private readonly TestObjectCreator objectCreator;

        public ResourcePropertiesTests()
        {
            objectCreator = new TestObjectCreator(TestContext);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
        }

        [TestMethod]
        public void HappyPath()
        {
            var prefix = Guid.NewGuid();

            var property1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty()
            {
                Name = $"{prefix}_Property1",
            };
            var property2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty()
            {
                Name = $"{prefix}_Property2",
            };

            objectCreator.CreateProperties(new[] { property1, property2 });

            // Create resource with property configuration
            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            unmanagedResource.AddProperty(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertySettings(property1.Id)
            {
                Value = "Property Value 1",
            });
            objectCreator.CreateResource(unmanagedResource);

            var resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(1, resource.Properties.Count);

            var propertyConfiguration = resource.Properties.First();
            Assert.AreEqual(property1.Id, propertyConfiguration.Id);
            Assert.AreEqual("Property Value 1", propertyConfiguration.Value);

            // Update resource with new property configuration
            resource.AddProperty(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertySettings(property2.Id)
            {
                Value = "Property Value 2",
            });
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(2, resource.Properties.Count);

            var expectedPropertyConfigurationData = new Dictionary<Guid, string>
            {
                { property1.Id, "Property Value 1" },
                { property2.Id, "Property Value 2" },
            };
            foreach (var propertyConfig in resource.Properties)
            {
                Assert.IsTrue(expectedPropertyConfigurationData.ContainsKey(propertyConfig.Id));
                Assert.AreEqual(expectedPropertyConfigurationData[propertyConfig.Id], propertyConfig.Value);
            }

            // Remove property configuration
            var propertyConfigToRemove = resource.Properties.First(pc => pc.Id == property1.Id);
            resource.RemoveProperty(propertyConfigToRemove);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(1, resource.Properties.Count);

            propertyConfiguration = resource.Properties.First();
            Assert.AreEqual(property2.Id, propertyConfiguration.Id);
            Assert.AreEqual("Property Value 2", propertyConfiguration.Value);
        }

        [TestMethod]
        public void CreateWithNotExistingPropertyThrowsException()
        {
            var prefix = Guid.NewGuid();

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            var invalidPropertyId = Guid.NewGuid();
            unmanagedResource.AddProperty(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertySettings(invalidPropertyId)
            {
                Value = "Some Value",
            });

            try
            {
                objectCreator.CreateResource(unmanagedResource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Property with ID '{invalidPropertyId}' not found.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourcePropertyConfigurationError = resourceConfigurationError as ResourceInvalidPropertySettingsError;
                Assert.IsNotNull(invalidResourcePropertyConfigurationError);
                Assert.AreEqual(errorMessage, invalidResourcePropertyConfigurationError.ErrorMessage);
                Assert.AreEqual(invalidPropertyId, invalidResourcePropertyConfigurationError.PropertyId);
                Assert.AreEqual(unmanagedResource.Id, invalidResourcePropertyConfigurationError.Id);

                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void UpdateWithNotExistingPropertyThrowsException()
        {
            var prefix = Guid.NewGuid();

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            objectCreator.CreateResource(unmanagedResource);
            var resource = TestContext.Api.Resources.Read(unmanagedResource.Id);

            var invalidPropertyId = Guid.NewGuid();
            resource.AddProperty(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertySettings(invalidPropertyId)
            {
                Value = "Some Value",
            });

            try
            {
                TestContext.Api.Resources.Update(resource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Property with ID '{invalidPropertyId}' not found.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourcePropertyConfigurationError = resourceConfigurationError as ResourceInvalidPropertySettingsError;
                Assert.IsNotNull(invalidResourcePropertyConfigurationError);
                Assert.AreEqual(errorMessage, invalidResourcePropertyConfigurationError.ErrorMessage);
                Assert.AreEqual(invalidPropertyId, invalidResourcePropertyConfigurationError.PropertyId);
                Assert.AreEqual(resource.Id, invalidResourcePropertyConfigurationError.Id);

                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void CreateWithInvalidPropertyValueLengthThrowsException()
        {
            var prefix = Guid.NewGuid();
            var property = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty()
            {
                Name = $"{prefix}_Property",
            };
            objectCreator.CreateProperties(new[] { property });

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            unmanagedResource.AddProperty(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertySettings(property.Id)
            {
                Value = new string('A', 151), // Assuming max length is 150
            });

            try
            {
                objectCreator.CreateResource(unmanagedResource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Property value length is limited to 150 characters.";
                Assert.AreEqual(errorMessage, ex.Message);
                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourcePropertyConfigurationError = resourceConfigurationError as ResourceInvalidPropertySettingsError;
                Assert.IsNotNull(invalidResourcePropertyConfigurationError);
                Assert.AreEqual(errorMessage, invalidResourcePropertyConfigurationError.ErrorMessage);
                Assert.AreEqual(property.Id, invalidResourcePropertyConfigurationError.PropertyId);
                Assert.AreEqual(unmanagedResource.Id, invalidResourcePropertyConfigurationError.Id);
                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void UpdateWithInvalidPropertyValueLengthThrowsException()
        {
            var prefix = Guid.NewGuid();
            var property = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty()
            {
                Name = $"{prefix}_Property",
            };
            objectCreator.CreateProperties(new[] { property });

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            objectCreator.CreateResource(unmanagedResource);

            var resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            resource.AddProperty(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertySettings(property.Id)
            {
                Value = new string('A', 151), // Assuming max length is 150
            });

            try
            {
                TestContext.Api.Resources.Update(resource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Property value length is limited to 150 characters.";
                Assert.AreEqual(errorMessage, ex.Message);
                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);
                var invalidResourcePropertyConfigurationError = resourceConfigurationError as ResourceInvalidPropertySettingsError;
                Assert.IsNotNull(invalidResourcePropertyConfigurationError);
                Assert.AreEqual(errorMessage, invalidResourcePropertyConfigurationError.ErrorMessage);
                Assert.AreEqual(property.Id, invalidResourcePropertyConfigurationError.PropertyId);
                Assert.AreEqual(resource.Id, invalidResourcePropertyConfigurationError.Id);
                return;
            }
            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void CreateWithDuplicateSettingsThrowsException()
        {
            var prefix = Guid.NewGuid();
            var property1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty()
            {
                Name = $"{prefix}_Property1",
            };
            var property2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty()
            {
                Name = $"{prefix}_Property2",
            };
            objectCreator.CreateProperties([property1, property2]);

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            }
            .AddProperty(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertySettings(property1.Id)
            {
                Value = "A",
            })
            .AddProperty(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertySettings(property2.Id)
            {
                Value = "B",
            })
            .AddProperty(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertySettings(property1.Id)
            {
                Value = "C",
            });

            try
            {
                objectCreator.CreateResource(unmanagedResource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Property with ID '{property1.Id}' is defined 2 times. Duplicate property settings are not allowed.";
                Assert.AreEqual(errorMessage, ex.Message);
                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourcePropertyConfigurationError = resourceConfigurationError as ResourceInvalidPropertySettingsError;
                Assert.IsNotNull(invalidResourcePropertyConfigurationError);
                Assert.AreEqual(errorMessage, invalidResourcePropertyConfigurationError.ErrorMessage);
                Assert.AreEqual(property1.Id, invalidResourcePropertyConfigurationError.PropertyId);
                Assert.AreEqual(unmanagedResource.Id, invalidResourcePropertyConfigurationError.Id);
                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void UpdateWithDuplicateSettingsThrowsException()
        {
            var prefix = Guid.NewGuid();
            var property1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty()
            {
                Name = $"{prefix}_Property1",
            };
            var property2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty()
            {
                Name = $"{prefix}_Property2",
            };
            objectCreator.CreateProperties([property1, property2]);

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            }
            .AddProperty(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertySettings(property1.Id)
            {
                Value = "A",
            })
            .AddProperty(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertySettings(property2.Id)
            {
                Value = "B",
            });
            objectCreator.CreateResource(unmanagedResource);

            var resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            resource.AddProperty(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertySettings(property1.Id)
             {
                 Value = "C",
             });

            try
            {
                TestContext.Api.Resources.Update(resource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Property with ID '{property1.Id}' is defined 2 times. Duplicate property settings are not allowed.";
                Assert.AreEqual(errorMessage, ex.Message);
                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourcePropertyConfigurationError = resourceConfigurationError as ResourceInvalidPropertySettingsError;
                Assert.IsNotNull(invalidResourcePropertyConfigurationError);
                Assert.AreEqual(errorMessage, invalidResourcePropertyConfigurationError.ErrorMessage);
                Assert.AreEqual(property1.Id, invalidResourcePropertyConfigurationError.PropertyId);
                Assert.AreEqual(unmanagedResource.Id, invalidResourcePropertyConfigurationError.Id);
                return;
            }

            Assert.Fail("Exception not thrown");
        }
    }
}
