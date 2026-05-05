namespace RT_MediaOps.Plan.RST.ResourceProperties
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

			var property1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty()
			{
				Name = $"{prefix}_Property1",
			};
			var property2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty()
			{
				Name = $"{prefix}_Property2",
			};
			var property3 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty()
			{
				Name = $"{prefix}_Property3",
			};

			objectCreator.CreateResourceProperties([property1, property2, property3]);

			var unmangedResource1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
			{
				Name = $"{prefix}_Resource1",
			};
			unmangedResource1.AddProperty(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertySettings(property1)
			{
				Value = "Test",
			});
			unmangedResource1.AddProperty(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertySettings(property2)
			{
				Value = "Test",
			});

			var unmangedResource2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
			{
				Name = $"{prefix}_Resource2",
			};
			unmangedResource2.AddProperty(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePropertySettings(property1)
			{
				Value = "Test",
			});

			objectCreator.CreateResources([unmangedResource1, unmangedResource2]);

			MediaOpsBulkException<Guid>? expectedException = null;
			try
			{
				TestContext.Api.ResourceProperties.Delete([property1, property2, property3]);
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
			Assert.IsTrue(expectedException.Result.SuccessfulIds.Contains(property3.Id));

			Assert.AreEqual(2, expectedException.Result.UnsuccessfulIds.Count);
			Assert.IsTrue(expectedException.Result.UnsuccessfulIds.Contains(property1.Id));
			Assert.IsTrue(expectedException.Result.UnsuccessfulIds.Contains(property2.Id));

			property3 = TestContext.Api.ResourceProperties.Read(property3.Id);
			Assert.IsNull(property3);

			expectedException.Result.TraceDataPerItem.TryGetValue(property1.Id, out var traceData1);
			Assert.IsNotNull(traceData1);
			Assert.AreEqual(1, traceData1.ErrorData.Count);
			var resourcePropertyInUseError = traceData1.ErrorData.OfType<ResourcePropertyInUseError>().SingleOrDefault();
			Assert.IsNotNull(resourcePropertyInUseError);
			Assert.AreEqual($"Resource property '{property1.Name}' is in use by 2 resource(s).", resourcePropertyInUseError.ErrorMessage);
			Assert.AreEqual(2, resourcePropertyInUseError.ResourceIds.Count);
			Assert.IsTrue(resourcePropertyInUseError.ResourceIds.Contains(unmangedResource1.Id));
			Assert.IsTrue(resourcePropertyInUseError.ResourceIds.Contains(unmangedResource2.Id));

			expectedException.Result.TraceDataPerItem.TryGetValue(property2.Id, out var traceData2);
			Assert.IsNotNull(traceData2);
			Assert.AreEqual(1, traceData2.ErrorData.Count);
			resourcePropertyInUseError = traceData2.ErrorData.OfType<ResourcePropertyInUseError>().SingleOrDefault();
			Assert.IsNotNull(resourcePropertyInUseError);
			Assert.AreEqual($"Resource property '{property2.Name}' is in use by 1 resource(s).", resourcePropertyInUseError.ErrorMessage);
			Assert.AreEqual(1, resourcePropertyInUseError.ResourceIds.Count);
			Assert.IsTrue(resourcePropertyInUseError.ResourceIds.Contains(unmangedResource1.Id));
		}
	}
}
