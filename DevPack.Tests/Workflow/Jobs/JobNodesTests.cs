namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class JobNodesTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public JobNodesTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void CreateJob_WithResourcePoolNode_RoundTripPersistsNodeAndTimings()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var node = new JobResourcePoolNode(pool) { Alias = "PoolNode", IconImage = "icon-pool" };

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
			};
			job.NodeGraph.Add(node);

			job = objectCreator.CreateJob(job);

			Assert.IsNotNull(job);
			Assert.AreEqual(1, job.NodeGraph.Nodes.Count);

			var roundTrippedNode = job.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Single();
			Assert.AreEqual(pool.Id, roundTrippedNode.ResourcePoolId);
			Assert.AreEqual("PoolNode", roundTrippedNode.Alias);
			Assert.AreEqual("icon-pool", roundTrippedNode.IconImage);
			Assert.AreEqual(job.Start, roundTrippedNode.Start);
			Assert.AreEqual(job.End, roundTrippedNode.End);

			// Re-read from API to make sure persistence is consistent (not just the returned in-memory instance).
			var reread = TestContext.Api.Jobs.Read(job.Id);
			Assert.IsNotNull(reread);
			var rereadNode = reread.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Single();
			Assert.AreEqual(pool.Id, rereadNode.ResourcePoolId);
			Assert.AreEqual("PoolNode", rereadNode.Alias);
			Assert.AreEqual("icon-pool", rereadNode.IconImage);
			Assert.AreEqual(job.Start, rereadNode.Start);
			Assert.AreEqual(job.End, rereadNode.End);
		}

		[TestMethod]
		public void CreateJob_WithResourceNode_RoundTripPersistsNodeAndTimings()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var node = new JobResourceNode(pool, resource) { Alias = "ResourceNode", IconImage = "icon-res" };

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(5),
			};
			job.NodeGraph.Add(node);

			job = objectCreator.CreateJob(job);

			Assert.IsNotNull(job);
			Assert.AreEqual(1, job.NodeGraph.Nodes.Count);

			var roundTrippedNode = job.NodeGraph.Nodes.OfType<JobResourceNode>().Single();
			Assert.AreEqual(pool.Id, roundTrippedNode.ResourcePoolId);
			Assert.AreEqual(resource.Id, roundTrippedNode.ResourceId);
			Assert.AreEqual("ResourceNode", roundTrippedNode.Alias);
			Assert.AreEqual("icon-res", roundTrippedNode.IconImage);
			Assert.AreEqual(job.Start, roundTrippedNode.Start);
			Assert.AreEqual(job.End, roundTrippedNode.End);
		}

		[TestMethod]
		public void CreateJob_WithMultipleNodesAndConnection_PersistsGraphStructure()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var resourceNode = new JobResourceNode(pool, resource) { Alias = "RN" };
			var poolNode = new JobResourcePoolNode(pool) { Alias = "PN" };

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(5),
			};
			job.NodeGraph
				.Add(resourceNode)
				.Add(poolNode)
				.Connect(resourceNode, poolNode);

			job = objectCreator.CreateJob(job);

			Assert.AreEqual(2, job.NodeGraph.Nodes.Count);
			Assert.AreEqual(1, job.NodeGraph.Connections.Count);

			var reread = TestContext.Api.Jobs.Read(job.Id);
			Assert.AreEqual(2, reread.NodeGraph.Nodes.Count);
			Assert.AreEqual(1, reread.NodeGraph.Connections.Count);

			var connection = reread.NodeGraph.Connections.Single();
			Assert.IsInstanceOfType(connection.From, typeof(JobResourceNode));
			Assert.IsInstanceOfType(connection.To, typeof(JobResourcePoolNode));
		}

		[TestMethod]
		public void UpdateJob_AddingNewNode_PersistsAddedNode()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(5),
			};
			job.NodeGraph.Add(new JobResourcePoolNode(pool) { Alias = "First" });
			job = objectCreator.CreateJob(job);

			job.NodeGraph.Add(new JobResourcePoolNode(pool) { Alias = "Second" });
			job = TestContext.Api.Jobs.Update(job);

			Assert.AreEqual(2, job.NodeGraph.Nodes.Count);
			CollectionAssert.AreEquivalent(
				new[] { "First", "Second" },
				job.NodeGraph.Nodes.Select(n => n.Alias).ToList());

			var reread = TestContext.Api.Jobs.Read(job.Id);
			Assert.AreEqual(2, reread.NodeGraph.Nodes.Count);
			CollectionAssert.AreEquivalent(
				new[] { "First", "Second" },
				reread.NodeGraph.Nodes.Select(n => n.Alias).ToList());
		}

		[TestMethod]
		public void UpdateJob_RemovingNode_PersistsRemoval()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var nodeToKeep = new JobResourcePoolNode(pool) { Alias = "Keep" };
			var nodeToRemove = new JobResourcePoolNode(pool) { Alias = "Remove" };

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(5),
			};
			job.NodeGraph.Add(nodeToKeep).Add(nodeToRemove);
			job = objectCreator.CreateJob(job);

			var persistedNodeToRemove = job.NodeGraph.Nodes.Single(n => n.Alias == "Remove");
			job.NodeGraph.Remove(persistedNodeToRemove);

			job = TestContext.Api.Jobs.Update(job);

			Assert.AreEqual(1, job.NodeGraph.Nodes.Count);
			Assert.AreEqual("Keep", job.NodeGraph.Nodes.Single().Alias);

			var reread = TestContext.Api.Jobs.Read(job.Id);
			Assert.AreEqual(1, reread.NodeGraph.Nodes.Count);
			Assert.AreEqual("Keep", reread.NodeGraph.Nodes.Single().Alias);
		}

		[TestMethod]
		public void UpdateJob_ChangingNodeAliasAndIcon_PersistsChanges()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var node = new JobResourcePoolNode(pool) { Alias = "Original", IconImage = "icon-1" };

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(5),
			};
			job.NodeGraph.Add(node);
			job = objectCreator.CreateJob(job);

			var persistedNode = job.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Single();
			persistedNode.Alias = "Updated";
			persistedNode.IconImage = "icon-2";

			job = TestContext.Api.Jobs.Update(job);

			var reread = TestContext.Api.Jobs.Read(job.Id);
			var rereadNode = reread.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Single();
			Assert.AreEqual("Updated", rereadNode.Alias);
			Assert.AreEqual("icon-2", rereadNode.IconImage);
		}

		[TestMethod]
		public void UpdateJob_ChangingJobStartAndEnd_PropagatesToExistingNodes()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(5),
			};
			job.NodeGraph.Add(new JobResourcePoolNode(pool));
			job = objectCreator.CreateJob(job);

			var newStart = currentTime.AddMinutes(30);
			var newEnd = newStart.AddMinutes(10);
			job.Start = newStart;
			job.End = newEnd;
			job = TestContext.Api.Jobs.Update(job);

			var reread = TestContext.Api.Jobs.Read(job.Id);
			Assert.AreEqual(newStart, reread.Start);
			Assert.AreEqual(newEnd, reread.End);

			var rereadNode = reread.NodeGraph.Nodes.Single();
			Assert.AreEqual(newStart, rereadNode.Start);
			Assert.AreEqual(newEnd, rereadNode.End);
		}

		[TestMethod]
		public void JobNode_NewInstance_HasDefaultStateValues()
		{
			var node = new JobResourcePoolNode(Guid.NewGuid());

			Assert.AreEqual(ResourceSelectionState.Unknown, node.ResourceSelectionState);
			Assert.AreEqual(NodeConfigurationStatus.Unknown, node.NodeConfigurationStatus);
			Assert.AreEqual(default(DateTimeOffset), node.Start);
			Assert.AreEqual(default(DateTimeOffset), node.End);
		}

		[TestMethod]
		public void JobNode_IsResourcePoolNode_ReturnsTrueAndExposesTypedNode()
		{
			var poolId = Guid.NewGuid();
			JobNode node = new JobResourcePoolNode(poolId);

			Assert.IsTrue(node.IsResourcePoolNode(out var poolNode));
			Assert.IsNotNull(poolNode);
			Assert.AreEqual(poolId, poolNode.ResourcePoolId);

			Assert.IsFalse(node.IsResourceNode(out var resourceNode));
			Assert.IsNull(resourceNode);
		}

		[TestMethod]
		public void JobNode_IsResourceNode_ReturnsTrueAndExposesTypedNode()
		{
			var poolId = Guid.NewGuid();
			var resourceId = Guid.NewGuid();
			JobNode node = new JobResourceNode(poolId, resourceId);

			Assert.IsTrue(node.IsResourceNode(out var resourceNode));
			Assert.IsNotNull(resourceNode);
			Assert.AreEqual(poolId, resourceNode.ResourcePoolId);
			Assert.AreEqual(resourceId, resourceNode.ResourceId);

			Assert.IsFalse(node.IsResourcePoolNode(out var poolNode));
			Assert.IsNull(poolNode);
		}

		[TestMethod]
		public void JobResourcePoolNode_NullResourcePool_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new JobResourcePoolNode((ResourcePool)null));
		}

		[TestMethod]
		public void JobResourcePoolNode_EmptyResourcePoolId_Throws()
		{
			Assert.ThrowsException<ArgumentException>(() => new JobResourcePoolNode(Guid.Empty));
		}

		[TestMethod]
		public void JobResourceNode_NullResource_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new JobResourceNode(Guid.NewGuid(), (Resource)null));
		}

		[TestMethod]
		public void JobResourceNode_NullResourcePool_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new JobResourceNode((ResourcePool)null, Guid.NewGuid()));
		}

		[TestMethod]
		public void JobResourceNode_EmptyIds_Throws()
		{
			Assert.ThrowsException<ArgumentException>(() => new JobResourceNode(Guid.Empty, Guid.NewGuid()));
			Assert.ThrowsException<ArgumentException>(() => new JobResourceNode(Guid.NewGuid(), Guid.Empty));
		}
	}
}
