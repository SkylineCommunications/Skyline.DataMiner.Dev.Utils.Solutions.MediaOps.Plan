namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	internal abstract class NodeGraphValidator<TNode> : ApiObjectValidator where TNode : NodeBase
	{
		private readonly Guid apiObjectId;
		private readonly NodeGraph<TNode> nodeGraph;
		private readonly IReadOnlyDictionary<Guid, Resource> resourcesById;
		private readonly IReadOnlyDictionary<Guid, ResourcePool> resourcePoolsById;

		private readonly Lazy<HashSet<string>> lazyNodeIds;

		private protected NodeGraphValidator(
			Guid apiObjectId,
			NodeGraph<TNode> nodeGraph,
			IReadOnlyDictionary<Guid, Resource> resourcesById,
			IReadOnlyDictionary<Guid, ResourcePool> resourcePoolsById)
		{
			if (apiObjectId == Guid.Empty)
			{
				throw new ArgumentException("API object ID cannot be an empty GUID.", nameof(apiObjectId));
			}

			this.apiObjectId = apiObjectId;
			this.nodeGraph = nodeGraph ?? throw new ArgumentNullException(nameof(nodeGraph));
			this.resourcesById = resourcesById ?? throw new ArgumentNullException(nameof(resourcesById));
			this.resourcePoolsById = resourcePoolsById ?? throw new ArgumentNullException(nameof(resourcePoolsById));

			lazyNodeIds = new Lazy<HashSet<string>>(() => new HashSet<string>(nodeGraph.Nodes.Select(x => x.Id).Where(id => id != null)));

			Validate();
		}

		private protected Guid ApiObjectId => apiObjectId;

		private HashSet<string> NodeIds => lazyNodeIds.Value;

		protected abstract MediaOpsErrorData CreateEmptyNodeIdError(string errorMessage);
		protected abstract MediaOpsErrorData CreateDuplicateNodeIdError(string nodeId, string errorMessage);
		protected abstract MediaOpsErrorData CreateNodeAliasError(string nodeId, string alias, string errorMessage);
		protected abstract MediaOpsErrorData CreateResourceNodeError(string nodeId, Guid resourceId, Guid resourcePoolId, string errorMessage);
		protected abstract MediaOpsErrorData CreateResourcePoolNodeError(string nodeId, Guid resourcePoolId, string errorMessage);
		protected abstract MediaOpsErrorData CreateEmptyConnectionIdError(string errorMessage);
		protected abstract MediaOpsErrorData CreateDuplicateConnectionIdError(string connectionId, string errorMessage);
		protected abstract MediaOpsErrorData CreateConnectionInvalidNodeLinkError(string connectionId, string nodeId, string errorMessage);

		private void Validate()
		{
			ValidateNodes();
			ValidateConnections();
		}

		private void ValidateNodes()
		{
			ValidateNodeIds();
			ValidateNodeAliases();

			foreach (var node in nodeGraph.Nodes)
			{
				switch (node)
				{
					case IResourceNode resourceNode:
						ValidateResourceNode(node.Id, resourceNode);
						break;

					case IResourcePoolNode poolNode:
						ValidateResourcePoolNode(node.Id, poolNode);
						break;
				}
			}
		}

		private void ValidateNodeIds()
		{
			var requiringValidation = nodeGraph.Nodes.ToList();

			foreach (var node in requiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Id)).ToArray())
			{
				ReportError(apiObjectId, CreateEmptyNodeIdError("ID cannot be empty."));

				requiringValidation.Remove(node);
			}

			var duplicates = requiringValidation
				.GroupBy(node => node.Id)
				.Where(g => g.Count() > 1)
				.SelectMany(x => x)
				.ToList();

			foreach (var node in duplicates)
			{
				ReportError(apiObjectId, CreateDuplicateNodeIdError(node.Id, "Node has a duplicate ID."));
			}
		}

		private void ValidateNodeAliases()
		{
			foreach (var node in nodeGraph.Nodes.Where(x => InputValidator.IsNonEmptyText(x.Alias) && !InputValidator.HasValidTextLength(x.Alias)))
			{
				ReportError(apiObjectId, CreateNodeAliasError(node.Id, node.Alias, $"Alias exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters."));
			}
		}

		private void ValidateResourceNode(string nodeId, IResourceNode resourceNode)
		{
			if (!resourcesById.TryGetValue(resourceNode.ResourceId, out var resource))
			{
				ReportError(apiObjectId, CreateResourceNodeError(nodeId, resourceNode.ResourceId, resourceNode.ResourcePoolId, $"Resource with ID '{resourceNode.ResourceId}' does not exist."));
				return;
			}

			if (resource.State != ResourceState.Complete)
			{
				ReportError(apiObjectId, CreateResourceNodeError(nodeId, resourceNode.ResourceId, resourceNode.ResourcePoolId, $"Resource with ID '{resourceNode.ResourceId}' is not in a valid state. Only resources in 'Complete' state can be used."));
				return;
			}

			if (!resourcePoolsById.TryGetValue(resourceNode.ResourcePoolId, out var resourcePool))
			{
				ReportError(apiObjectId, CreateResourceNodeError(nodeId, resourceNode.ResourceId, resourceNode.ResourcePoolId, $"Resource pool with ID '{resourceNode.ResourcePoolId}' does not exist."));
				return;
			}

			if (resourcePool.State != ResourcePoolState.Complete)
			{
				ReportError(apiObjectId, CreateResourceNodeError(nodeId, resourceNode.ResourceId, resourceNode.ResourcePoolId, $"Resource pool with ID '{resourceNode.ResourcePoolId}' is not in a valid state. Only resource pools in 'Complete' state can be used."));
				return;
			}

			if (!resource.ResourcePoolIds.Contains(resourcePool.Id))
			{
				ReportError(apiObjectId, CreateResourceNodeError(nodeId, resourceNode.ResourceId, resourceNode.ResourcePoolId, $"Resource with ID '{resourceNode.ResourceId}' is not part of resource pool with ID '{resourceNode.ResourcePoolId}'."));
				return;
			}
		}

		private void ValidateResourcePoolNode(string nodeId, IResourcePoolNode poolNode)
		{
			if (!resourcePoolsById.TryGetValue(poolNode.ResourcePoolId, out var resourcePool))
			{
				ReportError(apiObjectId, CreateResourcePoolNodeError(nodeId, poolNode.ResourcePoolId, $"Resource pool with ID '{poolNode.ResourcePoolId}' does not exist."));
				return;
			}

			if (resourcePool.State != ResourcePoolState.Complete)
			{
				ReportError(apiObjectId, CreateResourcePoolNodeError(nodeId, poolNode.ResourcePoolId, $"Resource pool with ID '{poolNode.ResourcePoolId}' is not in a valid state. Only resource pools in 'Complete' state can be used."));
			}
		}

		private void ValidateConnections()
		{
			ValidateConnectionIds();

			foreach (var connection in nodeGraph.Connections)
			{
				ValidateConnection(connection);
			}
		}

		private void ValidateConnectionIds()
		{
			var requiringValidation = nodeGraph.Connections.ToList();

			foreach (var connection in requiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Id)).ToArray())
			{
				ReportError(apiObjectId, CreateEmptyConnectionIdError("ID cannot be empty."));

				requiringValidation.Remove(connection);
			}

			var duplicates = requiringValidation
				.GroupBy(connection => connection.Id)
				.Where(g => g.Count() > 1)
				.SelectMany(x => x)
				.ToList();

			foreach (var connection in duplicates)
			{
				ReportError(apiObjectId, CreateDuplicateConnectionIdError(connection.Id, "Connection has a duplicate ID."));
			}
		}

		private void ValidateConnection(NodeConnection<TNode> connection)
		{
			if (connection.From == null || connection.To == null)
			{
				ReportError(apiObjectId, CreateConnectionInvalidNodeLinkError(connection.Id, string.Empty, "Connection must link two valid nodes."));
				return;
			}

			if (!NodeIds.Contains(connection.From.Id))
			{
				ReportError(apiObjectId, CreateConnectionInvalidNodeLinkError(connection.Id, connection.From.Id, $"Node with ID '{connection.From.Id}' does not exist in the graph."));
				return;
			}

			if (!NodeIds.Contains(connection.To.Id))
			{
				ReportError(apiObjectId, CreateConnectionInvalidNodeLinkError(connection.Id, connection.To.Id, $"Node with ID '{connection.To.Id}' does not exist in the graph."));
				return;
			}

			if (connection.From.Id == connection.To.Id)
			{
				ReportError(apiObjectId, CreateConnectionInvalidNodeLinkError(connection.Id, connection.From.Id, "Connection cannot link a node to itself."));
			}
		}
	}

	internal class JobNodeGraphValidator : NodeGraphValidator<JobNode>
	{
		private JobNodeGraphValidator(Guid jobId, NodeGraph<JobNode> nodeGraph, IReadOnlyDictionary<Guid, Resource> resourcesById, IReadOnlyDictionary<Guid, ResourcePool> resourcePoolsById)
			: base(jobId, nodeGraph, resourcesById, resourcePoolsById)
		{
		}

		public static ApiObjectValidator Validate(Guid jobId, NodeGraph<JobNode> nodeGraph, IReadOnlyDictionary<Guid, Resource> resourcesById, IReadOnlyDictionary<Guid, ResourcePool> resourcePoolsById)
		{
			return new JobNodeGraphValidator(jobId, nodeGraph, resourcesById, resourcePoolsById);
		}

		protected override MediaOpsErrorData CreateConnectionInvalidNodeLinkError(string connectionId, string nodeId, string errorMessage)
		{
			return new JobNodeGraphConnectionWithInvalidNodeError
			{
				ErrorMessage = errorMessage,
				NodeId = nodeId,
				ConnectionId = connectionId,
				Id = ApiObjectId,
			};
		}

		protected override MediaOpsErrorData CreateDuplicateConnectionIdError(string connectionId, string errorMessage)
		{
			return new JobNodeGraphDuplicateConnectionIdError
			{
				ErrorMessage = errorMessage,
				ConnectionId = connectionId,
				Id = ApiObjectId,
			};
		}

		protected override MediaOpsErrorData CreateDuplicateNodeIdError(string nodeId, string errorMessage)
		{
			return new JobNodeGraphDuplicateNodeIdError
			{
				ErrorMessage = errorMessage,
				NodeId = nodeId,
				Id = ApiObjectId,
			};
		}

		protected override MediaOpsErrorData CreateEmptyConnectionIdError(string errorMessage)
		{
			return new JobNodeGraphEmptyConnectionIdError
			{
				ErrorMessage = errorMessage,
				Id = ApiObjectId,
			};
		}

		protected override MediaOpsErrorData CreateEmptyNodeIdError(string errorMessage)
		{
			return new JobNodeGraphEmptyNodeIdError
			{
				ErrorMessage = errorMessage,
				Id = ApiObjectId,
			};
		}

		protected override MediaOpsErrorData CreateNodeAliasError(string nodeId, string alias, string errorMessage)
		{
			return new JobNodeGraphInvalidNodeAliasError
			{
				ErrorMessage = errorMessage,
				Alias = alias,
				NodeId = nodeId,
				Id = ApiObjectId,
			};
		}

		protected override MediaOpsErrorData CreateResourceNodeError(string nodeId, Guid resourceId, Guid resourcePoolId, string errorMessage)
		{
			return new JobNodeGraphInvalidResourceNodeError
			{
				ErrorMessage = errorMessage,
				ResourcePoolId = resourcePoolId,
				ResourceId = resourceId,
				NodeId = nodeId,
				Id = ApiObjectId,
			};
		}

		protected override MediaOpsErrorData CreateResourcePoolNodeError(string nodeId, Guid resourcePoolId, string errorMessage)
		{
			return new JobNodeGraphInvalidResourcePoolNodeError
			{
				ErrorMessage = errorMessage,
				ResourcePoolId = resourcePoolId,
				NodeId = nodeId,
				Id = ApiObjectId,
			};
		}
	}

	internal class WorkflowNodeGraphValidator : NodeGraphValidator<WorkflowNode>
	{
		private WorkflowNodeGraphValidator(Guid workflowId, NodeGraph<WorkflowNode> nodeGraph, IReadOnlyDictionary<Guid, Resource> resourcesById, IReadOnlyDictionary<Guid, ResourcePool> resourcePoolsById)
			: base(workflowId, nodeGraph, resourcesById, resourcePoolsById)
		{
		}

		public static ApiObjectValidator Validate(Guid workflowId, NodeGraph<WorkflowNode> nodeGraph, IReadOnlyDictionary<Guid, Resource> resourcesById, IReadOnlyDictionary<Guid, ResourcePool> resourcePoolsById)
		{
			return new WorkflowNodeGraphValidator(workflowId, nodeGraph, resourcesById, resourcePoolsById);
		}

		protected override MediaOpsErrorData CreateConnectionInvalidNodeLinkError(string connectionId, string nodeId, string errorMessage)
		{
			return new WorkflowNodeGraphConnectionWithInvalidNodeError
			{
				ErrorMessage = errorMessage,
				NodeId = nodeId,
				ConnectionId = connectionId,
				Id = ApiObjectId,
			};
		}

		protected override MediaOpsErrorData CreateDuplicateConnectionIdError(string connectionId, string errorMessage)
		{
			return new WorkflowNodeGraphDuplicateConnectionIdError
			{
				ErrorMessage = errorMessage,
				ConnectionId = connectionId,
				Id = ApiObjectId,
			};
		}

		protected override MediaOpsErrorData CreateDuplicateNodeIdError(string nodeId, string errorMessage)
		{
			return new WorkflowNodeGraphDuplicateNodeIdError
			{
				ErrorMessage = errorMessage,
				NodeId = nodeId,
				Id = ApiObjectId,
			};
		}

		protected override MediaOpsErrorData CreateEmptyConnectionIdError(string errorMessage)
		{
			return new WorkflowNodeGraphEmptyConnectionIdError
			{
				ErrorMessage = errorMessage,
				Id = ApiObjectId,
			};
		}

		protected override MediaOpsErrorData CreateEmptyNodeIdError(string errorMessage)
		{
			return new WorkflowNodeGraphEmptyNodeIdError
			{
				ErrorMessage = errorMessage,
				Id = ApiObjectId,
			};
		}

		protected override MediaOpsErrorData CreateNodeAliasError(string nodeId, string alias, string errorMessage)
		{
			return new WorkflowNodeGraphInvalidNodeAliasError
			{
				ErrorMessage = errorMessage,
				Alias = alias,
				NodeId = nodeId,
				Id = ApiObjectId,
			};
		}

		protected override MediaOpsErrorData CreateResourceNodeError(string nodeId, Guid resourceId, Guid resourcePoolId, string errorMessage)
		{
			return new WorkflowNodeGraphInvalidResourceNodeError
			{
				ErrorMessage = errorMessage,
				ResourcePoolId = resourcePoolId,
				ResourceId = resourceId,
				NodeId = nodeId,
				Id = ApiObjectId,
			};
		}

		protected override MediaOpsErrorData CreateResourcePoolNodeError(string nodeId, Guid resourcePoolId, string errorMessage)
		{
			return new WorkflowNodeGraphInvalidResourcePoolNodeError
			{
				ErrorMessage = errorMessage,
				ResourcePoolId = resourcePoolId,
				NodeId = nodeId,
				Id = ApiObjectId,
			};
		}
	}
}
