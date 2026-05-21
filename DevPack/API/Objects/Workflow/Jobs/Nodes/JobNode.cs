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
		/// Parses properties from the specified storage section.
		/// </summary>
		/// <param name="section">The storage workflow nodes section to parse.</param>
		private void ParseSection(StorageWorkflow.NodesSection section)
		{
			Start = section.NodeStartTime.Value;
			End = section.NodeEndTime.Value;
			CoreReservationId = section.ReservationId;

			ResourceSelectionState = EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Resourceselectstate, ResourceSelectionState>(section.ResourceSelectState.Value);
			NodeConfigurationStatus = EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Nodeconfigurationstatus, NodeConfigurationStatus>(section.NodeConfigurationStatus.Value);
		}
	}
}
