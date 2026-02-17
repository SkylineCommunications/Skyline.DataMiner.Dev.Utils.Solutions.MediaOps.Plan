namespace RT_MediaOps.Plan.RST.Capabilities
{
    using System;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class DeleteTests : IDisposable
    {
        private readonly TestObjectCreator objectCreator;

        public DeleteTests()
        {
            objectCreator = new TestObjectCreator(TestContext);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
        }

        [TestMethod]
        public void DeleteCapabilityWhenAssignedToResourcesThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capability1 = new Capability()
            {
                Name = $"{prefix}_Capability1",
            }
            .SetDiscretes(new[] { "Option1", "Option2" });
            var capability2 = new Capability()
            {
                Name = $"{prefix}_Capability2",
            }
            .SetDiscretes(new[] { "Option1", "Option2" });
            var capability3 = new Capability()
            {
                Name = $"{prefix}_Capability3",
            }
            .SetDiscretes(new[] { "Option1", "Option2" });

            objectCreator.CreateCapabilities([capability1, capability2, capability3]);

            var unmangedResource1 = new UnmanagedResource()
            {
                Name = $"{prefix}_Resource1",
            }
            .AddCapability(new CapabilitySettings(capability1).AddDiscrete("Option1"))
            .AddCapability(new CapabilitySettings(capability2).AddDiscrete("Option2"));

            var unmangedResource2 = new UnmanagedResource()
            {
                Name = $"{prefix}_Resource2",
            }
            .AddCapability(new CapabilitySettings(capability1).AddDiscrete("Option2"));

            objectCreator.CreateResources([unmangedResource1, unmangedResource2]);

            MediaOpsBulkException<Guid>? expectedException = null;
            try
            {
                TestContext.Api.Capabilities.Delete([capability1, capability2, capability3]);
            }
            catch (MediaOpsBulkException<Guid> ex)
            {
                expectedException = ex;
            }

            if (expectedException == null)
            {
                Assert.Fail("Expected exception was not thrown.");
            }

            Assert.AreEqual(1, expectedException.Result.SuccessfulIds.Count);
            Assert.IsTrue(expectedException.Result.SuccessfulIds.Contains(capability3.ID));

            Assert.AreEqual(2, expectedException.Result.UnsuccessfulIds.Count);
            Assert.IsTrue(expectedException.Result.UnsuccessfulIds.Contains(capability1.ID));
            Assert.IsTrue(expectedException.Result.UnsuccessfulIds.Contains(capability2.ID));

            capability3 = TestContext.Api.Capabilities.Read(capability3.ID);
            Assert.IsNull(capability3);

            expectedException.Result.TraceDataPerItem.TryGetValue(capability1.ID, out var traceData1);
            Assert.IsNotNull(traceData1);
            Assert.AreEqual(1, traceData1.ErrorData.Count);
            var capabilityInUseError = traceData1.ErrorData.OfType<CapabilityInUseByResourcesError>().SingleOrDefault();
            Assert.IsNotNull(capabilityInUseError);
            Assert.AreEqual($"Capability '{capability1.Name}' is in use by 2 resource(s).", capabilityInUseError.ErrorMessage);
            Assert.AreEqual(2, capabilityInUseError.ResourceIds.Count);
            Assert.IsTrue(capabilityInUseError.ResourceIds.Contains(unmangedResource1.ID));
            Assert.IsTrue(capabilityInUseError.ResourceIds.Contains(unmangedResource2.ID));

            expectedException.Result.TraceDataPerItem.TryGetValue(capability2.ID, out var traceData2);
            Assert.IsNotNull(traceData2);
            Assert.AreEqual(1, traceData2.ErrorData.Count);
            capabilityInUseError = traceData2.ErrorData.OfType<CapabilityInUseByResourcesError>().SingleOrDefault();
            Assert.IsNotNull(capabilityInUseError);
            Assert.AreEqual($"Capability '{capability2.Name}' is in use by 1 resource(s).", capabilityInUseError.ErrorMessage);
            Assert.AreEqual(1, capabilityInUseError.ResourceIds.Count);
            Assert.IsTrue(capabilityInUseError.ResourceIds.Contains(unmangedResource1.ID));
        }

        [TestMethod]
        public void DeleteCapabilityWhenAssignedToResourcePoolsThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capability1 = new Capability()
            {
                Name = $"{prefix}_Capability1",
            }
            .SetDiscretes(new[] { "Option1", "Option2" });
            var capability2 = new Capability()
            {
                Name = $"{prefix}_Capability2",
            }
            .SetDiscretes(new[] { "Option1", "Option2" });
            var capability3 = new Capability()
            {
                Name = $"{prefix}_Capability3",
            }
            .SetDiscretes(new[] { "Option1", "Option2" });

            objectCreator.CreateCapabilities([capability1, capability2, capability3]);

            var pool1 = new ResourcePool()
            {
                Name = $"{prefix}_ResourcePool1",
            }
            .AddCapability(new CapabilitySettings(capability1).AddDiscrete("Option1"))
            .AddCapability(new CapabilitySettings(capability2).AddDiscrete("Option2"));

            var pool2 = new ResourcePool()
            {
                Name = $"{prefix}_ResourcePool2",
            }
            .AddCapability(new CapabilitySettings(capability1).AddDiscrete("Option2"));

            objectCreator.CreateResourcePools([pool1, pool2]);

            MediaOpsBulkException<Guid>? expectedException = null;
            try
            {
                TestContext.Api.Capabilities.Delete([capability1, capability2, capability3]);
            }
            catch (MediaOpsBulkException<Guid> ex)
            {
                expectedException = ex;
            }

            if (expectedException == null)
            {
                Assert.Fail("Expected exception was not thrown.");
            }

            Assert.AreEqual(1, expectedException.Result.SuccessfulIds.Count);
            Assert.IsTrue(expectedException.Result.SuccessfulIds.Contains(capability3.ID));

            Assert.AreEqual(2, expectedException.Result.UnsuccessfulIds.Count);
            Assert.IsTrue(expectedException.Result.UnsuccessfulIds.Contains(capability1.ID));
            Assert.IsTrue(expectedException.Result.UnsuccessfulIds.Contains(capability2.ID));

            capability3 = TestContext.Api.Capabilities.Read(capability3.ID);
            Assert.IsNull(capability3);

            expectedException.Result.TraceDataPerItem.TryGetValue(capability1.ID, out var traceData1);
            Assert.IsNotNull(traceData1);
            Assert.AreEqual(1, traceData1.ErrorData.Count);
            var capabilityInUseError = traceData1.ErrorData.OfType<CapabilityInUseByResourcePoolsError>().SingleOrDefault();
            Assert.IsNotNull(capabilityInUseError);
            Assert.AreEqual($"Capability '{capability1.Name}' is in use by 2 resource pool(s).", capabilityInUseError.ErrorMessage);
            Assert.AreEqual(2, capabilityInUseError.ResourcePoolIds.Count);
            Assert.IsTrue(capabilityInUseError.ResourcePoolIds.Contains(pool1.ID));
            Assert.IsTrue(capabilityInUseError.ResourcePoolIds.Contains(pool2.ID));

            expectedException.Result.TraceDataPerItem.TryGetValue(capability2.ID, out var traceData2);
            Assert.IsNotNull(traceData2);
            Assert.AreEqual(1, traceData2.ErrorData.Count);
            capabilityInUseError = traceData2.ErrorData.OfType<CapabilityInUseByResourcePoolsError>().SingleOrDefault();
            Assert.IsNotNull(capabilityInUseError);
            Assert.AreEqual($"Capability '{capability2.Name}' is in use by 1 resource pool(s).", capabilityInUseError.ErrorMessage);
            Assert.AreEqual(1, capabilityInUseError.ResourcePoolIds.Count);
            Assert.IsTrue(capabilityInUseError.ResourcePoolIds.Contains(pool1.ID));
        }

        [TestMethod]
        public void DeleteCapabilityWhenAssignedToResourcesAndResourcePoolsThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capability1 = new Capability()
            {
                Name = $"{prefix}_Capability1",
            }
            .SetDiscretes(new[] { "Option1", "Option2" });
            var capability2 = new Capability()
            {
                Name = $"{prefix}_Capability2",
            }
            .SetDiscretes(new[] { "Option1", "Option2" });
            var capability3 = new Capability()
            {
                Name = $"{prefix}_Capability3",
            }
            .SetDiscretes(new[] { "Option1", "Option2" });

            objectCreator.CreateCapabilities([capability1, capability2, capability3]);

            var unmangedResource = new UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            }
            .AddCapability(new CapabilitySettings(capability1).AddDiscrete("Option1"))
            .AddCapability(new CapabilitySettings(capability2).AddDiscrete("Option2"));

            objectCreator.CreateResource(unmangedResource);

            var pool = new ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            }
            .AddCapability(new CapabilitySettings(capability1).AddDiscrete("Option2"));

            objectCreator.CreateResourcePool(pool);

            MediaOpsBulkException<Guid>? expectedException = null;
            try
            {
                TestContext.Api.Capabilities.Delete([capability1, capability2, capability3]);
            }
            catch (MediaOpsBulkException<Guid> ex)
            {
                expectedException = ex;
            }

            if (expectedException == null)
            {
                Assert.Fail("Expected exception was not thrown.");
            }

            Assert.AreEqual(1, expectedException.Result.SuccessfulIds.Count);
            Assert.IsTrue(expectedException.Result.SuccessfulIds.Contains(capability3.ID));

            Assert.AreEqual(2, expectedException.Result.UnsuccessfulIds.Count);
            Assert.IsTrue(expectedException.Result.UnsuccessfulIds.Contains(capability1.ID));
            Assert.IsTrue(expectedException.Result.UnsuccessfulIds.Contains(capability2.ID));

            capability3 = TestContext.Api.Capabilities.Read(capability3.ID);
            Assert.IsNull(capability3);

            // Capability 1
            expectedException.Result.TraceDataPerItem.TryGetValue(capability1.ID, out var traceData1);
            Assert.IsNotNull(traceData1);
            Assert.AreEqual(2, traceData1.ErrorData.Count);

            var capabillity1InUseErrors = traceData1.ErrorData.OfType<CapabilityInUseError>().ToList();
            Assert.AreEqual(2, capabillity1InUseErrors.Count);

            var capabilityInUseByPoolError = capabillity1InUseErrors.OfType<CapabilityInUseByResourcePoolsError>().SingleOrDefault();
            Assert.IsNotNull(capabilityInUseByPoolError);
            Assert.AreEqual($"Capability '{capability1.Name}' is in use by 1 resource pool(s).", capabilityInUseByPoolError.ErrorMessage);
            Assert.AreEqual(1, capabilityInUseByPoolError.ResourcePoolIds.Count);
            Assert.IsTrue(capabilityInUseByPoolError.ResourcePoolIds.Contains(pool.ID));

            var capability1InUseByResourceError = capabillity1InUseErrors.OfType<CapabilityInUseByResourcesError>().SingleOrDefault();
            Assert.IsNotNull(capability1InUseByResourceError);
            Assert.AreEqual($"Capability '{capability1.Name}' is in use by 1 resource(s).", capability1InUseByResourceError.ErrorMessage);
            Assert.AreEqual(1, capability1InUseByResourceError.ResourceIds.Count);
            Assert.IsTrue(capability1InUseByResourceError.ResourceIds.Contains(unmangedResource.ID));

            // Capability 2
            expectedException.Result.TraceDataPerItem.TryGetValue(capability2.ID, out var traceData2);
            Assert.IsNotNull(traceData2);
            Assert.AreEqual(1, traceData2.ErrorData.Count);

            var capabillity2InUseErrors = traceData2.ErrorData.OfType<CapabilityInUseError>().ToList();
            Assert.AreEqual(1, capabillity2InUseErrors.Count);

            var capability2InUseByResourceError = capabillity2InUseErrors.OfType<CapabilityInUseByResourcesError>().SingleOrDefault();
            Assert.IsNotNull(capability2InUseByResourceError);
            Assert.AreEqual($"Capability '{capability2.Name}' is in use by 1 resource(s).", capability2InUseByResourceError.ErrorMessage);
            Assert.AreEqual(1, capability2InUseByResourceError.ResourceIds.Count);
            Assert.IsTrue(capability2InUseByResourceError.ResourceIds.Contains(unmangedResource.ID));
        }

        [TestMethod]
        public void DeleteCapability_PartOfResourcePoolConfiguration_ThrowsException()
        {
            var prefix = Guid.NewGuid();
            var capability = new Capability
            {
                Name = $"{prefix}_Capability",
            }.SetDiscretes(["A", "B", "C"]);

            objectCreator.CreateCapabilities([capability]);

            var resourcePool = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };

            resourcePool.OrchestrationSettings.SetCapabilities([new CapabilitySetting(capability)]);
            objectCreator.CreateResourcePool(resourcePool);

            try
            {
                TestContext.Api.Capabilities.Delete(capability);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Capability '{capability.Name}' is in use by 1 resource pool(s).";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

                var capabilityInUseError = ex.TraceData.ErrorData.Single() as CapabilityInUseByResourcePoolsError;
                Assert.IsNotNull(capabilityInUseError);
                Assert.AreEqual(errorMessage, capabilityInUseError.ErrorMessage);
                Assert.IsNotNull(capabilityInUseError.ResourcePoolIds);
                Assert.AreEqual(1, capabilityInUseError.ResourcePoolIds.Count());
                Assert.AreEqual(resourcePool.ID, capabilityInUseError.ResourcePoolIds.Single());

                return;
            }

            Assert.Fail();
        }

        [TestMethod]
        public void DeleteCapability_PartOfResourcePoolOrchestrationEvents_ThrowsException()
        {
            var prefix = Guid.NewGuid();
            var capability = new Capability
            {
                Name = $"{prefix}_Capability",
            }.SetDiscretes(["A", "B", "C"]);

            objectCreator.CreateCapabilities([capability]);

            var resourcePool = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };

            resourcePool.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
            {
                new OrchestrationEvent
                {
                    EventType = OrchestrationEventType.PrerollStart,
                    ExecutionDetails = new ScriptExecutionDetails("SomeScript").AddCapability(new CapabilitySetting(capability) { Value = capability.Discretes.First() })
                },
            });
            objectCreator.CreateResourcePool(resourcePool);

            try
            {
                TestContext.Api.Capabilities.Delete(capability);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Capability '{capability.Name}' is in use by 1 resource pool(s).";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

                var capabilityUseError = ex.TraceData.ErrorData.Single() as CapabilityInUseByResourcePoolsError;
                Assert.IsNotNull(capabilityUseError);
                Assert.AreEqual(errorMessage, capabilityUseError.ErrorMessage);
                Assert.IsNotNull(capabilityUseError.ResourcePoolIds);
                Assert.AreEqual(1, capabilityUseError.ResourcePoolIds.Count());
                Assert.AreEqual(resourcePool.ID, capabilityUseError.ResourcePoolIds.Single());

                return;
            }

            Assert.Fail();
        }
    }
}
