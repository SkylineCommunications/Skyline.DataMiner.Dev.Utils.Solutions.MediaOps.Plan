namespace RT_MediaOps.Plan.Workflow.RecurringJobs
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class RecurringJobDuplicateTests
	{
		[TestMethod]
		public void Duplicate_WithId_UsesSuppliedId()
		{
			var recurringJob = new RecurringJob { Name = "Original" };
			var newId = Guid.NewGuid();

			var duplicate = recurringJob.Duplicate(newId);

			Assert.AreEqual(newId, duplicate.Id);
			Assert.AreNotEqual(recurringJob.Id, duplicate.Id);
		}

		[TestMethod]
		public void Duplicate_WithId_EmptyGuid_Throws()
		{
			var recurringJob = new RecurringJob { Name = "Original" };

			Assert.ThrowsException<ArgumentException>(() => recurringJob.Duplicate(Guid.Empty));
		}

		[TestMethod]
		public void Duplicate_Parameterless_GeneratesNewId()
		{
			var recurringJob = new RecurringJob { Name = "Original" };

			var duplicate = recurringJob.Duplicate();

			Assert.AreNotEqual(Guid.Empty, duplicate.Id);
			Assert.AreNotEqual(recurringJob.Id, duplicate.Id);
		}

		[TestMethod]
		public void Duplicate_Parameterless_TwoDuplicates_HaveDifferentIds()
		{
			var recurringJob = new RecurringJob { Name = "Original" };

			var first = recurringJob.Duplicate();
			var second = recurringJob.Duplicate();

			Assert.AreNotEqual(first.Id, second.Id);
		}

		[TestMethod]
		public void Duplicate_MarksResultAsNewUnsavedRecurringJobWithUserDefinedId()
		{
			var recurringJob = new RecurringJob { Name = "Original" };

			var duplicate = recurringJob.Duplicate(Guid.NewGuid());

			Assert.IsTrue(duplicate.IsNew, "Duplicated recurring job must be a new, unsaved instance.");
			Assert.IsTrue(duplicate.HasUserDefinedId, "Duplicated recurring job must retain the user-defined id contract.");
		}

		[TestMethod]
		public void Duplicate_CopiesScalarFields()
		{
			var recurringJob = new RecurringJob
			{
				Name = "Original",
				Description = "A description",
				Priority = RecurringJobPriority.High,
				Start = new DateTimeOffset(2024, 6, 1, 10, 0, 0, TimeSpan.Zero),
				Duration = TimeSpan.FromHours(2),
				PreRollDuration = TimeSpan.FromMinutes(5),
				PostRollDuration = TimeSpan.FromMinutes(10),
				TimeZone = TimeZoneInfo.Utc,
				DesiredJobState = DesiredJobState.Tentative,
				Notes = "Some notes",
				OrganizationId = Guid.NewGuid(),
				OwnerId = Guid.NewGuid(),
				JobTypeCategoryId = "CategoryA",
			};

			var duplicate = recurringJob.Duplicate(Guid.NewGuid());

			Assert.AreEqual("Original", duplicate.Name);
			Assert.AreEqual("A description", duplicate.Description);
			Assert.AreEqual(RecurringJobPriority.High, duplicate.Priority);
			Assert.AreEqual(recurringJob.Start, duplicate.Start);
			Assert.AreEqual(TimeSpan.FromHours(2), duplicate.Duration);
			Assert.AreEqual(TimeSpan.FromMinutes(5), duplicate.PreRollDuration);
			Assert.AreEqual(TimeSpan.FromMinutes(10), duplicate.PostRollDuration);
			Assert.AreEqual(TimeZoneInfo.Utc, duplicate.TimeZone);
			Assert.AreEqual(DesiredJobState.Tentative, duplicate.DesiredJobState);
			Assert.AreEqual("Some notes", duplicate.Notes);
			Assert.AreEqual(recurringJob.OrganizationId, duplicate.OrganizationId);
			Assert.AreEqual(recurringJob.OwnerId, duplicate.OwnerId);
			Assert.AreEqual("CategoryA", duplicate.JobTypeCategoryId);
		}

		[TestMethod]
		public void Duplicate_CopiesRecurringPattern()
		{
			var recurringJob = new RecurringJob { Name = "Original" };
			recurringJob.Pattern.RepeatType = RepeatType.Weekly;
			recurringJob.Pattern.RepeatEvery = 2;
			recurringJob.Pattern.WeekDays = WeekDays.Monday | WeekDays.Wednesday;
			recurringJob.Pattern.EndDate = new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero);

			var duplicate = recurringJob.Duplicate(Guid.NewGuid());

			Assert.AreEqual(RepeatType.Weekly, duplicate.Pattern.RepeatType);
			Assert.AreEqual(2, duplicate.Pattern.RepeatEvery);
			Assert.AreEqual(WeekDays.Monday | WeekDays.Wednesday, duplicate.Pattern.WeekDays);
			Assert.AreEqual(recurringJob.Pattern.EndDate, duplicate.Pattern.EndDate);
		}

		[TestMethod]
		public void Duplicate_MutatingDuplicatePattern_DoesNotAffectOriginal()
		{
			var recurringJob = new RecurringJob { Name = "Original" };
			recurringJob.Pattern.RepeatType = RepeatType.Daily;
			recurringJob.Pattern.RepeatEvery = 1;

			var duplicate = recurringJob.Duplicate(Guid.NewGuid());
			duplicate.Pattern.RepeatType = RepeatType.Weekly;
			duplicate.Pattern.RepeatEvery = 3;

			Assert.AreEqual(RepeatType.Daily, recurringJob.Pattern.RepeatType);
			Assert.AreEqual(1, recurringJob.Pattern.RepeatEvery);
		}

		[TestMethod]
		public void Duplicate_CopiesContacts()
		{
			var contactId = Guid.NewGuid();
			var recurringJob = new RecurringJob { Name = "Original" };
			recurringJob.AddContact(contactId);

			var duplicate = recurringJob.Duplicate(Guid.NewGuid());

			Assert.AreEqual(1, duplicate.ContactIds.Count);
			Assert.IsTrue(duplicate.ContactIds.Contains(contactId));
		}

		[TestMethod]
		public void Duplicate_MutatingDuplicateContacts_DoesNotAffectOriginal()
		{
			var recurringJob = new RecurringJob { Name = "Original" };
			recurringJob.AddContact(Guid.NewGuid());

			var duplicate = recurringJob.Duplicate(Guid.NewGuid());
			duplicate.AddContact(Guid.NewGuid());

			Assert.AreEqual(1, recurringJob.ContactIds.Count);
			Assert.AreEqual(2, duplicate.ContactIds.Count);
		}

		[TestMethod]
		public void Duplicate_ScalarFieldsAreIndependent()
		{
			var recurringJob = new RecurringJob
			{
				Name = "Original",
				Description = "Original description",
				Priority = RecurringJobPriority.Normal,
			};

			var duplicate = recurringJob.Duplicate(Guid.NewGuid());

			duplicate.Name = "Duplicate";
			duplicate.Description = "Duplicate description";
			duplicate.Priority = RecurringJobPriority.High;

			Assert.AreEqual("Original", recurringJob.Name);
			Assert.AreEqual("Original description", recurringJob.Description);
			Assert.AreEqual(RecurringJobPriority.Normal, recurringJob.Priority);
		}

		[TestMethod]
		public void Duplicate_NullOriginal_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new RecurringJob(null, Guid.NewGuid()));
		}

		[TestMethod]
		public void Duplicate_CopiesNodeGraph_WithFreshNodeIds()
		{
			var recurringJob = new RecurringJob { Name = "Original" };

			var poolNode = new RecurringJobResourcePoolNode(Guid.NewGuid()) { Alias = "Pool", IconImage = "pool.png" };
			var resourceNode = new RecurringJobResourceNode(Guid.NewGuid(), Guid.NewGuid()) { Alias = "Resource", IconImage = "res.png" };

			recurringJob.NodeGraph
				.Add(poolNode)
				.Add(resourceNode)
				.Connect(resourceNode, poolNode)
				.Link(poolNode, resourceNode);

			var duplicate = recurringJob.Duplicate(Guid.NewGuid());

			Assert.AreEqual(2, duplicate.NodeGraph.Nodes.Count);
			Assert.AreEqual(1, duplicate.NodeGraph.Connections.Count);
			Assert.AreEqual(1, duplicate.NodeGraph.Links.Count());

			// The cloned nodes must not share references or ids with the originals.
			foreach (var duplicatedNode in duplicate.NodeGraph.Nodes)
			{
				Assert.IsFalse(recurringJob.NodeGraph.Nodes.Any(orig => ReferenceEquals(orig, duplicatedNode)));
				Assert.IsFalse(recurringJob.NodeGraph.Nodes.Any(orig => orig.Id == duplicatedNode.Id));
			}

			// Type-specific state (Alias/IconImage plus ResourcePoolId/ResourceId) must be preserved.
			var duplicatedPool = duplicate.NodeGraph.Nodes.OfType<RecurringJobResourcePoolNode>().Single();
			Assert.AreEqual("Pool", duplicatedPool.Alias);
			Assert.AreEqual("pool.png", duplicatedPool.IconImage);
			Assert.AreEqual(poolNode.ResourcePoolId, duplicatedPool.ResourcePoolId);

			var duplicatedResource = duplicate.NodeGraph.Nodes.OfType<RecurringJobResourceNode>().Single();
			Assert.AreEqual("Resource", duplicatedResource.Alias);
			Assert.AreEqual("res.png", duplicatedResource.IconImage);
			Assert.AreEqual(resourceNode.ResourcePoolId, duplicatedResource.ResourcePoolId);
			Assert.AreEqual(resourceNode.ResourceId, duplicatedResource.ResourceId);

			// Connection and link must reference the duplicated nodes, not the originals.
			var connection = duplicate.NodeGraph.Connections.Single();
			Assert.AreSame(duplicatedResource, connection.From);
			Assert.AreSame(duplicatedPool, connection.To);

			Assert.AreSame(duplicatedPool, duplicate.NodeGraph.GetParent(duplicatedResource));
		}

		[TestMethod]
		public void Duplicate_MutatingDuplicateNodeGraph_DoesNotAffectOriginal()
		{
			var recurringJob = new RecurringJob { Name = "Original" };
			var poolNode = new RecurringJobResourcePoolNode(Guid.NewGuid());
			recurringJob.NodeGraph.Add(poolNode);

			var duplicate = recurringJob.Duplicate(Guid.NewGuid());
			duplicate.NodeGraph.Add(new RecurringJobResourcePoolNode(Guid.NewGuid()));

			Assert.AreEqual(1, recurringJob.NodeGraph.Nodes.Count);
			Assert.AreEqual(2, duplicate.NodeGraph.Nodes.Count);
		}

		[TestMethod]
		public void Duplicate_CopiesNodeScopedOrchestrationReference_RetargetedAtDuplicatedNode()
		{
			var recurringJob = new RecurringJob { Name = "Original" };

			var referencedNode = new RecurringJobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var referencingNode = new RecurringJobResourcePoolNode(Guid.NewGuid());

			referencingNode.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid())
			{
				Reference = new ResourceNameReference(referencedNode.Id),
			});

			recurringJob.NodeGraph
				.Add(referencedNode)
				.Add(referencingNode);

			var duplicate = recurringJob.Duplicate(Guid.NewGuid());

			var duplicatedReferenced = duplicate.NodeGraph.Nodes.OfType<RecurringJobResourceNode>().Single();
			var duplicatedReferencing = duplicate.NodeGraph.Nodes.OfType<RecurringJobResourcePoolNode>().Single();

			var reference = duplicatedReferencing.OrchestrationSettings.Capabilities.Single().Reference;
			Assert.AreEqual(duplicatedReferenced.Id, reference.NodeId, "Reference must be retargeted at the duplicated node.");
			Assert.AreNotEqual(referencedNode.Id, reference.NodeId, "Reference must no longer point at the original node.");
		}

		[TestMethod]
		public void Duplicate_MutatingDuplicateOrchestrationSettings_DoesNotAffectOriginal()
		{
			var recurringJob = new RecurringJob { Name = "Original" };
			var referencingNode = new RecurringJobResourcePoolNode(Guid.NewGuid());
			referencingNode.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()));
			recurringJob.NodeGraph.Add(referencingNode);

			var duplicate = recurringJob.Duplicate(Guid.NewGuid());

			var duplicatedNode = duplicate.NodeGraph.Nodes.OfType<RecurringJobResourcePoolNode>().Single();
			duplicatedNode.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()));

			Assert.AreEqual(1, referencingNode.OrchestrationSettings.Capabilities.Count);
			Assert.AreEqual(2, duplicatedNode.OrchestrationSettings.Capabilities.Count);
		}

		[TestMethod]
		public void Duplicate_CopiesRecurringJobLevelOrchestrationSettings_RetargetingNodeReferences()
		{
			var recurringJob = new RecurringJob { Name = "Original" };

			var referencedNode = new RecurringJobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			recurringJob.NodeGraph.Add(referencedNode);

			recurringJob.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid())
			{
				Reference = new ResourceNameReference(referencedNode.Id),
			});

			var duplicate = recurringJob.Duplicate(Guid.NewGuid());

			var duplicatedReferenced = duplicate.NodeGraph.Nodes.OfType<RecurringJobResourceNode>().Single();

			Assert.AreEqual(1, duplicate.OrchestrationSettings.Capabilities.Count);
			var reference = duplicate.OrchestrationSettings.Capabilities.Single().Reference;
			Assert.AreEqual(duplicatedReferenced.Id, reference.NodeId);
		}

		[TestMethod]
		public void Duplicate_RecurringJobOrchestrationSettings_AreIndependentInstances()
		{
			var recurringJob = new RecurringJob { Name = "Original" };
			recurringJob.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()));

			var duplicate = recurringJob.Duplicate(Guid.NewGuid());

			Assert.AreNotSame(recurringJob.OrchestrationSettings, duplicate.OrchestrationSettings);

			duplicate.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()));

			Assert.AreEqual(1, recurringJob.OrchestrationSettings.Capabilities.Count);
			Assert.AreEqual(2, duplicate.OrchestrationSettings.Capabilities.Count);
		}

		[TestMethod]
		public void Duplicate_CopiesRecurringJobLevelCustomPropertySettings()
		{
			// RecurringJob has no public AddCustomProperty method; use FromJob to populate properties.
			var job = new Job
			{
				Name = "Original",
				Start = new DateTimeOffset(2024, 6, 1, 10, 0, 0, TimeSpan.Zero),
				End = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero),
			};
			job.AddCustomProperty(new CustomPropertySetting("Tag") { Value = "live" });

			var original = RecurringJob.FromJob(job);
			var duplicate = original.Duplicate(Guid.NewGuid());

			Assert.AreEqual(1, duplicate.CustomPropertySettings.Count);
			var setting = duplicate.CustomPropertySettings.Single();
			Assert.AreEqual("Tag", setting.Name);
			Assert.AreEqual("live", setting.Value);
		}

		[TestMethod]
		public void Duplicate_CopiesNodeCustomPropertySettings_OntoDuplicatedNode()
		{
			var recurringJob = new RecurringJob { Name = "Original" };
			var node = new RecurringJobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			recurringJob.NodeGraph.Add(node);
			node.AddCustomProperty(new CustomPropertySetting("Channel") { Value = "1" });

			var duplicate = recurringJob.Duplicate(Guid.NewGuid());

			var duplicatedNode = duplicate.NodeGraph.Nodes.OfType<RecurringJobResourceNode>().Single();

			Assert.AreEqual(1, duplicatedNode.CustomPropertySettings.Count);
			Assert.AreEqual("Channel", duplicatedNode.CustomPropertySettings.Single().Name);
			Assert.AreEqual("1", duplicatedNode.CustomPropertySettings.Single().Value);

			// Mutating the duplicated node's properties must not affect the original.
			duplicatedNode.AddCustomProperty(new CustomPropertySetting("Extra") { Value = "x" });
			Assert.AreEqual(1, node.CustomPropertySettings.Count);
			Assert.AreEqual(2, duplicatedNode.CustomPropertySettings.Count);
		}
	}
}
