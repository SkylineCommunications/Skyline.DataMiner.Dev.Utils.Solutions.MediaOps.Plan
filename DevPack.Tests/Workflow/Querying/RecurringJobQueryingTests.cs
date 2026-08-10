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
	public sealed class RecurringJobQueryingTests
	{
		private static TestObjectCreator? objectCreator;
		private static RecurringJobFilteringSetup? setup;

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		private static RecurringJobFilteringSetup Setup => setup ?? throw new InvalidOperationException("Test setup was not initialized.");

		[ClassInitialize]
		public static void ClassInitialize(TestContext context)
		{
			objectCreator = new TestObjectCreator(TestContext);
			setup = new RecurringJobFilteringSetup(objectCreator, TestContext);
		}

		[ClassCleanup(ClassCleanupBehavior.EndOfClass)]
		public static void ClassCleanup()
		{
			objectCreator?.Dispose();
			objectCreator = null;
			setup = null;
		}

		private FilterElement<RecurringJob> RecurringJobFilter => new ORFilterElement<RecurringJob>(Setup.RecurringJobs.Select(x => RecurringJobExposers.Id.Equal(x.Id)).ToArray());

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query.
		/// </summary>
		private Tuple<RecurringJob[], IQuery<RecurringJob>>[] RecurringJobQueryTestCases => new[]
		{
			new Tuple<RecurringJob[], IQuery<RecurringJob>>(
				[Setup.RecurringJob1!, Setup.RecurringJob2!, Setup.RecurringJob3!],
				RecurringJobFilter.ToQuery().OrderBy(RecurringJobExposers.Name)),
			new Tuple<RecurringJob[], IQuery<RecurringJob>>(
				[Setup.RecurringJob3!, Setup.RecurringJob2!, Setup.RecurringJob1!],
				RecurringJobFilter.ToQuery().OrderByDescending(RecurringJobExposers.Name)),

			new Tuple<RecurringJob[], IQuery<RecurringJob>>(
				[Setup.RecurringJob1!, Setup.RecurringJob2!, Setup.RecurringJob3!],
				RecurringJobFilter.ToQuery().OrderBy(RecurringJobExposers.Start)),
			new Tuple<RecurringJob[], IQuery<RecurringJob>>(
				[Setup.RecurringJob3!, Setup.RecurringJob2!, Setup.RecurringJob1!],
				RecurringJobFilter.ToQuery().OrderByDescending(RecurringJobExposers.Duration)),

			new Tuple<RecurringJob[], IQuery<RecurringJob>>(
				[Setup.RecurringJob3!, Setup.RecurringJob2!],
				RecurringJobFilter.AND(RecurringJobExposers.Duration.GreaterThan(TimeSpan.FromHours(1))).ToQuery().OrderByDescending(RecurringJobExposers.Name)),
			new Tuple<RecurringJob[], IQuery<RecurringJob>>(
				[],
				RecurringJobFilter.AND(RecurringJobExposers.Description.Contains("Unknown description")).ToQuery().OrderBy(RecurringJobExposers.Name)),
		};

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query with a limit.
		/// </summary>
		private Tuple<RecurringJob[], IQuery<RecurringJob>>[] RecurringJobLimitedQueryTestCases => new[]
		{
			new Tuple<RecurringJob[], IQuery<RecurringJob>>(
				[Setup.RecurringJob1!],
				RecurringJobFilter.ToQuery().OrderBy(RecurringJobExposers.Name).Limit(1)),
			new Tuple<RecurringJob[], IQuery<RecurringJob>>(
				[Setup.RecurringJob1!, Setup.RecurringJob2!],
				RecurringJobFilter.ToQuery().OrderBy(RecurringJobExposers.Name).Limit(2)),
			new Tuple<RecurringJob[], IQuery<RecurringJob>>(
				[Setup.RecurringJob3!, Setup.RecurringJob2!],
				RecurringJobFilter.ToQuery().OrderByDescending(RecurringJobExposers.Start).Limit(2)),
			new Tuple<RecurringJob[], IQuery<RecurringJob>>(
				[Setup.RecurringJob1!, Setup.RecurringJob2!, Setup.RecurringJob3!],
				RecurringJobFilter.ToQuery().OrderBy(RecurringJobExposers.Name).Limit(10)),
		};

		[TestMethod]
		public void ReadRecurringJobsWithQuery()
		{
			foreach (var (expectedObjects, query) in RecurringJobQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.RecurringJobs, expectedObjects, query);
			}
		}

		[TestMethod]
		public void CountRecurringJobsWithQuery()
		{
			foreach (var (expectedObjects, query) in RecurringJobQueryTestCases)
			{
				QueryAssert.Count(TestContext.Api.RecurringJobs, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadRecurringJobsPagedWithQuery_DefaultPageSize()
		{
			foreach (var (expectedObjects, query) in RecurringJobQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.RecurringJobs, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadRecurringJobsPagedWithQuery_CustomPageSize()
		{
			foreach (var (expectedObjects, query) in RecurringJobQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.RecurringJobs, expectedObjects, query, 2);
			}
		}

		[TestMethod]
		public void ReadRecurringJobsWithLimitedQuery()
		{
			foreach (var (expectedObjects, query) in RecurringJobLimitedQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.RecurringJobs, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadRecurringJobsWithUnsupportedOrderByThrowsException()
		{
			var query = RecurringJobFilter.ToQuery().OrderBy(RecurringJobExposers.Pattern.EndDate);

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.RecurringJobs.Read(query).ToList());
		}
	}
}
