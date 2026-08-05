namespace RT_MediaOps.Plan.Workflow.Filtering
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class RecurringJobFilteringTests
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
		/// Gets the expected objects mapped to the applied filter.
		/// </summary>
		private Tuple<RecurringJob[], FilterElement<RecurringJob>>[] RecurringJobFilterTestCases => new[]
		{
			new Tuple<RecurringJob[], FilterElement<RecurringJob>>(Setup.RecurringJobs, RecurringJobFilter),
			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([Setup.RecurringJob1!], RecurringJobFilter.AND(RecurringJobExposers.Name.Equal(Setup.RecurringJob1!.Name))),

			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([Setup.RecurringJob1!], RecurringJobFilter.AND(RecurringJobExposers.Description.Equal("First recurring job"))),
			new Tuple<RecurringJob[], FilterElement<RecurringJob>>(Setup.RecurringJobs, RecurringJobFilter.AND(RecurringJobExposers.Description.Contains("recurring job"))),
			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([], RecurringJobFilter.AND(RecurringJobExposers.Description.Contains("Unknown description"))),

			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([Setup.RecurringJob3!], RecurringJobFilter.AND(RecurringJobExposers.Notes.Equal("Notes of the third recurring job"))),
			new Tuple<RecurringJob[], FilterElement<RecurringJob>>(Setup.RecurringJobs, RecurringJobFilter.AND(RecurringJobExposers.Notes.Contains("Notes of the"))),
			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([], RecurringJobFilter.AND(RecurringJobExposers.Notes.Contains("Unknown notes"))),

			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([Setup.RecurringJob1!], RecurringJobFilter.AND(RecurringJobExposers.Priority.Equal(RecurringJobPriority.High))),
			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([Setup.RecurringJob2!], RecurringJobFilter.AND(RecurringJobExposers.Priority.Equal(RecurringJobPriority.Normal))),
			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([Setup.RecurringJob3!], RecurringJobFilter.AND(RecurringJobExposers.Priority.Equal(RecurringJobPriority.Low))),
			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([Setup.RecurringJob2!, Setup.RecurringJob3!], RecurringJobFilter.AND(RecurringJobExposers.Priority.NotEqual(RecurringJobPriority.High))),

			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([Setup.RecurringJob1!], RecurringJobFilter.AND(RecurringJobExposers.Start.Equal(Setup.BaseTime.AddHours(1)))),
			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([Setup.RecurringJob2!, Setup.RecurringJob3!], RecurringJobFilter.AND(RecurringJobExposers.Start.GreaterThan(Setup.BaseTime.AddHours(2)))),
			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([], RecurringJobFilter.AND(RecurringJobExposers.Start.Equal(Setup.BaseTime.AddDays(-1)))),

			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([Setup.RecurringJob1!], RecurringJobFilter.AND(RecurringJobExposers.Duration.Equal(TimeSpan.FromHours(1)))),
			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([Setup.RecurringJob2!, Setup.RecurringJob3!], RecurringJobFilter.AND(RecurringJobExposers.Duration.GreaterThan(TimeSpan.FromHours(1)))),
			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([], RecurringJobFilter.AND(RecurringJobExposers.Duration.Equal(TimeSpan.FromDays(7)))),

			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([Setup.RecurringJob1!], RecurringJobFilter.AND(RecurringJobExposers.DesiredJobState.Equal(DesiredJobState.Draft))),
			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([Setup.RecurringJob2!, Setup.RecurringJob3!], RecurringJobFilter.AND(RecurringJobExposers.DesiredJobState.Equal(DesiredJobState.Tentative))),

			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([Setup.RecurringJob1!], RecurringJobFilter.AND(RecurringJobExposers.JobTypeCategoryId.Equal(Setup.RecurringJob1!.JobTypeCategoryId))),
			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([], RecurringJobFilter.AND(RecurringJobExposers.JobTypeCategoryId.Equal($"Unknown_{Setup.Prefix}"))),

			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([Setup.RecurringJob1!], RecurringJobFilter.AND(RecurringJobExposers.OrganizationId.Equal(Setup.OrganizationId))),
			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([], RecurringJobFilter.AND(RecurringJobExposers.OrganizationId.Equal(Guid.NewGuid()))),

			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([Setup.RecurringJob1!], RecurringJobFilter.AND(RecurringJobExposers.OwnerId.Equal(Setup.OwnerId))),
			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([], RecurringJobFilter.AND(RecurringJobExposers.OwnerId.Equal(Guid.NewGuid()))),

			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([Setup.RecurringJob1!], RecurringJobFilter.AND(RecurringJobExposers.Pattern.EndDate.Equal(Setup.RecurringJob1!.Pattern.EndDate))),
			new Tuple<RecurringJob[], FilterElement<RecurringJob>>([Setup.RecurringJob2!, Setup.RecurringJob3!], RecurringJobFilter.AND(RecurringJobExposers.Pattern.EndDate.NotEqual(Setup.RecurringJob1!.Pattern.EndDate))),
		};

		[TestMethod]
		public void ReadRecurringJobsWithFilter()
		{
			foreach (var (expectedObjects, filter) in RecurringJobFilterTestCases)
			{
				var expectedObjectIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
				var actualObjectIds = TestContext.Api.RecurringJobs.Read(filter).Select(x => x.Id).OrderBy(x => x).ToList();

				Assert.IsTrue(expectedObjectIds.SequenceEqual(actualObjectIds), filter.ToString());
			}
		}

		[TestMethod]
		public void CountRecurringJobsWithFilter()
		{
			foreach (var (expectedObjects, filter) in RecurringJobFilterTestCases)
			{
				Assert.AreEqual(expectedObjects.Length, TestContext.Api.RecurringJobs.Count(filter), filter.ToString());
			}
		}

		[TestMethod]
		public void ReadRecurringJobsPagedWithFilter_DefaultPageSize()
		{
			foreach (var (expectedObjects, filter) in RecurringJobFilterTestCases)
			{
				var expectedObjectIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
				var pages = TestContext.Api.RecurringJobs.ReadPaged(filter).ToList();
				var actualObjectIds = pages.SelectMany(x => x).Select(x => x.Id).OrderBy(x => x).ToList();

				Assert.AreEqual(1, pages.Count, filter.ToString());
				Assert.IsTrue(expectedObjectIds.SequenceEqual(actualObjectIds), filter.ToString());
			}
		}

		[TestMethod]
		public void FilterOnPreRollDurationIsNotSupported()
		{
			var filter = RecurringJobExposers.PreRollDuration.Equal(TimeSpan.FromMinutes(10));

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.RecurringJobs.Count(filter));
		}

		[TestMethod]
		public void FilterOnPostRollDurationIsNotSupported()
		{
			var filter = RecurringJobExposers.PostRollDuration.Equal(TimeSpan.FromMinutes(10));

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.RecurringJobs.Count(filter));
		}
	}
}
