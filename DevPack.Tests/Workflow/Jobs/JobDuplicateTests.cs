namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class JobDuplicateTests
	{
		private static readonly DateTimeOffset BaseStart = new DateTimeOffset(2024, 6, 1, 10, 0, 0, TimeSpan.Zero);
		private static readonly DateTimeOffset BaseEnd = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

		[TestMethod]
		public void Duplicate_WithId_UsesSuppliedId()
		{
			var job = new Job { Name = "Original", Start = BaseStart, End = BaseEnd };
			var newId = Guid.NewGuid();

			var duplicate = job.Duplicate(newId);

			Assert.AreEqual(newId, duplicate.Id);
			Assert.AreNotEqual(job.Id, duplicate.Id);
		}

		[TestMethod]
		public void Duplicate_WithId_EmptyGuid_Throws()
		{
			var job = new Job { Name = "Original", Start = BaseStart, End = BaseEnd };

			Assert.ThrowsException<ArgumentException>(() => job.Duplicate(Guid.Empty));
		}

		[TestMethod]
		public void Duplicate_Parameterless_GeneratesNewId()
		{
			var job = new Job { Name = "Original", Start = BaseStart, End = BaseEnd };

			var duplicate = job.Duplicate();

			Assert.AreNotEqual(Guid.Empty, duplicate.Id);
			Assert.AreNotEqual(job.Id, duplicate.Id);
		}

		[TestMethod]
		public void Duplicate_Parameterless_TwoDuplicates_HaveDifferentIds()
		{
			var job = new Job { Name = "Original", Start = BaseStart, End = BaseEnd };

			var first = job.Duplicate();
			var second = job.Duplicate();

			Assert.AreNotEqual(first.Id, second.Id);
		}

		[TestMethod]
		public void Duplicate_MarksResultAsNewUnsavedJobWithUserDefinedId()
		{
			var job = new Job { Name = "Original", Start = BaseStart, End = BaseEnd };

			var duplicate = job.Duplicate(Guid.NewGuid());

			Assert.IsTrue(duplicate.IsNew, "Duplicated job must be a new, unsaved instance.");
			Assert.IsTrue(duplicate.HasUserDefinedId, "Duplicated job must retain the user-defined id contract.");
		}

		[TestMethod]
		public void Duplicate_CopiesScalarFields()
		{
			var preRoll = BaseStart - TimeSpan.FromMinutes(5);
			var postRoll = BaseEnd + TimeSpan.FromMinutes(10);

			var job = new Job
			{
				Name = "Original",
				Description = "A description",
				Priority = JobPriority.High,
				Start = BaseStart,
				End = BaseEnd,
				PreRollStart = preRoll,
				PostRollEnd = postRoll,
				Notes = "Some notes",
				OrganizationId = Guid.NewGuid(),
				OwnerId = Guid.NewGuid(),
				JobTypeCategoryId = "CategoryA",
			};

			var duplicate = job.Duplicate(Guid.NewGuid());

			Assert.AreEqual("Original", duplicate.Name);
			Assert.AreEqual("A description", duplicate.Description);
			Assert.AreEqual(JobPriority.High, duplicate.Priority);
			Assert.AreEqual(BaseStart, duplicate.Start);
			Assert.AreEqual(BaseEnd, duplicate.End);
			Assert.AreEqual(preRoll, duplicate.PreRollStart);
			Assert.AreEqual(postRoll, duplicate.PostRollEnd);
			Assert.AreEqual("Some notes", duplicate.Notes);
			Assert.AreEqual(job.OrganizationId, duplicate.OrganizationId);
			Assert.AreEqual(job.OwnerId, duplicate.OwnerId);
			Assert.AreEqual("CategoryA", duplicate.JobTypeCategoryId);
		}

		[TestMethod]
		public void Duplicate_DoesNotCopyRecurringJobId()
		{
			var job = new Job { Name = "Original", Start = BaseStart, End = BaseEnd, RecurringJobId = Guid.NewGuid() };

			var duplicate = job.Duplicate(Guid.NewGuid());

			Assert.AreEqual(Guid.Empty, duplicate.RecurringJobId, "Duplicated job should not inherit the RecurringJobId of the original.");
		}

		[TestMethod]
		public void Duplicate_CopiesContacts()
		{
			var contactId = Guid.NewGuid();
			var job = new Job { Name = "Original", Start = BaseStart, End = BaseEnd };
			job.AddContact(contactId);

			var duplicate = job.Duplicate(Guid.NewGuid());

			Assert.AreEqual(1, duplicate.ContactIds.Count);
			Assert.IsTrue(duplicate.ContactIds.Contains(contactId));
		}

		[TestMethod]
		public void Duplicate_MutatingDuplicateContacts_DoesNotAffectOriginal()
		{
			var job = new Job { Name = "Original", Start = BaseStart, End = BaseEnd };
			job.AddContact(Guid.NewGuid());

			var duplicate = job.Duplicate(Guid.NewGuid());
			duplicate.AddContact(Guid.NewGuid());

			Assert.AreEqual(1, job.ContactIds.Count);
			Assert.AreEqual(2, duplicate.ContactIds.Count);
		}

		[TestMethod]
		public void Duplicate_ScalarFieldsAreIndependent()
		{
			var job = new Job
			{
				Name = "Original",
				Description = "Original description",
				Priority = JobPriority.Normal,
				Start = BaseStart,
				End = BaseEnd,
			};

			var duplicate = job.Duplicate(Guid.NewGuid());

			duplicate.Name = "Duplicate";
			duplicate.Description = "Duplicate description";
			duplicate.Priority = JobPriority.High;

			Assert.AreEqual("Original", job.Name);
			Assert.AreEqual("Original description", job.Description);
			Assert.AreEqual(JobPriority.Normal, job.Priority);
		}

		[TestMethod]
		public void Duplicate_NullOriginal_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new Job(null, Guid.NewGuid()));
		}

		[TestMethod]
		public void Duplicate_CopiesNodeGraph_WithFreshNodeIds()
		{
			var job = new Job { Name = "Original", Start = BaseStart, End = BaseEnd };

			var poolNode = new JobResourcePoolNode(Guid.NewGuid()) { Alias = "Pool", IconImage = "pool.png" };
			var resourceNode = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid()) { Alias = "Resource", IconImage = "res.png" };

			job.NodeGraph
				.Add(poolNode)
				.Add(resourceNode)
				.Connect(resourceNode, poolNode)
				.Link(poolNode, resourceNode);

			var duplicate = job.Duplicate(Guid.NewGuid());

			Assert.AreEqual(2, duplicate.NodeGraph.Nodes.Count);
			Assert.AreEqual(1, duplicate.NodeGraph.Connections.Count);
			Assert.AreEqual(1, duplicate.NodeGraph.Links.Count());

			// The cloned nodes must not share references or ids with the originals.
			foreach (var duplicatedNode in duplicate.NodeGraph.Nodes)
			{
				Assert.IsFalse(job.NodeGraph.Nodes.Any(orig => ReferenceEquals(orig, duplicatedNode)));
				Assert.IsFalse(job.NodeGraph.Nodes.Any(orig => orig.Id == duplicatedNode.Id));
			}

			// Type-specific state (Alias/IconImage plus ResourcePoolId/ResourceId) must be preserved.
			var duplicatedPool = duplicate.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Single();
			Assert.AreEqual("Pool", duplicatedPool.Alias);
			Assert.AreEqual("pool.png", duplicatedPool.IconImage);
			Assert.AreEqual(poolNode.ResourcePoolId, duplicatedPool.ResourcePoolId);

			var duplicatedResource = duplicate.NodeGraph.Nodes.OfType<JobResourceNode>().Single();
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
			var job = new Job { Name = "Original", Start = BaseStart, End = BaseEnd };
			var poolNode = new JobResourcePoolNode(Guid.NewGuid());
			job.NodeGraph.Add(poolNode);

			var duplicate = job.Duplicate(Guid.NewGuid());
			duplicate.NodeGraph.Add(new JobResourcePoolNode(Guid.NewGuid()));

			Assert.AreEqual(1, job.NodeGraph.Nodes.Count);
			Assert.AreEqual(2, duplicate.NodeGraph.Nodes.Count);
		}

		[TestMethod]
		public void Duplicate_CopiesNodeScopedOrchestrationReference_RetargetedAtDuplicatedNode()
		{
			var job = new Job { Name = "Original", Start = BaseStart, End = BaseEnd };

			var referencedNode = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var referencingNode = new JobResourcePoolNode(Guid.NewGuid());

			referencingNode.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid())
			{
				Reference = new ResourceNameReference(referencedNode.Id),
			});

			job.NodeGraph
				.Add(referencedNode)
				.Add(referencingNode);

			var duplicate = job.Duplicate(Guid.NewGuid());

			var duplicatedReferenced = duplicate.NodeGraph.Nodes.OfType<JobResourceNode>().Single();
			var duplicatedReferencing = duplicate.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Single();

			var reference = duplicatedReferencing.OrchestrationSettings.Capabilities.Single().Reference;
			Assert.AreEqual(duplicatedReferenced.Id, reference.NodeId, "Reference must be retargeted at the duplicated node.");
			Assert.AreNotEqual(referencedNode.Id, reference.NodeId, "Reference must no longer point at the original node.");
		}

		[TestMethod]
		public void Duplicate_MutatingDuplicateOrchestrationSettings_DoesNotAffectOriginal()
		{
			var job = new Job { Name = "Original", Start = BaseStart, End = BaseEnd };
			var referencingNode = new JobResourcePoolNode(Guid.NewGuid());
			referencingNode.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()));
			job.NodeGraph.Add(referencingNode);

			var duplicate = job.Duplicate(Guid.NewGuid());

			var duplicatedNode = duplicate.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Single();
			duplicatedNode.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()));

			Assert.AreEqual(1, referencingNode.OrchestrationSettings.Capabilities.Count);
			Assert.AreEqual(2, duplicatedNode.OrchestrationSettings.Capabilities.Count);
		}

		[TestMethod]
		public void Duplicate_CopiesJobLevelOrchestrationSettings_RetargetingNodeReferences()
		{
			var job = new Job { Name = "Original", Start = BaseStart, End = BaseEnd };

			var referencedNode = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			job.NodeGraph.Add(referencedNode);

			job.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid())
			{
				Reference = new ResourceNameReference(referencedNode.Id),
			});

			var duplicate = job.Duplicate(Guid.NewGuid());

			var duplicatedReferenced = duplicate.NodeGraph.Nodes.OfType<JobResourceNode>().Single();

			Assert.AreEqual(1, duplicate.OrchestrationSettings.Capabilities.Count);
			var reference = duplicate.OrchestrationSettings.Capabilities.Single().Reference;
			Assert.AreEqual(duplicatedReferenced.Id, reference.NodeId);
		}

		[TestMethod]
		public void Duplicate_JobOrchestrationSettings_AreIndependentInstances()
		{
			var job = new Job { Name = "Original", Start = BaseStart, End = BaseEnd };
			job.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()));

			var duplicate = job.Duplicate(Guid.NewGuid());

			Assert.AreNotSame(job.OrchestrationSettings, duplicate.OrchestrationSettings);

			duplicate.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()));

			Assert.AreEqual(1, job.OrchestrationSettings.Capabilities.Count);
			Assert.AreEqual(2, duplicate.OrchestrationSettings.Capabilities.Count);
		}

		[TestMethod]
		public void Duplicate_CopiesJobLevelCustomPropertySettings()
		{
			var job = new Job { Name = "Original", Start = BaseStart, End = BaseEnd };
			job.AddCustomProperty(new CustomPropertySetting("Tag") { Value = "live" });

			var duplicate = job.Duplicate(Guid.NewGuid());

			Assert.AreEqual(1, duplicate.CustomPropertySettings.Count);
			var setting = duplicate.CustomPropertySettings.Single();
			Assert.AreEqual("Tag", setting.Name);
			Assert.AreEqual("live", setting.Value);
		}

		[TestMethod]
		public void Duplicate_MutatingDuplicateCustomProperties_DoesNotAffectOriginal()
		{
			var job = new Job { Name = "Original", Start = BaseStart, End = BaseEnd };
			job.AddCustomProperty(new CustomPropertySetting("Tag") { Value = "live" });

			var duplicate = job.Duplicate(Guid.NewGuid());

			duplicate.AddCustomProperty(new CustomPropertySetting("Region") { Value = "eu" });

			Assert.AreEqual(1, job.CustomPropertySettings.Count);
			Assert.AreEqual(2, duplicate.CustomPropertySettings.Count);
		}

		[TestMethod]
		public void Duplicate_CopiesNodeCustomPropertySettings_OntoDuplicatedNode()
		{
			var job = new Job { Name = "Original", Start = BaseStart, End = BaseEnd };
			var node = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			job.NodeGraph.Add(node);
			node.AddCustomProperty(new CustomPropertySetting("Channel") { Value = "1" });

			var duplicate = job.Duplicate(Guid.NewGuid());

			var duplicatedNode = duplicate.NodeGraph.Nodes.OfType<JobResourceNode>().Single();

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
