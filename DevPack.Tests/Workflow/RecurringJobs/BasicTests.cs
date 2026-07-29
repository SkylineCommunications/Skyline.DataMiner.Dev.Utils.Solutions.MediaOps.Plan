namespace RT_MediaOps.Plan.Workflow.RecurringJobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	using SLDataGateway.API.Querying;

	using RecurringJobBehavior = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Behaviors.Recurringjob_Behavior;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class BasicTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public BasicTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

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
			var firstRecurringJob = TestContext.Api.RecurringJobs.Read().FirstOrDefault();
			if (firstRecurringJob == null && Config.IsQaOps)
			{
				Assert.Inconclusive("No recurring job exists on the QAOps system after package installation.");
			}

			var jobToVerify = TestContext.Api.RecurringJobs.Read(firstRecurringJob.Id);

			Assert.AreEqual(firstRecurringJob, jobToVerify);
		}

		[TestMethod]
		public void ReadRecurringJobByName()
		{
			var firstRecurringJob = TestContext.Api.RecurringJobs.Read().FirstOrDefault();
			if (firstRecurringJob == null && Config.IsQaOps)
			{
				Assert.Inconclusive("No recurring job exists on the QAOps system after package installation.");
			}

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

		[TestMethod]
		public void CountWithEmptyFilterReturnsZero()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<RecurringJob>(idsToRetrieve.Select(x => RecurringJobExposers.Id.Equal(x)).ToArray());

			var count = TestContext.Api.RecurringJobs.Count(emptyFilter);
			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public void CountByIdReturnsSingleRecurringJob()
		{
			var recurringJob = objectCreator.CreateRecurringJob(NewValidRecurringJob($"{Guid.NewGuid()}_RecurringJob"));

			var count = TestContext.Api.RecurringJobs.Count(RecurringJobExposers.Id.Equal(recurringJob.Id));
			Assert.AreEqual(1, count);
		}

		[TestMethod]
		public void CountByNameReturnsMatchingRecurringJob()
		{
			var name = $"{Guid.NewGuid()}_RecurringJob";

			objectCreator.CreateRecurringJob(NewValidRecurringJob(name));

			var count = TestContext.Api.RecurringJobs.Count(RecurringJobExposers.Name.Equal(name));
			Assert.AreEqual(1, count);
		}

		[TestMethod]
		public void CountByUnknownNameReturnsZero()
		{
			var count = TestContext.Api.RecurringJobs.Count(RecurringJobExposers.Name.Equal($"{Guid.NewGuid()}_Unknown"));
			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public void CountByUnknownIdReturnsZero()
		{
			var count = TestContext.Api.RecurringJobs.Count(RecurringJobExposers.Id.Equal(Guid.NewGuid()));
			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public void HappyPathCreate()
		{
			var prefix = Guid.NewGuid();
			var name = $"{prefix}_RecurringJob";

			var recurringJob = NewValidRecurringJob(name);
			recurringJob.Description = "Initial description";
			recurringJob.Duration = TimeSpan.FromMinutes(10);

			var created = objectCreator.CreateRecurringJob(recurringJob);

			var read = TestContext.Api.RecurringJobs.Read(recurringJob.Id);
			Assert.IsNotNull(read);
			Assert.AreEqual(name, read.Name);
			Assert.AreEqual("Initial description", read.Description);
			Assert.AreEqual(TimeSpan.FromMinutes(10), read.Duration);
			Assert.AreEqual(RecurringJobState.Active, read.State);
			Assert.AreEqual(RepeatType.Daily, read.Pattern.RepeatType);
			Assert.AreEqual(1, read.Pattern.RepeatEvery);
			Assert.AreEqual(created, read);
		}

		[TestMethod]
		public void CreateWithUserDefinedIdPersistsId()
		{
			var id = Guid.NewGuid();
			var recurringJob = NewValidRecurringJob($"{id}_RecurringJob", id);

			var created = objectCreator.CreateRecurringJob(recurringJob);

			Assert.AreEqual(id, created.Id);
			Assert.IsNotNull(TestContext.Api.RecurringJobs.Read(id));
		}

		[TestMethod]
		public void CreatePersistsBackendDomInstanceInActiveState()
		{
			var recurringJob = NewValidRecurringJob($"{Guid.NewGuid()}_RecurringJob");

			objectCreator.CreateRecurringJob(recurringJob);

			var planApi = (MediaOpsPlanApi)TestContext.Api;
			var instance = planApi.DomHelpers.SlcWorkflowHelper.GetRecurringJobs(new[] { recurringJob.Id }).SingleOrDefault();

			Assert.IsNotNull(instance, "Expected the backend recurring job DOM instance to exist after creation.");
			Assert.AreEqual(RecurringJobBehavior.StatusesEnum.Active, instance.Status);
			Assert.AreEqual(recurringJob.Name, instance.JobInfo.JobName);
		}

		[TestMethod]
		public void CreateWithInvalidPatternThrowsException()
		{
			// A default RecurringPattern has RepeatType.Never which is not a valid recurring pattern.
			var recurringJob = new RecurringJob
			{
				Name = $"{Guid.NewGuid()}_RecurringJob",
			};

			try
			{
				objectCreator.CreateRecurringJob(recurringJob);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<RecurringJobInvalidPatternError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected RecurringJobInvalidPatternError.");
				Assert.AreEqual(recurringJob.Id, error.Id);
				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void CreateWeeklyPatternWithoutWeekDaysThrowsException()
		{
			var recurringJob = new RecurringJob
			{
				Name = $"{Guid.NewGuid()}_RecurringJob",
			};
			recurringJob.Pattern.RepeatType = RepeatType.Weekly;
			recurringJob.Pattern.RepeatEvery = 1;

			try
			{
				objectCreator.CreateRecurringJob(recurringJob);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<RecurringJobInvalidPatternError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected RecurringJobInvalidPatternError.");
				Assert.AreEqual(recurringJob.Id, error.Id);
				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void CreateWithNameExceedingMaxLengthThrowsException()
		{
			var recurringJob = NewValidRecurringJob(new string('a', 151));

			try
			{
				objectCreator.CreateRecurringJob(recurringJob);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<RecurringJobInvalidNameError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected RecurringJobInvalidNameError.");
				Assert.AreEqual(recurringJob.Id, error.Id);
				Assert.AreEqual(recurringJob.Name, error.Name);
				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void CreateWithWhiteSpacesAsNameThrowsException()
		{
			var recurringJob = NewValidRecurringJob("   ");

			try
			{
				objectCreator.CreateRecurringJob(recurringJob);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<RecurringJobInvalidNameError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected RecurringJobInvalidNameError.");
				Assert.AreEqual(recurringJob.Id, error.Id);
				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void CreateWithDescriptionExceedingMaxSizeThrowsException()
		{
			var recurringJob = NewValidRecurringJob($"{Guid.NewGuid()}_RecurringJob");
			recurringJob.Description = new string('a', 32767);

			try
			{
				objectCreator.CreateRecurringJob(recurringJob);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<RecurringJobInvalidDescriptionError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected RecurringJobInvalidDescriptionError.");
				Assert.AreEqual(recurringJob.Id, error.Id);
				Assert.AreEqual(recurringJob.Description, error.Description);
				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void CreateWithPreRollNotMultipleOfSecondsThrowsException()
		{
			var recurringJob = NewValidRecurringJob($"{Guid.NewGuid()}_RecurringJob");
			recurringJob.PreRollDuration = TimeSpan.FromMilliseconds(1500);

			try
			{
				objectCreator.CreateRecurringJob(recurringJob);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<RecurringJobInvalidPreRollError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected RecurringJobInvalidPreRollError.");
				Assert.AreEqual(recurringJob.Id, error.Id);
				Assert.AreEqual(recurringJob.PreRollDuration, error.PreRollDuration);
				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void CreateWithNegativePostRollThrowsException()
		{
			var recurringJob = NewValidRecurringJob($"{Guid.NewGuid()}_RecurringJob");
			recurringJob.PostRollDuration = TimeSpan.FromSeconds(-30);

			try
			{
				objectCreator.CreateRecurringJob(recurringJob);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<RecurringJobInvalidPostRollError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected RecurringJobInvalidPostRollError.");
				Assert.AreEqual(recurringJob.Id, error.Id);
				Assert.AreEqual(recurringJob.PostRollDuration, error.PostRollDuration);
				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void CreateWithUserDefinedIdAlreadyInUseThrowsException()
		{
			var id = Guid.NewGuid();

			objectCreator.CreateRecurringJob(NewValidRecurringJob($"{id}_RecurringJob_1", id));

			var second = NewValidRecurringJob($"{id}_RecurringJob_2", id);

			try
			{
				objectCreator.CreateRecurringJob(second);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<RecurringJobIdInUseError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected RecurringJobIdInUseError.");
				Assert.AreEqual(id, error.Id);
				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void CreateBulkWithDuplicateIdsThrowsException()
		{
			var id = Guid.NewGuid();

			var first = NewValidRecurringJob($"{id}_RecurringJob_1", id);
			var second = NewValidRecurringJob($"{id}_RecurringJob_2", id);

			try
			{
				objectCreator.CreateRecurringJobs(new[] { first, second });
			}
			catch (MediaOpsBulkException<Guid> ex)
			{
				Assert.IsTrue(ex.Result.TraceDataPerItem.TryGetValue(id, out var traceData), "No trace data for duplicate ID.");
				var duplicateErrors = traceData.ErrorData.OfType<RecurringJobDuplicateIdError>().ToList();
				Assert.AreEqual(2, duplicateErrors.Count);
				Assert.IsTrue(duplicateErrors.All(e => e.Id == id));
				return;
			}

			Assert.Fail("Expected MediaOpsBulkException was not thrown.");
		}

		[TestMethod]
		public void UpdateExistingRecurringJobIsBlocked()
		{
			var recurringJob = objectCreator.CreateRecurringJob(NewValidRecurringJob($"{Guid.NewGuid()}_RecurringJob"));

			// Re-read so the recurring job is no longer considered new; updating an existing recurring job is not allowed.
			var read = TestContext.Api.RecurringJobs.Read(recurringJob.Id);
			read.Description = "Updated description";

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.RecurringJobs.Update(read));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<RecurringJobInvalidStateError>().Any(),
				"Expected a RecurringJobInvalidStateError when updating an existing recurring job.");

			// The stored recurring job must remain unchanged.
			var reread = TestContext.Api.RecurringJobs.Read(recurringJob.Id);
			Assert.AreEqual(recurringJob.Description, reread.Description);
		}

		[TestMethod]
		public void ReadByIdsReturnsRequestedRecurringJobs()
		{
			var prefix = Guid.NewGuid();

			var job1 = objectCreator.CreateRecurringJob(NewValidRecurringJob($"{prefix}_RecurringJob_1"));
			var job2 = objectCreator.CreateRecurringJob(NewValidRecurringJob($"{prefix}_RecurringJob_2"));

			var results = TestContext.Api.RecurringJobs.Read(new[] { job1.Id, job2.Id }).ToList();

			Assert.AreEqual(2, results.Count);
			Assert.IsTrue(results.Any(j => j.Id == job1.Id));
			Assert.IsTrue(results.Any(j => j.Id == job2.Id));
		}

		private static RecurringJob NewValidRecurringJob(string name)
		{
			return ConfigurePattern(new RecurringJob { Name = name });
		}

		private static RecurringJob NewValidRecurringJob(string name, Guid id)
		{
			return ConfigurePattern(new RecurringJob(id) { Name = name });
		}

		private static RecurringJob ConfigurePattern(RecurringJob recurringJob)
		{
			recurringJob.Pattern.RepeatType = RepeatType.Daily;
			recurringJob.Pattern.RepeatEvery = 1;
			return recurringJob;
		}
	}
}
