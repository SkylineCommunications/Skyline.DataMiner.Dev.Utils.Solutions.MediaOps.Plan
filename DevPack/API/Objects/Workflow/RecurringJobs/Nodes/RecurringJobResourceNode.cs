namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a recurring job node associated with a resource within a resource pool.
	/// </summary>
	public class RecurringJobResourceNode : RecurringJobNode, IResourceNode
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RecurringJobResourceNode"/> class.
		/// </summary>
		public RecurringJobResourceNode() : base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RecurringJobResourceNode"/> class with a resource pool and resource.
		/// </summary>
		/// <param name="resourcePool">The resource pool associated with this node.</param>
		/// <param name="resource">The resource associated with this node.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePool"/> or <paramref name="resource"/> is null.</exception>
		public RecurringJobResourceNode(ResourcePool resourcePool, Resource resource)
			: this(resourcePool?.Id ?? throw new ArgumentNullException(nameof(resourcePool)), resource?.Id ?? throw new ArgumentNullException(nameof(resource)))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RecurringJobResourceNode"/> class with resource pool and resource identifiers.
		/// </summary>
		/// <param name="resourcePoolId">The unique identifier of the resource pool.</param>
		/// <param name="resourceId">The unique identifier of the resource.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="resourcePoolId"/> or <paramref name="resourceId"/> is <see cref="Guid.Empty"/>.</exception>
		public RecurringJobResourceNode(Guid resourcePoolId, Guid resourceId) : base()
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

		internal RecurringJobResourceNode(MediaOpsPlanApi planApi, StorageWorkflow.NodesSection section) : base(planApi, section)
		{
			ParseSection(section);
			InitTracking();
		}

		/// <inheritdoc/>
		public Guid ResourcePoolId { get; private set; }

		/// <inheritdoc/>
		public Guid ResourceId { get; private set; }

		internal override void ApplyRecurringJobNodeChanges(StorageWorkflow.NodesSection section)
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
