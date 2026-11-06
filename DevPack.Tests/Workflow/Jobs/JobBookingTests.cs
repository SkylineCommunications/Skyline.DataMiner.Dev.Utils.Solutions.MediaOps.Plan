namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Tests for <see cref="IJobsRepository.GetBooking(Guid)"/> and <see cref="IJobsRepository.GetResourceUsages(Job)"/>.
	/// </summary>
	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class JobBookingTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public JobBookingTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void GetBooking_DraftJob_ReturnsNull()
		{
			var setup = CreateSetup();
			var job = objectCreator.CreateJob(CreateJob(setup, new JobResourceNode(setup.Pool, setup.Resource)));

			Assert.IsNull(TestContext.Api.Jobs.GetBooking(job.Id));
		}

		[TestMethod]
		public void GetBooking_ConfirmedJob_ReturnsTheBookedWindowAndNodeRequirements()
		{
			var setup = CreateSetup();
			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(setup.NumberCapacity.Id) { Value = 25 });
			node.OrchestrationSettings.AddCapacity(new RangeCapacitySetting(setup.RangeCapacity.Id) { MinValue = 10, MaxValue = 20 });

			var job = objectCreator.CreateJob(CreateJob(setup, node));
			job = TestContext.Api.Jobs.SaveAsTentative(job);
			job = TestContext.Api.Jobs.Confirm(job);

			var booking = TestContext.Api.Jobs.GetBooking(job.Id);

			Assert.IsNotNull(booking);
			Assert.IsTrue(booking.UsesResources);
			Assert.AreEqual(job.PreRollStart, booking.Start);
			Assert.AreEqual(job.PostRollEnd, booking.End);

			var bookedNode = booking.Nodes.Single();
			Assert.AreEqual(node.Id, bookedNode.NodeId);
			Assert.AreEqual(setup.Resource.Id, bookedNode.ResourceId);
			Assert.AreEqual(setup.Pool.Id, bookedNode.ResourcePoolId);
			Assert.AreEqual(25m, bookedNode.Capacities.OfType<NumberCapacitySetting>().Single().Value);

			var range = bookedNode.Capacities.OfType<RangeCapacitySetting>().Single();
			Assert.AreEqual(10m, range.MinValue);
			Assert.AreEqual(20m, range.MaxValue);
		}

		[TestMethod]
		public void GetBooking_CanceledJob_DoesNotUseResources()
		{
			var setup = CreateSetup();
			var job = objectCreator.CreateJob(CreateJob(setup, new JobResourceNode(setup.Pool, setup.Resource)));
			job = TestContext.Api.Jobs.SaveAsTentative(job);
			job = TestContext.Api.Jobs.Cancel(job);

			var booking = TestContext.Api.Jobs.GetBooking(job.Id);

			Assert.IsNotNull(booking);
			Assert.IsFalse(booking.UsesResources);
		}

		[TestMethod]
		public void GetResourceUsages_UnsavedJob_ReturnsTheRequirementsOfResourceAndPoolNodes()
		{
			var setup = CreateSetup();
			var resourceNode = new JobResourceNode(setup.Pool, setup.Resource);
			resourceNode.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(setup.NumberCapacity.Id) { Value = 40 });
			resourceNode.OrchestrationSettings.AddCapacity(new RangeCapacitySetting(setup.RangeCapacity.Id));
			var poolNode = new JobResourcePoolNode(setup.Pool.Id);

			var usages = TestContext.Api.Jobs.GetResourceUsages(CreateJob(setup, resourceNode, poolNode));

			Assert.AreEqual(2, usages.Count);

			var resourceUsage = usages.Single(x => x.NodeId == resourceNode.Id);
			Assert.AreEqual(setup.Resource.Id, resourceUsage.ResourceId);
			Assert.AreEqual(40m, resourceUsage.Capacities.OfType<NumberCapacitySetting>().Single().Value, "Expected a range capacity without a value not to be required.");

			var poolUsage = usages.Single(x => x.NodeId == poolNode.Id);
			Assert.AreEqual(Guid.Empty, poolUsage.ResourceId);
			Assert.AreEqual(setup.Pool.Id, poolUsage.ResourcePoolId);
		}

		[TestMethod]
		public void GetResourceUsages_JobThatAlreadyEnded_ReturnsNothing()
		{
			var setup = CreateSetup();
			var job = CreateJob(setup, new JobResourceNode(setup.Pool, setup.Resource));
			job.PreRollStart = DateTime.UtcNow.AddHours(-3).RoundToNextSecond();
			job.Start = job.PreRollStart;
			job.End = job.PreRollStart.AddHours(1);
			job.PostRollEnd = job.End;

			Assert.AreEqual(0, TestContext.Api.Jobs.GetResourceUsages(job).Count);
		}

		private static Job CreateJob(Setup setup, params JobNode[] nodes)
		{
			var start = DateTime.UtcNow.RoundToNextSecond().AddHours(1);
			var job = new Job
			{
				Name = $"{setup.Prefix}_Job",
				Start = start,
				End = start.AddHours(1),
				PreRollStart = start,
				PostRollEnd = start.AddHours(1),
			};

			foreach (var node in nodes)
			{
				job.NodeGraph.Add(node);
			}

			return job;
		}

		private Setup CreateSetup()
		{
			var prefix = Guid.NewGuid();

			var numberCapacity = new NumberCapacity { Name = $"{prefix}_Bandwidth", RangeMin = 0, RangeMax = 100 };
			objectCreator.CreateCapacity(numberCapacity);

			var rangeCapacity = new RangeCapacity { Name = $"{prefix}_Frequency" };
			objectCreator.CreateCapacity(rangeCapacity);

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource.AddCapacity(new NumberCapacitySetting(numberCapacity.Id) { Value = 100 });
			resource.AddCapacity(new RangeCapacitySetting(rangeCapacity.Id) { MinValue = 0, MaxValue = 100 });
			resource = TestContext.Api.Resources.Complete(objectCreator.CreateResource(resource));

			return new Setup(prefix, pool, resource, numberCapacity, rangeCapacity);
		}

		private sealed class Setup
		{
			public Setup(Guid prefix, ResourcePool pool, Resource resource, NumberCapacity numberCapacity, RangeCapacity rangeCapacity)
			{
				Prefix = prefix;
				Pool = pool;
				Resource = resource;
				NumberCapacity = numberCapacity;
				RangeCapacity = rangeCapacity;
			}

			public Guid Prefix { get; }

			public ResourcePool Pool { get; }

			public Resource Resource { get; }

			public NumberCapacity NumberCapacity { get; }

			public RangeCapacity RangeCapacity { get; }
		}
	}
}
