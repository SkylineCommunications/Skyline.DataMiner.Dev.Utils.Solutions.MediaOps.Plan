namespace RT_MediaOps.Plan.Workflow.Jobs
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
	public sealed class JobLinkTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public JobLinkTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void NewJob_HasNoLinks()
		{
			var job = new Job { Name = "Test" };

			Assert.IsNotNull(job.Links);
			Assert.AreEqual(0, job.Links.Count);
		}

		[TestMethod]
		public void CreateJob_WithLink_PersistsLink()
		{
			var prefix = Guid.NewGuid();
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{prefix}_Booking" });

			var job = NewJob($"{prefix}_Job");
			job.AddLink(new JobLink(objectType, "booking-1")
			{
				ObjectName = "Evening show",
				Url = "https://example.invalid/booking/1",
			});

			job = objectCreator.CreateJob(job);

			var returned = TestContext.Api.Jobs.Read(job.Id);
			Assert.IsNotNull(returned);
			Assert.AreEqual(1, returned.Links.Count);

			var link = returned.Links.Single();
			Assert.AreEqual(objectType.Id, link.ObjectTypeId);
			Assert.AreEqual("booking-1", link.ObjectId);
			Assert.AreEqual("Evening show", link.ObjectName);
			Assert.AreEqual("https://example.invalid/booking/1", link.Url);
			Assert.AreNotEqual(Guid.Empty, link.Id);
		}

		[TestMethod]
		public void CreateJob_WithLink_StoresJobAsParent()
		{
			var prefix = Guid.NewGuid();
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{prefix}_Booking" });

			var job = NewJob($"{prefix}_Job");
			job.AddLink(new JobLink(objectType, "booking-1") { ObjectName = "Evening show" });
			job = objectCreator.CreateJob(job);

			var jobObjectType = TestContext.Api.RelationshipObjectTypes
				.Read(RelationshipObjectTypeExposers.Name.Equal("Job"))
				.Single();

			var relationship = TestContext.Api.Relationships
				.Read(RelationshipExposers.Parent.ObjectId.Equal(job.Id.ToString()))
				.Single();

			Assert.AreEqual(jobObjectType.Id, relationship.Parent.ObjectTypeId);
			Assert.AreEqual(job.Id.ToString(), relationship.Parent.ObjectId);
			Assert.AreEqual(job.Name, relationship.Parent.ObjectName);
			Assert.IsNull(relationship.Parent.Url, "The job side of a link never carries a URL.");

			Assert.AreEqual(objectType.Id, relationship.Child.ObjectTypeId);
			Assert.AreEqual("booking-1", relationship.Child.ObjectId);
		}

		[TestMethod]
		public void UpdateJob_ChangingLinkDetails_UpdatesSameRelationship()
		{
			var prefix = Guid.NewGuid();
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{prefix}_Booking" });

			var job = NewJob($"{prefix}_Job");
			job.AddLink(new JobLink(objectType, "booking-1") { ObjectName = "Original" });
			job = objectCreator.CreateJob(job);

			var originalLinkId = job.Links.Single().Id;

			job.AddLink(new JobLink(objectType, "booking-1")
			{
				ObjectName = "Renamed",
				Url = "https://example.invalid/renamed",
			});
			TestContext.Api.Jobs.Update(job);

			var returned = TestContext.Api.Jobs.Read(job.Id);
			Assert.IsNotNull(returned);
			Assert.AreEqual(1, returned.Links.Count, "Adding a link to the same object must not create a duplicate.");

			var link = returned.Links.Single();
			Assert.AreEqual(originalLinkId, link.Id);
			Assert.AreEqual("Renamed", link.ObjectName);
			Assert.AreEqual("https://example.invalid/renamed", link.Url);
		}

		[TestMethod]
		public void UpdateJob_RemovingLink_DeletesRelationship()
		{
			var prefix = Guid.NewGuid();
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{prefix}_Booking" });

			var job = NewJob($"{prefix}_Job");
			job.AddLink(new JobLink(objectType, "booking-1"));
			job.AddLink(new JobLink(objectType, "booking-2"));
			job = objectCreator.CreateJob(job);

			Assert.AreEqual(2, job.Links.Count);
			var removedLinkId = job.Links.Single(x => x.ObjectId == "booking-1").Id;

			job.RemoveLink(new JobLink(objectType, "booking-1"));
			TestContext.Api.Jobs.Update(job);

			var returned = TestContext.Api.Jobs.Read(job.Id);
			Assert.IsNotNull(returned);
			Assert.AreEqual(1, returned.Links.Count);
			Assert.AreEqual("booking-2", returned.Links.Single().ObjectId);
			Assert.IsNull(TestContext.Api.Relationships.Read(removedLinkId));
		}

		[TestMethod]
		public void UpdateJob_SetLinks_ReplacesCollection()
		{
			var prefix = Guid.NewGuid();
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{prefix}_Booking" });

			var job = NewJob($"{prefix}_Job");
			job.AddLink(new JobLink(objectType, "booking-1"));
			job.AddLink(new JobLink(objectType, "booking-2"));
			job = objectCreator.CreateJob(job);

			job.SetLinks([new JobLink(objectType, "booking-2") { ObjectName = "Kept" }, new JobLink(objectType, "booking-3")]);
			TestContext.Api.Jobs.Update(job);

			var returned = TestContext.Api.Jobs.Read(job.Id);
			Assert.IsNotNull(returned);
			CollectionAssert.AreEquivalent(new[] { "booking-2", "booking-3" }, returned.Links.Select(x => x.ObjectId).ToArray());
			Assert.AreEqual("Kept", returned.Links.Single(x => x.ObjectId == "booking-2").ObjectName);
		}

		[TestMethod]
		public void DeleteJob_RemovesLinks()
		{
			var prefix = Guid.NewGuid();
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{prefix}_Booking" });

			var job = NewJob($"{prefix}_Job");
			job.AddLink(new JobLink(objectType, "booking-1"));
			job = objectCreator.CreateJob(job);

			var linkId = job.Links.Single().Id;

			TestContext.Api.Jobs.Delete(job);

			Assert.IsNull(TestContext.Api.Relationships.Read(linkId));
		}

		[TestMethod]
		public void DuplicateJob_CopiesLinksWithNewRelationshipIds()
		{
			var prefix = Guid.NewGuid();
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{prefix}_Booking" });

			var job = NewJob($"{prefix}_Job");
			job.AddLink(new JobLink(objectType, "booking-1") { ObjectName = "Evening show" });
			job = objectCreator.CreateJob(job);

			var originalLinkId = job.Links.Single().Id;

			var duplicate = job.Duplicate();
			duplicate.Name = $"{prefix}_Duplicate";
			duplicate = objectCreator.CreateJob(duplicate);

			var returned = TestContext.Api.Jobs.Read(duplicate.Id);
			Assert.IsNotNull(returned);
			Assert.AreEqual(1, returned.Links.Count);

			var link = returned.Links.Single();
			Assert.AreEqual(objectType.Id, link.ObjectTypeId);
			Assert.AreEqual("booking-1", link.ObjectId);
			Assert.AreEqual("Evening show", link.ObjectName);
			Assert.AreNotEqual(originalLinkId, link.Id, "The duplicate must own a separate relationship.");

			Assert.IsNotNull(TestContext.Api.Relationships.Read(originalLinkId), "The original job keeps its own link.");
		}

		[TestMethod]
		public void ReadJob_WithLinkStoredJobAsChild_StillExposesOtherEndpoint()
		{
			var prefix = Guid.NewGuid();
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{prefix}_Booking" });

			var job = objectCreator.CreateJob(NewJob($"{prefix}_Job"));

			var jobObjectType = TestContext.Api.RelationshipObjectTypes
				.Read(RelationshipObjectTypeExposers.Name.Equal("Job"))
				.Single();

			// The solution always writes the job as parent, but every reader handles the reverse defensively.
			objectCreator.CreateRelationship(new Relationship(new RelationshipData
			{
				Parent = new RelationshipEndpoint(objectType, "booking-1") { ObjectName = "Evening show" },
				Child = new RelationshipEndpoint(jobObjectType, job.Id.ToString()) { ObjectName = job.Name },
			}));

			var returned = TestContext.Api.Jobs.Read(job.Id);
			Assert.IsNotNull(returned);
			Assert.AreEqual(1, returned.Links.Count);

			var link = returned.Links.Single();
			Assert.AreEqual(objectType.Id, link.ObjectTypeId);
			Assert.AreEqual("booking-1", link.ObjectId);
			Assert.AreEqual("Evening show", link.ObjectName);
		}

		[TestMethod]
		public void DuplicateJob_WithLinkStoredJobAsChild_StoresDuplicateAsParent()
		{
			var prefix = Guid.NewGuid();
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{prefix}_Booking" });

			var job = objectCreator.CreateJob(NewJob($"{prefix}_Job"));
			var jobObjectType = ReadJobObjectType();

			objectCreator.CreateRelationship(new Relationship(new RelationshipData
			{
				Parent = new RelationshipEndpoint(objectType, "booking-1") { ObjectName = "Evening show" },
				Child = new RelationshipEndpoint(jobObjectType, job.Id.ToString()) { ObjectName = job.Name },
			}));

			var duplicate = TestContext.Api.Jobs.Read(job.Id).Duplicate();
			duplicate.Name = $"{prefix}_Duplicate";
			duplicate = objectCreator.CreateJob(duplicate);

			var relationship = TestContext.Api.Relationships
				.Read(RelationshipExposers.Parent.ObjectId.Equal(duplicate.Id.ToString()))
				.Single();

			Assert.AreEqual(jobObjectType.Id, relationship.Parent.ObjectTypeId, "A duplicated link is a new link, so the job goes on the parent side.");
			Assert.AreEqual(objectType.Id, relationship.Child.ObjectTypeId);
			Assert.AreEqual("booking-1", relationship.Child.ObjectId);
		}

		[TestMethod]
		public void CreateJob_WithLink_WithoutJobObjectType_ReportsError()
		{
			var prefix = Guid.NewGuid();
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{prefix}_Booking" });
			var jobObjectType = ReadJobObjectType();

			var job = NewJob($"{prefix}_Job");
			job.AddLink(new JobLink(objectType, "booking-1"));

			RenameJobObjectType(jobObjectType.Id, $"{prefix}_NotAJob");
			try
			{
				var exception = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreateJob(job));

				Assert.IsNotNull(exception.TraceData.ErrorData.OfType<JobLinkObjectTypeNotFoundError>().SingleOrDefault());
			}
			finally
			{
				RenameJobObjectType(jobObjectType.Id, "Job");
			}
		}

		private static RelationshipObjectType ReadJobObjectType()
			=> TestContext.Api.RelationshipObjectTypes.Read(RelationshipObjectTypeExposers.Name.Equal("Job")).Single();

		// Writes straight to DOM: the API refuses to create, rename or delete an object type with a reserved name.
		private static void RenameJobObjectType(Guid id, string name)
		{
			var helper = ((MediaOpsPlanApi)TestContext.Api).DomHelpers.SlcRelationshipsHelper;

			var instance = helper.GetObjectTypes(new[] { id }).Single();
			instance.ObjectTypeInfo.ObjectName = name;

			helper.DomHelper.DomInstances.Update(instance.ToInstance());
		}

		private static Job NewJob(string name)
		{
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			return new Job
			{
				Name = name,
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
		}
	}
}
