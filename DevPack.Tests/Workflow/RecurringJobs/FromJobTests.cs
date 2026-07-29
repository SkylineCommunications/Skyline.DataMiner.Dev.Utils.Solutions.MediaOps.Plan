namespace RT_MediaOps.Plan.Workflow.RecurringJobs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class FromJobTests
	{
		private static readonly DateTimeOffset BaseStartTime = new DateTimeOffset(2024, 6, 1, 10, 0, 0, TimeSpan.Zero);

		[TestMethod]
		public void FromJob_NullJob_ThrowsArgumentNullException()
		{
			Assert.ThrowsException<ArgumentNullException>(() => RecurringJob.FromJob(null));
		}

		[TestMethod]
		public void FromJob_Name_IsCopied()
		{
			var job = new Job { Name = "My Job", Start = BaseStartTime, End = BaseStartTime + TimeSpan.FromHours(1) };

			var recurringJob = RecurringJob.FromJob(job);

			Assert.AreEqual("My Job", recurringJob.Name);
		}

		[TestMethod]
		public void FromJob_Description_IsCopied()
		{
			var job = new Job { Name = "Test", Description = "A description", Start = BaseStartTime, End = BaseStartTime + TimeSpan.FromHours(1) };

			var recurringJob = RecurringJob.FromJob(job);

			Assert.AreEqual("A description", recurringJob.Description);
		}

		[TestMethod]
		public void FromJob_Priority_IsMappedCorrectly()
		{
			var job = new Job { Name = "Test", Priority = JobPriority.High, Start = BaseStartTime, End = BaseStartTime + TimeSpan.FromHours(1) };

			var recurringJob = RecurringJob.FromJob(job);

			Assert.AreEqual(RecurringJobPriority.High, recurringJob.Priority);
		}

		[TestMethod]
		public void FromJob_LowPriority_IsMappedCorrectly()
		{
			var job = new Job { Name = "Test", Priority = JobPriority.Low, Start = BaseStartTime, End = BaseStartTime + TimeSpan.FromHours(1) };

			var recurringJob = RecurringJob.FromJob(job);

			Assert.AreEqual(RecurringJobPriority.Low, recurringJob.Priority);
		}

		[TestMethod]
		public void FromJob_Duration_IsCalculatedFromStartAndEnd()
		{
			var duration = TimeSpan.FromHours(2);
			var job = new Job { Name = "Test", Start = BaseStartTime, End = BaseStartTime + duration };

			var recurringJob = RecurringJob.FromJob(job);

			Assert.AreEqual(BaseStartTime, recurringJob.Start);
			Assert.AreEqual(duration, recurringJob.Duration);
		}

		[TestMethod]
		public void FromJob_PreRollAndPostRoll_AreCopied()
		{
			var preRoll = TimeSpan.FromMinutes(15);
			var postRoll = TimeSpan.FromMinutes(10);
			var duration = TimeSpan.FromHours(1);
			var end = BaseStartTime + duration;
			var job = new Job
			{
				Name = "Test",
				Start = BaseStartTime,
				End = end,
				PreRollStart = BaseStartTime - preRoll,
				PostRollEnd = end + postRoll,
			};

			var recurringJob = RecurringJob.FromJob(job);

			Assert.AreEqual(preRoll, recurringJob.PreRollDuration);
			Assert.AreEqual(postRoll, recurringJob.PostRollDuration);
		}

		[TestMethod]
		public void FromJob_OrganizationId_IsCopied()
		{
			var orgId = Guid.NewGuid();
			var job = new Job { Name = "Test", Start = BaseStartTime, End = BaseStartTime + TimeSpan.FromHours(1), OrganizationId = orgId };

			var recurringJob = RecurringJob.FromJob(job);

			Assert.AreEqual(orgId, recurringJob.OrganizationId);
		}

		[TestMethod]
		public void FromJob_OwnerId_IsCopied()
		{
			var ownerId = Guid.NewGuid();
			var job = new Job { Name = "Test", Start = BaseStartTime, End = BaseStartTime + TimeSpan.FromHours(1), OwnerId = ownerId };

			var recurringJob = RecurringJob.FromJob(job);

			Assert.AreEqual(ownerId, recurringJob.OwnerId);
		}

		[TestMethod]
		public void FromJob_CategoryId_IsCopied()
		{
			var job = new Job { Name = "Test", Start = BaseStartTime, End = BaseStartTime + TimeSpan.FromHours(1), JobTypeCategoryId = "cat-001" };

			var recurringJob = RecurringJob.FromJob(job);

			Assert.AreEqual("cat-001", recurringJob.JobTypeCategoryId);
		}

		[TestMethod]
		public void FromJob_ContactIds_AreCopied()
		{
			var contact1 = Guid.NewGuid();
			var contact2 = Guid.NewGuid();
			var job = new Job { Name = "Test", Start = BaseStartTime, End = BaseStartTime + TimeSpan.FromHours(1) };
			job.AddContact(contact1);
			job.AddContact(contact2);

			var recurringJob = RecurringJob.FromJob(job);

			CollectionAssert.AreEquivalent(new[] { contact1, contact2 }, new List<Guid>(recurringJob.ContactIds));
		}

		[TestMethod]
		public void FromJob_NoContacts_ResultHasEmptyContactIds()
		{
			var job = new Job { Name = "Test", Start = BaseStartTime, End = BaseStartTime + TimeSpan.FromHours(1) };

			var recurringJob = RecurringJob.FromJob(job);

			Assert.AreEqual(0, recurringJob.ContactIds.Count);
		}

		[TestMethod]
		public void FromJob_AllMetadataFields_AreCopied()
		{
			var orgId = Guid.NewGuid();
			var ownerId = Guid.NewGuid();
			var contactId = Guid.NewGuid();
			var duration = TimeSpan.FromHours(3);
			var job = new Job
			{
				Name = "Full test",
				Description = "Full description",
				Priority = JobPriority.High,
				Start = BaseStartTime,
				End = BaseStartTime + duration,
				OrganizationId = orgId,
				OwnerId = ownerId,
				JobTypeCategoryId = "full-cat",
			};
			job.AddContact(contactId);

			var recurringJob = RecurringJob.FromJob(job);

			Assert.AreEqual("Full test", recurringJob.Name);
			Assert.AreEqual("Full description", recurringJob.Description);
			Assert.AreEqual(RecurringJobPriority.High, recurringJob.Priority);
			Assert.AreEqual(duration, recurringJob.Duration);
			Assert.AreEqual(orgId, recurringJob.OrganizationId);
			Assert.AreEqual(ownerId, recurringJob.OwnerId);
			Assert.AreEqual("full-cat", recurringJob.JobTypeCategoryId);
			CollectionAssert.Contains(new List<Guid>(recurringJob.ContactIds), contactId);
		}

		[TestMethod]
		public void FromJob_ClonesNodeGraph_WithFreshNodeAndConnectionIds()
		{
			var poolId = Guid.NewGuid();
			var resourceId = Guid.NewGuid();

			var job = new Job { Name = "Test", Start = BaseStartTime, End = BaseStartTime + TimeSpan.FromHours(1) };
			var resourceNode = new JobResourceNode(poolId, resourceId) { Alias = "RN", IconImage = "icon-1" };
			var poolNode = new JobResourcePoolNode(poolId) { Alias = "PN", IconImage = "icon-2" };
			job.NodeGraph.Add(resourceNode).Add(poolNode).Connect(resourceNode, poolNode);

			var recurringJob = RecurringJob.FromJob(job);

			// Same shape.
			Assert.AreEqual(2, recurringJob.NodeGraph.Nodes.Count);
			Assert.AreEqual(1, recurringJob.NodeGraph.Connections.Count);

			// Same node types.
			Assert.AreEqual(1, recurringJob.NodeGraph.Nodes.OfType<RecurringJobResourceNode>().Count());
			Assert.AreEqual(1, recurringJob.NodeGraph.Nodes.OfType<RecurringJobResourcePoolNode>().Count());

			var recurringResourceNode = recurringJob.NodeGraph.Nodes.OfType<RecurringJobResourceNode>().Single();
			var recurringPoolNode = recurringJob.NodeGraph.Nodes.OfType<RecurringJobResourcePoolNode>().Single();

			// Resource/pool ids preserved.
			Assert.AreEqual(poolId, recurringResourceNode.ResourcePoolId);
			Assert.AreEqual(resourceId, recurringResourceNode.ResourceId);
			Assert.AreEqual(poolId, recurringPoolNode.ResourcePoolId);

			// Alias and icon preserved.
			Assert.AreEqual("RN", recurringResourceNode.Alias);
			Assert.AreEqual("icon-1", recurringResourceNode.IconImage);
			Assert.AreEqual("PN", recurringPoolNode.Alias);
			Assert.AreEqual("icon-2", recurringPoolNode.IconImage);

			// Node ids are regenerated.
			var jobNodeIds = job.NodeGraph.Nodes.Select(n => n.Id).ToHashSet();
			foreach (var node in recurringJob.NodeGraph.Nodes)
			{
				Assert.IsFalse(jobNodeIds.Contains(node.Id), $"Recurring job node id {node.Id} was not regenerated.");
			}

			// Connections reference the new recurring job nodes.
			var connection = recurringJob.NodeGraph.Connections.Single();
			Assert.AreSame(recurringResourceNode, connection.From);
			Assert.AreSame(recurringPoolNode, connection.To);
		}

		[TestMethod]
		public void FromJob_CopiesCustomPropertiesOnJobAndNodes()
		{
			var poolId = Guid.NewGuid();
			var resourceId = Guid.NewGuid();

			var job = new Job { Name = "Test", Start = BaseStartTime, End = BaseStartTime + TimeSpan.FromHours(1) };
			job.AddCustomProperty(new CustomPropertySetting("Tag1") { Value = "Value1" });

			var resourceNode = new JobResourceNode(poolId, resourceId);
			resourceNode.AddCustomProperty(new CustomPropertySetting("Tag2") { Value = "Value2" });
			job.NodeGraph.Add(resourceNode);

			var recurringJob = RecurringJob.FromJob(job);

			Assert.AreEqual(1, recurringJob.CustomPropertySettings.Count);
			var property = recurringJob.CustomPropertySettings.Single();
			Assert.AreEqual("Tag1", property.Name);
			Assert.AreEqual("Value1", property.Value);

			var recurringNode = recurringJob.NodeGraph.Nodes.OfType<RecurringJobResourceNode>().Single();
			Assert.AreEqual(1, recurringNode.CustomPropertySettings.Count);
			var nodeProperty = recurringNode.CustomPropertySettings.Single();
			Assert.AreEqual("Tag2", nodeProperty.Name);
			Assert.AreEqual("Value2", nodeProperty.Value);
		}

		[TestMethod]
		public void FromJob_CopiedProperties_AreIndependentInstances()
		{
			var job = new Job { Name = "Test", Start = BaseStartTime, End = BaseStartTime + TimeSpan.FromHours(1) };
			job.AddCustomProperty(new CustomPropertySetting("Tag1") { Value = "Value1" });

			var recurringJob = RecurringJob.FromJob(job);

			var jobProperty = job.CustomPropertySettings.Single();
			var recurringProperty = recurringJob.CustomPropertySettings.Single();
			Assert.AreNotSame(jobProperty, recurringProperty);

			recurringProperty.Value = "Changed";

			Assert.AreEqual("Value1", job.CustomPropertySettings.Single().Value);
		}
	}
}
