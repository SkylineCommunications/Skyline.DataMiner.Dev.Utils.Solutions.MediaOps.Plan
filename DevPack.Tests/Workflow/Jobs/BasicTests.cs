namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

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
	}
}
