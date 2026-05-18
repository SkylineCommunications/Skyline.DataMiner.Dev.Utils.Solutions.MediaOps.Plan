namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using SLDataGateway.API.Querying;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class BasicTests
	{
		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		[TestMethod]
		public void ReadAllJobs()
		{
			try
			{
				TestContext.Api.Jobs.Read().ToArray();
				return;
			}
			catch (Exception)
			{
				Assert.Fail();
			}
		}

		[TestMethod]
		public void ReadJobById()
		{
			var firstJob = TestContext.Api.Jobs.Read().FirstOrDefault();
			if (firstJob == null)
				return;

			var jobToVerify = TestContext.Api.Jobs.Read(firstJob.Id);

			Assert.AreEqual(firstJob, jobToVerify);
		}

		[TestMethod]
		public void ReadJobByName()
		{
			var firstJob = TestContext.Api.Jobs.Read().FirstOrDefault();
			if (firstJob == null)
				return;

			var jobToVerify = TestContext.Api.Jobs.Read(JobExposers.Name.Equal(firstJob.Name)).First();

			Assert.AreEqual(firstJob, jobToVerify);
		}

		[TestMethod]
		public void UpdateUnmodifiedJob()
		{
			var job = TestContext.Api.Jobs.Read().FirstOrDefault();
			if (job == null)
				return;

			var updatedJob = TestContext.Api.Jobs.Update(job);

			Assert.AreEqual(job, updatedJob);
		}

		[TestMethod]
		public void ReadWithEmptyFilterReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Job>(idsToRetrieve.Select(x => JobExposers.Id.Equal(x)).ToArray());

			var jobs = TestContext.Api.Jobs.Read(emptyFilter);
			Assert.IsNotNull(jobs);
			Assert.AreEqual(0, jobs.Count());
		}

		[TestMethod]
		public void CountWithEmptyFilterReturnsZero()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Job>(idsToRetrieve.Select(x => JobExposers.Id.Equal(x)).ToArray());

			var count = TestContext.Api.Jobs.Count(emptyFilter);
			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public void ReadWithEmptyQueryReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Job>(idsToRetrieve.Select(x => JobExposers.Id.Equal(x)).ToArray());
			var queryWithEmptyFilter = emptyFilter.ToQuery();

			var jobs = TestContext.Api.Jobs.Read(queryWithEmptyFilter);
			Assert.IsNotNull(jobs);
			Assert.AreEqual(0, jobs.Count());
		}
	}
}
