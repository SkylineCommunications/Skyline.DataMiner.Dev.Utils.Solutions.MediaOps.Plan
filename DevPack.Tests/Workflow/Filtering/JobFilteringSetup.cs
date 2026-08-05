namespace RT_MediaOps.Plan.Workflow.Filtering
{
	using System;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	internal sealed class JobFilteringSetup
	{
		private readonly TestObjectCreator objectCreator;
		private readonly IntegrationTestContext testContext;

		public JobFilteringSetup(TestObjectCreator objectCreator, IntegrationTestContext testContext)
		{
			this.objectCreator = objectCreator;
			this.testContext = testContext;

			Prefix = Guid.NewGuid().ToString();
			BaseTime = new DateTimeOffset(DateTime.UtcNow.RoundToNextSecond());

			OrganizationId = Guid.NewGuid();
			OwnerId = Guid.NewGuid();
			RecurringJobId = Guid.NewGuid();

			CreateJobs();
		}

		public string Prefix { get; }

		public DateTimeOffset BaseTime { get; }

		public Guid OrganizationId { get; }

		public Guid OwnerId { get; }

		public Guid RecurringJobId { get; }

		public Job[] Jobs => new[]
		{
			DraftJob1!,
			DraftJob2!,
			TentativeJob3!,
		};

		public Job? DraftJob1 { get; private set; }

		public Job? DraftJob2 { get; private set; }

		public Job? TentativeJob3 { get; private set; }

		private void CreateJobs()
		{
			var job1 = new Job(new JobData { Key = $"Key_1_{Prefix}" })
			{
				Name = $"Job_Draft_1_{Prefix}",
				Description = "First draft job",
				Notes = "Notes of the first job",
				Priority = JobPriority.High,
				Start = BaseTime.AddHours(1),
				End = BaseTime.AddHours(2),
				PreRollStart = BaseTime.AddMinutes(50),
				PostRollEnd = BaseTime.AddHours(2).AddMinutes(10),
				JobTypeCategoryId = $"Category_A_{Prefix}",
				OrganizationId = OrganizationId,
				OwnerId = OwnerId,
				RecurringJobId = RecurringJobId,
			};

			var job2 = new Job(new JobData { Key = $"Key_2_{Prefix}" })
			{
				Name = $"Job_Draft_2_{Prefix}",
				Description = "Second draft job",
				Notes = "Notes of the second job",
				Priority = JobPriority.Normal,
				Start = BaseTime.AddHours(3),
				End = BaseTime.AddHours(4),
				PreRollStart = BaseTime.AddHours(3),
				PostRollEnd = BaseTime.AddHours(4),
				JobTypeCategoryId = $"Category_B_{Prefix}",
			};

			var job3 = new Job(new JobData { Key = $"Key_3_{Prefix}" })
			{
				Name = $"Job_Tentative_3_{Prefix}",
				Description = "Third job",
				Notes = "Notes of the third job",
				Priority = JobPriority.Low,
				Start = BaseTime.AddHours(5),
				End = BaseTime.AddHours(6),
				PreRollStart = BaseTime.AddHours(5),
				PostRollEnd = BaseTime.AddHours(6),
				JobTypeCategoryId = $"Category_A_{Prefix}",
			};

			DraftJob1 = objectCreator.CreateJob(job1);
			DraftJob2 = objectCreator.CreateJob(job2);
			TentativeJob3 = objectCreator.CreateJob(job3);

			TentativeJob3 = testContext.Api.Jobs.SaveAsTentative(TentativeJob3);

			DraftJob1 = testContext.Api.Jobs.Read(DraftJob1.Id);
			DraftJob2 = testContext.Api.Jobs.Read(DraftJob2.Id);
			TentativeJob3 = testContext.Api.Jobs.Read(TentativeJob3.Id);
		}
	}
}
