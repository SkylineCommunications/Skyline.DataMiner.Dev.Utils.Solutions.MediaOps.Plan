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

			section.CoreReservationNodeID = CoreReservationNodeId.Value;

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

		internal void SetCoreReservationNodeId(int nodeId)
		{
			if (nodeId <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(nodeId), "Node ID must be a positive integer.");
			}

			if (CoreReservationNodeId.HasValue)
			{
				throw new InvalidOperationException("Core reservation node ID has already been set.");
			}

			CoreReservationNodeId = nodeId;
		}

		/// <summary>
		/// Resolves the core reservation node ID for a node. For legacy nodes, whose node ID is a positive integer
		/// instead of a GUID, the core reservation node ID must equal that node ID, so it takes precedence. For other
		/// nodes the stored value is used, where a non-positive stored value (e.g. the default <c>0</c>) is treated as
		/// not yet assigned because valid IDs start at <c>1</c>.
		/// </summary>
		/// <param name="storedCoreReservationNodeId">The stored core reservation node ID, when present.</param>
		/// <param name="nodeId">The node ID, which for legacy nodes is a positive integer instead of a GUID.</param>
		/// <returns>The resolved core reservation node ID, or <c>null</c> when none could be determined.</returns>
		internal static int? ResolveCoreReservationNodeId(long? storedCoreReservationNodeId, string nodeId)
		{
			// Legacy nodes used a positive integer NodeID instead of a GUID. The core reservation node ID must match
			// that NodeID, so it takes precedence and is reused directly as the authoritative value.
			if (int.TryParse(nodeId, out var legacyNodeId) && legacyNodeId > 0)
			{
				return legacyNodeId;
			}

			// Valid core reservation node IDs start at 1; treat a stored 0 (or any non-positive value) as "not assigned yet".
			if (storedCoreReservationNodeId.HasValue && storedCoreReservationNodeId.Value > 0)
			{
				return (int)storedCoreReservationNodeId.Value;
			}

			return null;
		}

		/// <summary>
		/// Parses properties from the specified storage section.
		/// </summary>
		/// <param name="section">The storage workflow nodes section to parse.</param>
		private void ParseSection(StorageWorkflow.NodesSection section)
		{
			Start = section.NodeStartTime.Value;
			End = section.NodeEndTime.Value;
			CoreReservationNodeId = ResolveCoreReservationNodeId(section.CoreReservationNodeID, section.NodeID);

			ResourceSelectionState = section.ResourceSelectState.HasValue
				? EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Resourceselectstate, ResourceSelectionState>(section.ResourceSelectState.Value)
				: ResourceSelectionState.Unknown;
			NodeConfigurationStatus = section.NodeConfigurationStatus.HasValue
				? EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Nodeconfigurationstatus, NodeConfigurationStatus>(section.NodeConfigurationStatus.Value)
				: NodeConfigurationStatus.Unknown;
		}
	}
}
