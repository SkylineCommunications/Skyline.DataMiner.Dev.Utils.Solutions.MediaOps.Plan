namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class TransitionToRunningTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public TransitionToRunningTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void TransitionToRunning_DraftJob_ThrowsInvalidStateError()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddMinutes(10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(10),
				PostRollEnd = currentTime.AddMinutes(20),
			};

			job = objectCreator.CreateJob(job);

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.TransitionToRunning(job));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobInvalidStateError>().Any(),
				"Expected a JobInvalidStateError when transitioning a job that is not in the Confirmed state.");
		}

		[TestMethod]
		public void TransitionToRunning_TentativeJob_ThrowsInvalidStateError()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddMinutes(10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(10),
				PostRollEnd = currentTime.AddMinutes(20),
			};

			job = objectCreator.CreateJob(job);

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.TransitionToRunning(tentativeJob));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobInvalidStateError>().Any(),
				"Expected a JobInvalidStateError when transitioning a job that is not in the Confirmed state.");
		}

		[TestMethod]
		public void TransitionToRunning_ConfirmedJobWithFuturePreRoll_ThrowsPreRollStartNotReachedError()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			// The pre-roll start lies in the future, so the transition must be rejected.
			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddMinutes(10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(10),
				PostRollEnd = currentTime.AddMinutes(20),
			};

			job.NodeGraph.Add(new JobResourceNode(pool, resource));
			job = objectCreator.CreateJob(job);

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);
			var confirmedJob = TestContext.Api.Jobs.Confirm(tentativeJob);

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.TransitionToRunning(confirmedJob));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobPreRollStartNotReachedError>().Any(),
				"Expected a JobPreRollStartNotReachedError when the pre-roll start time has not yet passed.");
		}
	}
}
