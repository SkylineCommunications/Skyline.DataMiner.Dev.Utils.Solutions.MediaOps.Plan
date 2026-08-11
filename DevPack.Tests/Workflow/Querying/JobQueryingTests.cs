namespace RT_MediaOps.Plan.Workflow.Querying
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Querying;
	using RT_MediaOps.Plan.RegressionTests;
	using RT_MediaOps.Plan.Workflow.Filtering;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class JobQueryingTests
	{
		private static TestObjectCreator? objectCreator;
		private static JobFilteringSetup? setup;

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		private static JobFilteringSetup Setup => setup ?? throw new InvalidOperationException("Test setup was not initialized.");

		[ClassInitialize]
		public static void ClassInitialize(TestContext context)
		{
			objectCreator = new TestObjectCreator(TestContext);
			setup = new JobFilteringSetup(objectCreator, TestContext);
		}

		[ClassCleanup(ClassCleanupBehavior.EndOfClass)]
		public static void ClassCleanup()
		{
			objectCreator?.Dispose();
			objectCreator = null;
			setup = null;
		}

		private FilterElement<Job> JobFilter => new ORFilterElement<Job>(Setup.Jobs.Select(x => JobExposers.Id.Equal(x.Id)).ToArray());

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query.
		/// </summary>
		private Tuple<Job[], IQuery<Job>>[] JobQueryTestCases => new[]
		{
			new Tuple<Job[], IQuery<Job>>(
				[Setup.DraftJob1!, Setup.DraftJob2!, Setup.TentativeJob3!],
				JobFilter.ToQuery().OrderBy(JobExposers.Name)),
			new Tuple<Job[], IQuery<Job>>(
				[Setup.TentativeJob3!, Setup.DraftJob2!, Setup.DraftJob1!],
				JobFilter.ToQuery().OrderByDescending(JobExposers.Name)),

			new Tuple<Job[], IQuery<Job>>(
				[Setup.DraftJob1!, Setup.DraftJob2!, Setup.TentativeJob3!],
				JobFilter.ToQuery().OrderBy(JobExposers.Start)),
			new Tuple<Job[], IQuery<Job>>(
				[Setup.TentativeJob3!, Setup.DraftJob2!, Setup.DraftJob1!],
				JobFilter.ToQuery().OrderByDescending(JobExposers.End)),

			new Tuple<Job[], IQuery<Job>>(
				[Setup.DraftJob2!, Setup.DraftJob1!],
				JobFilter.AND(JobExposers.Name.Contains("Job_Draft")).ToQuery().OrderByDescending(JobExposers.Name)),
			new Tuple<Job[], IQuery<Job>>(
				[],
				JobFilter.AND(JobExposers.Name.Contains("Unknown")).ToQuery().OrderBy(JobExposers.Name)),
		};

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query with a limit.
		/// </summary>
		private Tuple<Job[], IQuery<Job>>[] JobLimitedQueryTestCases => new[]
		{
			new Tuple<Job[], IQuery<Job>>(
				[Setup.DraftJob1!],
				JobFilter.ToQuery().OrderBy(JobExposers.Name).Limit(1)),
			new Tuple<Job[], IQuery<Job>>(
				[Setup.DraftJob1!, Setup.DraftJob2!],
				JobFilter.ToQuery().OrderBy(JobExposers.Name).Limit(2)),
			new Tuple<Job[], IQuery<Job>>(
				[Setup.TentativeJob3!, Setup.DraftJob2!],
				JobFilter.ToQuery().OrderByDescending(JobExposers.Start).Limit(2)),
			new Tuple<Job[], IQuery<Job>>(
				[Setup.DraftJob1!, Setup.DraftJob2!, Setup.TentativeJob3!],
				JobFilter.ToQuery().OrderBy(JobExposers.Name).Limit(10)),
		};

		[TestMethod]
		public void ReadJobsWithQuery()
		{
			foreach (var (expectedObjects, query) in JobQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.Jobs, expectedObjects, query);
			}
		}

		[TestMethod]
		public void CountJobsWithQuery()
		{
			foreach (var (expectedObjects, query) in JobQueryTestCases)
			{
				QueryAssert.Count(TestContext.Api.Jobs, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadJobsPagedWithQuery_DefaultPageSize()
		{
			foreach (var (expectedObjects, query) in JobQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.Jobs, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadJobsPagedWithQuery_CustomPageSize()
		{
			foreach (var (expectedObjects, query) in JobQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.Jobs, expectedObjects, query, 2);
			}
		}

		[TestMethod]
		public void ReadJobsWithLimitedQuery()
		{
			foreach (var (expectedObjects, query) in JobLimitedQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.Jobs, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadJobsWithUnsupportedOrderByThrowsException()
		{
			var query = JobFilter.ToQuery().OrderBy(JobExposers.Capabilities.CapabilityId);

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.Jobs.Read(query).ToList());
		}
	}
}
