namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Base class for nodes within recurring jobs.
	/// </summary>
	public abstract class RecurringJobNode : NodeBase
	{
		private protected RecurringJobNode() : base()
		{
		}

		private protected RecurringJobNode(MediaOpsPlanApi planApi, StorageWorkflow.NodesSection section) : base(planApi, section)
		{
			ParseSection(section);
		}

		/// <summary>
		/// Gets the current configuration status of the node.
		/// </summary>
		/// <remarks>
		/// This value does not depict the actual state of the node: it is only recalculated and stored when the recurring job is created or updated.
		/// </remarks>
		public ConfigurationState ConfigurationState { get; internal set; }

		internal sealed override void ApplyChanges(StorageWorkflow.NodesSection section)
		{
			// Recurring jobs do not have a runtime start/end or a core reservation node ID, so besides the
			// configuration state no additional changes are applied beyond what NodeBase writes. Subclasses still
			// contribute their own storage fields (e.g. NodeType, ReferenceId, ParentReferenceId) through ApplyRecurringJobNodeChanges.
			section.NodeConfigurationStatus = EnumExtensions.TryMapEnum<ConfigurationState, StorageWorkflow.SlcWorkflowIds.Enums.Nodeconfigurationstatus>(ConfigurationState, out var storedConfigurationState)
				? storedConfigurationState
				: (StorageWorkflow.SlcWorkflowIds.Enums.Nodeconfigurationstatus?)null;

			ApplyRecurringJobNodeChanges(section);
		}

		/// <summary>
		/// Applies subclass-specific changes from this recurring job node to the specified storage section.
		/// </summary>
		/// <param name="section">The storage workflow nodes section to apply changes to.</param>
		internal abstract void ApplyRecurringJobNodeChanges(StorageWorkflow.NodesSection section);

		/// <summary>
		/// Determines whether this node represents a resource and, if so, returns it as a <see cref="RecurringJobResourceNode"/>.
		/// </summary>
		/// <param name="resourceNode">When this method returns, contains the current node as a <see cref="RecurringJobResourceNode"/> when it represents a resource; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this node represents a resource; otherwise, <c>false</c>.</returns>
		public bool IsResourceNode(out RecurringJobResourceNode resourceNode)
		{
			resourceNode = this as RecurringJobResourceNode;
			return resourceNode != null;
		}

		/// <summary>
		/// Determines whether this node represents a resource pool and, if so, returns it as a <see cref="RecurringJobResourcePoolNode"/>.
		/// </summary>
		/// <param name="resourcePoolNode">When this method returns, contains the current node as a <see cref="RecurringJobResourcePoolNode"/> when it represents a resource pool; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this node represents a resource pool; otherwise, <c>false</c>.</returns>
		public bool IsResourcePoolNode(out RecurringJobResourcePoolNode resourcePoolNode)
		{
			resourcePoolNode = this as RecurringJobResourcePoolNode;
			return resourcePoolNode != null;
		}

		/// <summary>
		/// Parses properties from the specified storage section.
		/// </summary>
		/// <param name="section">The storage workflow nodes section to parse.</param>
		private void ParseSection(StorageWorkflow.NodesSection section)
		{
			ConfigurationState = section.NodeConfigurationStatus.HasValue
				? EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Nodeconfigurationstatus, ConfigurationState>(section.NodeConfigurationStatus.Value)
				: ConfigurationState.Unknown;
		}
	}
}
