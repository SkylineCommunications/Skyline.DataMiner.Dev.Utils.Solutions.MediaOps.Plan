namespace RT_MediaOps.Plan.RST.Capacities
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
        public void WhenAssignedToResourcesThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capacity1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity1",
            };
            var capacity2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacity()
            {
                Name = $"{prefix}_Capacity2",
            };
            var capacity3 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity3",
            };

            objectCreator.CreateCapacities([capacity1, capacity2, capacity3]);

            var unmangedResource1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource1",
            }
            .AddCapacity(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity1)
            {
                Value = 10,
            })
            .AddCapacity(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacitySetting(capacity2)
            {
                MinValue = 20,
                MaxValue = 30,
            });

            var unmangedResource2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource2",
            }
            .AddCapacity(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity1)
            {
                Value = 50,
            });

            objectCreator.CreateResources([unmangedResource1, unmangedResource2]);

            MediaOpsBulkException<Guid>? expectedException = null;
            try
            {
                TestContext.Api.Capacities.Delete([capacity1, capacity2, capacity3]);
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
            Assert.IsTrue(expectedException.Result.SuccessfulIds.Contains(capacity3.ID));

            Assert.AreEqual(2, expectedException.Result.UnsuccessfulIds.Count);
            Assert.IsTrue(expectedException.Result.UnsuccessfulIds.Contains(capacity1.ID));
            Assert.IsTrue(expectedException.Result.UnsuccessfulIds.Contains(capacity2.ID));

            var storedCapacity3 = TestContext.Api.Capacities.Read(capacity3.ID);
            Assert.IsNull(storedCapacity3);

            expectedException.Result.TraceDataPerItem.TryGetValue(capacity1.ID, out var traceData1);
            Assert.IsNotNull(traceData1);
            Assert.AreEqual(1, traceData1.ErrorData.Count);

            var capacityInUseByResourcesError = traceData1.ErrorData.OfType<CapacityInUseByResourcesError>().SingleOrDefault();
            Assert.IsNotNull(capacityInUseByResourcesError);
            Assert.AreEqual($"Capacity '{capacity1.Name}' is in use by 2 resource(s).", capacityInUseByResourcesError.ErrorMessage);
            Assert.AreEqual(2, capacityInUseByResourcesError.ResourceIds.Count);
            Assert.IsTrue(capacityInUseByResourcesError.ResourceIds.Contains(unmangedResource1.ID));
            Assert.IsTrue(capacityInUseByResourcesError.ResourceIds.Contains(unmangedResource2.ID));

            expectedException.Result.TraceDataPerItem.TryGetValue(capacity2.ID, out var traceData2);
            Assert.IsNotNull(traceData2);
            Assert.AreEqual(1, traceData2.ErrorData.Count);

            capacityInUseByResourcesError = traceData2.ErrorData.OfType<CapacityInUseByResourcesError>().SingleOrDefault();
            Assert.IsNotNull(capacityInUseByResourcesError);
            Assert.AreEqual($"Capacity '{capacity2.Name}' is in use by 1 resource(s).", capacityInUseByResourcesError.ErrorMessage);
            Assert.AreEqual(1, capacityInUseByResourcesError.ResourceIds.Count);
            Assert.IsTrue(capacityInUseByResourcesError.ResourceIds.Contains(unmangedResource1.ID));
        }
    }
}
