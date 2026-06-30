namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class TentativeStateTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public TentativeStateTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void SaveAsTentative_CreatesCoreReservation_WithExpectedNameResourcesAndTimings()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};

			var resourceNode = new JobResourceNode(pool, resource);
			var poolNode = new JobResourcePoolNode(pool);

			job.NodeGraph
				.Add(resourceNode)
				.Add(poolNode)
				.Connect(resourceNode, poolNode);

			job = objectCreator.CreateJob(job);

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);
			Assert.IsNotNull(tentativeJob, "Expected the job to transition to the Tentative state.");

			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(job.Id))).ToList();

			Assert.AreEqual(1, reservations.Count, "Expected exactly one core reservation for the tentative job.");

			var reservation = reservations[0];
			Assert.AreEqual($"{tentativeJob.Name} [{tentativeJob.Key}]", reservation.Name);
			Assert.AreEqual(currentTime, reservation.Start);
			Assert.AreEqual(currentTime.AddMinutes(10), reservation.End);

			var usages = reservation.ResourcesInReservationInstance.OfType<ServiceResourceUsageDefinition>().ToList();
			Assert.AreEqual(1, usages.Count, "Expected the resource node to be booked as a single reservation usage.");
			Assert.AreEqual(resource.CoreResourceId, usages[0].GUID);
		}

		[TestMethod]
		public void SaveAsTentative_JobWithoutNodes_CreatesEmptyCoreReservation()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};

			job = objectCreator.CreateJob(job);

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);
			Assert.IsNotNull(tentativeJob, "Expected the job to transition to the Tentative state.");

			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(job.Id))).ToList();

			Assert.AreEqual(1, reservations.Count, "Expected a core reservation to be created even when the job has no nodes.");

			var reservation = reservations[0];
			Assert.AreEqual($"{tentativeJob.Name} [{tentativeJob.Key}]", reservation.Name);
			Assert.AreEqual(currentTime, reservation.Start);
			Assert.AreEqual(currentTime.AddMinutes(10), reservation.End);

			var usages = reservation.ResourcesInReservationInstance.OfType<ServiceResourceUsageDefinition>().ToList();
			Assert.AreEqual(0, usages.Count, "Expected the reservation of a node-less job to have no resource usages.");
		}
	}
}
