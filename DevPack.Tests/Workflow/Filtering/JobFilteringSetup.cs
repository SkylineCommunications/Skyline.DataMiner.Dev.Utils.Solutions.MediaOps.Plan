namespace RT_MediaOps.Plan.Workflow.Filtering
{
	using System;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.Categories.API;
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
			CategoryA_Id = Guid.NewGuid();
			CategoryB_Id = Guid.NewGuid();

			var scope = GetJobScope();
			CreateRecurringJob();
			CreateCategories(scope);
			CreateOrchestrationParameters();
			CreateSchedulingProperty();
			CreateJobs();
		}

		public string Prefix { get; }

		public DateTimeOffset BaseTime { get; }

		public Guid OrganizationId { get; }

		public Guid OwnerId { get; }

		public Guid RecurringJobId { get; }

		public Guid CategoryA_Id { get; }

		public Guid CategoryB_Id { get; }

		public Job[] Jobs => new[]
		{
			DraftJob1!,
			DraftJob2!,
			TentativeJob3!,
		};

		public Job? DraftJob1 { get; private set; }

		public Job? DraftJob2 { get; private set; }

		public Job? TentativeJob3 { get; private set; }

		public Capability? Capability { get; private set; }

		public NumberCapacity? Capacity { get; private set; }

		public TextConfiguration? Configuration { get; private set; }

		public StringProperty? Property { get; private set; }

		private void CreateOrchestrationParameters()
		{
			var capability = new Capability
			{
				Name = $"Capability_{Prefix}",
			};

			capability.SetDiscretes(["USA", "Belgium"]);

			Capability = objectCreator.CreateCapability(capability);

			Capacity = (NumberCapacity)objectCreator.CreateCapacity(new NumberCapacity
			{
				Name = $"Capacity_{Prefix}",
			});

			Configuration = objectCreator.CreateConfiguration(new TextConfiguration
			{
				Name = $"Configuration_{Prefix}",
				IsMandatory = false,
			});
		}

		private void CreateSchedulingProperty()
		{
			Property = objectCreator.CreateSchedulingProperty(new StringProperty
			{
				Name = $"Property_{Prefix}",
				SectionName = "General",
			});
		}

		private void CreateCategories(Scope scope)
		{
			objectCreator.CreateCategory(new Category(CategoryA_Id)
			{
				Name = $"CategoryA_{Guid.NewGuid()}",
				Scope = scope,
			});

			objectCreator.CreateCategory(new Category(CategoryB_Id)
			{
				Name = $"CategoryB_{Guid.NewGuid()}",
				Scope = scope,
			});
		}

		private Scope GetJobScope()
		{
			return testContext.CategoriesApi.Scopes.Read(CategoryScopes.JobTypes)
				?? throw new InvalidOperationException($"Category Scope '{CategoryScopes.JobTypes}' is not available");
		}

		private void CreateRecurringJob()
		{
			var recurringJob = new RecurringJob(RecurringJobId)
			{
				Name = $"RecurringJob_{Prefix}",
				Duration = TimeSpan.FromHours(1),
				Start = DateTimeOffset.UtcNow.AddHours(5),
			};

			recurringJob.Pattern.EndDate = DateTimeOffset.UtcNow.AddDays(10);
			recurringJob.Pattern.RepeatEvery = 1;
			recurringJob.Pattern.RepeatType = RepeatType.Daily;

			objectCreator.CreateRecurringJob(recurringJob);
		}

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
				JobTypeCategoryId = CategoryA_Id.ToString(),
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
				JobTypeCategoryId = CategoryB_Id.ToString(),
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
				JobTypeCategoryId = CategoryA_Id.ToString(),
			};

			job1.OrchestrationSettings.AddCapability(new CapabilitySetting(Capability!) { Value = "Belgium" });
			job1.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(Capacity!) { Value = 20 });
			job1.OrchestrationSettings.AddConfiguration(new TextConfigurationSetting(Configuration!) { Value = "First" });
			job1.AddProperty(new StringPropertySetting(Property!) { Value = "First" });

			job2.OrchestrationSettings.AddCapability(new CapabilitySetting(Capability!) { Value = "USA" });

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
