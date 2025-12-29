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
            Assert.IsTrue(expectedException.Result.SuccessfulIds.Contains(capacity3.Id));

            Assert.AreEqual(2, expectedException.Result.UnsuccessfulIds.Count);
            Assert.IsTrue(expectedException.Result.UnsuccessfulIds.Contains(capacity1.Id));
            Assert.IsTrue(expectedException.Result.UnsuccessfulIds.Contains(capacity2.Id));

            var storedCapacity3 = TestContext.Api.Capacities.Read(capacity3.Id);
            Assert.IsNull(storedCapacity3);

            expectedException.Result.TraceDataPerItem.TryGetValue(capacity1.Id, out var traceData1);
            Assert.IsNotNull(traceData1);
            Assert.AreEqual(1, traceData1.ErrorData.Count);
            var capacityInUseError = traceData1.ErrorData.OfType<CapacityInUseError>().SingleOrDefault();
            Assert.IsNotNull(capacityInUseError);
            Assert.AreEqual($"Capacity '{capacity1.Name}' is in use by 2 resource(s) and cannot be deleted.", capacityInUseError.ErrorMessage);
            Assert.AreEqual(2, capacityInUseError.ResourceIds.Count);
            Assert.IsTrue(capacityInUseError.ResourceIds.Contains(unmangedResource1.Id));
            Assert.IsTrue(capacityInUseError.ResourceIds.Contains(unmangedResource2.Id));

            expectedException.Result.TraceDataPerItem.TryGetValue(capacity2.Id, out var traceData2);
            Assert.IsNotNull(traceData2);
            Assert.AreEqual(1, traceData2.ErrorData.Count);
            capacityInUseError = traceData2.ErrorData.OfType<CapacityInUseError>().SingleOrDefault();
            Assert.IsNotNull(capacityInUseError);
            Assert.AreEqual($"Capacity '{capacity2.Name}' is in use by 1 resource(s) and cannot be deleted.", capacityInUseError.ErrorMessage);
            Assert.AreEqual(1, capacityInUseError.ResourceIds.Count);
            Assert.IsTrue(capacityInUseError.ResourceIds.Contains(unmangedResource1.Id));
        }
    }
}
