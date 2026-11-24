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
		public void ReadPartialRecurringJobsWithQuery_ReturnsSameRecurringJobsInSameOrder()
		{
			foreach (var (expectedObjects, query) in RecurringJobQueryTestCases.Concat(RecurringJobLimitedQueryTestCases))
			{
				var partialRecurringJobs = TestContext.Api.RecurringJobs.ReadPartial(query.WithFields(RecurringJobExposers.Name)).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), partialRecurringJobs.Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), partialRecurringJobs.Select(x => x.GetValue(RecurringJobExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedRecurringJobsWithQuery_DefaultPageSize_ReturnsSameRecurringJobsInOnePage()
		{
			foreach (var (expectedObjects, query) in RecurringJobQueryTestCases.Concat(RecurringJobLimitedQueryTestCases))
			{
				var pages = TestContext.Api.RecurringJobs.ReadPartialPaged(query.WithFields(RecurringJobExposers.Name)).ToList();

				Assert.AreEqual(1, pages.Count, $"Query: {query}");
				Assert.IsFalse(pages[0].HasNextPage, $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages[0].Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), pages[0].Select(x => x.GetValue(RecurringJobExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedRecurringJobsWithQuery_CustomPageSize_SplitsRecurringJobsOverPages()
		{
			var repository = (RecurringJobsRepository)TestContext.Api.RecurringJobs;

			foreach (var (expectedObjects, query) in RecurringJobQueryTestCases.Concat(RecurringJobLimitedQueryTestCases))
			{
				var pages = repository.ReadPartialPaged(query.WithFields(RecurringJobExposers.Name), 2).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages.SelectMany(x => x).Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(Enumerable.Range(0, pages.Count).ToList(), pages.Select(x => x.PageNumber).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(pages.Select((_, i) => i < pages.Count - 1).ToList(), pages.Select(x => x.HasNextPage).ToList(), $"Query: {query}");
				Assert.IsTrue(pages.All(x => x.Count <= 2), $"Query: {query}");
			}

			Assert.IsTrue(RecurringJobQueryTestCases.Any(x => x.Item1.Length > 2), "No query returns more than one page.");
		}

		[TestMethod]
		public void ReadPartialPagedRecurringJobsWithFilter_ReturnsAllRecurringJobs()
		{
			var pages = TestContext.Api.RecurringJobs.ReadPartialPaged(RecurringJobFilter.WithFields(RecurringJobExposers.Name, RecurringJobExposers.Resources)).ToList();
			var recurringJobsById = TestContext.Api.RecurringJobs.Read(RecurringJobFilter).ToDictionary(x => x.Id);

			Assert.AreEqual(1, pages.Count);
			CollectionAssert.AreEquivalent(recurringJobsById.Keys.ToList(), pages[0].Select(x => x.Id).ToList());

			foreach (var partialRecurringJob in pages[0])
			{
				var recurringJob = recurringJobsById[partialRecurringJob.Id];
				Assert.AreEqual(recurringJob.Name, partialRecurringJob.GetValue(RecurringJobExposers.Name));
				CollectionAssert.AreEqual(recurringJob.NodeGraph.Nodes.OfType<IResourceNode>().Select(x => x.ResourceId).ToList(), partialRecurringJob.GetValues(RecurringJobExposers.Resources).ToList(), recurringJob.Name);
			}
		}

		[TestMethod]
		public void ReadPartialRecurringJobsWithFilter_ReturnsSameValuesAsFullRead()
		{
			var partialFilter = RecurringJobFilter.WithFields(
				RecurringJobExposers.Id,
				RecurringJobExposers.Name,
				RecurringJobExposers.Description,
				RecurringJobExposers.Notes,
				RecurringJobExposers.Priority,
				RecurringJobExposers.Start,
				RecurringJobExposers.Duration,
				RecurringJobExposers.DesiredJobState,
				RecurringJobExposers.OrganizationId,
				RecurringJobExposers.OwnerId,
				RecurringJobExposers.JobTypeCategoryId,
				RecurringJobExposers.Resources,
				RecurringJobExposers.ResourcePools,
				RecurringJobExposers.Pattern.EndDate);

			var partialRecurringJobsById = TestContext.Api.RecurringJobs.ReadPartial(partialFilter).ToDictionary(x => x.Id);
			var recurringJobs = TestContext.Api.RecurringJobs.Read(RecurringJobFilter).ToList();

			Assert.AreEqual(recurringJobs.Count, partialRecurringJobsById.Count);

			foreach (var recurringJob in recurringJobs)
			{
				var partialRecurringJob = partialRecurringJobsById[recurringJob.Id];

				Assert.AreEqual(recurringJob.Id, partialRecurringJob.GetValue(RecurringJobExposers.Id));
				Assert.AreEqual(recurringJob.Name, partialRecurringJob.GetValue(RecurringJobExposers.Name));
				Assert.AreEqual(recurringJob.Description, partialRecurringJob.GetValue(RecurringJobExposers.Description));
				Assert.AreEqual(recurringJob.Notes, partialRecurringJob.GetValue(RecurringJobExposers.Notes));
				Assert.AreEqual(recurringJob.Priority, partialRecurringJob.GetValue(RecurringJobExposers.Priority));
				Assert.AreEqual(recurringJob.Start, partialRecurringJob.GetValue(RecurringJobExposers.Start));
				Assert.AreEqual(recurringJob.Start.Offset, partialRecurringJob.GetValue(RecurringJobExposers.Start).Offset);
				Assert.AreEqual(recurringJob.Duration, partialRecurringJob.GetValue(RecurringJobExposers.Duration));
				Assert.AreEqual(recurringJob.DesiredJobState, partialRecurringJob.GetValue(RecurringJobExposers.DesiredJobState));
				Assert.AreEqual(recurringJob.OrganizationId, partialRecurringJob.GetValue(RecurringJobExposers.OrganizationId));
				Assert.AreEqual(recurringJob.OwnerId, partialRecurringJob.GetValue(RecurringJobExposers.OwnerId));
				Assert.AreEqual(recurringJob.JobTypeCategoryId, partialRecurringJob.GetValue(RecurringJobExposers.JobTypeCategoryId));
				Assert.AreEqual(recurringJob.Pattern.EndDate, partialRecurringJob.GetValue(RecurringJobExposers.Pattern.EndDate));
				Assert.AreEqual(recurringJob.Pattern.EndDate.Offset, partialRecurringJob.GetValue(RecurringJobExposers.Pattern.EndDate).Offset);

				CollectionAssert.AreEqual(recurringJob.NodeGraph.Nodes.OfType<IResourceNode>().Select(x => x.ResourceId).ToList(), partialRecurringJob.GetValues(RecurringJobExposers.Resources).ToList(), recurringJob.Name);
				CollectionAssert.AreEqual(recurringJob.NodeGraph.Nodes.Select(GetResourcePoolId).Where(x => x != Guid.Empty).ToList(), partialRecurringJob.GetValues(RecurringJobExposers.ResourcePools).ToList(), recurringJob.Name);
			}

			// The setup must cover the multi-section fields, otherwise the comparison above proves little.
			Assert.IsTrue(recurringJobs.Any(x => x.NodeGraph.Nodes.OfType<IResourceNode>().Any() && x.NodeGraph.Nodes.OfType<IResourcePoolNode>().Any()), "No recurring job with both node types.");
		}

		[TestMethod]
		public void ReadPartialRecurringJobs_UnknownField_ThrowsBeforeReading()
		{
			var unknownField = new Exposer<RecurringJob, string>(x => x.Name, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.RecurringJobs.ReadPartial(RecurringJobFilter.WithFields(unknownField)));
		}

		[TestMethod]
		public void ReadRecurringJobsWithUnsupportedOrderByThrowsException()
		{
			var query = RecurringJobFilter.ToQuery().OrderBy(RecurringJobExposers.Pattern.EndDate);

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.RecurringJobs.Read(query).ToList());
		}

		private static Guid GetResourcePoolId(RecurringJobNode node)
		{
			switch (node)
			{
				case IResourceNode resourceNode:
					return resourceNode.ResourcePoolId;
				case IResourcePoolNode resourcePoolNode:
					return resourcePoolNode.ResourcePoolId;
				default:
					return Guid.Empty;
			}
		}
	}
}
