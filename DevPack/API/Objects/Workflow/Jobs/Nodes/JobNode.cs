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
		public DateTimeOffset Start { get; internal set; }

		/// <summary>
		/// Gets the end time of the node.
		/// </summary>
		public DateTimeOffset End { get; internal set; }

		/// <summary>
		/// Gets the current selection state of the resource.
		/// </summary>
		public ResourceSelectionState ResourceSelectionState { get; private set; }

		/// <summary>
		/// Gets the current configuration status of the node.
		/// </summary>
		public NodeConfigurationStatus NodeConfigurationStatus { get; private set; }

		internal int? CoreReservationNodeId { get; private set; }

		internal sealed override void ApplyChanges(StorageWorkflow.NodesSection section)
		{
			section.NodeStartTime = Start.UtcDateTime;
			section.NodeEndTime = End.UtcDateTime;

			if (CoreReservationNodeId.HasValue)
			{
				section.CoreReservationNodeID = CoreReservationNodeId.Value;
			}

			ApplyJobNodeChanges(section);
		}

		/// <summary>
		/// Applies subclass-specific changes from this job node to the specified storage section.
		/// </summary>
		/// <param name="section">The storage workflow nodes section to apply changes to.</param>
		internal abstract void ApplyJobNodeChanges(StorageWorkflow.NodesSection section);

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

		internal void SetCoreServationNodeId(int nodeId)
		{
			if (nodeId <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(nodeId), "Node ID must be a positive integer.");
			}

			if (!IsNew)
			{
				throw new InvalidOperationException("Core reservation node ID can only be set for new nodes.");
			}

			if (CoreReservationNodeId.HasValue)
			{
				throw new InvalidOperationException("Core reservation node ID has already been set.");
			}

			CoreReservationNodeId = nodeId;
		}

		/// <summary>
		/// Parses properties from the specified storage section.
		/// </summary>
		/// <param name="section">The storage workflow nodes section to parse.</param>
		private void ParseSection(StorageWorkflow.NodesSection section)
		{
			Start = section.NodeStartTime.Value;
			End = section.NodeEndTime.Value;
			CoreReservationNodeId = section.CoreReservationNodeID.HasValue ? (int)section.CoreReservationNodeID.Value : null;

			ResourceSelectionState = section.ResourceSelectState.HasValue
				? EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Resourceselectstate, ResourceSelectionState>(section.ResourceSelectState.Value)
				: ResourceSelectionState.Unknown;
			NodeConfigurationStatus = section.NodeConfigurationStatus.HasValue
				? EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Nodeconfigurationstatus, NodeConfigurationStatus>(section.NodeConfigurationStatus.Value)
				: NodeConfigurationStatus.Unknown;
		}
	}
}
