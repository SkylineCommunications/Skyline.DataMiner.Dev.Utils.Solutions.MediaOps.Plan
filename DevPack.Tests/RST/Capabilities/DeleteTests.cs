namespace RT_MediaOps.Plan.RST.Capabilities
{
    using System;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

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

            var capability1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability1",
            }
            .SetDiscretes(new[] { "Option1", "Option2" });
            var capability2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability2",
            }
            .SetDiscretes(new[] { "Option1", "Option2" });
            var capability3 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability3",
            }
            .SetDiscretes(new[] { "Option1", "Option2" });

            objectCreator.CreateCapabilities([capability1, capability2, capability3]);

            var unmangedResource1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource1",
            }
            .AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySetting(capability1).AddDiscrete("Option1"))
            .AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySetting(capability2).AddDiscrete("Option2"));

            var unmangedResource2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource2",
            }
            .AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySetting(capability1).AddDiscrete("Option2"));

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
            Assert.IsTrue(expectedException.Result.SuccessfulIds.Contains(capability3.Id));

            Assert.AreEqual(2, expectedException.Result.UnsuccessfulIds.Count);
            Assert.IsTrue(expectedException.Result.UnsuccessfulIds.Contains(capability1.Id));
            Assert.IsTrue(expectedException.Result.UnsuccessfulIds.Contains(capability2.Id));

            capability3 = TestContext.Api.Capabilities.Read(capability3.Id);
            Assert.IsNull(capability3);

            expectedException.Result.TraceDataPerItem.TryGetValue(capability1.Id, out var traceData1);
            Assert.IsNotNull(traceData1);
            Assert.AreEqual(1, traceData1.ErrorData.Count);
            var capabilityInUseError = traceData1.ErrorData.OfType<CapabilityInUseError>().SingleOrDefault();
            Assert.IsNotNull(capabilityInUseError);
            Assert.AreEqual($"Capability '{capability1.Name}' is in use by 2 resource(s) and cannot be deleted.", capabilityInUseError.ErrorMessage);
            Assert.AreEqual(0, capabilityInUseError.ResourcePoolIds.Count);
            Assert.AreEqual(2, capabilityInUseError.ResourceIds.Count);
            Assert.IsTrue(capabilityInUseError.ResourceIds.Contains(unmangedResource1.Id));
            Assert.IsTrue(capabilityInUseError.ResourceIds.Contains(unmangedResource2.Id));

            expectedException.Result.TraceDataPerItem.TryGetValue(capability2.Id, out var traceData2);
            Assert.IsNotNull(traceData2);
            Assert.AreEqual(1, traceData2.ErrorData.Count);
            capabilityInUseError = traceData2.ErrorData.OfType<CapabilityInUseError>().SingleOrDefault();
            Assert.IsNotNull(capabilityInUseError);
            Assert.AreEqual($"Capability '{capability2.Name}' is in use by 1 resource(s) and cannot be deleted.", capabilityInUseError.ErrorMessage);
            Assert.AreEqual(0, capabilityInUseError.ResourcePoolIds.Count);
            Assert.AreEqual(1, capabilityInUseError.ResourceIds.Count);
            Assert.IsTrue(capabilityInUseError.ResourceIds.Contains(unmangedResource1.Id));
        }

        [TestMethod]
        public void DeleteCapabilityWhenAssignedToResourcePoolsThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capability1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability1",
            }
            .SetDiscretes(new[] { "Option1", "Option2" });
            var capability2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability2",
            }
            .SetDiscretes(new[] { "Option1", "Option2" });
            var capability3 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability3",
            }
            .SetDiscretes(new[] { "Option1", "Option2" });

            objectCreator.CreateCapabilities([capability1, capability2, capability3]);

            var pool1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool1",
            }
            .AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySetting(capability1).AddDiscrete("Option1"))
            .AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySetting(capability2).AddDiscrete("Option2"));

            var pool2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool2",
            }
            .AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySetting(capability1).AddDiscrete("Option2"));

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
            Assert.IsTrue(expectedException.Result.SuccessfulIds.Contains(capability3.Id));

            Assert.AreEqual(2, expectedException.Result.UnsuccessfulIds.Count);
            Assert.IsTrue(expectedException.Result.UnsuccessfulIds.Contains(capability1.Id));
            Assert.IsTrue(expectedException.Result.UnsuccessfulIds.Contains(capability2.Id));

            capability3 = TestContext.Api.Capabilities.Read(capability3.Id);
            Assert.IsNull(capability3);

            expectedException.Result.TraceDataPerItem.TryGetValue(capability1.Id, out var traceData1);
            Assert.IsNotNull(traceData1);
            Assert.AreEqual(1, traceData1.ErrorData.Count);
            var capabilityInUseError = traceData1.ErrorData.OfType<CapabilityInUseError>().SingleOrDefault();
            Assert.IsNotNull(capabilityInUseError);
            Assert.AreEqual($"Capability '{capability1.Name}' is in use by 2 resource pool(s) and cannot be deleted.", capabilityInUseError.ErrorMessage);
            Assert.AreEqual(0, capabilityInUseError.ResourceIds.Count);
            Assert.AreEqual(2, capabilityInUseError.ResourcePoolIds.Count);
            Assert.IsTrue(capabilityInUseError.ResourcePoolIds.Contains(pool1.Id));
            Assert.IsTrue(capabilityInUseError.ResourcePoolIds.Contains(pool2.Id));

            expectedException.Result.TraceDataPerItem.TryGetValue(capability2.Id, out var traceData2);
            Assert.IsNotNull(traceData2);
            Assert.AreEqual(1, traceData2.ErrorData.Count);
            capabilityInUseError = traceData2.ErrorData.OfType<CapabilityInUseError>().SingleOrDefault();
            Assert.IsNotNull(capabilityInUseError);
            Assert.AreEqual($"Capability '{capability2.Name}' is in use by 1 resource pool(s) and cannot be deleted.", capabilityInUseError.ErrorMessage);
            Assert.AreEqual(0, capabilityInUseError.ResourceIds.Count);
            Assert.AreEqual(1, capabilityInUseError.ResourcePoolIds.Count);
            Assert.IsTrue(capabilityInUseError.ResourcePoolIds.Contains(pool1.Id));
        }

        [TestMethod]
        public void DeleteCapabilityWhenAssignedToResourcesAndResourcePoolsThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capability1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability1",
            }
            .SetDiscretes(new[] { "Option1", "Option2" });
            var capability2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability2",
            }
            .SetDiscretes(new[] { "Option1", "Option2" });
            var capability3 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability3",
            }
            .SetDiscretes(new[] { "Option1", "Option2" });

            objectCreator.CreateCapabilities([capability1, capability2, capability3]);

            var unmangedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            }
            .AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySetting(capability1).AddDiscrete("Option1"))
            .AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySetting(capability2).AddDiscrete("Option2"));

            objectCreator.CreateResource(unmangedResource);

            var pool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            }
            .AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySetting(capability1).AddDiscrete("Option2"));

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
            Assert.IsTrue(expectedException.Result.SuccessfulIds.Contains(capability3.Id));

            Assert.AreEqual(2, expectedException.Result.UnsuccessfulIds.Count);
            Assert.IsTrue(expectedException.Result.UnsuccessfulIds.Contains(capability1.Id));
            Assert.IsTrue(expectedException.Result.UnsuccessfulIds.Contains(capability2.Id));

            capability3 = TestContext.Api.Capabilities.Read(capability3.Id);
            Assert.IsNull(capability3);

            expectedException.Result.TraceDataPerItem.TryGetValue(capability1.Id, out var traceData1);
            Assert.IsNotNull(traceData1);
            Assert.AreEqual(1, traceData1.ErrorData.Count);
            var capabilityInUseError = traceData1.ErrorData.OfType<CapabilityInUseError>().SingleOrDefault();
            Assert.IsNotNull(capabilityInUseError);
            Assert.AreEqual($"Capability '{capability1.Name}' is in use by 1 resource(s) and 1 resource pool(s) and cannot be deleted.", capabilityInUseError.ErrorMessage);
            Assert.AreEqual(1, capabilityInUseError.ResourceIds.Count);
            Assert.IsTrue(capabilityInUseError.ResourceIds.Contains(unmangedResource.Id));
            Assert.AreEqual(1, capabilityInUseError.ResourcePoolIds.Count);
            Assert.IsTrue(capabilityInUseError.ResourcePoolIds.Contains(pool.Id));

            expectedException.Result.TraceDataPerItem.TryGetValue(capability2.Id, out var traceData2);
            Assert.IsNotNull(traceData2);
            Assert.AreEqual(1, traceData2.ErrorData.Count);
            capabilityInUseError = traceData2.ErrorData.OfType<CapabilityInUseError>().SingleOrDefault();
            Assert.IsNotNull(capabilityInUseError);
            Assert.AreEqual($"Capability '{capability2.Name}' is in use by 1 resource(s) and cannot be deleted.", capabilityInUseError.ErrorMessage);
            Assert.AreEqual(0, capabilityInUseError.ResourcePoolIds.Count);
            Assert.AreEqual(1, capabilityInUseError.ResourceIds.Count);
            Assert.IsTrue(capabilityInUseError.ResourceIds.Contains(unmangedResource.Id));
        }
    }
}
