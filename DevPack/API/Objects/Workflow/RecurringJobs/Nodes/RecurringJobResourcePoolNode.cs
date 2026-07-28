namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a recurring job node associated with a resource pool.
	/// </summary>
	public class RecurringJobResourcePoolNode : RecurringJobNode, IResourcePoolNode
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RecurringJobResourcePoolNode"/> class.
		/// </summary>
		public RecurringJobResourcePoolNode() : base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RecurringJobResourcePoolNode"/> class with a resource pool.
		/// </summary>
		/// <param name="resourcePool">The resource pool associated with this node.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePool"/> is null.</exception>
		public RecurringJobResourcePoolNode(ResourcePool resourcePool)
			: this(resourcePool?.Id ?? throw new ArgumentNullException(nameof(resourcePool)))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RecurringJobResourcePoolNode"/> class with a resource pool identifier.
		/// </summary>
		/// <param name="resourcePoolId">The unique identifier of the resource pool.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="resourcePoolId"/> is <see cref="Guid.Empty"/>.</exception>
		public RecurringJobResourcePoolNode(Guid resourcePoolId) : base()
		{
			if (resourcePoolId == Guid.Empty)
			{
				throw new ArgumentException(nameof(resourcePoolId));
			}

			ResourcePoolId = resourcePoolId;
		}

		internal RecurringJobResourcePoolNode(MediaOpsPlanApi planApi, StorageWorkflow.NodesSection section) : base(planApi, section)
		{
			ParseSection(section);
			InitTracking();
		}

		/// <inheritdoc/>
		public Guid ResourcePoolId { get; private set; }

		internal override void ApplyRecurringJobNodeChanges(StorageWorkflow.NodesSection section)
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
