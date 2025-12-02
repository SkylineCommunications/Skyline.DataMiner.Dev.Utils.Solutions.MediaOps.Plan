namespace RT_MediaOps.Plan.RST.Resources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    using Storage = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class ResourcePropertiesTests
    {
        private readonly IntegrationTestContext testContext;
        private readonly ResourceStudioObjectCreator objectCreator;

        public ResourcePropertiesTests()
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

            var propertyIds = objectCreator.CreateProperties(new[] { property1, property2 }).ToArray();

            // Create resource with property configuration
            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            unmanagedResource.AddPropertyConfiguration(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertyConfiguration(propertyIds[0])
            {
                Value = "Property Value 1",
            });
            var resourceId = objectCreator.CreateResource(unmanagedResource);

            var resource = testContext.Api.Resources.Read(resourceId);
            Assert.AreEqual(1, resource.PropertyConfigurations.Count);

            var propertyConfiguration = resource.PropertyConfigurations.First();
            Assert.AreEqual(propertyIds[0], propertyConfiguration.Id);
            Assert.AreEqual("Property Value 1", propertyConfiguration.Value);

            // Update resource with new property configuration
            resource.AddPropertyConfiguration(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertyConfiguration(propertyIds[1])
            {
                Value = "Property Value 2",
            });
            testContext.Api.Resources.Update(resource);

            resource = testContext.Api.Resources.Read(resourceId);
            Assert.AreEqual(2, resource.PropertyConfigurations.Count);

            var expectedPropertyConfigurationData = new Dictionary<Guid, string>
            {
                { propertyIds[0], "Property Value 1" },
                { propertyIds[1], "Property Value 2" },
            };
            foreach (var propertyConfig in resource.PropertyConfigurations)
            {
                Assert.IsTrue(expectedPropertyConfigurationData.ContainsKey(propertyConfig.Id));
                Assert.AreEqual(expectedPropertyConfigurationData[propertyConfig.Id], propertyConfig.Value);
            }

            // Remove property configuration
            var propertyConfigToRemove = resource.PropertyConfigurations.First(pc => pc.Id == propertyIds[0]);
            resource.RemovePropertyConfiguration(propertyConfigToRemove);
            testContext.Api.Resources.Update(resource);

            resource = testContext.Api.Resources.Read(resourceId);
            Assert.AreEqual(1, resource.PropertyConfigurations.Count);

            propertyConfiguration = resource.PropertyConfigurations.First();
            Assert.AreEqual(propertyIds[1], propertyConfiguration.Id);
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

            var invalidPeropertyId = Guid.NewGuid();
            unmanagedResource.AddPropertyConfiguration(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertyConfiguration(invalidPeropertyId)
            {
                Value = "Some Value",
            });

            try
            {
                objectCreator.CreateResource(unmanagedResource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Property with ID '{invalidPeropertyId}' not found.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourcePropertyConfigurationError = resourceConfigurationError as InvalidResourcePropertyConfigurationError;
                Assert.IsNotNull(invalidResourcePropertyConfigurationError);
                Assert.AreEqual(errorMessage, invalidResourcePropertyConfigurationError.ErrorMessage);

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

            var resourceId  = objectCreator.CreateResource(unmanagedResource);
            var resource = testContext.Api.Resources.Read(resourceId);

            var invalidPeropertyId = Guid.NewGuid();
            resource.AddPropertyConfiguration(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertyConfiguration(invalidPeropertyId)
            {
                Value = "Some Value",
            });

            try
            {
                testContext.Api.Resources.Update(resource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Property with ID '{invalidPeropertyId}' not found.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourcePropertyConfigurationError = resourceConfigurationError as InvalidResourcePropertyConfigurationError;
                Assert.IsNotNull(invalidResourcePropertyConfigurationError);
                Assert.AreEqual(errorMessage, invalidResourcePropertyConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Exception not thrown");
        }
    }
}
