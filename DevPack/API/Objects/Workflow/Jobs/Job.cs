namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a job in MediaOps Plan.
	/// </summary>
	public class Job : ApiObject
	{
		private StorageWorkflow.JobsInstance originalInstance;
		private StorageWorkflow.JobsInstance updatedInstance;

		/// <summary>
		/// Initializes a new instance of the <see cref="Job"/> class.
		/// </summary>
		public Job() : base()
		{
			IsNew = true;

			OrchestrationSettings = new WorkflowOrchestrationSettings();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Job"/> class with a specific job ID.
		/// </summary>
		public Job(Guid jobId) : base(jobId)
		{
			IsNew = true;
			HasUserDefinedId = true;

			OrchestrationSettings = new WorkflowOrchestrationSettings();
		}

		internal Job(MediaOpsPlanApi planApi, StorageWorkflow.JobsInstance instance) : base(instance.ID.Id)
		{
			ParseInstance(planApi, instance);
			InitTracking();
		}

		/// <summary>
		/// Gets or sets the name of the job.
		/// </summary>
		public override string Name { get; set; }

		/// <summary>
		/// Gets the auto generated key of the job, which is assigned by the system and cannot be modified by users.
		/// </summary>
		public string Key { get; internal set; }

		/// <summary>
		/// Gets or sets the description of the job.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Gets or sets the priority of the job.
		/// </summary>
		public JobPriority Priority { get; set; } = JobPriority.Normal;

		/// <summary>
		/// Gets or sets the start time of the job.
		/// </summary>
		public DateTimeOffset Start { get; set; }

		/// <summary>
		/// Gets or sets the end time of the job.
		/// </summary>
		public DateTimeOffset End { get; set; }

		/// <summary>
		/// Gets or sets the pre-roll of the job.
		/// </summary>
		public TimeSpan PreRoll { get; set; }

		/// <summary>
		/// Gets or sets the post-roll of the job.
		/// </summary>
		public TimeSpan PostRoll { get; set; }

		/// <summary>
		/// Gets or sets the notes or additional information.
		/// </summary>
		public string Notes { get; set; }

		/// <summary>
		/// Gets the workflow ID associated with the job.
		/// </summary>
		public Guid WorkflowId { get; internal set; }

		/// <summary>
		/// Gets the state of the job.
		/// </summary>
		public JobState State { get; private set; }

		/// <summary>
		/// Gets the orchestration settings assigned to this job.
		/// </summary>
		public OrchestrationSettings OrchestrationSettings { get; set; }

		internal StorageWorkflow.JobsInstance OriginalInstance => originalInstance;

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
		/// Determines whether the specified object is equal to the current job instance.
		/// </summary>
		/// <param name="obj">The object to compare with the current job instance.</param>
		/// <returns>true if the specified object is a job and has the same values for all properties as the current
		/// instance; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			if (obj is not Job other)
			{
				return false;
			}

			return Id == other.Id &&
				   Name == other.Name &&
				   OrchestrationSettings == other.OrchestrationSettings;
		}

		internal StorageWorkflow.JobsInstance GetInstanceWithChanges()
		{
			if (updatedInstance == null)
			{
				updatedInstance = IsNew ? new StorageWorkflow.JobsInstance(Id) : originalInstance.Clone();
			}

			updatedInstance.JobInfo.JobName = Name;
			updatedInstance.JobInfo.JobID = Key;
			updatedInstance.JobInfo.JobDescription = Description;
			updatedInstance.JobInfo.JobStart = Start.UtcDateTime;
			updatedInstance.JobInfo.JobEnd = End.UtcDateTime;
			updatedInstance.JobInfo.Preroll = PreRoll != TimeSpan.Zero ? Start.Add(-PreRoll).UtcDateTime : Start.UtcDateTime;
			updatedInstance.JobInfo.Postroll = PostRoll != TimeSpan.Zero ? End.Add(PostRoll).UtcDateTime : End.UtcDateTime;
			updatedInstance.JobInfo.JobNotes = Notes;
			updatedInstance.JobInfo.Workflow = WorkflowId != Guid.Empty ? WorkflowId : null;

			updatedInstance.JobExecution.JobConfiguration = OrchestrationSettings.Id;

			updatedInstance.JobInfo.JobPriority = EnumExtensions.MapEnum<JobPriority, StorageWorkflow.SlcWorkflowIds.Enums.Jobpriority>(Priority);

			return updatedInstance;
		}

		private void ParseInstance(MediaOpsPlanApi planApi, StorageWorkflow.JobsInstance instance)
		{
			this.originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

			Name = instance.JobInfo.JobName;
			Key = instance.JobInfo.JobID;
			Description = instance.JobInfo.JobDescription;
			Start = instance.JobInfo.JobStart.Value;
			End = instance.JobInfo.JobEnd.Value;
			PreRoll = instance.JobInfo.Preroll.HasValue ? (Start - instance.JobInfo.Preroll.Value) : TimeSpan.Zero;
			PostRoll = instance.JobInfo.Postroll.HasValue ? (instance.JobInfo.Postroll.Value - End) : TimeSpan.Zero;
			Notes = instance.JobInfo.JobNotes;
			WorkflowId = instance.JobInfo.Workflow ?? Guid.Empty;

			Priority = instance.JobInfo.JobPriority.HasValue
				? EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Jobpriority, JobPriority>(instance.JobInfo.JobPriority.Value)
				: JobPriority.Normal;
			State = EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Behaviors.Job_Behavior.StatusesEnum, JobState>(instance.Status);

			if (instance.JobExecution.JobConfiguration == null || instance.JobExecution.JobConfiguration == Guid.Empty)
			{
				OrchestrationSettings = new WorkflowOrchestrationSettings();
			}
			else
			{
				var domConfiguration = planApi.DomHelpers.SlcWorkflowHelper.GetConfigurations([instance.JobExecution.JobConfiguration.Value]).FirstOrDefault();
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
