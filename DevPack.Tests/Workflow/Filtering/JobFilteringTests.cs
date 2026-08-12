namespace RT_MediaOps.Plan.Workflow.Filtering
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class JobFilteringTests
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
		/// Gets the expected objects mapped to the applied filter.
		/// </summary>
		private Tuple<Job[], FilterElement<Job>>[] JobFilterTestCases => new[]
		{
			new Tuple<Job[], FilterElement<Job>>(Setup.Jobs, JobFilter),
			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!, Setup.DraftJob2!], JobFilter.AND(JobExposers.Name.Contains("Job_Draft"))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!], JobFilter.AND(JobExposers.Key.Equal(Setup.DraftJob1!.Key))),
			new Tuple<Job[], FilterElement<Job>>([], JobFilter.AND(JobExposers.Key.Equal($"Unknown_{Setup.Prefix}"))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!], JobFilter.AND(JobExposers.Description.Equal("First draft job"))),
			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!, Setup.DraftJob2!], JobFilter.AND(JobExposers.Description.Contains("draft job"))),
			new Tuple<Job[], FilterElement<Job>>([], JobFilter.AND(JobExposers.Description.Contains("Unknown description"))),

			new Tuple<Job[], FilterElement<Job>>([Setup.TentativeJob3!], JobFilter.AND(JobExposers.Notes.Equal("Notes of the third job"))),
			new Tuple<Job[], FilterElement<Job>>(Setup.Jobs, JobFilter.AND(JobExposers.Notes.Contains("Notes of the"))),
			new Tuple<Job[], FilterElement<Job>>([], JobFilter.AND(JobExposers.Notes.Contains("Unknown notes"))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!], JobFilter.AND(JobExposers.Start.Equal(Setup.BaseTime.AddHours(1)))),
			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob2!, Setup.TentativeJob3!], JobFilter.AND(JobExposers.Start.GreaterThan(Setup.BaseTime.AddHours(2)))),
			new Tuple<Job[], FilterElement<Job>>([], JobFilter.AND(JobExposers.Start.Equal(Setup.BaseTime.AddDays(-1)))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!], JobFilter.AND(JobExposers.End.Equal(Setup.BaseTime.AddHours(2)))),
			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!, Setup.DraftJob2!], JobFilter.AND(JobExposers.End.LessThan(Setup.BaseTime.AddHours(5)))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!], JobFilter.AND(JobExposers.PreRollStart.Equal(Setup.BaseTime.AddMinutes(50)))),
			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob2!], JobFilter.AND(JobExposers.PreRollStart.Equal(Setup.BaseTime.AddHours(3)))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!], JobFilter.AND(JobExposers.PostRollEnd.Equal(Setup.BaseTime.AddHours(2).AddMinutes(10)))),
			new Tuple<Job[], FilterElement<Job>>([Setup.TentativeJob3!], JobFilter.AND(JobExposers.PostRollEnd.Equal(Setup.BaseTime.AddHours(6)))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!], JobFilter.AND(JobExposers.RecurringJobId.Equal(Setup.RecurringJobId))),
			new Tuple<Job[], FilterElement<Job>>([], JobFilter.AND(JobExposers.RecurringJobId.Equal(Guid.NewGuid()))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!, Setup.TentativeJob3!], JobFilter.AND(JobExposers.JobTypeCategoryId.Equal(Setup.CategoryA_Id.ToString()))),
			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob2!], JobFilter.AND(JobExposers.JobTypeCategoryId.Equal(Setup.CategoryB_Id.ToString()))),
			new Tuple<Job[], FilterElement<Job>>([], JobFilter.AND(JobExposers.JobTypeCategoryId.Equal(Guid.NewGuid().ToString()))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!], JobFilter.AND(JobExposers.Priority.Equal(JobPriority.High))),
			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob2!], JobFilter.AND(JobExposers.Priority.Equal(JobPriority.Normal))),
			new Tuple<Job[], FilterElement<Job>>([Setup.TentativeJob3!], JobFilter.AND(JobExposers.Priority.Equal(JobPriority.Low))),
			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob2!, Setup.TentativeJob3!], JobFilter.AND(JobExposers.Priority.NotEqual(JobPriority.High))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!, Setup.DraftJob2!], JobFilter.AND(JobExposers.State.Equal(JobState.Draft))),
			new Tuple<Job[], FilterElement<Job>>([Setup.TentativeJob3!], JobFilter.AND(JobExposers.State.Equal(JobState.Tentative))),
			new Tuple<Job[], FilterElement<Job>>([], JobFilter.AND(JobExposers.State.Equal(JobState.Canceled))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!], JobFilter.AND(JobExposers.OrganizationId.Equal(Setup.OrganizationId))),
			new Tuple<Job[], FilterElement<Job>>([], JobFilter.AND(JobExposers.OrganizationId.Equal(Guid.NewGuid()))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!], JobFilter.AND(JobExposers.OwnerId.Equal(Setup.OwnerId))),
			new Tuple<Job[], FilterElement<Job>>([], JobFilter.AND(JobExposers.OwnerId.Equal(Guid.NewGuid()))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!, Setup.DraftJob2!], JobFilter.AND(JobExposers.Capabilities.CapabilityId.Equal(Setup.Capability!.Id))),
			new Tuple<Job[], FilterElement<Job>>([], JobFilter.AND(JobExposers.Capabilities.CapabilityId.Equal(Guid.NewGuid()))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!], JobFilter.AND(JobExposers.Capabilities.Discretes.Contains("Belgium"))),
			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob2!], JobFilter.AND(JobExposers.Capabilities.Discretes.Contains("USA"))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!], JobFilter.AND(JobExposers.Capacities.CapacityId.Equal(Setup.Capacity!.Id))),
			new Tuple<Job[], FilterElement<Job>>([], JobFilter.AND(JobExposers.Capacities.CapacityId.Equal(Guid.NewGuid()))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!], JobFilter.AND(JobExposers.Configurations.ConfigurationId.Equal(Setup.Configuration!.Id))),
			new Tuple<Job[], FilterElement<Job>>([], JobFilter.AND(JobExposers.Configurations.ConfigurationId.Equal(Guid.NewGuid()))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!], JobFilter.AND(JobExposers.Properties.PropertyId.Equal(Setup.Property!.Id))),
			new Tuple<Job[], FilterElement<Job>>([], JobFilter.AND(JobExposers.Properties.PropertyId.Equal(Guid.NewGuid()))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob2!], JobFilter.AND(JobExposers.ActionRequired.Equal(true))),
			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!, Setup.TentativeJob3!], JobFilter.AND(JobExposers.ActionRequired.Equal(false))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob2!], JobFilter.AND(JobExposers.Nodes.ConfigurationState.Equal(ConfigurationState.MandatoryValuesMissing))),
			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!], JobFilter.AND(JobExposers.Nodes.ConfigurationState.Equal(ConfigurationState.NoParametersDefined))),
			new Tuple<Job[], FilterElement<Job>>([], JobFilter.AND(JobExposers.Nodes.ConfigurationState.Equal(ConfigurationState.AllValuesProvided))),

			new Tuple<Job[], FilterElement<Job>>([Setup.DraftJob1!, Setup.DraftJob2!], JobFilter.AND(JobExposers.ConfigurationState.Equal(ConfigurationState.AllValuesProvided))),
			new Tuple<Job[], FilterElement<Job>>([Setup.TentativeJob3!], JobFilter.AND(JobExposers.ConfigurationState.Equal(ConfigurationState.NoParametersDefined))),
			new Tuple<Job[], FilterElement<Job>>([], JobFilter.AND(JobExposers.ConfigurationState.Equal(ConfigurationState.MandatoryValuesMissing))),
		};

		[TestMethod]
		public void ReadJobsWithFilter()
		{
			foreach (var (expectedObjects, filter) in JobFilterTestCases)
			{
				var expectedObjectIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
				var actualObjectIds = TestContext.Api.Jobs.Read(filter).Select(x => x.Id).OrderBy(x => x).ToList();

				Assert.IsTrue(expectedObjectIds.SequenceEqual(actualObjectIds), filter.ToString());
			}
		}

		[TestMethod]
		public void CountJobsWithFilter()
		{
			foreach (var (expectedObjects, filter) in JobFilterTestCases)
			{
				Assert.AreEqual(expectedObjects.Length, TestContext.Api.Jobs.Count(filter), filter.ToString());
			}
		}

		[TestMethod]
		public void ReadJobsPagedWithFilter_DefaultPageSize()
		{
			foreach (var (expectedObjects, filter) in JobFilterTestCases)
			{
				var expectedObjectIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
				var pages = TestContext.Api.Jobs.ReadPaged(filter).ToList();
				var actualObjectIds = pages.SelectMany(x => x).Select(x => x.Id).OrderBy(x => x).ToList();

				Assert.AreEqual(1, pages.Count, filter.ToString());
				Assert.IsTrue(expectedObjectIds.SequenceEqual(actualObjectIds), filter.ToString());
			}
		}

		[TestMethod]
		public void ReadJobsPagedWithFilter_CustomPageSize()
		{
			foreach (var (expectedObjects, filter) in JobFilterTestCases)
			{
				var expectedObjectIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
				var pages = TestContext.Api.Jobs.ReadPaged(filter, 2).ToList();
				var actualObjectIds = pages.SelectMany(x => x).Select(x => x.Id).OrderBy(x => x).ToList();

				Assert.IsTrue(expectedObjectIds.SequenceEqual(actualObjectIds), filter.ToString());

				foreach (var page in pages)
				{
					Assert.IsTrue(page.Count() <= 2, filter.ToString());
				}
			}
		}
	}
}
