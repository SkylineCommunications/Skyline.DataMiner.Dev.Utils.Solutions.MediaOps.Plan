namespace RT_MediaOps.Plan.Workflow.Workflows
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class WorkflowDuplicateTests
	{
		[TestMethod]
		public void Duplicate_WithId_UsesSuppliedId()
		{
			var workflow = new Workflow { Name = "Original" };
			var newId = Guid.NewGuid();

			var duplicate = workflow.Duplicate(newId);

			Assert.AreEqual(newId, duplicate.Id);
			Assert.AreNotEqual(workflow.Id, duplicate.Id);
		}

		[TestMethod]
		public void Duplicate_WithId_EmptyGuid_Throws()
		{
			var workflow = new Workflow { Name = "Original" };

			Assert.ThrowsException<ArgumentException>(() => workflow.Duplicate(Guid.Empty));
		}

		[TestMethod]
		public void Duplicate_Parameterless_GeneratesNewId()
		{
			var workflow = new Workflow { Name = "Original" };

			var duplicate = workflow.Duplicate();

			Assert.AreNotEqual(Guid.Empty, duplicate.Id);
			Assert.AreNotEqual(workflow.Id, duplicate.Id);
		}

		[TestMethod]
		public void Duplicate_Parameterless_TwoDuplicates_HaveDifferentIds()
		{
			var workflow = new Workflow { Name = "Original" };

			var first = workflow.Duplicate();
			var second = workflow.Duplicate();

			Assert.AreNotEqual(first.Id, second.Id);
		}

		[TestMethod]
		public void Duplicate_MarksResultAsNewUnsavedWorkflowWithUserDefinedId()
		{
			var workflow = new Workflow { Name = "Original" };

			var duplicate = workflow.Duplicate(Guid.NewGuid());

			Assert.IsTrue(duplicate.IsNew, "Duplicated workflow must be a new, unsaved instance.");
			Assert.IsTrue(duplicate.HasUserDefinedId, "Duplicated workflow must retain the user-defined id contract.");
		}

		[TestMethod]
		public void Duplicate_CopiesScalarFields()
		{
			var workflow = new Workflow
			{
				Name = "Original",
				Description = "A description",
				Priority = WorkflowPriority.High,
				IsFavorite = true,
				PreRoll = TimeSpan.FromSeconds(7),
				PostRoll = TimeSpan.FromSeconds(11),
				Notes = "Some notes",
			};

			var duplicate = workflow.Duplicate(Guid.NewGuid());

			Assert.AreEqual("Original", duplicate.Name);
			Assert.AreEqual("A description", duplicate.Description);
			Assert.AreEqual(WorkflowPriority.High, duplicate.Priority);
			Assert.IsTrue(duplicate.IsFavorite);
			Assert.AreEqual(TimeSpan.FromSeconds(7), duplicate.PreRoll);
			Assert.AreEqual(TimeSpan.FromSeconds(11), duplicate.PostRoll);
			Assert.AreEqual("Some notes", duplicate.Notes);
		}

		[TestMethod]
		public void Duplicate_ScalarFieldsAreIndependent()
		{
			var workflow = new Workflow
			{
				Name = "Original",
				Description = "Original description",
				Priority = WorkflowPriority.Normal,
			};

			var duplicate = workflow.Duplicate(Guid.NewGuid());

			duplicate.Name = "Duplicate";
			duplicate.Description = "Duplicate description";
			duplicate.Priority = WorkflowPriority.High;

			Assert.AreEqual("Original", workflow.Name);
			Assert.AreEqual("Original description", workflow.Description);
			Assert.AreEqual(WorkflowPriority.Normal, workflow.Priority);
		}

		[TestMethod]
		public void Duplicate_NullOriginal_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new Workflow(null, Guid.NewGuid()));
		}

		[TestMethod]
		public void Duplicate_CopiesNodeGraph_WithFreshNodeIds()
		{
			var workflow = new Workflow { Name = "Original" };

			var poolNode = new WorkflowResourcePoolNode(Guid.NewGuid()) { Alias = "Pool", IconImage = "pool.png" };
			var resourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid()) { Alias = "Resource", IconImage = "res.png" };

			workflow.NodeGraph
				.Add(poolNode)
				.Add(resourceNode)
				.Connect(resourceNode, poolNode)
				.Link(poolNode, resourceNode);

			var duplicate = workflow.Duplicate(Guid.NewGuid());

			Assert.AreEqual(2, duplicate.NodeGraph.Nodes.Count);
			Assert.AreEqual(1, duplicate.NodeGraph.Connections.Count);
			Assert.AreEqual(1, duplicate.NodeGraph.Links.Count());

			// The cloned nodes must not share references or ids with the originals.
			foreach (var duplicatedNode in duplicate.NodeGraph.Nodes)
			{
				Assert.IsFalse(workflow.NodeGraph.Nodes.Any(orig => ReferenceEquals(orig, duplicatedNode)));
				Assert.IsFalse(workflow.NodeGraph.Nodes.Any(orig => orig.Id == duplicatedNode.Id));
			}

			// Type-specific state (Alias/IconImage plus ResourcePoolId/ResourceId) must be preserved.
			var duplicatedPool = duplicate.NodeGraph.Nodes.OfType<WorkflowResourcePoolNode>().Single();
			Assert.AreEqual("Pool", duplicatedPool.Alias);
			Assert.AreEqual("pool.png", duplicatedPool.IconImage);
			Assert.AreEqual(poolNode.ResourcePoolId, duplicatedPool.ResourcePoolId);

			var duplicatedResource = duplicate.NodeGraph.Nodes.OfType<WorkflowResourceNode>().Single();
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
			var workflow = new Workflow { Name = "Original" };

			var poolNode = new WorkflowResourcePoolNode(Guid.NewGuid());
			workflow.NodeGraph.Add(poolNode);

			var duplicate = workflow.Duplicate(Guid.NewGuid());

			duplicate.NodeGraph.Add(new WorkflowResourcePoolNode(Guid.NewGuid()));

			Assert.AreEqual(1, workflow.NodeGraph.Nodes.Count);
			Assert.AreEqual(2, duplicate.NodeGraph.Nodes.Count);
		}

		[TestMethod]
		public void Duplicate_CopiesNodeScopedOrchestrationReference_RetargetedAtDuplicatedNode()
		{
			var workflow = new Workflow { Name = "Original" };

			var referencedNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var referencingNode = new WorkflowResourcePoolNode(Guid.NewGuid());

			referencingNode.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid())
			{
				Reference = new ResourceNameReference(referencedNode.Id),
			});

			workflow.NodeGraph
				.Add(referencedNode)
				.Add(referencingNode);

			var duplicate = workflow.Duplicate(Guid.NewGuid());

			var duplicatedReferenced = duplicate.NodeGraph.Nodes.OfType<WorkflowResourceNode>().Single();
			var duplicatedReferencing = duplicate.NodeGraph.Nodes.OfType<WorkflowResourcePoolNode>().Single();

			var reference = duplicatedReferencing.OrchestrationSettings.Capabilities.Single().Reference;
			Assert.AreEqual(duplicatedReferenced.Id, reference.NodeId, "Reference must be retargeted at the duplicated node.");
			Assert.AreNotEqual(referencedNode.Id, reference.NodeId, "Reference must no longer point at the original node.");
		}

		[TestMethod]
		public void Duplicate_MutatingDuplicateOrchestrationSettings_DoesNotAffectOriginal()
		{
			var workflow = new Workflow { Name = "Original" };
			var referencingNode = new WorkflowResourcePoolNode(Guid.NewGuid());
			referencingNode.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()));
			workflow.NodeGraph.Add(referencingNode);

			var duplicate = workflow.Duplicate(Guid.NewGuid());

			var duplicatedNode = duplicate.NodeGraph.Nodes.OfType<WorkflowResourcePoolNode>().Single();
			duplicatedNode.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()));

			Assert.AreEqual(1, referencingNode.OrchestrationSettings.Capabilities.Count);
			Assert.AreEqual(2, duplicatedNode.OrchestrationSettings.Capabilities.Count);
		}

		[TestMethod]
		public void Duplicate_CopiesWorkflowLevelOrchestrationSettings_RetargetingNodeReferences()
		{
			var workflow = new Workflow { Name = "Original" };

			var referencedNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(referencedNode);

			workflow.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid())
			{
				Reference = new ResourceNameReference(referencedNode.Id),
			});

			var duplicate = workflow.Duplicate(Guid.NewGuid());

			var duplicatedReferenced = duplicate.NodeGraph.Nodes.OfType<WorkflowResourceNode>().Single();

			Assert.AreEqual(1, duplicate.OrchestrationSettings.Capabilities.Count);
			var reference = duplicate.OrchestrationSettings.Capabilities.Single().Reference;
			Assert.AreEqual(duplicatedReferenced.Id, reference.NodeId);
		}

		[TestMethod]
		public void Duplicate_WorkflowOrchestrationSettings_AreIndependentInstances()
		{
			var workflow = new Workflow { Name = "Original" };
			workflow.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()));

			var duplicate = workflow.Duplicate(Guid.NewGuid());

			Assert.AreNotSame(workflow.OrchestrationSettings, duplicate.OrchestrationSettings);

			duplicate.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()));

			Assert.AreEqual(1, workflow.OrchestrationSettings.Capabilities.Count);
			Assert.AreEqual(2, duplicate.OrchestrationSettings.Capabilities.Count);
		}

		[TestMethod]
		public void Duplicate_CopiesWorkflowLevelPropertySettings()
		{
			var workflow = new Workflow { Name = "Original" };
			workflow.AddCustomProperty(new CustomPropertySetting("Tag") { Value = "live" });

			var duplicate = workflow.Duplicate(Guid.NewGuid());

			Assert.AreEqual(1, duplicate.CustomPropertySettings.Count);
			var setting = duplicate.CustomPropertySettings.Single();
			Assert.AreEqual("Tag", setting.Name);
			Assert.AreEqual("live", setting.Value);
		}

		[TestMethod]
		public void Duplicate_MutatingDuplicateCustomProperties_DoesNotAffectOriginal()
		{
			var workflow = new Workflow { Name = "Original" };
			workflow.AddCustomProperty(new CustomPropertySetting("Tag") { Value = "live" });

			var duplicate = workflow.Duplicate(Guid.NewGuid());

			duplicate.AddCustomProperty(new CustomPropertySetting("Region") { Value = "eu" });

			Assert.AreEqual(1, workflow.CustomPropertySettings.Count);
			Assert.AreEqual(2, duplicate.CustomPropertySettings.Count);
		}

		[TestMethod]
		public void Duplicate_CopiesNodeCustomPropertySettings_OntoDuplicatedNode()
		{
			var workflow = new Workflow { Name = "Original" };
			var node = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(node);
			node.AddCustomProperty(new CustomPropertySetting("Channel") { Value = "1" });

			var duplicate = workflow.Duplicate(Guid.NewGuid());

			var duplicatedNode = duplicate.NodeGraph.Nodes.OfType<WorkflowResourceNode>().Single();

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
