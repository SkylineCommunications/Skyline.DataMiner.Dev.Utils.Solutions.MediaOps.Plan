namespace RT_MediaOps.Plan.RST.Resources
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class ResourcePoolUnassignmentUsageTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public ResourcePoolUnassignmentUsageTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void Unassign_ResourceUsedFromPoolByJob_IsRefused()
		{
			var prefix = Guid.NewGuid();
			var pool = CreateCompletePool($"{prefix}_Pool");
			var resource = CreateCompleteResource($"{prefix}_Resource", pool);
			var job = CreateTentativeJob($"{prefix}_Job", new JobResourceNode(pool, resource));

			var ex = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => TestContext.Api.ResourcePools.UnassignResourcesFromPool(pool.Id, [resource]));

			Assert.IsTrue(ex.Result.TraceDataPerItem.TryGetValue(resource.Id, out var traceData));
			var error = traceData.ErrorData.OfType<ResourcePoolAssignmentInUseByJobsError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(resource.Id, error.Id);
			Assert.AreEqual(pool.Id, error.ResourcePoolId);
			CollectionAssert.AreEqual(new[] { job.Id }, error.JobIds.ToArray());

			AssertAssignedPools(resource.Id, pool.Id);
		}

		[TestMethod]
		public void Update_ResourceRemovedFromPoolUsedByWorkflow_IsRefused()
		{
			var prefix = Guid.NewGuid();
			var pool = CreateCompletePool($"{prefix}_Pool");
			var resource = CreateCompleteResource($"{prefix}_Resource", pool);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			workflow.NodeGraph.Add(new WorkflowResourceNode(pool, resource));
			workflow = objectCreator.CreateWorkflow(workflow);

			resource.UnassignFromPool(pool);
			var ex = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Resources.Update(resource));

			var error = ex.TraceData.ErrorData.OfType<ResourcePoolAssignmentInUseByWorkflowsError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(pool.Id, error.ResourcePoolId);
			CollectionAssert.AreEqual(new[] { workflow.Id }, error.WorkflowIds.ToArray());

			AssertAssignedPools(resource.Id, pool.Id);
		}

		[TestMethod]
		public void Unassign_ResourceUsedFromOtherPool_IsAllowed()
		{
			var prefix = Guid.NewGuid();
			var usedPool = CreateCompletePool($"{prefix}_UsedPool");
			var otherPool = CreateCompletePool($"{prefix}_OtherPool");
			var resource = CreateCompleteResource($"{prefix}_Resource", usedPool, otherPool);
			CreateTentativeJob($"{prefix}_Job", new JobResourceNode(usedPool, resource));

			TestContext.Api.ResourcePools.UnassignResourcesFromPool(otherPool.Id, [resource]);

			AssertAssignedPools(resource.Id, usedPool.Id);
		}

		[TestMethod]
		public void Unassign_PoolOnlyUsedByResourcePoolNode_IsAllowed()
		{
			var prefix = Guid.NewGuid();
			var pool = CreateCompletePool($"{prefix}_Pool");
			var resource = CreateCompleteResource($"{prefix}_Resource", pool);
			CreateCompleteResource($"{prefix}_OtherResource", pool);
			CreateTentativeJob($"{prefix}_Job", new JobResourcePoolNode(pool));

			TestContext.Api.ResourcePools.UnassignResourcesFromPool(pool.Id, [resource]);

			AssertAssignedPools(resource.Id);
		}

		[TestMethod]
		public void Unassign_ResourceUsedByCanceledJob_IsAllowed()
		{
			var prefix = Guid.NewGuid();
			var pool = CreateCompletePool($"{prefix}_Pool");
			var resource = CreateCompleteResource($"{prefix}_Resource", pool);
			var job = CreateTentativeJob($"{prefix}_Job", new JobResourceNode(pool, resource));
			TestContext.Api.Jobs.Cancel(job);

			TestContext.Api.ResourcePools.UnassignResourcesFromPool(pool.Id, [resource]);

			AssertAssignedPools(resource.Id);
		}

		[TestMethod]
		public void Unassign_MixedResources_OnlyUnusedResourcesAreUnassigned()
		{
			var prefix = Guid.NewGuid();
			var pool = CreateCompletePool($"{prefix}_Pool");
			var usedResource = CreateCompleteResource($"{prefix}_UsedResource", pool);
			var unusedResource = CreateCompleteResource($"{prefix}_UnusedResource", pool);
			CreateTentativeJob($"{prefix}_Job", new JobResourceNode(pool, usedResource));

			var ex = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => TestContext.Api.ResourcePools.UnassignResourcesFromPool(pool.Id, [usedResource, unusedResource]));

			CollectionAssert.AreEquivalent(new[] { usedResource.Id }, ex.Result.UnsuccessfulIds.ToArray());
			CollectionAssert.AreEquivalent(new[] { unusedResource.Id }, ex.Result.SuccessfulIds.ToArray());
			AssertAssignedPools(usedResource.Id, pool.Id);
			AssertAssignedPools(unusedResource.Id);
		}

		private static void AssertAssignedPools(Guid resourceId, params Guid[] expectedPoolIds)
		{
			var storedResource = TestContext.Api.Resources.Read(resourceId);
			CollectionAssert.AreEquivalent(expectedPoolIds, storedResource.ResourcePoolIds.ToArray());
		}

		private ResourcePool CreateCompletePool(string name)
		{
			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = name });
			return TestContext.Api.ResourcePools.Complete(pool);
		}

		private Resource CreateCompleteResource(string name, params ResourcePool[] pools)
		{
			var resource = new UnmanagedResource { Name = name };
			foreach (var pool in pools)
			{
				resource.AssignToPool(pool);
			}

			resource = objectCreator.CreateResource(resource);
			return TestContext.Api.Resources.Complete(resource);
		}

		private Job CreateTentativeJob(string name, JobNode node)
		{
			var currentTime = DateTime.UtcNow.RoundToNextSecond();
			var job = new Job
			{
				Name = name,
				Start = currentTime.AddHours(1),
				End = currentTime.AddHours(2),
				PreRollStart = currentTime.AddHours(1),
				PostRollEnd = currentTime.AddHours(2),
			};
			job.NodeGraph.Add(node);

			job = objectCreator.CreateJob(job);
			return TestContext.Api.Jobs.SaveAsTentative(job);
		}
	}
}
