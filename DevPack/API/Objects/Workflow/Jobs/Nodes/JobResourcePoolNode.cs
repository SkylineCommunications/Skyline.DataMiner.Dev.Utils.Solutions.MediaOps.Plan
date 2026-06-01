namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a job node associated with a resource pool.
	/// </summary>
	public class JobResourcePoolNode : JobNode, IResourcePoolNode
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JobResourcePoolNode"/> class with a resource pool.
		/// </summary>
		/// <param name="resourcePool">The resource pool associated with this node.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePool"/> is null.</exception>
		public JobResourcePoolNode(ResourcePool resourcePool)
			: this (resourcePool?.Id ?? throw new ArgumentNullException(nameof(resourcePool)))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JobResourcePoolNode"/> class with a resource pool identifier.
		/// </summary>
		/// <param name="resourcePoolId">The unique identifier of the resource pool.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="resourcePoolId"/> is <see cref="Guid.Empty"/>.</exception>
		public JobResourcePoolNode(Guid resourcePoolId) : base()
		{
			if (resourcePoolId == Guid.Empty)
			{
				throw new ArgumentException(nameof(resourcePoolId));
			}

			ResourcePoolId = resourcePoolId;
		}

		internal JobResourcePoolNode(MediaOpsPlanApi planApi, StorageWorkflow.NodesSection section) : base(planApi, section)
		{
			ParseSection(section);
			InitTracking();
		}

		/// <inheritdoc/>
		public Guid ResourcePoolId { get; private set; }

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
