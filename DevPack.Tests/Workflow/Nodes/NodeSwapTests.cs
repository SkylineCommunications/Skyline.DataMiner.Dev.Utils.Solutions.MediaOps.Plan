namespace RT_MediaOps.Plan.Workflow.Nodes
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	public sealed class NodeSwapTests
	{
		[TestMethod]
		public void Workflow_Swap_RetargetsConnectionsAndLinks()
		{
			var workflow = new Workflow { Name = "Workflow" };

			var poolNode = new WorkflowResourcePoolNode(Guid.NewGuid());
			var resourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var childNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());

			workflow.NodeGraph
				.Add(poolNode)
				.Add(resourceNode)
				.Add(childNode)
				.Connect(resourceNode, poolNode)
				.Link(poolNode, childNode);

			var newResourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());

			workflow.NodeGraph.Swap(resourceNode, newResourceNode);

			Assert.IsTrue(workflow.NodeGraph.Nodes.Contains(newResourceNode));
			Assert.IsFalse(workflow.NodeGraph.Nodes.Contains(resourceNode));
			Assert.AreEqual(3, workflow.NodeGraph.Nodes.Count);

			var connection = workflow.NodeGraph.Connections.Single();
			Assert.AreSame(newResourceNode, connection.From);
			Assert.AreSame(poolNode, connection.To);
		}

		[TestMethod]
		public void Workflow_Swap_RetargetsParentLink()
		{
			var workflow = new Workflow { Name = "Workflow" };

			var poolNode = new WorkflowResourcePoolNode(Guid.NewGuid());
			var childNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());

			workflow.NodeGraph
				.Add(poolNode)
				.Add(childNode)
				.Link(poolNode, childNode);

			var newPoolNode = new WorkflowResourcePoolNode(Guid.NewGuid());

			workflow.NodeGraph.Swap(poolNode, newPoolNode);

			Assert.AreSame(newPoolNode, workflow.NodeGraph.GetParent(childNode));
		}

		[TestMethod]
		public void Workflow_Swap_RetargetsNodeScopedReferences()
		{
			var workflow = new Workflow { Name = "Workflow" };

			var resourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var referencingNode = new WorkflowResourcePoolNode(Guid.NewGuid());

			referencingNode.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid())
			{
				Reference = new ResourceNameReference(resourceNode.Id),
			});

			workflow.NodeGraph
				.Add(resourceNode)
				.Add(referencingNode);

			var newResourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());

			workflow.NodeGraph.Swap(resourceNode, newResourceNode);

			var reference = referencingNode.OrchestrationSettings.Capabilities.Single().Reference;
			Assert.AreEqual(newResourceNode.Id, reference.NodeId);
		}

		[TestMethod]
		public void Workflow_Swap_AdaptsCopiedReference_WhenReferencedNodeIsSwappedAfterwards()
		{
			var workflow = new Workflow { Name = "Workflow" };

			// Node that will be referenced and later swapped out itself.
			var referencedNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());

			// Existing node holding a reference to referencedNode; it will be swapped out for newNode.
			var sourceNode = new WorkflowResourcePoolNode(Guid.NewGuid());
			sourceNode.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid())
			{
				Reference = new ResourceNameReference(referencedNode.Id),
			});

			workflow.NodeGraph
				.Add(referencedNode)
				.Add(sourceNode);

			// Copy the orchestration settings (including the reference to referencedNode) onto a new node and swap it in.
			var newNode = new WorkflowResourcePoolNode(Guid.NewGuid());
			newNode.CopyOrchestrationSettingsFrom(sourceNode);
			workflow.NodeGraph.Swap(sourceNode, newNode);

			// Sanity: swapping newNode in keeps the copied reference pointing at referencedNode.
			Assert.AreEqual(referencedNode.Id, newNode.OrchestrationSettings.Capabilities.Single().Reference.NodeId);

			// Now swap referencedNode; because newNode already lives in the graph, its copied reference is adapted.
			var replacementReferencedNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Swap(referencedNode, replacementReferencedNode);

			var reference = newNode.OrchestrationSettings.Capabilities.Single().Reference;
			Assert.AreEqual(replacementReferencedNode.Id, reference.NodeId);
		}

		[TestMethod]
		public void Workflow_Swap_DoesNotAdaptReference_WhenReferencedNodeSwappedBeforeNewNodeJoinsGraph()
		{
			var workflow = new Workflow { Name = "Workflow" };

			var referencedNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var sourceNode = new WorkflowResourcePoolNode(Guid.NewGuid());

			workflow.NodeGraph
				.Add(referencedNode)
				.Add(sourceNode);

			// New node prepared outside the graph, referencing referencedNode.
			var newNode = new WorkflowResourcePoolNode(Guid.NewGuid());
			newNode.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid())
			{
				Reference = new ResourceNameReference(referencedNode.Id),
			});

			// Swap referencedNode FIRST, while newNode is still outside the graph and therefore not reachable.
			var replacementReferencedNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Swap(referencedNode, replacementReferencedNode);

			// Swap newNode in afterwards; its reference was never retargeted because it was outside the graph earlier.
			workflow.NodeGraph.Swap(sourceNode, newNode);

			var reference = newNode.OrchestrationSettings.Capabilities.Single().Reference;
			Assert.AreEqual(referencedNode.Id, reference.NodeId);
		}

		[TestMethod]
		public void Workflow_Swap_FiveMutuallyReferencingNodes_AllReferencesRetargeted_WhenCopyingAtSwapTime()
		{
			var workflow = new Workflow { Name = "Workflow" };

			// Five nodes that each reference all of the others.
			var originalNodes = new List<WorkflowResourceNode>();
			for (int i = 0; i < 5; i++)
			{
				originalNodes.Add(new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid()));
			}

			foreach (var node in originalNodes)
			{
				workflow.NodeGraph.Add(node);

				foreach (var other in originalNodes.Where(n => n != node))
				{
					node.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid())
					{
						Reference = new ResourceNameReference(other.Id),
					});
				}
			}

			// Correct procedure: copy from the old node and swap, one node at a time.
			var replacements = new List<WorkflowResourceNode>();
			foreach (var oldNode in originalNodes)
			{
				var newNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
				newNode.CopyOrchestrationSettingsFrom(oldNode);
				workflow.NodeGraph.Swap(oldNode, newNode);
				replacements.Add(newNode);
			}

			// Every reference on every replacement node must point at a replacement node, never at an original.
			var originalIds = new HashSet<string>(originalNodes.Select(n => n.Id));
			var replacementIds = new HashSet<string>(replacements.Select(n => n.Id));

			foreach (var node in replacements)
			{
				var referencedIds = node.OrchestrationSettings.Capabilities.Select(c => c.Reference.NodeId).ToList();

				Assert.AreEqual(4, referencedIds.Count);
				Assert.IsTrue(referencedIds.All(replacementIds.Contains), "All references must point at replacement nodes.");
				Assert.IsFalse(referencedIds.Any(originalIds.Contains), "No reference may still point at an original node.");

				// A node must not reference itself and must reference each of the four other replacements exactly once.
				CollectionAssert.AreEquivalent(replacementIds.Where(id => id != node.Id).ToList(), referencedIds);
			}
		}

		[TestMethod]
		public void Workflow_Swap_RetargetsOwnerLevelReferences()
		{
			var workflow = new Workflow { Name = "Workflow" };

			var resourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(resourceNode);

			workflow.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid())
			{
				Reference = new ResourceNameReference(resourceNode.Id),
			});

			var newResourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());

			workflow.NodeGraph.Swap(resourceNode, newResourceNode);

			var reference = workflow.OrchestrationSettings.Capabilities.Single().Reference;
			Assert.AreEqual(newResourceNode.Id, reference.NodeId);
		}

		[TestMethod]
		public void Workflow_Swap_ResourceToPool_IsAllowed()
		{
			var workflow = new Workflow { Name = "Workflow" };

			var resourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(resourceNode);

			var newPoolNode = new WorkflowResourcePoolNode(Guid.NewGuid());

			workflow.NodeGraph.Swap(resourceNode, newPoolNode);

			Assert.IsTrue(workflow.NodeGraph.Nodes.Contains(newPoolNode));
		}

		[TestMethod]
		public void Workflow_Swap_TracksOriginalNode()
		{
			var workflow = new Workflow { Name = "Workflow" };

			var resourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(resourceNode);

			var newResourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Swap(resourceNode, newResourceNode);

			Assert.AreSame(resourceNode, workflow.NodeGraph.GetOriginalNode(newResourceNode));
		}

		[TestMethod]
		public void Workflow_Swap_Again_KeepsOriginalNode()
		{
			var workflow = new Workflow { Name = "Workflow" };

			var resourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(resourceNode);

			var firstReplacement = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Swap(resourceNode, firstReplacement);

			var secondReplacement = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Swap(firstReplacement, secondReplacement);

			Assert.AreSame(resourceNode, workflow.NodeGraph.GetOriginalNode(secondReplacement));
		}

		[TestMethod]
		public void Job_Swap_ResourceToPool_ReportedByValidatorAfterSwapSucceeds()
		{
			var job = new Job { Name = "Job" };

			var resourceNode = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			job.NodeGraph.Add(resourceNode);

			var newPoolNode = new JobResourcePoolNode(Guid.NewGuid());

			// The swap itself no longer throws; the type rule is validated later, at save time.
			job.NodeGraph.Swap(resourceNode, newPoolNode);
			Assert.IsTrue(job.NodeGraph.Nodes.Contains(newPoolNode));

			var error = GetJobSwapErrors(job).SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(job.Id, error.Id);
			Assert.AreEqual(resourceNode.Id, error.NodeId);
			Assert.AreEqual(newPoolNode.Id, error.TargetNodeId);
		}

		[TestMethod]
		public void Job_Swap_PoolToResourceToPool_NetTransitionAllowed()
		{
			var job = new Job { Name = "Job" };

			var poolNode = new JobResourcePoolNode(Guid.NewGuid());
			job.NodeGraph.Add(poolNode);

			// pool -> resource (allowed) then resource -> pool. The net original->final transition is pool -> pool,
			// which is allowed, so the intermediate resource->pool step must not be reported.
			var resourceNode = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			job.NodeGraph.Swap(poolNode, resourceNode);

			var finalPoolNode = new JobResourcePoolNode(Guid.NewGuid());
			job.NodeGraph.Swap(resourceNode, finalPoolNode);

			Assert.AreSame(poolNode, job.NodeGraph.GetOriginalNode(finalPoolNode));
			Assert.AreEqual(0, GetJobSwapErrors(job).Count);
		}

		[TestMethod]
		public void Job_Swap_PoolToResource_IsAllowed()
		{
			var job = new Job { Name = "Job" };

			var poolNode = new JobResourcePoolNode(Guid.NewGuid());
			job.NodeGraph.Add(poolNode);

			var newResourceNode = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());

			job.NodeGraph.Swap(poolNode, newResourceNode);

			Assert.IsTrue(job.NodeGraph.Nodes.Contains(newResourceNode));
		}

		[TestMethod]
		public void Job_Swap_ResourceToResource_IsAllowed()
		{
			var job = new Job { Name = "Job" };

			var resourceNode = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			job.NodeGraph.Add(resourceNode);

			var newResourceNode = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());

			job.NodeGraph.Swap(resourceNode, newResourceNode);

			Assert.IsTrue(job.NodeGraph.Nodes.Contains(newResourceNode));
		}

		[TestMethod]
		public void Swap_NodeNotInGraph_Throws()
		{
			var workflow = new Workflow { Name = "Workflow" };

			var resourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var notInGraph = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var newNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());

			workflow.NodeGraph.Add(resourceNode);

			Assert.ThrowsException<InvalidOperationException>(() => workflow.NodeGraph.Swap(notInGraph, newNode));
		}

		[TestMethod]
		public void Swap_NewNodeAlreadyInGraph_Throws()
		{
			var workflow = new Workflow { Name = "Workflow" };

			var resourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var otherNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());

			workflow.NodeGraph
				.Add(resourceNode)
				.Add(otherNode);

			Assert.ThrowsException<InvalidOperationException>(() => workflow.NodeGraph.Swap(resourceNode, otherNode));
		}

		[TestMethod]
		public void Swap_NullArguments_Throw()
		{
			var workflow = new Workflow { Name = "Workflow" };
			var resourceNode = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			workflow.NodeGraph.Add(resourceNode);

			Assert.ThrowsException<ArgumentNullException>(() => workflow.NodeGraph.Swap(null, resourceNode));
			Assert.ThrowsException<ArgumentNullException>(() => workflow.NodeGraph.Swap(resourceNode, null));
		}

		[TestMethod]
		public void CopyPropertiesFrom_CopiesCustomProperties()
		{
			var source = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			source.AddCustomProperty(new CustomPropertySetting("Color") { Value = "Red" });

			var target = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			target.CopyPropertiesFrom(source);

			var copied = target.CustomPropertySettings.Single();
			Assert.AreEqual("Color", copied.Name);
			Assert.AreEqual("Red", copied.Value);
		}

		[TestMethod]
		public void CopyPropertiesFrom_NullSource_Throws()
		{
			var target = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());

			Assert.ThrowsException<ArgumentNullException>(() => target.CopyPropertiesFrom(null));
		}

		[TestMethod]
		public void CopyOrchestrationSettingsFrom_RetargetsSelfReferences()
		{
			var source = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			source.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid())
			{
				Reference = new ResourceNameReference(source.Id),
			});

			var target = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());
			target.CopyOrchestrationSettingsFrom(source);

			var reference = target.OrchestrationSettings.Capabilities.Single().Reference;
			Assert.AreEqual(target.Id, reference.NodeId);
		}

		[TestMethod]
		public void CopyOrchestrationSettingsFrom_NullSource_Throws()
		{
			var target = new WorkflowResourceNode(Guid.NewGuid(), Guid.NewGuid());

			Assert.ThrowsException<ArgumentNullException>(() => target.CopyOrchestrationSettingsFrom(null));
		}

		private static List<JobNodeSwapNotAllowedError> GetJobSwapErrors(Job job)
		{
			// Run the job node graph validator and collect only the swap-related errors. Other validation errors
			// (e.g. unknown resource/pool ids) are irrelevant here and are filtered out.
			var validator = JobNodeGraphValidator.Validate(
				job.Id,
				job.NodeGraph,
				new Dictionary<Guid, Resource>(),
				new Dictionary<Guid, ResourcePool>());

			return validator.TraceDataPerItem.TryGetValue(job.Id, out var traceData)
				? traceData.ErrorData.OfType<JobNodeSwapNotAllowedError>().ToList()
				: new List<JobNodeSwapNotAllowedError>();
		}
	}
}
