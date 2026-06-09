namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
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
		public void NodeGraph_CreateJob_HappyPath_WithResourceAndPoolNodes()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};

			var resourceNode = new JobResourceNode(pool, resource);
			var poolNode = new JobResourcePoolNode(pool);

			job.NodeGraph
				.Add(resourceNode)
				.Add(poolNode)
				.Connect(resourceNode, poolNode);

			job = objectCreator.CreateJob(job);

			Assert.IsNotNull(job);
			Assert.AreEqual(2, job.NodeGraph.Nodes.Count);
			Assert.AreEqual(1, job.NodeGraph.Connections.Count);
		}

		[TestMethod]
		public void NodeGraph_CreateJob_NonExistingResource_Fails()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var missingResourceId = Guid.NewGuid();
			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
			var node = new JobResourceNode(pool.Id, missingResourceId);
			job.NodeGraph.Add(node);

			try
			{
				objectCreator.CreateJob(job);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobNodeGraphInvalidResourceNodeError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(job.Id, error.Id);
				Assert.AreEqual(node.Id, error.NodeId);
				Assert.AreEqual(missingResourceId, error.ResourceId);
				Assert.AreEqual(pool.Id, error.ResourcePoolId);
				Assert.AreEqual($"Resource with ID '{missingResourceId}' does not exist.", error.ErrorMessage);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateJob_ResourceNotComplete_Fails()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			// Leave resource in Draft state (do not Complete).
			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
			var node = new JobResourceNode(pool, resource);
			job.NodeGraph.Add(node);

			try
			{
				objectCreator.CreateJob(job);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobNodeGraphInvalidResourceNodeError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(job.Id, error.Id);
				Assert.AreEqual(node.Id, error.NodeId);
				Assert.AreEqual(resource.Id, error.ResourceId);
				Assert.AreEqual($"Resource with ID '{resource.Id}' is not in a valid state. Only resources in 'Complete' state can be used.", error.ErrorMessage);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateJob_ResourcePoolNotComplete_Fails()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			// Pool stays in Draft.
			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
			var node = new JobResourcePoolNode(pool);
			job.NodeGraph.Add(node);

			try
			{
				objectCreator.CreateJob(job);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobNodeGraphInvalidResourcePoolNodeError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(job.Id, error.Id);
				Assert.AreEqual(node.Id, error.NodeId);
				Assert.AreEqual(pool.Id, error.ResourcePoolId);
				Assert.AreEqual($"Resource pool with ID '{pool.Id}' is not in a valid state. Only resource pools in 'Complete' state can be used.", error.ErrorMessage);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateJob_NonExistingResourcePool_Fails()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var missingPoolId = Guid.NewGuid();
			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
			var node = new JobResourcePoolNode(missingPoolId);
			job.NodeGraph.Add(node);

			try
			{
				objectCreator.CreateJob(job);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobNodeGraphInvalidResourcePoolNodeError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(job.Id, error.Id);
				Assert.AreEqual(node.Id, error.NodeId);
				Assert.AreEqual(missingPoolId, error.ResourcePoolId);
				Assert.AreEqual($"Resource pool with ID '{missingPoolId}' does not exist.", error.ErrorMessage);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateJob_ResourceNotInSpecifiedPool_Fails()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var poolA = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_PoolA" });
			poolA = TestContext.Api.ResourcePools.Complete(poolA);
			var poolB = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_PoolB" });
			poolB = TestContext.Api.ResourcePools.Complete(poolB);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(poolA);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
			var node = new JobResourceNode(poolB, resource);
			job.NodeGraph.Add(node);

			try
			{
				objectCreator.CreateJob(job);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobNodeGraphInvalidResourceNodeError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(job.Id, error.Id);
				Assert.AreEqual(node.Id, error.NodeId);
				Assert.AreEqual(resource.Id, error.ResourceId);
				Assert.AreEqual(poolB.Id, error.ResourcePoolId);
				Assert.AreEqual($"Resource with ID '{resource.Id}' is not part of resource pool with ID '{poolB.Id}'.", error.ErrorMessage);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateJob_NodeAliasTooLong_Fails()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
			var node = new JobResourcePoolNode(pool)
			{
				Alias = new string('a', 151),
			};
			job.NodeGraph.Add(node);

			try
			{
				objectCreator.CreateJob(job);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobNodeGraphInvalidNodeAliasError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(job.Id, error.Id);
				Assert.AreEqual(node.Id, error.NodeId);
				Assert.AreEqual(node.Alias, error.Alias);
				Assert.AreEqual("Alias exceeds maximum length of 150 characters.", error.ErrorMessage);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateJob_ConnectionLinksNodeToItself_Fails()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
			var node = new JobResourcePoolNode(pool);
			job.NodeGraph.Add(node).Connect(node, node);

			try
			{
				objectCreator.CreateJob(job);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobNodeGraphConnectionWithInvalidNodeError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(job.Id, error.Id);
				Assert.AreEqual(node.Id, error.NodeId);
				Assert.AreEqual(job.NodeGraph.Connections.Single().Id, error.ConnectionId);
				Assert.AreEqual("Connection cannot link a node to itself.", error.ErrorMessage);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateJob_MultipleNodeErrors_AllReported()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var missingResourceId = Guid.NewGuid();
			var missingPoolId = Guid.NewGuid();

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
			var badResourceNode = new JobResourceNode(pool.Id, missingResourceId);
			var badPoolNode = new JobResourcePoolNode(missingPoolId);
			var aliasNode = new JobResourcePoolNode(pool)
			{
				Alias = new string('b', 151),
			};
			job.NodeGraph.Add(badResourceNode).Add(badPoolNode).Add(aliasNode);

			try
			{
				objectCreator.CreateJob(job);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var nodeGraphErrors = ex.TraceData.ErrorData.OfType<JobNodeGraphError>().ToList();
				Assert.AreEqual(3, nodeGraphErrors.Count);
				Assert.IsTrue(nodeGraphErrors.All(x => x.Id == job.Id));

				Assert.AreEqual(1, nodeGraphErrors.OfType<JobNodeGraphInvalidResourceNodeError>().Count());
				Assert.AreEqual(1, nodeGraphErrors.OfType<JobNodeGraphInvalidResourcePoolNodeError>().Count());
				Assert.AreEqual(1, nodeGraphErrors.OfType<JobNodeGraphInvalidNodeAliasError>().Count());
			}
		}

		[TestMethod]
		public void NodeGraph_UpdateJob_AddInvalidResourceNode_Fails()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = objectCreator.CreateJob(new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			});

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var missingResourceId = Guid.NewGuid();
			var node = new JobResourceNode(pool.Id, missingResourceId);
			job.NodeGraph.Add(node);

			try
			{
				TestContext.Api.Jobs.Update(job);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobNodeGraphInvalidResourceNodeError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(job.Id, error.Id);
				Assert.AreEqual(node.Id, error.NodeId);
				Assert.AreEqual(missingResourceId, error.ResourceId);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateJobs_BulkOneInvalid_OnlyInvalidIsReported()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var validJob = new Job
			{
				Name = $"{prefix}_Valid",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
			validJob.NodeGraph.Add(new JobResourceNode(pool, resource));

			var invalidJob = new Job
			{
				Name = $"{prefix}_Invalid",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
			var missingResourceId = Guid.NewGuid();
			var invalidNode = new JobResourceNode(pool.Id, missingResourceId);
			invalidJob.NodeGraph.Add(invalidNode);

			try
			{
				objectCreator.CreateJobs([validJob, invalidJob]);
				Assert.Fail("Expected MediaOpsBulkException was not thrown.");
			}
			catch (MediaOpsBulkException<Guid> bulkException)
			{
				Assert.IsTrue(bulkException.Result.SuccessfulIds.Contains(validJob.Id));
				Assert.IsTrue(bulkException.Result.UnsuccessfulIds.Contains(invalidJob.Id));

				Assert.IsTrue(bulkException.Result.TraceDataPerItem.TryGetValue(invalidJob.Id, out var invalidTrace));
				var error = invalidTrace.ErrorData.OfType<JobNodeGraphInvalidResourceNodeError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(invalidJob.Id, error.Id);
				Assert.AreEqual(invalidNode.Id, error.NodeId);
				Assert.AreEqual(missingResourceId, error.ResourceId);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateJob_HappyPath_EmptyNodeGraph()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
			job = objectCreator.CreateJob(job);

			Assert.IsNotNull(job);
			Assert.AreEqual(0, job.NodeGraph.Nodes.Count);
			Assert.AreEqual(0, job.NodeGraph.Connections.Count);
		}

		[TestMethod]
		public void NodeGraph_CreateJob_NodeAliasAtMaxLength_Succeeds()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
			var node = new JobResourcePoolNode(pool)
			{
				Alias = new string('a', 150),
			};
			job.NodeGraph.Add(node);

			job = objectCreator.CreateJob(job);

			Assert.IsNotNull(job);
			Assert.AreEqual(1, job.NodeGraph.Nodes.Count);
			Assert.AreEqual(new string('a', 150), job.NodeGraph.Nodes.Single().Alias);
		}

		[TestMethod]
		public void NodeGraph_CreateJob_ResourceNodeWithDraftPool_Fails()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			// Pool stays in Draft.
			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });

			var completePool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_CompletePool" });
			completePool = TestContext.Api.ResourcePools.Complete(completePool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(completePool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
			};
			var node = new JobResourceNode(pool.Id, resource.Id);
			job.NodeGraph.Add(node);

			try
			{
				objectCreator.CreateJob(job);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobNodeGraphInvalidResourceNodeError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(job.Id, error.Id);
				Assert.AreEqual(node.Id, error.NodeId);
				Assert.AreEqual(resource.Id, error.ResourceId);
				Assert.AreEqual(pool.Id, error.ResourcePoolId);
				Assert.AreEqual($"Resource pool with ID '{pool.Id}' is not in a valid state. Only resource pools in 'Complete' state can be used.", error.ErrorMessage);
			}
		}

		[TestMethod]
		public void NodeGraph_UpdateJob_AddValidNode_NodeIsActuallyAdded()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var job = objectCreator.CreateJob(new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			});

			Assert.AreEqual(0, job.NodeGraph.Nodes.Count);

			var node = new JobResourcePoolNode(pool);
			job.NodeGraph.Add(node);

			job = TestContext.Api.Jobs.Update(job);

			Assert.AreEqual(1, job.NodeGraph.Nodes.Count);
			Assert.IsNotNull(job.NodeGraph.Nodes.OfType<JobResourcePoolNode>().SingleOrDefault());
		}

		[TestMethod]
		public void NodeGraph_UpdateJob_AddConnection_ConnectionIsActuallyAdded()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var resourceNode = new JobResourceNode(pool, resource);
			var poolNode = new JobResourcePoolNode(pool);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
			job.NodeGraph.Add(resourceNode).Add(poolNode);
			job = objectCreator.CreateJob(job);

			Assert.AreEqual(0, job.NodeGraph.Connections.Count);

			var fromNode = job.NodeGraph.Nodes.OfType<JobResourceNode>().Single();
			var toNode = job.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Single();
			job.NodeGraph.Connect(fromNode, toNode);

			job = TestContext.Api.Jobs.Update(job);

			Assert.AreEqual(1, job.NodeGraph.Connections.Count);
		}

		[TestMethod]
		public void NodeGraph_UpdateJob_RemoveConnection_ConnectionIsActuallyRemoved()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var resourceNode = new JobResourceNode(pool, resource);
			var poolNode = new JobResourcePoolNode(pool);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
			job.NodeGraph.Add(resourceNode).Add(poolNode).Connect(resourceNode, poolNode);
			job = objectCreator.CreateJob(job);

			Assert.AreEqual(1, job.NodeGraph.Connections.Count);

			job.NodeGraph.Disconnect(job.NodeGraph.Connections.Single());

			job = TestContext.Api.Jobs.Update(job);

			Assert.AreEqual(0, job.NodeGraph.Connections.Count);
			Assert.AreEqual(2, job.NodeGraph.Nodes.Count);
		}

		[TestMethod]
		public void NodeGraph_UpdateJob_RemoveNode_NodeIsActuallyRemoved()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var resourceNode = new JobResourceNode(pool, resource);
			var poolNode = new JobResourcePoolNode(pool);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
			job.NodeGraph.Add(resourceNode).Add(poolNode);

			job = objectCreator.CreateJob(job);

			Assert.AreEqual(2, job.NodeGraph.Nodes.Count);

			job.NodeGraph.Remove(job.NodeGraph.Nodes.OfType<JobResourceNode>().Single());

			job = TestContext.Api.Jobs.Update(job);

			Assert.AreEqual(1, job.NodeGraph.Nodes.Count);
			Assert.IsNull(job.NodeGraph.Nodes.OfType<JobResourceNode>().SingleOrDefault());
			Assert.IsNotNull(job.NodeGraph.Nodes.OfType<JobResourcePoolNode>().SingleOrDefault());
		}

		[TestMethod]
		public void NodeGraph_CreateJob_WithParentChildLink_HappyPath()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};

			var parentNode = new JobResourcePoolNode(pool);
			var childNode = new JobResourceNode(pool, resource);

			job.NodeGraph
				.Add(parentNode)
				.Add(childNode)
				.Link(parentNode, childNode);

			job = objectCreator.CreateJob(job);

			Assert.IsNotNull(job);
			Assert.AreEqual(2, job.NodeGraph.Nodes.Count);

			var persistedParent = job.NodeGraph.GetParent(job.NodeGraph.Nodes.Single(x => x.Id == childNode.Id));
			Assert.IsNotNull(persistedParent);
			Assert.AreEqual(parentNode.Id, persistedParent.Id);

			var persistedChildren = job.NodeGraph.GetChildren(job.NodeGraph.Nodes.Single(x => x.Id == parentNode.Id)).ToList();
			Assert.AreEqual(1, persistedChildren.Count);
			Assert.AreEqual(childNode.Id, persistedChildren.Single().Id);
		}

		[TestMethod]
		public void NodeGraph_RemoveParentNode_CascadesToChildren()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
			};

			var parentNode = new JobResourcePoolNode(pool);
			var childNode = new JobResourcePoolNode(pool);

			job.NodeGraph
				.Add(parentNode)
				.Add(childNode)
				.Link(parentNode, childNode);

			job.NodeGraph.Remove(parentNode);

			Assert.AreEqual(0, job.NodeGraph.Nodes.Count);
			Assert.IsNull(job.NodeGraph.GetParent(childNode));
		}

		[TestMethod]
		public void NodeGraph_Unlink_RemovesParentChildLink()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
			};

			var parentNode = new JobResourcePoolNode(pool);
			var childNode = new JobResourcePoolNode(pool);

			job.NodeGraph
				.Add(parentNode)
				.Add(childNode)
				.Link(parentNode, childNode);

			Assert.AreEqual(parentNode.Id, job.NodeGraph.GetParent(childNode).Id);

			job.NodeGraph.Unlink(childNode);

			Assert.IsNull(job.NodeGraph.GetParent(childNode));
			Assert.IsFalse(job.NodeGraph.GetChildren(parentNode).Any());
			Assert.AreEqual(2, job.NodeGraph.Nodes.Count);
		}

		[TestMethod]
		public void NodeGraph_CreateJob_LinkNodeToItself_Fails()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
			};
			var node = new JobResourcePoolNode(pool);
			job.NodeGraph.Add(node).Link(node, node);

			try
			{
				objectCreator.CreateJob(job);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobNodeGraphInvalidLinkError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(job.Id, error.Id);
				Assert.AreEqual(node.Id, error.ParentNodeId);
				Assert.AreEqual(node.Id, error.ChildNodeId);
				Assert.AreEqual("A node cannot be linked to itself.", error.ErrorMessage);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateJob_CascadedLink_Fails()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
			};
			var nodeA = new JobResourcePoolNode(pool);
			var nodeB = new JobResourcePoolNode(pool);
			var nodeC = new JobResourcePoolNode(pool);

			job.NodeGraph
				.Add(nodeA)
				.Add(nodeB)
				.Add(nodeC)
				.Link(nodeA, nodeB)
				.Link(nodeB, nodeC);

			try
			{
				objectCreator.CreateJob(job);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobNodeGraphInvalidLinkError>().FirstOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(job.Id, error.Id);
			}
		}

		[TestMethod]
		public void NodeGraph_CreateJob_ChildNodeWithConnection_Fails()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
			};
			var parentNode = new JobResourcePoolNode(pool);
			var childNode = new JobResourcePoolNode(pool);
			var otherNode = new JobResourcePoolNode(pool);

			job.NodeGraph
				.Add(parentNode)
				.Add(childNode)
				.Add(otherNode)
				.Link(parentNode, childNode)
				.Connect(childNode, otherNode);

			try
			{
				objectCreator.CreateJob(job);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<JobNodeGraphInvalidLinkError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(job.Id, error.Id);
				Assert.AreEqual(parentNode.Id, error.ParentNodeId);
				Assert.AreEqual(childNode.Id, error.ChildNodeId);
				Assert.AreEqual($"Child node with ID '{childNode.Id}' cannot participate in any connection.", error.ErrorMessage);
			}
		}
	}
}
