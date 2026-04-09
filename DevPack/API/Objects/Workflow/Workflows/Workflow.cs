namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

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
		public Workflow(Guid jobId) : base(jobId)
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
				hash = (hash * 23) + (OrchestrationSettings != null ? OrchestrationSettings.GetHashCode() : 0);

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
				   OrchestrationSettings == other.OrchestrationSettings;
		}

		internal StorageWorkflow.WorkflowsInstance GetInstanceWithChanges()
		{
			if (updatedInstance == null)
			{
				updatedInstance = IsNew ? new StorageWorkflow.WorkflowsInstance(Id) : originalInstance.Clone();
			}

			updatedInstance.WorkflowInfo.WorkflowName = Name;

			updatedInstance.WorkflowExecution.WorkflowConfiguration = OrchestrationSettings.Id;

			return updatedInstance;
		}

		private void ParseInstance(MediaOpsPlanApi planApi, StorageWorkflow.WorkflowsInstance instance)
		{
			this.originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

			Name = instance.WorkflowInfo.WorkflowName;

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
