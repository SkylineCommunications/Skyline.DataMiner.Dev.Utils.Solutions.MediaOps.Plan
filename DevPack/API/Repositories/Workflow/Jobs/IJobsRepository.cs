namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.SDM;

    /// <summary>
    /// Defines methods for managing <see cref="Job"/> objects.
    /// </summary>
    public interface IJobsRepository : IRepository<Job>
    {
        /// <summary>
        /// Set the state of a specific orchestration event for a job.
        /// </summary>
        /// <param name="id">The unique identifier of the job.</param>
        /// <param name="updateDetails">An object containing the new state information and any associated metadata. Cannot be null.</param>
        void SetOrchestrationState(Guid id, OrchestrationUpdateDetails updateDetails);
    }
}
