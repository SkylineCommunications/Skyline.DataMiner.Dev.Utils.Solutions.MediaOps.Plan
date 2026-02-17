namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

    /// <summary>
    /// Represents a job in MediaOps Plan.
    /// </summary>
    public class Job : ApiObject
    {
        private StorageWorkflow.JobsInstance originalInstance;

        /// <summary>
        /// Initializes a new instance of the <see cref="Job"/> class.
        /// </summary>
        public Job() : base()
        {
            IsNew = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Job"/> class with a specific job ID.
        /// </summary>
        public Job(Guid jobId) : base(jobId)
        {
            IsNew = true;
            HasUserDefinedId = true;
        }

        internal Job(StorageWorkflow.JobsInstance instance) : base(instance.ID.Id)
        {
            ParseInstance(instance);
            InitTracking();
        }

        /// <summary>
        /// Gets or sets the name of the job.
        /// </summary>
        public override string Name { get; set; }

		/// <summary>
		/// Gets or sets the description of the job.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Gets or sets the priority of the job.
		/// </summary>
		public JobPriority Priority { get; set; } = JobPriority.Normal;

		/// <summary>
		/// Gets or sets the notes or additional information.
		/// </summary>
		public string Notes { get; set; }

		/// <summary>
		/// Gets or sets the workflow ID associated with the job.
		/// </summary>
		public Guid WorkflowId { get; set; }

		internal StorageWorkflow.JobsInstance OriginalInstance => originalInstance;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + ID.GetHashCode();
                hash = (hash * 23) + (Name != null ? Name.GetHashCode() : 0);

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

            return ID == other.ID &&
                   Name == other.Name;
        }

        private void ParseInstance(StorageWorkflow.JobsInstance instance)
        {
            this.originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

            Name = instance.JobInfo.JobName;
			Description = instance.JobInfo.JobDescription;
			Notes = instance.JobInfo.JobNotes;
			WorkflowId = instance.JobInfo.Workflow ?? Guid.Empty;

			Priority = instance.JobInfo.JobPriority.HasValue
				? EnumExtensions.MapEnum<StorageWorkflow.SlcWorkflowIds.Enums.Jobpriority, JobPriority>(instance.JobInfo.JobPriority.Value)
				: JobPriority.Normal;
		}
    }
}
