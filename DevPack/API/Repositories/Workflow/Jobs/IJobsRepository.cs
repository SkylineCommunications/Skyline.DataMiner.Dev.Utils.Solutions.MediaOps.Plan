namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using Skyline.DataMiner.SDM;

    /// <summary>
    /// Defines methods for managing <see cref="Job"/> objects.
    /// </summary>
    public interface IJobsRepository : IReadableRepository<Job>
    {
        /// <summary>
        /// Reads all Jobs.
        /// </summary>
        /// <returns>An enumerable collection of all Jobs.</returns>
        IEnumerable<Job> Read();

        /// <summary>
        /// Reads a single Job by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Job.</param>
        /// <returns>The Job with the specified identifier, or <c>null</c> if not found.</returns>
        Job Read(Guid id);

        /// <summary>
        /// Reads multiple Jobs by their unique identifiers.
        /// </summary>
        /// <param name="ids">A collection of unique identifiers.</param>
        /// <returns>An enumerable collection of Jobs matching the specified identifiers.</returns>
        IEnumerable<Job> Read(IEnumerable<Guid> ids);

        /// <summary>
        /// Set the state of a specific orchestration event for a job.
        /// </summary>
        /// <param name="id">The unique identifier of the job.</param>
        /// <param name="updateDetails">An object containing the new state information and any associated metadata. Cannot be null.</param>
        void SetOrchestrationState(Guid id, OrchestrationUpdateDetails updateDetails);
    }
}
