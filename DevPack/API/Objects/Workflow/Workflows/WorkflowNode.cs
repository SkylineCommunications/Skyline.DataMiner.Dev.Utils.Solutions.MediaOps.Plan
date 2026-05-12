namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a node inside a workflow graph.
	/// </summary>
	public class WorkflowNode
	{
		/// <summary>
		/// Gets or sets the unique node ID in the workflow graph.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Gets or sets the node alias.
		/// </summary>
		public string Alias { get; set; }

		/// <summary>
		/// Gets or sets the node type.
		/// </summary>
		public WorkflowNodeType? Type { get; set; }

		/// <summary>
		/// Gets or sets the reference ID.
		/// </summary>
		public Guid ReferenceId { get; set; }

		/// <summary>
		/// Gets or sets the parent reference ID.
		/// </summary>
		public Guid ParentReferenceId { get; set; }

		/// <summary>
		/// Gets or sets the node icon.
		/// </summary>
		public string Icon { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether automatic configuration is enabled.
		/// </summary>
		public bool? AutomaticConfiguration { get; set; }

		/// <summary>
		/// Gets or sets the serialized configuration parameters.
		/// </summary>
		public string ConfigurationParameters { get; set; }

		/// <summary>
		/// Gets or sets the ad-hoc control script.
		/// </summary>
		public string AdHocControlScript { get; set; }

		/// <summary>
		/// Gets or sets the node configuration execution order.
		/// </summary>
		public long? NodeConfigurationExecutionOrder { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this node should be reserved.
		/// </summary>
		public bool? ReserveNode { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this node is hidden.
		/// </summary>
		public bool? Hidden { get; set; }

		/// <summary>
		/// Gets or sets the planned node start time.
		/// </summary>
		public DateTime? StartTime { get; set; }

		/// <summary>
		/// Gets or sets the planned node end time.
		/// </summary>
		public DateTime? EndTime { get; set; }

		/// <summary>
		/// Gets or sets linked booking IDs.
		/// </summary>
		public string LinkedBookingIds { get; set; }

		/// <summary>
		/// Gets or sets the resource selection mode.
		/// </summary>
		public WorkflowNodeResourceSelectMode? ResourceSelectMode { get; set; }

		/// <summary>
		/// Gets or sets the resource selection state.
		/// </summary>
		public WorkflowNodeResourceSelectState? ResourceSelectState { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this node is billable.
		/// </summary>
		public bool? Billable { get; set; }

		/// <summary>
		/// Gets or sets the node configuration reference.
		/// </summary>
		public Guid? ConfigurationId { get; set; }

		/// <summary>
		/// Gets or sets the node configuration status.
		/// </summary>
		public WorkflowNodeConfigurationStatus? ConfigurationStatus { get; set; }

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + (Id?.GetHashCode() ?? 0);
				hash = (hash * 23) + (Alias?.GetHashCode() ?? 0);
				hash = (hash * 23) + Type.GetHashCode();
				hash = (hash * 23) + ReferenceId.GetHashCode();
				hash = (hash * 23) + ParentReferenceId.GetHashCode();
				hash = (hash * 23) + (Icon?.GetHashCode() ?? 0);
				hash = (hash * 23) + AutomaticConfiguration.GetHashCode();
				hash = (hash * 23) + (ConfigurationParameters?.GetHashCode() ?? 0);
				hash = (hash * 23) + (AdHocControlScript?.GetHashCode() ?? 0);
				hash = (hash * 23) + NodeConfigurationExecutionOrder.GetHashCode();
				hash = (hash * 23) + ReserveNode.GetHashCode();
				hash = (hash * 23) + Hidden.GetHashCode();
				hash = (hash * 23) + StartTime.GetHashCode();
				hash = (hash * 23) + EndTime.GetHashCode();
				hash = (hash * 23) + (LinkedBookingIds?.GetHashCode() ?? 0);
				hash = (hash * 23) + ResourceSelectMode.GetHashCode();
				hash = (hash * 23) + ResourceSelectState.GetHashCode();
				hash = (hash * 23) + Billable.GetHashCode();
				hash = (hash * 23) + ConfigurationId.GetHashCode();
				hash = (hash * 23) + ConfigurationStatus.GetHashCode();
				return hash;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not WorkflowNode other)
			{
				return false;
			}

			return Id == other.Id
				&& Alias == other.Alias
				&& Type == other.Type
				&& ReferenceId == other.ReferenceId
				&& ParentReferenceId == other.ParentReferenceId
				&& Icon == other.Icon
				&& AutomaticConfiguration == other.AutomaticConfiguration
				&& ConfigurationParameters == other.ConfigurationParameters
				&& AdHocControlScript == other.AdHocControlScript
				&& NodeConfigurationExecutionOrder == other.NodeConfigurationExecutionOrder
				&& ReserveNode == other.ReserveNode
				&& Hidden == other.Hidden
				&& StartTime == other.StartTime
				&& EndTime == other.EndTime
				&& LinkedBookingIds == other.LinkedBookingIds
				&& ResourceSelectMode == other.ResourceSelectMode
				&& ResourceSelectState == other.ResourceSelectState
				&& Billable == other.Billable
				&& ConfigurationId == other.ConfigurationId
				&& ConfigurationStatus == other.ConfigurationStatus;
		}

		internal static WorkflowNode FromStorageSection(StorageWorkflow.NodesSection section)
		{
			if (section == null)
			{
				return null;
			}

			return new WorkflowNode
			{
				Id = section.NodeID,
				Alias = section.NodeAlias,
				Type = section.NodeType.HasValue ? (WorkflowNodeType?)(int)section.NodeType.Value : null,
				ReferenceId = section.ReferenceId,
				ParentReferenceId = section.ParentReferenceId,
				Icon = section.NodeIcon,
				AutomaticConfiguration = section.AutomaticConfiguration,
				ConfigurationParameters = section.ConfigurationParameters,
				AdHocControlScript = section.AdHocControlScript,
				NodeConfigurationExecutionOrder = section.NodeConfigurationExecutionOrder,
				ReserveNode = section.ReserveNode,
				Hidden = section.Hidden,
				StartTime = section.NodeStartTime,
				EndTime = section.NodeEndTime,
				LinkedBookingIds = section.LinkedBookingIds,
				ResourceSelectMode = section.ResourceSelectMode.HasValue ? (WorkflowNodeResourceSelectMode?)(int)section.ResourceSelectMode.Value : null,
				ResourceSelectState = section.ResourceSelectState.HasValue ? (WorkflowNodeResourceSelectState?)(int)section.ResourceSelectState.Value : null,
				Billable = section.Billable,
				ConfigurationId = section.NodeConfiguration,
				ConfigurationStatus = section.NodeConfigurationStatus.HasValue ? (WorkflowNodeConfigurationStatus?)(int)section.NodeConfigurationStatus.Value : null,
			};
		}

		internal StorageWorkflow.NodesSection ToStorageSection()
		{
			var section = new StorageWorkflow.NodesSection
			{
				NodeID = Id,
				NodeAlias = Alias,
				NodeType = Type.HasValue ? (StorageWorkflow.SlcWorkflowIds.Enums.Nodetype?)(int)Type.Value : null,
				NodeIcon = Icon,
				AutomaticConfiguration = AutomaticConfiguration,
				ConfigurationParameters = ConfigurationParameters,
				AdHocControlScript = AdHocControlScript,
				NodeConfigurationExecutionOrder = NodeConfigurationExecutionOrder,
				ReserveNode = ReserveNode,
				Hidden = Hidden,
				NodeStartTime = StartTime,
				NodeEndTime = EndTime,
				LinkedBookingIds = LinkedBookingIds,
				ResourceSelectMode = ResourceSelectMode.HasValue ? (StorageWorkflow.SlcWorkflowIds.Enums.Resourceselectmode?)(int)ResourceSelectMode.Value : null,
				ResourceSelectState = ResourceSelectState.HasValue ? (StorageWorkflow.SlcWorkflowIds.Enums.Resourceselectstate?)(int)ResourceSelectState.Value : null,
				Billable = Billable,
				NodeConfiguration = ConfigurationId,
				NodeConfigurationStatus = ConfigurationStatus.HasValue ? (StorageWorkflow.SlcWorkflowIds.Enums.Nodeconfigurationstatus?)(int)ConfigurationStatus.Value : null,
			};

			section.ReferenceId = ReferenceId;
			section.ParentReferenceId = ParentReferenceId;

			return section;
		}
	}

	/// <summary>
	/// Defines workflow node types.
	/// </summary>
	public enum WorkflowNodeType
	{
		Source = 0,
		SourceType = 1,
		Resource = 2,
		ResourcePool = 3,
		Destination = 4,
		DestinationType = 5,
		SourceInstance = 6,
		DestinationInstance = 7,
	}

	/// <summary>
	/// Defines node resource selection modes.
	/// </summary>
	public enum WorkflowNodeResourceSelectMode
	{
		Manual = 0,
		AutoSelectAtBooking = 1,
		AutoSelectAtRuntime = 2,
	}

	/// <summary>
	/// Defines node resource selection states.
	/// </summary>
	public enum WorkflowNodeResourceSelectState
	{
		Selected = 0,
		PendingAutoSelection = 1,
		PendingManualSelection = 2,
		RequestedResource = 3,
		Error = 4,
	}

	/// <summary>
	/// Defines node configuration statuses.
	/// </summary>
	public enum WorkflowNodeConfigurationStatus
	{
		NoValuesNeeded = 0,
		MandatoryValuesMissing = 1,
		NonMandatoryValuesMissing = 2,
		AllValuesProvided = 3,
		NoParametersDefined = 4,
	}
}
