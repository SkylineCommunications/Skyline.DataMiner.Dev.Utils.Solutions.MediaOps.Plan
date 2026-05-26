namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Base class for nodes within jobs.
	/// </summary>
	public abstract class JobNode : NodeBase
	{
		private protected JobNode() : base()
		{
		}

		private protected JobNode(MediaOpsPlanApi planApi, StorageWorkflow.NodesSection section) : base(planApi, section)
		{
			ParseSection(section);
		}

		/// <summary>
		/// Gets the start time of the node.
		/// </summary>
		public DateTimeOffset Start { get; private set; }

		/// <summary>
		/// Gets the end time of the node.
		/// </summary>
		public DateTimeOffset End { get; private set; }

		/// <summary>
		/// Gets the current selection state of the resource.
		/// </summary>
		public ResourceSelectionState ResourceSelectionState { get; private set; }

		/// <summary>
		/// Gets the current configuration status of the node.
		/// </summary>
		public NodeConfigurationStatus NodeConfigurationStatus { get; private set; }

		/// <summary>
		/// Gets the internal unique identifier for the core reservation associated with this job node.
		/// </summary>
		internal Guid CoreReservationId { get; private set; }

		/// <summary>
		/// Determines whether this node represents a resource and, if so, returns it as a <see cref="JobResourceNode"/>.
		/// </summary>
		/// <param name="resourceNode">When this method returns, contains the current node as a <see cref="JobResourceNode"/> when it represents a resource; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this node represents a resource; otherwise, <c>false</c>.</returns>
		public bool IsResourceNode(out JobResourceNode resourceNode)
		{
			resourceNode = this as JobResourceNode;
			return resourceNode != null;
		}

		/// <summary>
		/// Determines whether this node represents a resource pool and, if so, returns it as a <see cref="JobResourcePoolNode"/>.
		/// </summary>
		/// <param name="resourcePoolNode">When this method returns, contains the current node as a <see cref="JobResourcePoolNode"/> when it represents a resource pool; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this node represents a resource pool; otherwise, <c>false</c>.</returns>
		public bool IsResourcePoolNode(out JobResourcePoolNode resourcePoolNode)
		{
			resourcePoolNode = this as JobResourcePoolNode;
			return resourcePoolNode != null;
		}

		/// <summary>
		/// Parses properties from the specified storage section.
		/// </summary>
		/// <param name="section">The storage workflow nodes section to parse.</param>
		private void ParseSection(StorageWorkflow.NodesSection section)
		{
			Start = section.NodeStartTime.Value;
			End = section.NodeEndTime.Value;
			CoreReservationId = section.ReservationId;

			ResourceSelectionState = section.ResourceSelectState.HasValue
				? EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Resourceselectstate, ResourceSelectionState>(section.ResourceSelectState.Value)
				: ResourceSelectionState.Unknown;
			NodeConfigurationStatus = section.NodeConfigurationStatus.HasValue
				? EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Nodeconfigurationstatus, NodeConfigurationStatus>(section.NodeConfigurationStatus.Value)
				: NodeConfigurationStatus.Unknown;
		}
	}
}
