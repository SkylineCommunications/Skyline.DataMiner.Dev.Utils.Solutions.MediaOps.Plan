namespace RT_MediaOps.Plan.Workflow.Filtering
{
	using System;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	internal sealed class RecurringJobFilteringSetup
	{
		private readonly TestObjectCreator objectCreator;
		private readonly IntegrationTestContext testContext;

		public RecurringJobFilteringSetup(TestObjectCreator objectCreator, IntegrationTestContext testContext)
		{
			this.objectCreator = objectCreator;
			this.testContext = testContext;

			Prefix = Guid.NewGuid().ToString();
			BaseTime = new DateTimeOffset(DateTime.UtcNow.RoundToNextSecond());

			OrganizationId = Guid.NewGuid();
			OwnerId = Guid.NewGuid();

			CreateRecurringJobs();
		}

		public string Prefix { get; }

		public DateTimeOffset BaseTime { get; }

		public Guid OrganizationId { get; }

		public Guid OwnerId { get; }

		public DateTimeOffset EndDate1 => BaseTime.AddDays(10);

		public DateTimeOffset EndDate2 => BaseTime.AddDays(20);

		public RecurringJob[] RecurringJobs => new[]
		{
			RecurringJob1!,
			RecurringJob2!,
			RecurringJob3!,
		};

		public RecurringJob? RecurringJob1 { get; private set; }

		public RecurringJob? RecurringJob2 { get; private set; }

		public RecurringJob? RecurringJob3 { get; private set; }

		private void CreateRecurringJobs()
		{
			var recurringJob1 = new RecurringJob
			{
				Name = $"RecurringJob_1_{Prefix}",
				Description = "First recurring job",
				Notes = "Notes of the first recurring job",
				Priority = RecurringJobPriority.High,
				Start = BaseTime.AddHours(1),
				Duration = TimeSpan.FromHours(1),
				PreRollDuration = TimeSpan.FromMinutes(10),
				PostRollDuration = TimeSpan.FromMinutes(10),
				DesiredJobState = DesiredJobState.Draft,
				JobTypeCategoryId = "Scheduling",
				OrganizationId = OrganizationId,
				OwnerId = OwnerId,
			};

			recurringJob1.Pattern.RepeatType = RepeatType.Daily;
			recurringJob1.Pattern.RepeatEvery = 1;
			recurringJob1.Pattern.EndDate = EndDate1;

			var recurringJob2 = new RecurringJob
			{
				Name = $"RecurringJob_2_{Prefix}",
				Description = "Second recurring job",
				Notes = "Notes of the second recurring job",
				Priority = RecurringJobPriority.Normal,
				Start = BaseTime.AddHours(3),
				Duration = TimeSpan.FromHours(2),
				DesiredJobState = DesiredJobState.Tentative,
			};

			recurringJob2.Pattern.RepeatType = RepeatType.Daily;
			recurringJob2.Pattern.RepeatEvery = 2;
			recurringJob2.Pattern.EndDate = EndDate2;

			var recurringJob3 = new RecurringJob
			{
				Name = $"RecurringJob_3_{Prefix}",
				Description = "Third recurring job",
				Notes = "Notes of the third recurring job",
				Priority = RecurringJobPriority.Low,
				Start = BaseTime.AddHours(5),
				Duration = TimeSpan.FromHours(3),
				DesiredJobState = DesiredJobState.Tentative,
			};

			recurringJob3.Pattern.RepeatType = RepeatType.Daily;
			recurringJob3.Pattern.RepeatEvery = 3;
			recurringJob3.Pattern.EndDate = EndDate2;

			RecurringJob1 = objectCreator.CreateRecurringJob(recurringJob1);
			RecurringJob2 = objectCreator.CreateRecurringJob(recurringJob2);
			RecurringJob3 = objectCreator.CreateRecurringJob(recurringJob3);

			RecurringJob1 = testContext.Api.RecurringJobs.Read(RecurringJob1.Id);
			RecurringJob2 = testContext.Api.RecurringJobs.Read(RecurringJob2.Id);
			RecurringJob3 = testContext.Api.RecurringJobs.Read(RecurringJob3.Id);
		}
	}
}
