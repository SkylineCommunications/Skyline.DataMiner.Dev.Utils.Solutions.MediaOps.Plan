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

			new Tuple<Job[], IQuery<Job>>(
				[Setup.DraftJob1!, Setup.DraftJob2!],
				JobFilter.AND(JobExposers.HasError.Equal(true)).ToQuery().OrderBy(JobExposers.Name)),
			new Tuple<Job[], IQuery<Job>>(
				[Setup.TentativeJob3!],
				JobFilter.AND(JobExposers.HasError.Equal(false)).ToQuery().OrderBy(JobExposers.Name)),

			new Tuple<Job[], IQuery<Job>>(
				[Setup.DraftJob2!, Setup.DraftJob1!],
				JobFilter.AND(JobExposers.Errors.Code.Equal(Setup.ErrorCodeA)).ToQuery().OrderByDescending(JobExposers.Name)),
			new Tuple<Job[], IQuery<Job>>(
				[Setup.DraftJob2!],
				JobFilter.AND(JobExposers.Errors.Message.Contains("could not be stopped")).ToQuery().OrderBy(JobExposers.Name)),
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
		public void ReadPartialJobsWithQuery_ReturnsSameJobsInSameOrder()
		{
			foreach (var (expectedObjects, query) in JobQueryTestCases.Concat(JobLimitedQueryTestCases))
			{
				var partialJobs = TestContext.Api.Jobs.ReadPartial(SelectIdAndName(query)).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), partialJobs.Select(x => x.GetValue(JobExposers.Id)).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), partialJobs.Select(x => x.GetValue(JobExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedJobsWithQuery_DefaultPageSize_ReturnsSameJobsInOnePage()
		{
			foreach (var (expectedObjects, query) in JobQueryTestCases.Concat(JobLimitedQueryTestCases))
			{
				var pages = TestContext.Api.Jobs.ReadPartialPaged(SelectIdAndName(query)).ToList();

				Assert.AreEqual(1, pages.Count, $"Query: {query}");
				Assert.IsFalse(pages[0].HasNextPage, $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages[0].Select(x => x.GetValue(JobExposers.Id)).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), pages[0].Select(x => x.GetValue(JobExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedJobsWithQuery_CustomPageSize_SplitsJobsOverPages()
		{
			foreach (var (expectedObjects, query) in JobQueryTestCases.Concat(JobLimitedQueryTestCases))
			{
				var pages = TestContext.Api.Jobs.ReadPartialPaged(SelectIdAndName(query), 2).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages.SelectMany(x => x).Select(x => x.GetValue(JobExposers.Id)).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(Enumerable.Range(0, pages.Count).ToList(), pages.Select(x => x.PageNumber).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(pages.Select((_, i) => i < pages.Count - 1).ToList(), pages.Select(x => x.HasNextPage).ToList(), $"Query: {query}");
				Assert.IsTrue(pages.All(x => x.Count <= 2), $"Query: {query}");
			}

			Assert.IsTrue(JobQueryTestCases.Any(x => x.Item1.Length > 2), "No query returns more than one page.");
		}

		[TestMethod]
		public void ReadPartialPagedJobsWithFilter_ReturnsAllJobs()
		{
			var partialQuery = new PartialQuery<Job>(JobFilter.ToQuery())
				.Include(JobExposers.Id)
				.Include(JobExposers.Name)
				.Include(JobExposers.Resources);

			var pages = TestContext.Api.Jobs.ReadPartialPaged(partialQuery).ToList();
			var jobsById = TestContext.Api.Jobs.Read(JobFilter).ToDictionary(x => x.Id);

			Assert.AreEqual(1, pages.Count);
			CollectionAssert.AreEquivalent(jobsById.Keys.ToList(), pages[0].Select(x => x.GetValue(JobExposers.Id)).ToList());

			foreach (var partialJob in pages[0])
			{
				var job = jobsById[partialJob.GetValue(JobExposers.Id)];
				Assert.AreEqual(job.Name, partialJob.GetValue(JobExposers.Name));
				CollectionAssert.AreEqual(job.NodeGraph.Nodes.OfType<IResourceNode>().Select(x => x.ResourceId).ToList(), partialJob.GetValues(JobExposers.Resources).ToList(), job.Name);
			}
		}

		[TestMethod]
		public void ReadPartialJobsWithFilter_ReturnsSameValuesAsFullRead()
		{
			var partialQuery = new PartialQuery<Job>(JobFilter.ToQuery())
				.Include(JobExposers.Id)
				.Include(JobExposers.Name)
				.Include(JobExposers.Key)
				.Include(JobExposers.Description)
				.Include(JobExposers.Notes)
				.Include(JobExposers.JobTypeCategoryId)
				.Include(JobExposers.Start)
				.Include(JobExposers.End)
				.Include(JobExposers.PreRollStart)
				.Include(JobExposers.PostRollEnd)
				.Include(JobExposers.Duration)
				.Include(JobExposers.RecurringJobId)
				.Include(JobExposers.ActionRequired)
				.Include(JobExposers.Priority)
				.Include(JobExposers.State)
				.Include(JobExposers.ConfigurationState)
				.Include(JobExposers.OrganizationId)
				.Include(JobExposers.OwnerId)
				.Include(JobExposers.HasError)
				.Include(JobExposers.Errors.Code)
				.Include(JobExposers.Errors.Message)
				.Include(JobExposers.Resources)
				.Include(JobExposers.ResourcePools)
				.Include(JobExposers.Nodes.ConfigurationState)
				.Include(JobExposers.Capabilities.CapabilityId)
				.Include(JobExposers.Capabilities.Discretes)
				.Include(JobExposers.Capacities.CapacityId)
				.Include(JobExposers.Configurations.ConfigurationId)
				.Include(JobExposers.Properties.PropertyId);

			var partialJobsById = TestContext.Api.Jobs.ReadPartial(partialQuery).ToDictionary(x => x.GetValue(JobExposers.Id));
			var jobs = TestContext.Api.Jobs.Read(JobFilter).ToList();

			Assert.AreEqual(jobs.Count, partialJobsById.Count);

			foreach (var job in jobs)
			{
				var partialJob = partialJobsById[job.Id];

				Assert.AreEqual(job.Name, partialJob.GetValue(JobExposers.Name));
				Assert.AreEqual(job.Key, partialJob.GetValue(JobExposers.Key));
				Assert.AreEqual(job.Description, partialJob.GetValue(JobExposers.Description));
				Assert.AreEqual(job.Notes, partialJob.GetValue(JobExposers.Notes));
				Assert.AreEqual(job.JobTypeCategoryId, partialJob.GetValue(JobExposers.JobTypeCategoryId));
				Assert.AreEqual(job.Start, partialJob.GetValue(JobExposers.Start));
				Assert.AreEqual(job.Start.Offset, partialJob.GetValue(JobExposers.Start).Offset);
				Assert.AreEqual(job.End, partialJob.GetValue(JobExposers.End));
				Assert.AreEqual(job.PreRollStart, partialJob.GetValue(JobExposers.PreRollStart));
				Assert.AreEqual(job.PostRollEnd, partialJob.GetValue(JobExposers.PostRollEnd));
				Assert.AreEqual(job.Duration, partialJob.GetValue(JobExposers.Duration));
				Assert.AreEqual(job.RecurringJobId, partialJob.GetValue(JobExposers.RecurringJobId));
				Assert.AreEqual(job.ActionRequired, partialJob.GetValue(JobExposers.ActionRequired));
				Assert.AreEqual(job.Priority, partialJob.GetValue(JobExposers.Priority));
				Assert.AreEqual(job.State, partialJob.GetValue(JobExposers.State));
				Assert.AreEqual(job.ConfigurationState, partialJob.GetValue(JobExposers.ConfigurationState));
				Assert.AreEqual(job.OrganizationId, partialJob.GetValue(JobExposers.OrganizationId));
				Assert.AreEqual(job.OwnerId, partialJob.GetValue(JobExposers.OwnerId));

				Assert.AreEqual(job.Errors.Count > 0, partialJob.GetValue(JobExposers.HasError));
				CollectionAssert.AreEqual(job.Errors.Select(x => x.Code).ToList(), partialJob.GetValues(JobExposers.Errors.Code).ToList(), job.Name);
				CollectionAssert.AreEqual(job.Errors.Select(x => x.Message).Where(x => x != null).ToList(), partialJob.GetValues(JobExposers.Errors.Message).ToList(), job.Name);
				CollectionAssert.AreEqual(job.NodeGraph.Nodes.OfType<IResourceNode>().Select(x => x.ResourceId).ToList(), partialJob.GetValues(JobExposers.Resources).ToList(), job.Name);
				CollectionAssert.AreEqual(job.NodeGraph.Nodes.Select(GetResourcePoolId).Where(x => x != Guid.Empty).ToList(), partialJob.GetValues(JobExposers.ResourcePools).ToList(), job.Name);
				CollectionAssert.AreEqual(job.NodeGraph.Nodes.Select(x => x.ConfigurationState).ToList(), partialJob.GetValues(JobExposers.Nodes.ConfigurationState).ToList(), job.Name);
				CollectionAssert.AreEqual(job.OrchestrationSettings.Capabilities.Select(x => x.Id).ToList(), partialJob.GetValues(JobExposers.Capabilities.CapabilityId).ToList(), job.Name);
				CollectionAssert.AreEqual(job.OrchestrationSettings.Capabilities.Select(x => x.Value).Where(x => x != null).ToList(), partialJob.GetValues(JobExposers.Capabilities.Discretes).ToList(), job.Name);
				CollectionAssert.AreEqual(job.OrchestrationSettings.Capacities.Select(x => x.Id).ToList(), partialJob.GetValues(JobExposers.Capacities.CapacityId).ToList(), job.Name);
				CollectionAssert.AreEqual(job.OrchestrationSettings.Configurations.Select(x => x.Id).ToList(), partialJob.GetValues(JobExposers.Configurations.ConfigurationId).ToList(), job.Name);
				CollectionAssert.AreEqual(job.PropertySettings.Select(x => x.Id).ToList(), partialJob.GetValues(JobExposers.Properties.PropertyId).ToList(), job.Name);
			}

			// The setup must cover the multi-section and external fields, otherwise the comparison above proves little.
			Assert.IsTrue(jobs.Any(x => x.Errors.Count > 1), "No job with multiple errors.");
			Assert.IsTrue(jobs.Any(x => x.NodeGraph.Nodes.OfType<IResourceNode>().Any() && x.NodeGraph.Nodes.OfType<IResourcePoolNode>().Any()), "No job with both node types.");
			Assert.IsTrue(jobs.Any(x => x.OrchestrationSettings.Capabilities.Any() && x.OrchestrationSettings.Capacities.Any() && x.OrchestrationSettings.Configurations.Any()), "No job with orchestration settings.");
			Assert.IsTrue(jobs.Any(x => x.PropertySettings.Any()), "No job with property settings.");
		}

		[TestMethod]
		public void ReadPartialJobs_UnknownField_ThrowsBeforeReading()
		{
			var unknownField = new Exposer<Job, string>(x => x.Name, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.Jobs.ReadPartial(new PartialQuery<Job>(JobFilter.ToQuery()).Include(unknownField)));
		}

		private static IPartialQuery<Job> SelectIdAndName(IQuery<Job> query)
		{
			return new PartialQuery<Job>(query).Include(JobExposers.Id).Include(JobExposers.Name);
		}

		private static Guid GetResourcePoolId(JobNode node)
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
			var queries = new[]
			{
				JobFilter.ToQuery().OrderBy(JobExposers.Capabilities.CapabilityId),
				JobFilter.ToQuery().OrderBy(JobExposers.HasError),
				JobFilter.ToQuery().OrderBy(JobExposers.Errors.Code),
			};

			foreach (var query in queries)
			{
				Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.Jobs.Read(query).ToList());
			}
		}
	}
}
