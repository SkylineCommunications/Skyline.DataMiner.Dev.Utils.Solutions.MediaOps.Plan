namespace RT_MediaOps.Plan.RST.Capabilities
{
    using System;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class AssignmentTests : IDisposable
    {
        private readonly TestObjectCreator objectCreator;

        public AssignmentTests()
        {
            objectCreator = new TestObjectCreator(TestContext);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
        }

        [TestMethod]
        public void RemovingUsedDiscreteValueThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capability = new Capability()
            {
                Name = $"{prefix}_Capability",
            }
            .SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapability(capability);

            var unmagendResource = new UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            }
            .AddCapability(new CapabilitySetting(capability.Id).SetDiscretes(new[] { "Value 1", "Value 2" }));
            objectCreator.CreateResource(unmagendResource);

            var resourcePool1 = new ResourcePool()
            {
                Name = $"{prefix}_ResourcePool1",
            }
            .AddCapability(new CapabilitySetting(capability.Id).SetDiscretes(new[] { "Value 1" }));
            resourcePool1.OrchestrationSettings
                .AddCapability(new CapabilitySetting(capability.Id).SetDiscretes(new[] { "Value 2" }))
                .AddOrchestrationEvent(new OrchestrationEvent()
                {
                    EventType = OrchestrationEventType.PrerollStart,
                    ExecutionDetails = new ScriptExecutionDetails("script 1")
                    .AddCapability(new CapabilitySetting(capability.Id).SetDiscretes(new[] { "Value 3" })),
                });

            var resourcePool2 = new ResourcePool()
            {
                Name = $"{prefix}_ResourcePool2",
            }
            .AddCapability(new CapabilitySetting(capability.Id).SetDiscretes(new[] { "Value 2" }));
            resourcePool2.OrchestrationSettings
                .AddCapability(new CapabilitySetting(capability.Id).SetDiscretes(new[] { "Value 3" }))
                .AddOrchestrationEvent(new OrchestrationEvent()
                {
                    EventType = OrchestrationEventType.PrerollStart,
                    ExecutionDetails = new ScriptExecutionDetails("script 1")
                    .AddCapability(new CapabilitySetting(capability.Id).SetDiscretes(new[] { "Value 1" })),
                });

            var resourcePool3 = new ResourcePool()
            {
                Name = $"{prefix}_ResourcePool3",
            }
            .AddCapability(new CapabilitySetting(capability.Id).SetDiscretes(new[] { "Value 3" }));
            resourcePool3.OrchestrationSettings
                .AddCapability(new CapabilitySetting(capability.Id).SetDiscretes(new[] { "Value 1" }))
                .AddOrchestrationEvent(new OrchestrationEvent()
                {
                    EventType = OrchestrationEventType.PrerollStart,
                    ExecutionDetails = new ScriptExecutionDetails("script 1")
                    .AddCapability(new CapabilitySetting(capability.Id).SetDiscretes(new[] { "Value 2" })),
                });

            objectCreator.CreateResourcePools([resourcePool1, resourcePool2, resourcePool3]);

            capability = TestContext.Api.Capabilities.Read(capability.Id);
            capability.RemoveDiscrete("Value 2");

            MediaOpsException expectedException = null;
            try
            {
                TestContext.Api.Capabilities.Update(capability);
            }
            catch (MediaOpsException ex)
            {
                expectedException = ex;
            }

            if (expectedException == null)
            {
                Assert.Fail("Expected exception was not thrown.");
            }

            Assert.AreEqual(2, expectedException.TraceData.ErrorData.Count);
            var capabilityDiscreteValueInUseErrors = expectedException.TraceData.ErrorData.OfType<CapabilityDiscreteValueInUseError>().ToList();
            Assert.AreEqual(2, capabilityDiscreteValueInUseErrors.Count);

            var capabilityDiscreteValueInUseByResourceError = capabilityDiscreteValueInUseErrors.OfType<CapabilityDiscreteValueInUseByResourcesError>().SingleOrDefault();
            Assert.IsNotNull(capabilityDiscreteValueInUseByResourceError);
            Assert.AreEqual("Value 2", capabilityDiscreteValueInUseByResourceError.DiscreteValue);
            Assert.AreEqual(1, capabilityDiscreteValueInUseByResourceError.ResourceIds.Count);
            Assert.IsTrue(capabilityDiscreteValueInUseByResourceError.ResourceIds.Contains(unmagendResource.Id));

            var capabilityDiscreteValueInUseByResourcePoolError = capabilityDiscreteValueInUseErrors.OfType<CapabilityDiscreteValueInUseByResourcePoolsError>().SingleOrDefault();
            Assert.IsNotNull(capabilityDiscreteValueInUseByResourcePoolError);
            Assert.AreEqual("Value 2", capabilityDiscreteValueInUseByResourcePoolError.DiscreteValue);
            Assert.AreEqual(3, capabilityDiscreteValueInUseByResourcePoolError.ResourcePoolIds.Count);
            Assert.IsTrue(capabilityDiscreteValueInUseByResourcePoolError.ResourcePoolIds.Contains(resourcePool1.Id));
            Assert.IsTrue(capabilityDiscreteValueInUseByResourcePoolError.ResourcePoolIds.Contains(resourcePool2.Id));
            Assert.IsTrue(capabilityDiscreteValueInUseByResourcePoolError.ResourcePoolIds.Contains(resourcePool3.Id));
        }
    }
}
