namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a workflow node associated with a resource within a resource pool.
	/// </summary>
	public class WorkflowResourceNode : WorkflowNode
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WorkflowResourceNode"/> class with a resource pool and resource.
		/// </summary>
		/// <param name="resourcePool">The resource pool associated with this node.</param>
		/// <param name="resource">The resource associated with this node.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePool"/> or <paramref name="resource"/> is null.</exception>
		public WorkflowResourceNode(ResourcePool resourcePool, Resource resource)
			: this(resourcePool?.Id ?? throw new ArgumentNullException(nameof(resourcePool)), resource?.Id ?? throw new ArgumentNullException(nameof(resource)))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WorkflowResourceNode"/> class with a resource pool identifier and resource.
		/// </summary>
		/// <param name="resourcePoolId">The unique identifier of the resource pool.</param>
		/// <param name="resource">The resource associated with this node.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="resource"/> is null.</exception>
		public WorkflowResourceNode(Guid resourcePoolId, Resource resource)
			: this(resourcePoolId, resource?.Id ?? throw new ArgumentNullException(nameof(resource)))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WorkflowResourceNode"/> class with a resource pool and resource identifier.
		/// </summary>
		/// <param name="resourcePool">The resource pool associated with this node.</param>
		/// <param name="resourceId">The unique identifier of the resource.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePool"/> is null.</exception>
		public WorkflowResourceNode(ResourcePool resourcePool, Guid resourceId)
			: this(resourcePool?.Id ?? throw new ArgumentNullException(nameof(resourcePool)), resourceId)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WorkflowResourceNode"/> class with resource pool and resource identifiers.
		/// </summary>
		/// <param name="resourcePoolId">The unique identifier of the resource pool.</param>
		/// <param name="resourceId">The unique identifier of the resource.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="resourcePoolId"/> or <paramref name="resourceId"/> is <see cref="Guid.Empty"/>.</exception>
		public WorkflowResourceNode(Guid resourcePoolId, Guid resourceId) : base()
		{
			if (resourcePoolId == Guid.Empty)
			{
				throw new ArgumentException(nameof(resourcePoolId));
			}

			if (resourceId == Guid.Empty)
			{
				throw new ArgumentException(nameof(resourceId));
			}

			ResourcePoolId = resourcePoolId;
			ResourceId = resourceId;
		}

		internal WorkflowResourceNode(StorageWorkflow.NodesSection section) : base(section)
		{
			ParseSection(section);
			InitTracking();
		}

		/// <summary>
		/// Gets the unique identifier of the resource pool associated with this node.
		/// </summary>
		public Guid ResourcePoolId { get; private set; }

		/// <summary>
		/// Gets the unique identifier of the resource associated with this node.
		/// </summary>
		public Guid ResourceId { get; private set; }

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not WorkflowResourceNode other)
			{
				return false;
			}

			return base.Equals(obj)
				&& ResourcePoolId == other.ResourcePoolId
				&& ResourceId == other.ResourceId;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = base.GetHashCode();
				hash = (hash * 23) + ResourcePoolId.GetHashCode();
				hash = (hash * 23) + ResourceId.GetHashCode();

				return hash;
			}
		}

		/// <inheritdoc/>
		internal override void ApplyChanges(StorageWorkflow.NodesSection section)
		{
			section.NodeType = StorageWorkflow.SlcWorkflowIds.Enums.Nodetype.Resource;
			section.ReferenceId = ResourceId;
			section.ParentReferenceId = ResourcePoolId;
		}

		private void ParseSection(StorageWorkflow.NodesSection section)
		{
			ResourcePoolId = section.ParentReferenceId;
			ResourceId = section.ReferenceId;
		}
	}
}
