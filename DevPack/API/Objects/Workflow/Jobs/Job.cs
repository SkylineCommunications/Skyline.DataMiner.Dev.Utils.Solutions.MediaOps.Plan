namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

    /// <summary>
    /// Represents a job in MediaOps Plan.
    /// </summary>
    public class Job : ApiObject
    {
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

        internal Job(JobsInstance instance) : base(instance.ID.Id)
        {
            ParseInstance(instance);
            InitTracking();
        }

        /// <summary>
        /// Gets or sets the name of the job.
        /// </summary>
        public override string Name { get; set; }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + Id.GetHashCode();
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

            return Id == other.Id &&
                   Name == other.Name;
        }

        private void ParseInstance(JobsInstance instance)
        {
            Name = instance.JobInfo.JobName;
        }
    }
}
