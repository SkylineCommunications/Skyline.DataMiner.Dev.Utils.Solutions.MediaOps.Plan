namespace RT_MediaOps.Plan.Workflow.RecurringJobs
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
		public void ReadAllRecurringJobs()
		{
			try
			{
				TestContext.Api.RecurringJobs.Read().ToArray();
				return;
			}
			catch (Exception)
			{
				Assert.Fail();
			}
		}

		[TestMethod]
		public void ReadRecurringJobById()
		{
			var firstRecurringJob = TestContext.Api.RecurringJobs.Read().First();
			var jobToVerify = TestContext.Api.RecurringJobs.Read(firstRecurringJob.Id);

			Assert.AreEqual(firstRecurringJob, jobToVerify);
		}

		[TestMethod]
		public void ReadRecurringJobByName()
		{
			var firstRecurringJob = TestContext.Api.RecurringJobs.Read().First();
			var jobToVerify = TestContext.Api.RecurringJobs.Read(RecurringJobExposers.Name.Equal(firstRecurringJob.Name)).First();

			Assert.AreEqual(firstRecurringJob, jobToVerify);
		}

		[TestMethod]
		public void ReadWithEmptyFilterReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<RecurringJob>(idsToRetrieve.Select(x => RecurringJobExposers.Id.Equal(x)).ToArray());

			var recurringJobs = TestContext.Api.RecurringJobs.Read(emptyFilter);
			Assert.IsNotNull(recurringJobs);
			Assert.AreEqual(0, recurringJobs.Count());
		}

		[TestMethod]
		public void ReadWithEmptyQueryReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<RecurringJob>(idsToRetrieve.Select(x => RecurringJobExposers.Id.Equal(x)).ToArray());
			var queryWithEmptyFilter = emptyFilter.ToQuery();

			var recurringJobs = TestContext.Api.RecurringJobs.Read(queryWithEmptyFilter);
			Assert.IsNotNull(recurringJobs);
			Assert.AreEqual(0, recurringJobs.Count());
		}
	}
}
