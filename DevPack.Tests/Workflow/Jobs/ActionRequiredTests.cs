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
	public sealed class ActionRequiredTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public ActionRequiredTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void JobWithoutNodes_DoesNotRequireAnAction()
		{
			var job = objectCreator.CreateJob(NewValidJob($"{Guid.NewGuid()}_Job"));

			AssertActionRequired(job, false);
		}

		[TestMethod]
		public void JobWithResourcePoolNode_RequiresAnAction()
		{
			var prefix = Guid.NewGuid();

			var job = NewValidJob($"{prefix}_Job");
			job.NodeGraph.Add(new JobResourcePoolNode(CreateResourcePool(prefix)));

			AssertActionRequired(objectCreator.CreateJob(job), true);
		}

		[TestMethod]
		public void JobWithResourceNode_DoesNotRequireAnAction()
		{
			var prefix = Guid.NewGuid();

			var resourcePool = CreateResourcePool(prefix);
			var resource = CreateResource(prefix, resourcePool);

			var job = NewValidJob($"{prefix}_Job");
			job.NodeGraph.Add(new JobResourceNode(resourcePool, resource));

			AssertActionRequired(objectCreator.CreateJob(job), false);
		}

		[TestMethod]
		public void JobWithResourceNodeInError_RequiresAnAction()
		{
			var prefix = Guid.NewGuid();

			var resourcePool = CreateResourcePool(prefix);
			var resource = CreateResource(prefix, resourcePool);

			var job = NewValidJob($"{prefix}_Job");
			job.NodeGraph.Add(new JobResourceNode(resourcePool, resource) { HasError = true });

			AssertActionRequired(objectCreator.CreateJob(job), true);
		}

		[TestMethod]
		public void JobWithResourcePoolNodeSwappedForResourceNode_NoLongerRequiresAnAction()
		{
			var prefix = Guid.NewGuid();

			var resourcePool = CreateResourcePool(prefix);
			var resource = CreateResource(prefix, resourcePool);

			var job = NewValidJob($"{prefix}_Job");
			job.NodeGraph.Add(new JobResourcePoolNode(resourcePool));

			var createdJob = objectCreator.CreateJob(job);
			AssertActionRequired(createdJob, true);

			var createdPoolNode = createdJob.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Single();
			createdJob.NodeGraph.Swap(createdPoolNode, new JobResourceNode(resourcePool, resource));

			AssertActionRequired(TestContext.Api.Jobs.Update(createdJob), false);
		}

		private static void AssertActionRequired(Job job, bool expected)
		{
			Assert.AreEqual(expected, job.ActionRequired);
			Assert.AreEqual(expected, TestContext.Api.Jobs.Read(job.Id).ActionRequired);
		}

		private static Job NewValidJob(string name)
		{
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			return new Job
			{
				Name = name,
				Start = currentTime.AddHours(1),
				End = currentTime.AddHours(2),
				PreRollStart = currentTime.AddHours(1),
				PostRollEnd = currentTime.AddHours(2),
			};
		}

		private ResourcePool CreateResourcePool(Guid prefix)
		{
			return TestContext.Api.ResourcePools.Complete(objectCreator.CreateResourcePool(new ResourcePool
			{
				Name = $"{prefix}_ResourcePool",
			}));
		}

		private Resource CreateResource(Guid prefix, ResourcePool resourcePool)
		{
			var resource = new UnmanagedResource
			{
				Name = $"{prefix}_Resource",
			}.AssignToPool(resourcePool);

			return TestContext.Api.Resources.Complete(objectCreator.CreateResource(resource));
		}
	}
}
