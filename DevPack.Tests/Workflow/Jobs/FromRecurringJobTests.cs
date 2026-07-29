namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class FromRecurringJobTests
	{
		private static readonly DateTimeOffset BaseStartTime = new DateTimeOffset(2024, 6, 1, 10, 0, 0, TimeSpan.Zero);

		[TestMethod]
		public void FromRecurringJob_NullRecurringJob_ThrowsArgumentNullException()
		{
			Assert.ThrowsException<ArgumentNullException>(() => Job.FromRecurringJob(null, BaseStartTime));
		}

		[TestMethod]
		public void FromRecurringJob_Name_IsCopied()
		{
			var recurringJob = new RecurringJob { Name = "My Recurring Job", Duration = TimeSpan.FromHours(1) };

			var job = Job.FromRecurringJob(recurringJob, BaseStartTime);

			Assert.AreEqual("My Recurring Job", job.Name);
		}

		[TestMethod]
		public void FromRecurringJob_Description_IsCopied()
		{
			var recurringJob = new RecurringJob { Name = "Test", Description = "A description", Duration = TimeSpan.FromHours(1) };

			var job = Job.FromRecurringJob(recurringJob, BaseStartTime);

			Assert.AreEqual("A description", job.Description);
		}

		[TestMethod]
		public void FromRecurringJob_Notes_IsCopied()
		{
			var recurringJob = new RecurringJob { Name = "Test", Notes = "Some notes", Duration = TimeSpan.FromHours(1) };

			var job = Job.FromRecurringJob(recurringJob, BaseStartTime);

			Assert.AreEqual("Some notes", job.Notes);
		}

		[TestMethod]
		public void FromRecurringJob_Priority_IsMappedCorrectly()
		{
			var recurringJob = new RecurringJob { Name = "Test", Priority = RecurringJobPriority.High, Duration = TimeSpan.FromHours(1) };

			var job = Job.FromRecurringJob(recurringJob, BaseStartTime);

			Assert.AreEqual(JobPriority.High, job.Priority);
		}

		[TestMethod]
		public void FromRecurringJob_LowPriority_IsMappedCorrectly()
		{
			var recurringJob = new RecurringJob { Name = "Test", Priority = RecurringJobPriority.Low, Duration = TimeSpan.FromHours(1) };

			var job = Job.FromRecurringJob(recurringJob, BaseStartTime);

			Assert.AreEqual(JobPriority.Low, job.Priority);
		}

		[TestMethod]
		public void FromRecurringJob_StartAndEnd_AreCalculatedCorrectly()
		{
			var duration = TimeSpan.FromHours(2);
			var recurringJob = new RecurringJob { Name = "Test", Duration = duration };

			var job = Job.FromRecurringJob(recurringJob, BaseStartTime);

			Assert.AreEqual(BaseStartTime, job.Start);
			Assert.AreEqual(BaseStartTime + duration, job.End);
		}

		[TestMethod]
		public void FromRecurringJob_PreRollAndPostRoll_AreCalculatedCorrectly()
		{
			var preRoll = TimeSpan.FromMinutes(15);
			var postRoll = TimeSpan.FromMinutes(10);
			var duration = TimeSpan.FromHours(1);
			var recurringJob = new RecurringJob
			{
				Name = "Test",
				Duration = duration,
				PreRollDuration = preRoll,
				PostRollDuration = postRoll,
			};

			var job = Job.FromRecurringJob(recurringJob, BaseStartTime);

			Assert.AreEqual(BaseStartTime - preRoll, job.PreRollStart);
			Assert.AreEqual(BaseStartTime + duration + postRoll, job.PostRollEnd);
		}

		[TestMethod]
		public void FromRecurringJob_OrganizationId_IsCopied()
		{
			var orgId = Guid.NewGuid();
			var recurringJob = new RecurringJob { Name = "Test", Duration = TimeSpan.FromHours(1), OrganizationId = orgId };

			var job = Job.FromRecurringJob(recurringJob, BaseStartTime);

			Assert.AreEqual(orgId, job.OrganizationId);
		}

		[TestMethod]
		public void FromRecurringJob_OwnerId_IsCopied()
		{
			var ownerId = Guid.NewGuid();
			var recurringJob = new RecurringJob { Name = "Test", Duration = TimeSpan.FromHours(1), OwnerId = ownerId };

			var job = Job.FromRecurringJob(recurringJob, BaseStartTime);

			Assert.AreEqual(ownerId, job.OwnerId);
		}

		[TestMethod]
		public void FromRecurringJob_CategoryId_IsCopied()
		{
			var recurringJob = new RecurringJob { Name = "Test", Duration = TimeSpan.FromHours(1), JobTypeCategoryId = "cat-001" };

			var job = Job.FromRecurringJob(recurringJob, BaseStartTime);

			Assert.AreEqual("cat-001", job.JobTypeCategoryId);
		}

		[TestMethod]
		public void FromRecurringJob_ContactIds_AreCopied()
		{
			var contact1 = Guid.NewGuid();
			var contact2 = Guid.NewGuid();
			var recurringJob = new RecurringJob { Name = "Test", Duration = TimeSpan.FromHours(1) };
			recurringJob.AddContact(contact1);
			recurringJob.AddContact(contact2);

			var job = Job.FromRecurringJob(recurringJob, BaseStartTime);

			CollectionAssert.AreEquivalent(new[] { contact1, contact2 }, new System.Collections.Generic.List<Guid>(job.ContactIds));
		}

		[TestMethod]
		public void FromRecurringJob_NoContacts_ResultHasEmptyContactIds()
		{
			var recurringJob = new RecurringJob { Name = "Test", Duration = TimeSpan.FromHours(1) };

			var job = Job.FromRecurringJob(recurringJob, BaseStartTime);

			Assert.AreEqual(0, job.ContactIds.Count);
		}

		[TestMethod]
		public void FromRecurringJob_AllMetadataFields_AreCopied()
		{
			var orgId = Guid.NewGuid();
			var ownerId = Guid.NewGuid();
			var contactId = Guid.NewGuid();
			var recurringJob = new RecurringJob
			{
				Name = "Full test",
				Description = "Full description",
				Priority = RecurringJobPriority.High,
				Duration = TimeSpan.FromHours(3),
				PreRollDuration = TimeSpan.FromMinutes(5),
				PostRollDuration = TimeSpan.FromMinutes(5),
				OrganizationId = orgId,
				OwnerId = ownerId,
				JobTypeCategoryId = "full-cat",
			};
			recurringJob.AddContact(contactId);

			var job = Job.FromRecurringJob(recurringJob, BaseStartTime);

			Assert.AreEqual("Full test", job.Name);
			Assert.AreEqual("Full description", job.Description);
			Assert.AreEqual(JobPriority.High, job.Priority);
			Assert.AreEqual(orgId, job.OrganizationId);
			Assert.AreEqual(ownerId, job.OwnerId);
			Assert.AreEqual("full-cat", job.JobTypeCategoryId);
			CollectionAssert.Contains(new System.Collections.Generic.List<Guid>(job.ContactIds), contactId);
		}
	}
}
