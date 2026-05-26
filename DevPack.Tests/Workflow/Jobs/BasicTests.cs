namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	using SLDataGateway.API.Querying;

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
		public void ReadAllJobs()
		{
			try
			{
				TestContext.Api.Jobs.Read().ToArray();
				return;
			}
			catch (Exception)
			{
				Assert.Fail();
			}
		}

		[TestMethod]
		public void ReadJobById()
		{
			var firstJob = TestContext.Api.Jobs.Read().FirstOrDefault();
			if (firstJob == null)
				return;

			var jobToVerify = TestContext.Api.Jobs.Read(firstJob.Id);

			Assert.AreEqual(firstJob, jobToVerify);
		}

		[TestMethod]
		public void ReadJobByName()
		{
			var firstJob = TestContext.Api.Jobs.Read().FirstOrDefault();
			if (firstJob == null)
				return;

			var jobToVerify = TestContext.Api.Jobs.Read(JobExposers.Name.Equal(firstJob.Name)).First();

			Assert.AreEqual(firstJob, jobToVerify);
		}

		[TestMethod]
		public void ReadWithEmptyFilterReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Job>(idsToRetrieve.Select(x => JobExposers.Id.Equal(x)).ToArray());

			var jobs = TestContext.Api.Jobs.Read(emptyFilter);
			Assert.IsNotNull(jobs);
			Assert.AreEqual(0, jobs.Count());
		}

		[TestMethod]
		public void CountWithEmptyFilterReturnsZero()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Job>(idsToRetrieve.Select(x => JobExposers.Id.Equal(x)).ToArray());

			var count = TestContext.Api.Jobs.Count(emptyFilter);
			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public void ReadWithEmptyQueryReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Job>(idsToRetrieve.Select(x => JobExposers.Id.Equal(x)).ToArray());
			var queryWithEmptyFilter = emptyFilter.ToQuery();

			var jobs = TestContext.Api.Jobs.Read(queryWithEmptyFilter);
			Assert.IsNotNull(jobs);
			Assert.AreEqual(0, jobs.Count());
		}

		[TestMethod]
		public void HappyPathCrud()
		{
			var prefix = Guid.NewGuid();
			var name = $"{prefix}_Job";

			var job = new Job
			{
				Name = name,
				Description = "Initial description",
				Notes = "Initial notes",
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(10),
			};

			job = objectCreator.CreateJob(job);

			var read = TestContext.Api.Jobs.Read(job.Id);
			Assert.IsNotNull(read);
			Assert.AreEqual(name, read.Name);
			Assert.AreEqual("Initial description", read.Description);
			Assert.AreEqual("Initial notes", read.Notes);

			// Update
			var updatedName = name + "_updated";
			read.Name = updatedName;
			read.Description = "Updated description";
			var updated = TestContext.Api.Jobs.Update(read);
			Assert.AreEqual(updatedName, updated.Name);
			Assert.AreEqual("Updated description", updated.Description);

			var rereadAfterUpdate = TestContext.Api.Jobs.Read(job.Id);
			Assert.IsNotNull(rereadAfterUpdate);
			Assert.AreEqual(updatedName, rereadAfterUpdate.Name);
			Assert.AreEqual("Updated description", rereadAfterUpdate.Description);

			// Delete
			TestContext.Api.Jobs.Delete(rereadAfterUpdate);
			Assert.IsNull(TestContext.Api.Jobs.Read(job.Id));
		}

		[TestMethod]
		public void CreateOrUpdate_CreatesAndUpdatesInSameCall()
		{
			var prefix = Guid.NewGuid();

			var newJob = new Job
			{
				Name = $"{prefix}_Job_New",
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
			};

			var existingJob = new Job
			{
				Name = $"{prefix}_Job_Existing",
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
			};
			existingJob = objectCreator.CreateJob(existingJob);

			existingJob.Description = "Updated via CreateOrUpdate";

			var results = TestContext.Api.Jobs.CreateOrUpdate(new[] { newJob, existingJob });
			objectCreator.StoreJobIds(results.Select(j => j.Id));

			Assert.AreEqual(2, results.Count);

			var rereadExisting = TestContext.Api.Jobs.Read(existingJob.Id);
			Assert.AreEqual("Updated via CreateOrUpdate", rereadExisting.Description);

			var rereadNew = TestContext.Api.Jobs.Read(results.Single(r => r.Id != existingJob.Id).Id);
			Assert.IsNotNull(rereadNew);
			Assert.AreEqual($"{prefix}_Job_New", rereadNew.Name);
		}

		[TestMethod]
		public void CreateOnExistingJobThrowsInvalidOperation()
		{
			var job = new Job
			{
				Name = $"{Guid.NewGuid()}_Job",
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
			};

			job = objectCreator.CreateJob(job);

			Assert.ThrowsException<InvalidOperationException>(() => TestContext.Api.Jobs.Create(job));
		}

		[TestMethod]
		public void UpdateOnNewJobThrowsInvalidOperation()
		{
			var job = new Job
			{
				Name = $"{Guid.NewGuid()}_Job",
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
			};

			Assert.ThrowsException<InvalidOperationException>(() => TestContext.Api.Jobs.Update(job));
		}

		[TestMethod]
		public void CreateWithUserDefinedIdAlreadyInUseThrowsException()
		{
			var id = Guid.NewGuid();

			var first = new Job(id)
			{
				Name = $"{id}_Job_1",
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
			};
			objectCreator.CreateJob(first);

			var second = new Job(id)
			{
				Name = $"{id}_Job_2",
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
			};

			try
			{
				objectCreator.CreateJob(second);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobIdInUseError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected JobIdInUseError.");
				Assert.AreEqual(id, error.Id);
				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void CreateBulkWithDuplicateIdsThrowsException()
		{
			var id = Guid.NewGuid();

			var job1 = new Job(id)
			{
				Name = $"{id}_Job_1",
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
			};

			var job2 = new Job(id)
			{
				Name = $"{id}_Job_2",
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
			};

			try
			{
				objectCreator.CreateJobs(new[] { job1, job2 });
			}
			catch (MediaOpsBulkException<Guid> ex)
			{
				Assert.IsTrue(ex.Result.TraceDataPerItem.TryGetValue(id, out var traceData), "No trace data for duplicate ID.");
				var duplicateErrors = traceData.ErrorData.OfType<JobDuplicateIdError>().ToList();
				Assert.AreEqual(2, duplicateErrors.Count);
				Assert.IsTrue(duplicateErrors.All(e => e.Id == id));
				return;
			}

			Assert.Fail("Expected MediaOpsBulkException was not thrown.");
		}

		[TestMethod]
		public void CreateWithNameExceedingMaxLengthThrowsException()
		{
			var job = new Job
			{
				Name = new string('a', 151),
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
			};

			try
			{
				objectCreator.CreateJob(job);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobInvalidNameError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected JobInvalidNameError.");
				Assert.AreEqual(job.Id, error.Id);
				Assert.AreEqual(job.Name, error.Name);
				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void CreateWithEmptyNameThrowsException()
		{
			var job = new Job
			{
				Key = $"{Guid.NewGuid()}_Key",
				Name = "   ",
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
			};

			try
			{
				objectCreator.CreateJob(job);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobInvalidNameError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected JobInvalidNameError.");
				Assert.AreEqual(job.Id, error.Id);
				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void CreateWithKeyExceedingMaxLengthThrowsException()
		{
			var job = new Job
			{
				Key = new string('a', 151),
				Name = $"{Guid.NewGuid()}_Job",
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
			};

			try
			{
				objectCreator.CreateJob(job);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobInvalidKeyError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected JobInvalidKeyError.");
				Assert.AreEqual(job.Id, error.Id);
				Assert.AreEqual(job.Key, error.Key);
				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void CreateWithEndBeforeStartThrowsException()
		{
			var start = DateTime.UtcNow;
			var job = new Job
			{
				Name = $"{Guid.NewGuid()}_Job",
				Start = start,
				End = start.AddMinutes(-5),
			};

			try
			{
				objectCreator.CreateJob(job);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobInvalidTimingError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected JobInvalidTimingError.");
				Assert.AreEqual(job.Id, error.Id);
				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void CreateWithDescriptionExceedingMaxSizeThrowsException()
		{
			var job = new Job
			{
				Name = $"{Guid.NewGuid()}_Job",
				Description = new string('a', 32767),
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
			};

			try
			{
				objectCreator.CreateJob(job);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobInvalidDescriptionError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected JobInvalidDescriptionError.");
				Assert.AreEqual(job.Id, error.Id);
				Assert.AreEqual(job.Description, error.Description);
				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void CreateWithNotesExceedingMaxSizeThrowsException()
		{
			var job = new Job
			{
				Name = $"{Guid.NewGuid()}_Job",
				Notes = new string('a', 32767),
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
			};

			try
			{
				objectCreator.CreateJob(job);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobInvalidNotesError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected JobInvalidNotesError.");
				Assert.AreEqual(job.Id, error.Id);
				Assert.AreEqual(job.Notes, error.Notes);
				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void CreateWithNegativePreRollThrowsException()
		{
			var job = new Job
			{
				Name = $"{Guid.NewGuid()}_Job",
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
				PreRoll = TimeSpan.FromSeconds(-1),
			};

			try
			{
				objectCreator.CreateJob(job);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobInvalidPreRollError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected JobInvalidPreRollError.");
				Assert.AreEqual(job.Id, error.Id);
				Assert.AreEqual(TimeSpan.FromSeconds(-1), error.PreRoll);
				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void CreateWithNegativePostRollThrowsException()
		{
			var job = new Job
			{
				Name = $"{Guid.NewGuid()}_Job",
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
				PostRoll = TimeSpan.FromSeconds(-1),
			};

			try
			{
				objectCreator.CreateJob(job);
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobInvalidPostRollError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected JobInvalidPostRollError.");
				Assert.AreEqual(job.Id, error.Id);
				Assert.AreEqual(TimeSpan.FromSeconds(-1), error.PostRoll);
				return;
			}

			Assert.Fail("Expected MediaOpsException was not thrown.");
		}

		[TestMethod]
		public void DeleteByIdRemovesJob()
		{
			var job = new Job
			{
				Name = $"{Guid.NewGuid()}_Job",
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
			};
			job = objectCreator.CreateJob(job);

			TestContext.Api.Jobs.Delete(job.Id);

			Assert.IsNull(TestContext.Api.Jobs.Read(job.Id));
		}

		[TestMethod]
		public void DeleteBulkByIdsRemovesJobs()
		{
			var prefix = Guid.NewGuid();

			var job1 = objectCreator.CreateJob(new Job
			{
				Name = $"{prefix}_Job_1",
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
			});

			var job2 = objectCreator.CreateJob(new Job
			{
				Name = $"{prefix}_Job_2",
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
			});

			TestContext.Api.Jobs.Delete(new[] { job1.Id, job2.Id });

			Assert.IsNull(TestContext.Api.Jobs.Read(job1.Id));
			Assert.IsNull(TestContext.Api.Jobs.Read(job2.Id));
		}

		[TestMethod]
		public void DeleteUnknownIdDoesNotThrow()
		{
			TestContext.Api.Jobs.Delete(Guid.NewGuid());
		}

		[TestMethod]
		public void ReadByIdsReturnsRequestedJobs()
		{
			var prefix = Guid.NewGuid();

			var job1 = objectCreator.CreateJob(new Job
			{
				Name = $"{prefix}_Job_1",
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
			});

			var job2 = objectCreator.CreateJob(new Job
			{
				Name = $"{prefix}_Job_2",
				Start = DateTime.UtcNow,
				End = DateTime.UtcNow.AddMinutes(5),
			});

			var results = TestContext.Api.Jobs.Read(new[] { job1.Id, job2.Id }).ToList();

			Assert.AreEqual(2, results.Count);
			Assert.IsTrue(results.Any(j => j.Id == job1.Id));
			Assert.IsTrue(results.Any(j => j.Id == job2.Id));
		}
	}
}
