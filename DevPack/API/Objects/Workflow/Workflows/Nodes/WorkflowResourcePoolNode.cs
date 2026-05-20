namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a workflow node associated with a resource pool.
	/// </summary>
	public class WorkflowResourcePoolNode : WorkflowNode
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WorkflowResourcePoolNode"/> class with a resource pool.
		/// </summary>
		/// <param name="resourcePool">The resource pool associated with this node.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePool"/> is null.</exception>
		public WorkflowResourcePoolNode(ResourcePool resourcePool)
			: this(resourcePool?.Id ?? throw new ArgumentNullException(nameof(resourcePool)))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WorkflowResourcePoolNode"/> class with a resource pool identifier.
		/// </summary>
		/// <param name="resourcePoolId">The unique identifier of the resource pool.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="resourcePoolId"/> is <see cref="Guid.Empty"/>.</exception>
		public WorkflowResourcePoolNode(Guid resourcePoolId) : base()
		{
			if (resourcePoolId == Guid.Empty)
			{
				throw new ArgumentException(nameof(resourcePoolId));
			}

			ResourcePoolId = resourcePoolId;
		}

		internal WorkflowResourcePoolNode(StorageWorkflow.NodesSection section) : base(section)
		{
			ParseSection(section);
			InitTracking();
		}

		/// <summary>
		/// Gets the unique identifier of the resource pool associated with this node.
		/// </summary>
		public Guid ResourcePoolId { get; private set; }

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not WorkflowResourceNode other)
			{
				return false;
			}

			return base.Equals(obj)
				&& ResourcePoolId == other.ResourcePoolId;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = base.GetHashCode();
				hash = (hash * 23) + ResourcePoolId.GetHashCode();

				return hash;
			}
		}

		internal override void ApplyChanges(StorageWorkflow.NodesSection section)
		{
			section.NodeType = StorageWorkflow.SlcWorkflowIds.Enums.Nodetype.ResourcePool;
			section.ReferenceId = ResourcePoolId;
		}

		private void ParseSection(StorageWorkflow.NodesSection section)
		{
			ResourcePoolId = section.ReferenceId;
		}
	}
}
