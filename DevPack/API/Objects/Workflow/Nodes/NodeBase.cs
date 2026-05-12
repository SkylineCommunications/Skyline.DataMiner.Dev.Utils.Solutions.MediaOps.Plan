namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Net.ServiceManager.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Base class for all node implementations in workflows, jobs, and recurring jobs.
	/// This class represents common node properties used across different contexts.
	/// </summary>
	public abstract class NodeBase : TrackableObject
	{
		private StorageWorkflow.NodesSection originalSection;
		private StorageWorkflow.NodesSection updatedSection;

		private protected NodeBase() : base()
		{
			Id = Guid.NewGuid().ToString();

			IsNew = true;
		}

		private protected NodeBase(StorageWorkflow.NodesSection section)
		{
			ParseSection(section);
		}

		/// <summary>
		/// Gets the unique identifier of the node, which is assigned by the system and cannot be modified by users.
		/// </summary>
		public string Id { get; private set; }

		/// <summary>
		/// Gets or sets the alias or display name of the node.
		/// </summary>
		public string Alias { get; set; }

		/// <summary>
		/// Gets or sets the icon of the node.
		/// </summary>
		public string IconImage { get; set; }

		internal abstract void ApplyChanges(StorageWorkflow.NodesSection section);

		internal StorageWorkflow.NodesSection GetSectionWithChanges()
		{
			if (updatedSection == null)
			{
				updatedSection = IsNew
					? new StorageWorkflow.NodesSection()
					{
						NodeID = Id,
					}
					: originalSection.Clone();
			}

			originalSection.NodeAlias = Alias;
			originalSection.NodeIcon = IconImage;

			return updatedSection;
		}

		private void ParseSection(StorageWorkflow.NodesSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Id = section.NodeID;
			Alias = section.NodeAlias;
			IconImage = section.NodeIcon;
		}
	}

	public abstract class JobNode : NodeBase
	{
		private protected JobNode() : base()
		{
		}

		private protected JobNode(StorageWorkflow.NodesSection section) : base(section)
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

		internal Guid CoreReservationId { get; private set; }

		private void ParseSection(StorageWorkflow.NodesSection section)
		{
			Start = section.NodeStartTime.Value;
			End = section.NodeEndTime.Value;
			CoreReservationId = section.ReservationId;

			ResourceSelectionState = EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Resourceselectstate, ResourceSelectionState>(section.ResourceSelectState.Value);
			NodeConfigurationStatus = EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Nodeconfigurationstatus, NodeConfigurationStatus>(section.NodeConfigurationStatus.Value);
		}
	}

	public abstract class WorkflowNode : NodeBase
	{
		private protected WorkflowNode() : base()
		{
		}

		private protected WorkflowNode(StorageWorkflow.NodesSection section) : base(section)
		{
		}
	}

	public abstract class RecurringJobNode : NodeBase
	{
		private protected RecurringJobNode() : base()
		{
		}

		private protected RecurringJobNode(StorageWorkflow.NodesSection section) : base(section)
		{
		}
	}
}
