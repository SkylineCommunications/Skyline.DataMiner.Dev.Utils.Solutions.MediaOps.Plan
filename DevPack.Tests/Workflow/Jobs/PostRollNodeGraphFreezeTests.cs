namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	using StorageWorkflow = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	[TestClass]
	public sealed class PostRollNodeGraphFreezeTests
	{
		private static readonly Guid JobId = Guid.NewGuid();

		#region Unchanged graph

		[TestMethod]
		public void Validate_UnchangedGraph_ReturnsNoErrors()
		{
			var (graph, _, _) = GraphWithTwoConnectedNodes();
			var original = SnapshotOf(graph);

			var errors = JobNodeGraphPostRollFreezeValidator.Validate(JobId, graph, original);

			Assert.AreEqual(0, errors.Count);
		}

		[TestMethod]
		public void Validate_NodeConfigurationChanged_ReturnsNoErrors()
		{
			var (graph, first, _) = GraphWithTwoConnectedNodes();
			var original = SnapshotOf(graph);

			first.Alias = "A new alias";

			var errors = JobNodeGraphPostRollFreezeValidator.Validate(JobId, graph, original);

			Assert.AreEqual(0, errors.Count, "Node configuration stays editable while a job runs in its post-roll.");
		}

		#endregion

		#region Nodes

		[TestMethod]
		public void Validate_SwappedNode_ReturnsSwapNotAllowed()
		{
			var (graph, first, _) = GraphWithTwoConnectedNodes();
			var original = SnapshotOf(graph);

			var replacement = NewNode();
			graph.Swap(first, replacement);

			var errors = JobNodeGraphPostRollFreezeValidator.Validate(JobId, graph, original);

			var error = errors.OfType<JobNodeSwappedInPostRollNotAllowedError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(JobId, error.Id);
			Assert.AreEqual(first.Id, error.NodeId);
			Assert.AreEqual(replacement.Id, error.TargetNodeId);
		}

		[TestMethod]
		public void Validate_SwappedNode_DoesNotReportRetargetedConnectionsLinksAndGroups()
		{
			var (graph, first, second) = GraphWithTwoConnectedNodes();
			graph.Link(second, first);
			graph.AddGroup("Group").Add(first).Add(second);

			var original = SnapshotOf(graph);

			graph.Swap(first, NewNode());

			var errors = JobNodeGraphPostRollFreezeValidator.Validate(JobId, graph, original);

			Assert.AreEqual(1, errors.Count, "A swap retargets the connection, link and group, which must not be reported separately.");
			Assert.IsInstanceOfType(errors[0], typeof(JobNodeSwappedInPostRollNotAllowedError));
		}

		[TestMethod]
		public void Validate_AddedNode_ReturnsNodeAddedNotAllowed()
		{
			var (graph, _, _) = GraphWithTwoConnectedNodes();
			var original = SnapshotOf(graph);

			var added = NewNode();
			graph.Add(added);

			var errors = JobNodeGraphPostRollFreezeValidator.Validate(JobId, graph, original);

			var error = errors.OfType<JobNodeAddedInPostRollNotAllowedError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(added.Id, error.NodeId);
		}

		[TestMethod]
		public void Validate_RemovedNode_ReturnsNodeRemovedNotAllowed()
		{
			var (graph, first, _) = GraphWithTwoConnectedNodes();
			var original = SnapshotOf(graph);

			graph.Remove(first);

			var errors = JobNodeGraphPostRollFreezeValidator.Validate(JobId, graph, original);

			var error = errors.OfType<JobNodeRemovedInPostRollNotAllowedError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(first.Id, error.NodeId);
		}

		#endregion

		#region Connections

		[TestMethod]
		public void Validate_AddedConnection_ReturnsConnectionChangeNotAllowed()
		{
			var (graph, first, second) = GraphWithTwoConnectedNodes();
			var original = SnapshotOf(graph);

			graph.Connect(second, first);

			var errors = JobNodeGraphPostRollFreezeValidator.Validate(JobId, graph, original);

			var error = errors.OfType<JobNodeConnectionChangedInPostRollNotAllowedError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(graph.Connections.Last().Id, error.ConnectionId);
		}

		[TestMethod]
		public void Validate_RemovedConnection_ReturnsConnectionChangeNotAllowed()
		{
			var (graph, _, _) = GraphWithTwoConnectedNodes();
			var original = SnapshotOf(graph);

			var connection = graph.Connections.Single();
			graph.Disconnect(connection);

			var errors = JobNodeGraphPostRollFreezeValidator.Validate(JobId, graph, original);

			var error = errors.OfType<JobNodeConnectionChangedInPostRollNotAllowedError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(connection.Id, error.ConnectionId);
		}

		[TestMethod]
		public void Validate_ReconfiguredConnection_ReturnsConnectionChangeNotAllowed()
		{
			var (graph, _, _) = GraphWithTwoConnectedNodes();
			var original = SnapshotOf(graph);

			var connection = graph.Connections.Single();
			connection.Configuration = new ShuffleLevelBasedConnectionConfiguration();

			var errors = JobNodeGraphPostRollFreezeValidator.Validate(JobId, graph, original);

			var error = errors.OfType<JobNodeConnectionChangedInPostRollNotAllowedError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(connection.Id, error.ConnectionId);
		}

		#endregion

		#region Links

		[TestMethod]
		public void Validate_AddedLink_ReturnsLinkChangeNotAllowed()
		{
			var (graph, first, second) = GraphWithTwoConnectedNodes();
			var original = SnapshotOf(graph);

			graph.Link(first, second);

			var errors = JobNodeGraphPostRollFreezeValidator.Validate(JobId, graph, original);

			var error = errors.OfType<JobNodeLinkChangedInPostRollNotAllowedError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(first.Id, error.ParentNodeId);
			Assert.AreEqual(second.Id, error.ChildNodeId);
		}

		[TestMethod]
		public void Validate_RemovedLink_ReturnsLinkChangeNotAllowed()
		{
			var (graph, first, second) = GraphWithTwoConnectedNodes();
			graph.Link(first, second);

			var original = SnapshotOf(graph);

			graph.Unlink(second);

			var errors = JobNodeGraphPostRollFreezeValidator.Validate(JobId, graph, original);

			var error = errors.OfType<JobNodeLinkChangedInPostRollNotAllowedError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(first.Id, error.ParentNodeId);
			Assert.AreEqual(second.Id, error.ChildNodeId);
		}

		#endregion

		#region Groups

		[TestMethod]
		public void Validate_AddedGroup_ReturnsGroupChangeNotAllowed()
		{
			var (graph, first, _) = GraphWithTwoConnectedNodes();
			var original = SnapshotOf(graph);

			graph.AddGroup("New group").Add(first);

			var errors = JobNodeGraphPostRollFreezeValidator.Validate(JobId, graph, original);

			var error = errors.OfType<JobNodeGroupChangedInPostRollNotAllowedError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual("New group", error.GroupName);
		}

		[TestMethod]
		public void Validate_RemovedGroup_ReturnsGroupChangeNotAllowed()
		{
			var (graph, first, _) = GraphWithTwoConnectedNodes();
			var group = graph.AddGroup("Group");
			group.Add(first);

			var original = SnapshotOf(graph);

			graph.RemoveGroup(group);

			var errors = JobNodeGraphPostRollFreezeValidator.Validate(JobId, graph, original);

			var error = errors.OfType<JobNodeGroupChangedInPostRollNotAllowedError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual("Group", error.GroupName);
		}

		[TestMethod]
		public void Validate_ChangedGroupMembership_ReturnsGroupChangeNotAllowed()
		{
			var (graph, first, second) = GraphWithTwoConnectedNodes();
			var group = graph.AddGroup("Group");
			group.Add(first);

			var original = SnapshotOf(graph);

			group.Add(second);

			var errors = JobNodeGraphPostRollFreezeValidator.Validate(JobId, graph, original);

			var error = errors.OfType<JobNodeGroupChangedInPostRollNotAllowedError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual("Group", error.GroupName);
		}

		#endregion

		#region Helpers

		private static JobResourceNode NewNode()
		{
			return new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
		}

		private static JobResourceNode ExistingNode()
		{
			var node = NewNode();
			node.IsNew = false;
			return node;
		}

		private static (NodeGraph<JobNode> Graph, JobResourceNode First, JobResourceNode Second) GraphWithTwoConnectedNodes()
		{
			var first = ExistingNode();
			var second = ExistingNode();
			var connection = new NodeConnection<JobNode>(first, second) { IsNew = false };

			// Mirrors how a stored job is parsed: existing nodes are handed to the constructor, they cannot be added.
			var graph = new NodeGraph<JobNode>(new JobNode[] { first, second }, new[] { connection });

			return (graph, first, second);
		}

		/// <summary>
		/// Builds the persisted representation of the graph, mirroring how a job is serialized to its DOM instance.
		/// </summary>
		private static StorageWorkflow.JobsInstance SnapshotOf(NodeGraph<JobNode> graph)
		{
			var instance = new StorageWorkflow.JobsInstance(Guid.NewGuid());

			foreach (var node in graph.Nodes)
			{
				instance.Nodes.Add(new StorageWorkflow.NodesSection { NodeID = node.Id });
			}

			foreach (var connection in graph.Connections)
			{
				var section = new StorageWorkflow.ConnectionsSection
				{
					ConnectionID = connection.Id,
					SourceNodeID = connection.From.Id,
					DestinationNodeID = connection.To.Id,
				};

				connection.Configuration.WriteTo(section);
				instance.Connections.Add(section);
			}

			foreach (var link in graph.Links)
			{
				instance.NodeRelationships.Add(new StorageWorkflow.NodeRelationshipsSection
				{
					ParentNodeID = link.Value.Id,
					ChildNodeID = link.Key.Id,
				});
			}

			foreach (var group in graph.Groups)
			{
				var section = new StorageWorkflow.NodeGroupsSection { GroupName = group.Name };
				foreach (var node in group.Nodes)
				{
					section.GroupNodeIds.Add(node.Id);
				}

				instance.NodeGroups.Add(section);
			}

			return instance;
		}

		#endregion
	}
}
