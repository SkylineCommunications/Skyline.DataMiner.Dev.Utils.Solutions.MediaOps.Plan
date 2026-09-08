namespace RT_MediaOps.Plan.Workflow.RecurringJobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class RecurringJobRelationshipTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public RecurringJobRelationshipTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void NewRecurringJob_HasNoRelationshipEndpoints()
		{
			var recurringJob = NewRecurringJob("Test");

			Assert.IsNotNull(recurringJob.RelationshipEndpoints);
			Assert.AreEqual(0, recurringJob.RelationshipEndpoints.Count);
		}

		[TestMethod]
		public void AddSetRemoveRelationshipEndpoints_ConfigureUnsavedRecurringJob()
		{
			var objectTypeId = Guid.NewGuid();
			var recurringJob = NewRecurringJob("Test");

			recurringJob.AddRelationshipEndpoint(new JobRelationshipEndpoint(objectTypeId) { ObjectId = "booking-1", ObjectName = "Original" });
			recurringJob.AddRelationshipEndpoint(new JobRelationshipEndpoint(objectTypeId) { ObjectId = "booking-1", ObjectName = "Updated" });

			Assert.AreEqual(1, recurringJob.RelationshipEndpoints.Count);
			Assert.AreEqual("Updated", recurringJob.RelationshipEndpoints.Single().ObjectName);

			recurringJob.SetRelationshipEndpoints([
				new JobRelationshipEndpoint(objectTypeId) { ObjectId = "booking-2" },
				new JobRelationshipEndpoint(objectTypeId) { ObjectId = "booking-3" },
			]);
			recurringJob.RemoveRelationshipEndpoint(new JobRelationshipEndpoint(objectTypeId) { ObjectId = "booking-2" });

			Assert.AreEqual("booking-3", recurringJob.RelationshipEndpoints.Single().ObjectId);
		}

		[TestMethod]
		public void CreateRecurringJob_WithRelationshipEndpoint_PersistsEndpointAndStoresOwnerAsParent()
		{
			var prefix = Guid.NewGuid();
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{prefix}_Booking" });
			var recurringJob = NewRecurringJob($"{prefix}_RecurringJob");
			recurringJob.AddRelationshipEndpoint(new JobRelationshipEndpoint(objectType)
			{
				ObjectId = "booking-1",
				ObjectName = "Evening show",
				Url = "https://example.invalid/booking/1",
			});

			recurringJob = objectCreator.CreateRecurringJob(recurringJob);
			var returned = TestContext.Api.RecurringJobs.Read(recurringJob.Id);
			var endpoint = returned.RelationshipEndpoints.Single();

			Assert.AreEqual(objectType.Id, endpoint.ObjectTypeId);
			Assert.AreEqual("booking-1", endpoint.ObjectId);
			Assert.AreEqual("Evening show", endpoint.ObjectName);
			Assert.AreEqual("https://example.invalid/booking/1", endpoint.Url);
			Assert.AreNotEqual(Guid.Empty, endpoint.Id);

			var jobObjectType = ReadJobObjectType();
			var relationship = TestContext.Api.Relationships.Read(endpoint.Id);
			Assert.AreEqual(jobObjectType.Id, relationship.Parent.ObjectTypeId);
			Assert.AreEqual(recurringJob.Id.ToString(), relationship.Parent.ObjectId);
			Assert.AreEqual(recurringJob.Name, relationship.Parent.ObjectName);
			Assert.AreEqual(objectType.Id, relationship.Child.ObjectTypeId);
		}

		[TestMethod]
		public void ReadRecurringJob_WithRelationshipStoredOwnerAsChild_ExposesEndpoint()
		{
			var prefix = Guid.NewGuid();
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{prefix}_Booking" });
			var recurringJob = objectCreator.CreateRecurringJob(NewRecurringJob($"{prefix}_RecurringJob"));
			var jobObjectType = ReadJobObjectType();

			objectCreator.CreateRelationship(new Relationship(new RelationshipData
			{
				Parent = new RelationshipEndpoint(objectType, "booking-1") { ObjectName = "Evening show" },
				Child = new RelationshipEndpoint(jobObjectType, recurringJob.Id.ToString()) { ObjectName = recurringJob.Name },
			}));

			var endpoint = TestContext.Api.RecurringJobs.Read(recurringJob.Id).RelationshipEndpoints.Single();
			Assert.AreEqual(objectType.Id, endpoint.ObjectTypeId);
			Assert.AreEqual("booking-1", endpoint.ObjectId);
			Assert.AreEqual("Evening show", endpoint.ObjectName);
		}

		[TestMethod]
		public void DuplicateRecurringJob_CopiesRelationshipWithIndependentId()
		{
			var prefix = Guid.NewGuid();
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{prefix}_Booking" });
			var recurringJob = NewRecurringJob($"{prefix}_RecurringJob");
			recurringJob.AddRelationshipEndpoint(new JobRelationshipEndpoint(objectType) { ObjectId = "booking-1", ObjectName = "Evening show" });
			recurringJob = objectCreator.CreateRecurringJob(recurringJob);
			var originalRelationshipId = recurringJob.RelationshipEndpoints.Single().Id;

			var duplicate = recurringJob.Duplicate();
			duplicate.Name = $"{prefix}_Duplicate";
			duplicate = objectCreator.CreateRecurringJob(duplicate);

			var duplicateEndpoint = TestContext.Api.RecurringJobs.Read(duplicate.Id).RelationshipEndpoints.Single();
			Assert.AreEqual("booking-1", duplicateEndpoint.ObjectId);
			Assert.AreNotEqual(originalRelationshipId, duplicateEndpoint.Id);
			Assert.IsNotNull(TestContext.Api.Relationships.Read(originalRelationshipId));
		}

		[TestMethod]
		public void FromJob_CopiesPersistedRelationshipWithIndependentId()
		{
			var prefix = Guid.NewGuid();
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{prefix}_Booking" });
			var job = NewJob($"{prefix}_Job");
			job.AddRelationshipEndpoint(new JobRelationshipEndpoint(objectType) { ObjectId = "booking-1", ObjectName = "Evening show" });
			job = objectCreator.CreateJob(job);
			var sourceRelationshipId = job.RelationshipEndpoints.Single().Id;

			var recurringJob = RecurringJob.FromJob(TestContext.Api.Jobs.Read(job.Id));
			recurringJob.Pattern.RepeatType = RepeatType.Daily;
			recurringJob.Pattern.RepeatEvery = 1;
			recurringJob.Pattern.EndDate = DateTime.UtcNow.AddDays(10);
			recurringJob = objectCreator.CreateRecurringJob(recurringJob);

			var copiedEndpoint = TestContext.Api.RecurringJobs.Read(recurringJob.Id).RelationshipEndpoints.Single();
			Assert.AreEqual("booking-1", copiedEndpoint.ObjectId);
			Assert.AreNotEqual(sourceRelationshipId, copiedEndpoint.Id);
			Assert.IsNotNull(TestContext.Api.Relationships.Read(sourceRelationshipId));
		}

		[TestMethod]
		public void FromRecurringJob_CopiesLegacyRelationshipToEachGeneratedJobWithIndependentIds()
		{
			var prefix = Guid.NewGuid();
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{prefix}_Booking" });
			var recurringJob = objectCreator.CreateRecurringJob(NewRecurringJob($"{prefix}_RecurringJob"));
			var jobObjectType = ReadJobObjectType();
			var legacyRelationship = objectCreator.CreateRelationship(new Relationship(new RelationshipData
			{
				Parent = new RelationshipEndpoint(objectType, "booking-1") { ObjectName = "Evening show" },
				Child = new RelationshipEndpoint(jobObjectType, recurringJob.Id.ToString()) { ObjectName = recurringJob.Name },
			}));
			var storedRecurringJob = TestContext.Api.RecurringJobs.Read(recurringJob.Id);

			var firstJob = objectCreator.CreateJob(Job.FromRecurringJob(storedRecurringJob, DateTime.UtcNow.AddHours(2).RoundToNextSecond()));
			var secondJob = objectCreator.CreateJob(Job.FromRecurringJob(storedRecurringJob, DateTime.UtcNow.AddHours(4).RoundToNextSecond()));
			var firstEndpoint = TestContext.Api.Jobs.Read(firstJob.Id).RelationshipEndpoints.Single();
			var secondEndpoint = TestContext.Api.Jobs.Read(secondJob.Id).RelationshipEndpoints.Single();

			Assert.AreEqual("booking-1", firstEndpoint.ObjectId);
			Assert.AreEqual("booking-1", secondEndpoint.ObjectId);
			Assert.AreNotEqual(legacyRelationship.Id, firstEndpoint.Id);
			Assert.AreNotEqual(legacyRelationship.Id, secondEndpoint.Id);
			Assert.AreNotEqual(firstEndpoint.Id, secondEndpoint.Id);
			Assert.IsNotNull(TestContext.Api.Relationships.Read(legacyRelationship.Id));
		}

		[TestMethod]
		public void CreateRecurringJob_WithUrlOnlyEndpoints_KeepsSeparateEndpoints()
		{
			var prefix = Guid.NewGuid();
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{prefix}_Document" });
			var recurringJob = NewRecurringJob($"{prefix}_RecurringJob");
			recurringJob.AddRelationshipEndpoint(new JobRelationshipEndpoint(objectType) { ObjectName = "Spec", Url = "https://example.invalid/spec" });
			recurringJob.AddRelationshipEndpoint(new JobRelationshipEndpoint(objectType) { ObjectName = "Manual", Url = "https://example.invalid/manual" });

			recurringJob = objectCreator.CreateRecurringJob(recurringJob);
			var returned = TestContext.Api.RecurringJobs.Read(recurringJob.Id);

			Assert.AreEqual(2, returned.RelationshipEndpoints.Count);
			CollectionAssert.AreEquivalent(new[] { "Spec", "Manual" }, returned.RelationshipEndpoints.Select(x => x.ObjectName).ToArray());
		}

		[TestMethod]
		public void CreateRecurringJob_WithoutJobObjectType_ReportsError()
		{
			var prefix = Guid.NewGuid();
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{prefix}_Booking" });
			var jobObjectType = ReadJobObjectType();
			var recurringJob = NewRecurringJob($"{prefix}_RecurringJob");
			recurringJob.AddRelationshipEndpoint(new JobRelationshipEndpoint(objectType) { ObjectId = "booking-1" });

			RenameJobObjectType(jobObjectType.Id, $"{prefix}_NotAJob");
			try
			{
				var exception = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreateRecurringJob(recurringJob));
				Assert.IsNotNull(exception.TraceData.ErrorData.OfType<JobRelationshipObjectTypeNotFoundError>().SingleOrDefault());
			}
			finally
			{
				RenameJobObjectType(jobObjectType.Id, RelationshipObjectType.JobObjectTypeName);
			}
		}

		[TestMethod]
		public void DeleteRecurringJob_RemovesLoadedRelationships()
		{
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_Booking" });
			var recurringJob = NewRecurringJob($"{Guid.NewGuid()}_RecurringJob");
			recurringJob.AddRelationshipEndpoint(new JobRelationshipEndpoint(objectType) { ObjectId = "booking-1" });
			recurringJob = objectCreator.CreateRecurringJob(recurringJob);
			var relationshipId = recurringJob.RelationshipEndpoints.Single().Id;

			TestContext.Api.RecurringJobs.Delete(recurringJob);

			Assert.IsNull(TestContext.Api.Relationships.Read(relationshipId));
		}

		[TestMethod]
		public void DeleteRecurringJob_WithoutLoadingRelationships_RemovesRelationships()
		{
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_Booking" });
			var recurringJob = objectCreator.CreateRecurringJob(NewRecurringJob($"{Guid.NewGuid()}_RecurringJob"));
			var relationship = objectCreator.CreateRelationship(new Relationship(new RelationshipData
			{
				Parent = new RelationshipEndpoint(objectType, "booking-1"),
				Child = new RelationshipEndpoint(ReadJobObjectType(), recurringJob.Id.ToString()),
			}));
			var unreadRelationships = TestContext.Api.RecurringJobs.Read(recurringJob.Id);

			TestContext.Api.RecurringJobs.Delete(unreadRelationships);

			Assert.IsNull(TestContext.Api.Relationships.Read(relationship.Id));
		}

		[TestMethod]
		public void UpdateRecurringJob_AfterChangingRelationships_RemainsBlocked()
		{
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_Booking" });
			var recurringJob = objectCreator.CreateRecurringJob(NewRecurringJob($"{Guid.NewGuid()}_RecurringJob"));
			var read = TestContext.Api.RecurringJobs.Read(recurringJob.Id);
			read.AddRelationshipEndpoint(new JobRelationshipEndpoint(objectType) { ObjectId = "booking-1" });

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.RecurringJobs.Update(read));

			Assert.IsTrue(exception.TraceData.ErrorData.OfType<RecurringJobInvalidStateError>().Any());
			Assert.AreEqual(0, TestContext.Api.RecurringJobs.Read(recurringJob.Id).RelationshipEndpoints.Count);
		}

		private static RecurringJob NewRecurringJob(string name)
		{
			var recurringJob = new RecurringJob
			{
				Name = name,
				Start = DateTime.UtcNow.AddHours(1),
				Duration = TimeSpan.FromHours(1),
			};
			recurringJob.Pattern.RepeatType = RepeatType.Daily;
			recurringJob.Pattern.RepeatEvery = 1;
			recurringJob.Pattern.EndDate = DateTime.UtcNow.AddDays(10);
			return recurringJob;
		}

		private static Job NewJob(string name)
		{
			var start = DateTime.UtcNow.AddHours(1).RoundToNextSecond();
			return new Job
			{
				Name = name,
				Start = start,
				End = start.AddHours(1),
				PreRollStart = start,
				PostRollEnd = start.AddHours(1),
			};
		}

		private static RelationshipObjectType ReadJobObjectType()
			=> TestContext.Api.RelationshipObjectTypes.Read(RelationshipObjectTypeExposers.Name.Equal(RelationshipObjectType.JobObjectTypeName)).Single();

		private static void RenameJobObjectType(Guid id, string name)
		{
			var helper = ((MediaOpsPlanApi)TestContext.Api).DomHelpers.SlcRelationshipsHelper;
			var instance = helper.GetObjectTypes(new[] { id }).Single();
			instance.ObjectTypeInfo.ObjectName = name;
			helper.DomHelper.DomInstances.Update(instance.ToInstance());
		}
	}
}
