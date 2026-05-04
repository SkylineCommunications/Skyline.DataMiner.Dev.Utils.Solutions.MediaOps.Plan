namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a workflow in MediaOps Plan.
	/// </summary>
	public class Workflow : ApiObject
	{
		private StorageWorkflow.WorkflowsInstance originalInstance;
		private StorageWorkflow.WorkflowsInstance updatedInstance;

		/// <summary>
		/// Initializes a new instance of the <see cref="Workflow"/> class.
		/// </summary>
		public Workflow() : base()
		{
			IsNew = true;

			OrchestrationSettings = new WorkflowOrchestrationSettings();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Workflow"/> class with a specific workflow ID.
		/// </summary>
		/// <param name="workflowId">The unique identifier of the workflow.</param>
		public Workflow(Guid workflowId) : base(workflowId)
		{
			IsNew = true;
			HasUserDefinedId = true;

			OrchestrationSettings = new WorkflowOrchestrationSettings();
		}

		internal Workflow(MediaOpsPlanApi planApi, StorageWorkflow.WorkflowsInstance instance) : base(instance.ID.Id)
		{
			ParseInstance(planApi, instance);
			InitTracking();
		}

		/// <summary>
		/// Gets or sets the name of the workflow.
		/// </summary>
		public override string Name { get; set; }

		/// <summary>
		/// Gets or sets the description of the workflow.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Gets or sets the priority of the workflow.
		/// </summary>
		public WorkflowPriority Priority { get; set; } = WorkflowPriority.Normal;

		/// <summary>
		/// Gets or sets a value indicating whether the workflow is a favorite.
		/// </summary>
		public bool IsFavorite { get; set; }

		/// <summary>
		/// Gets or sets the pre-roll of the workflow.
		/// </summary>
		public TimeSpan PreRoll { get; set; }

		/// <summary>
		/// Gets or sets the post-roll of the workflow.
		/// </summary>
		public TimeSpan PostRoll { get; set; }

		/// <summary>
		/// Gets or sets the notes of the workflow.
		/// </summary>
		public string Notes { get; set; }

		/// <summary>
		/// Gets information about who has locked the workflow for editing. If the workflow is not locked, this property will be null or empty.
		/// </summary>
		public string LockedBy { get; private set; }

		/// <summary>
		/// Gets the state of the workflow.
		/// </summary>
		public WorkflowState State { get; private set; }

		/// <summary>
		/// Gets the orchestration settings assigned to this workflow.
		/// </summary>
		public OrchestrationSettings OrchestrationSettings { get; set; }

		internal StorageWorkflow.WorkflowsInstance OriginalInstance => originalInstance;

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + Id.GetHashCode();
				hash = (hash * 23) + (Name != null ? Name.GetHashCode() : 0);
				hash = (hash * 23) + (Description != null ? Description.GetHashCode() : 0);
				hash = (hash * 23) + Priority.GetHashCode();
				hash = (hash * 23) + IsFavorite.GetHashCode();
				hash = (hash * 23) + PreRoll.GetHashCode();
				hash = (hash * 23) + PostRoll.GetHashCode();
				hash = (hash * 23) + (Notes != null ? Notes.GetHashCode() : 0);
				hash = (hash * 23) + (LockedBy != null ? LockedBy.GetHashCode() : 0);
				hash = (hash * 23) + (OrchestrationSettings != null ? OrchestrationSettings.GetHashCode() : 0);
				hash = (hash * 23) + State.GetHashCode();

				return hash;
			}
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current workflow instance.
		/// </summary>
		/// <param name="obj">The object to compare with the current workflow instance.</param>
		/// <returns>true if the specified object is a workflow and has the same values for all properties as the current
		/// instance; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			if (obj is not Workflow other)
			{
				return false;
			}

			return Id == other.Id &&
				   Name == other.Name &&
				   Description == other.Description &&
				   Priority == other.Priority &&
				   IsFavorite == other.IsFavorite &&
				   PreRoll == other.PreRoll &&
				   PostRoll == other.PostRoll &&
				   Notes == other.Notes &&
				   LockedBy == other.LockedBy &&
				   OrchestrationSettings == other.OrchestrationSettings &&
				   State == other.State;
		}

		internal StorageWorkflow.WorkflowsInstance GetInstanceWithChanges()
		{
			if (updatedInstance == null)
			{
				updatedInstance = IsNew ? new StorageWorkflow.WorkflowsInstance(Id) : originalInstance.Clone();
			}

			updatedInstance.WorkflowInfo.WorkflowName = Name;
			updatedInstance.WorkflowInfo.WorkflowDescription = Description;
			updatedInstance.WorkflowInfo.Favorite = IsFavorite;
			updatedInstance.WorkflowInfo.Preroll = PreRoll != TimeSpan.Zero ? PreRoll: null;
			updatedInstance.WorkflowInfo.Postroll = PostRoll != TimeSpan.Zero ? PostRoll : null;
			updatedInstance.WorkflowInfo.WorkflowNotes = Notes;

			updatedInstance.WorkflowExecution.WorkflowConfiguration = OrchestrationSettings.Id;

			updatedInstance.WorkflowInfo.Priority = EnumExtensions.MapEnum<WorkflowPriority, StorageWorkflow.SlcWorkflowIds.Enums.Priority>(Priority);

			return updatedInstance;
		}

		private void ParseInstance(MediaOpsPlanApi planApi, StorageWorkflow.WorkflowsInstance instance)
		{
			originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

			Name = instance.WorkflowInfo.WorkflowName;
			Description = instance.WorkflowInfo.WorkflowDescription;
			IsFavorite = instance.WorkflowInfo.Favorite.HasValue ? instance.WorkflowInfo.Favorite.Value : false;
			PreRoll = instance.WorkflowInfo.Preroll.HasValue ? instance.WorkflowInfo.Preroll.Value : TimeSpan.Zero;
			PostRoll = instance.WorkflowInfo.Postroll.HasValue ? instance.WorkflowInfo.Postroll.Value : TimeSpan.Zero;
			Notes = instance.WorkflowInfo.WorkflowNotes;
			LockedBy = instance.WorkflowInfo.LockedBy;

			Priority = instance.WorkflowInfo.Priority.HasValue ? EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Priority, WorkflowPriority>(instance.WorkflowInfo.Priority.Value) : WorkflowPriority.Normal;
			State = EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Behaviors.Workflow_Behavior.StatusesEnum, WorkflowState>(instance.Status);

			if (instance.WorkflowExecution.WorkflowConfiguration == null || instance.WorkflowExecution.WorkflowConfiguration == Guid.Empty)
			{
				OrchestrationSettings = new WorkflowOrchestrationSettings();
			}
			else
			{
				var domConfiguration = planApi.DomHelpers.SlcWorkflowHelper.GetConfigurations([instance.WorkflowExecution.WorkflowConfiguration.Value]).FirstOrDefault();
				if (domConfiguration != null)
				{
					OrchestrationSettings = new WorkflowOrchestrationSettings(planApi, domConfiguration);
				}
				else
				{
					OrchestrationSettings = new WorkflowOrchestrationSettings();
				}
			}
		}
	}
}
