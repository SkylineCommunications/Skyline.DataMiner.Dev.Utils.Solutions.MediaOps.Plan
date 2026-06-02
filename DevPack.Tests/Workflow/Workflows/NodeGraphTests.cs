namespace RT_MediaOps.Plan.Workflow.Workflows
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;
	using RT_MediaOps.Plan.RST;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class NodeGraphTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public NodeGraphTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void NodeGraph_CreateWorkflow_HappyPath_WithResourceAndPoolNodes()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };

			var resourceNode = new WorkflowResourceNode(pool, resource);
			var poolNode = new WorkflowResourcePoolNode(pool);

			workflow.NodeGraph
				.Add(resourceNode)
				.Add(poolNode)
				.Connect(resourceNode, poolNode);

			workflow = objectCreator.CreateWorkflow(workflow);

			Assert.IsNotNull(workflow);
			Assert.AreEqual(2, workflow.NodeGraph.Nodes.Count);
			Assert.AreEqual(1, workflow.NodeGraph.Connections.Count);
		}

		[TestMethod]
		public void NodeGraph_CreateWorkflow_NonExistingResource_Fails()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var missingResourceId = Guid.NewGuid();
			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			var node = new WorkflowResourceNode(pool.Id, missingResourceId);
			workflow.NodeGraph.Add(node);

			try
			{
				objectCreator.CreateWorkflow(workflow);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<WorkflowNodeGraphInvalidResourceNodeError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(workflow.Id, error.Id);
				Assert.AreEqual(node.Id, error.NodeId);
				Assert.AreEqual(missingResourceId, error.ResourceId);
				Assert.AreEqual(pool.Id, error.ResourcePoolId);
				Assert.AreEqual($"Resource with ID '{missingResourceId}' does not exist.", error.ErrorMessage);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateWorkflow_ResourceNotComplete_Fails()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			// Leave resource in Draft state (do not Complete).
			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			var node = new WorkflowResourceNode(pool, resource);
			workflow.NodeGraph.Add(node);

			try
			{
				objectCreator.CreateWorkflow(workflow);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<WorkflowNodeGraphInvalidResourceNodeError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(workflow.Id, error.Id);
				Assert.AreEqual(node.Id, error.NodeId);
				Assert.AreEqual(resource.Id, error.ResourceId);
				Assert.AreEqual($"Resource with ID '{resource.Id}' is not in a valid state. Only resources in 'Complete' state can be used.", error.ErrorMessage);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateWorkflow_ResourcePoolNotComplete_Fails()
		{
			var prefix = Guid.NewGuid();

			// Pool stays in Draft.
			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			var node = new WorkflowResourcePoolNode(pool);
			workflow.NodeGraph.Add(node);

			try
			{
				objectCreator.CreateWorkflow(workflow);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<WorkflowNodeGraphInvalidResourcePoolNodeError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(workflow.Id, error.Id);
				Assert.AreEqual(node.Id, error.NodeId);
				Assert.AreEqual(pool.Id, error.ResourcePoolId);
				Assert.AreEqual($"Resource pool with ID '{pool.Id}' is not in a valid state. Only resource pools in 'Complete' state can be used.", error.ErrorMessage);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateWorkflow_NonExistingResourcePool_Fails()
		{
			var prefix = Guid.NewGuid();

			var missingPoolId = Guid.NewGuid();
			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			var node = new WorkflowResourcePoolNode(missingPoolId);
			workflow.NodeGraph.Add(node);

			try
			{
				objectCreator.CreateWorkflow(workflow);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<WorkflowNodeGraphInvalidResourcePoolNodeError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(workflow.Id, error.Id);
				Assert.AreEqual(node.Id, error.NodeId);
				Assert.AreEqual(missingPoolId, error.ResourcePoolId);
				Assert.AreEqual($"Resource pool with ID '{missingPoolId}' does not exist.", error.ErrorMessage);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateWorkflow_ResourceNotInSpecifiedPool_Fails()
		{
			var prefix = Guid.NewGuid();

			var poolA = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_PoolA" });
			poolA = TestContext.Api.ResourcePools.Complete(poolA);
			var poolB = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_PoolB" });
			poolB = TestContext.Api.ResourcePools.Complete(poolB);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(poolA);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			var node = new WorkflowResourceNode(poolB, resource);
			workflow.NodeGraph.Add(node);

			try
			{
				objectCreator.CreateWorkflow(workflow);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<WorkflowNodeGraphInvalidResourceNodeError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(workflow.Id, error.Id);
				Assert.AreEqual(node.Id, error.NodeId);
				Assert.AreEqual(resource.Id, error.ResourceId);
				Assert.AreEqual(poolB.Id, error.ResourcePoolId);
				Assert.AreEqual($"Resource with ID '{resource.Id}' is not part of resource pool with ID '{poolB.Id}'.", error.ErrorMessage);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateWorkflow_NodeAliasTooLong_Fails()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			var node = new WorkflowResourcePoolNode(pool)
			{
				Alias = new string('a', 151),
			};
			workflow.NodeGraph.Add(node);

			try
			{
				objectCreator.CreateWorkflow(workflow);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<WorkflowNodeGraphInvalidNodeAliasError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(workflow.Id, error.Id);
				Assert.AreEqual(node.Id, error.NodeId);
				Assert.AreEqual(node.Alias, error.Alias);
				Assert.AreEqual("Alias exceeds maximum length of 150 characters.", error.ErrorMessage);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateWorkflow_ConnectionLinksNodeToItself_Fails()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			var node = new WorkflowResourcePoolNode(pool);
			workflow.NodeGraph.Add(node).Connect(node, node);

			try
			{
				objectCreator.CreateWorkflow(workflow);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<WorkflowNodeGraphConnectionWithInvalidNodeError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(workflow.Id, error.Id);
				Assert.AreEqual(node.Id, error.NodeId);
				Assert.AreEqual(workflow.NodeGraph.Connections.Single().Id, error.ConnectionId);
				Assert.AreEqual("Connection cannot link a node to itself.", error.ErrorMessage);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateWorkflow_MultipleNodeErrors_AllReported()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var missingResourceId = Guid.NewGuid();
			var missingPoolId = Guid.NewGuid();

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			var badResourceNode = new WorkflowResourceNode(pool.Id, missingResourceId);
			var badPoolNode = new WorkflowResourcePoolNode(missingPoolId);
			var aliasNode = new WorkflowResourcePoolNode(pool)
			{
				Alias = new string('b', 151),
			};
			workflow.NodeGraph.Add(badResourceNode).Add(badPoolNode).Add(aliasNode);

			try
			{
				objectCreator.CreateWorkflow(workflow);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var nodeGraphErrors = ex.TraceData.ErrorData.OfType<WorkflowNodeGraphError>().ToList();
				Assert.AreEqual(3, nodeGraphErrors.Count);
				Assert.IsTrue(nodeGraphErrors.All(x => x.Id == workflow.Id));

				Assert.AreEqual(1, nodeGraphErrors.OfType<WorkflowNodeGraphInvalidResourceNodeError>().Count());
				Assert.AreEqual(1, nodeGraphErrors.OfType<WorkflowNodeGraphInvalidResourcePoolNodeError>().Count());
				Assert.AreEqual(1, nodeGraphErrors.OfType<WorkflowNodeGraphInvalidNodeAliasError>().Count());
			}
		}

		[TestMethod]
		public void NodeGraph_UpdateWorkflow_AddInvalidResourceNode_Fails()
		{
			var prefix = Guid.NewGuid();

			var workflow = objectCreator.CreateWorkflow(new Workflow { Name = $"{prefix}_Workflow" });

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var missingResourceId = Guid.NewGuid();
			var node = new WorkflowResourceNode(pool.Id, missingResourceId);
			workflow.NodeGraph.Add(node);

			try
			{
				TestContext.Api.Workflows.Update(workflow);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<WorkflowNodeGraphInvalidResourceNodeError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(workflow.Id, error.Id);
				Assert.AreEqual(node.Id, error.NodeId);
				Assert.AreEqual(missingResourceId, error.ResourceId);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateWorkflows_BulkOneInvalid_OnlyInvalidIsReported()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var validWorkflow = new Workflow { Name = $"{prefix}_Valid" };
			validWorkflow.NodeGraph.Add(new WorkflowResourceNode(pool, resource));

			var invalidWorkflow = new Workflow { Name = $"{prefix}_Invalid" };
			var missingResourceId = Guid.NewGuid();
			var invalidNode = new WorkflowResourceNode(pool.Id, missingResourceId);
			invalidWorkflow.NodeGraph.Add(invalidNode);

			try
			{
				objectCreator.CreateWorkflows([validWorkflow, invalidWorkflow]);
				Assert.Fail("Expected MediaOpsBulkException was not thrown.");
			}
			catch (MediaOpsBulkException<Guid> bulkException)
			{
				Assert.IsTrue(bulkException.Result.SuccessfulIds.Contains(validWorkflow.Id));
				Assert.IsTrue(bulkException.Result.UnsuccessfulIds.Contains(invalidWorkflow.Id));

				Assert.IsTrue(bulkException.Result.TraceDataPerItem.TryGetValue(invalidWorkflow.Id, out var invalidTrace));
				var error = invalidTrace.ErrorData.OfType<WorkflowNodeGraphInvalidResourceNodeError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(invalidWorkflow.Id, error.Id);
				Assert.AreEqual(invalidNode.Id, error.NodeId);
				Assert.AreEqual(missingResourceId, error.ResourceId);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateWorkflow_HappyPath_EmptyNodeGraph()
		{
			var prefix = Guid.NewGuid();

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			workflow = objectCreator.CreateWorkflow(workflow);

			Assert.IsNotNull(workflow);
			Assert.AreEqual(0, workflow.NodeGraph.Nodes.Count);
			Assert.AreEqual(0, workflow.NodeGraph.Connections.Count);
		}

		[TestMethod]
		public void NodeGraph_CreateWorkflow_NodeAliasAtMaxLength_Succeeds()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			var node = new WorkflowResourcePoolNode(pool)
			{
				Alias = new string('a', 150),
			};
			workflow.NodeGraph.Add(node);

			workflow = objectCreator.CreateWorkflow(workflow);

			Assert.IsNotNull(workflow);
			Assert.AreEqual(1, workflow.NodeGraph.Nodes.Count);
			Assert.AreEqual(new string('a', 150), workflow.NodeGraph.Nodes.Single().Alias);
		}

		[TestMethod]
		public void NodeGraph_CreateWorkflow_ResourceNodeWithDraftPool_Fails()
		{
			var prefix = Guid.NewGuid();

			// Pool stays in Draft.
			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });

			var completePool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_CompletePool" });
			completePool = TestContext.Api.ResourcePools.Complete(completePool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(completePool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			var node = new WorkflowResourceNode(pool.Id, resource.Id);
			workflow.NodeGraph.Add(node);

			try
			{
				objectCreator.CreateWorkflow(workflow);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<WorkflowNodeGraphInvalidResourceNodeError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(workflow.Id, error.Id);
				Assert.AreEqual(node.Id, error.NodeId);
				Assert.AreEqual(resource.Id, error.ResourceId);
				Assert.AreEqual(pool.Id, error.ResourcePoolId);
				Assert.AreEqual($"Resource pool with ID '{pool.Id}' is not in a valid state. Only resource pools in 'Complete' state can be used.", error.ErrorMessage);
			}
		}

		[TestMethod]
		public void NodeGraph_UpdateWorkflow_AddValidNode_NodeIsActuallyAdded()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var workflow = objectCreator.CreateWorkflow(new Workflow { Name = $"{prefix}_Workflow" });

			Assert.AreEqual(0, workflow.NodeGraph.Nodes.Count);

			var node = new WorkflowResourcePoolNode(pool);
			workflow.NodeGraph.Add(node);

			workflow = TestContext.Api.Workflows.Update(workflow);

			Assert.AreEqual(1, workflow.NodeGraph.Nodes.Count);
			Assert.IsNotNull(workflow.NodeGraph.Nodes.OfType<WorkflowResourcePoolNode>().SingleOrDefault());
		}

		[TestMethod]
		public void NodeGraph_UpdateWorkflow_AddConnection_ConnectionIsActuallyAdded()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var resourceNode = new WorkflowResourceNode(pool, resource);
			var poolNode = new WorkflowResourcePoolNode(pool);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			workflow.NodeGraph.Add(resourceNode).Add(poolNode);
			workflow = objectCreator.CreateWorkflow(workflow);

			Assert.AreEqual(0, workflow.NodeGraph.Connections.Count);

			var fromNode = workflow.NodeGraph.Nodes.OfType<WorkflowResourceNode>().Single();
			var toNode = workflow.NodeGraph.Nodes.OfType<WorkflowResourcePoolNode>().Single();
			workflow.NodeGraph.Connect(fromNode, toNode);

			workflow = TestContext.Api.Workflows.Update(workflow);

			Assert.AreEqual(1, workflow.NodeGraph.Connections.Count);
		}

		[TestMethod]
		public void NodeGraph_UpdateWorkflow_RemoveConnection_ConnectionIsActuallyRemoved()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var resourceNode = new WorkflowResourceNode(pool, resource);
			var poolNode = new WorkflowResourcePoolNode(pool);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			workflow.NodeGraph.Add(resourceNode).Add(poolNode).Connect(resourceNode, poolNode);
			workflow = objectCreator.CreateWorkflow(workflow);

			Assert.AreEqual(1, workflow.NodeGraph.Connections.Count);

			workflow.NodeGraph.Disconnect(workflow.NodeGraph.Connections.Single());

			workflow = TestContext.Api.Workflows.Update(workflow);

			Assert.AreEqual(0, workflow.NodeGraph.Connections.Count);
			Assert.AreEqual(2, workflow.NodeGraph.Nodes.Count);
		}

		[TestMethod]
		public void NodeGraph_UpdateWorkflow_RemoveNode_NodeIsActuallyRemoved()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var resourceNode = new WorkflowResourceNode(pool, resource);
			var poolNode = new WorkflowResourcePoolNode(pool);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			workflow.NodeGraph.Add(resourceNode).Add(poolNode);

			workflow = objectCreator.CreateWorkflow(workflow);

			Assert.AreEqual(2, workflow.NodeGraph.Nodes.Count);

			workflow.NodeGraph.Remove(workflow.NodeGraph.Nodes.OfType<WorkflowResourceNode>().Single());

			workflow = TestContext.Api.Workflows.Update(workflow);

			Assert.AreEqual(1, workflow.NodeGraph.Nodes.Count);
			Assert.IsNull(workflow.NodeGraph.Nodes.OfType<WorkflowResourceNode>().SingleOrDefault());
			Assert.IsNotNull(workflow.NodeGraph.Nodes.OfType<WorkflowResourcePoolNode>().SingleOrDefault());
		}
	}
}
