namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using Skyline.DataMiner.SDM;

    /// <summary>
    /// Defines methods for managing <see cref="Workflow"/> objects.
    /// </summary>
    public interface IWorkflowsRepository : IReadableRepository<Workflow>
    {
        /// <summary>
        /// Reads all Workflows.
        /// </summary>
        /// <returns>An enumerable collection of all Workflows.</returns>
        IEnumerable<Workflow> Read();

        /// <summary>
        /// Reads a single Workflow by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Workflow.</param>
        /// <returns>The Workflow with the specified identifier, or <c>null</c> if not found.</returns>
        Workflow Read(Guid id);

        /// <summary>
        /// Reads multiple Workflows by their unique identifiers.
        /// </summary>
        /// <param name="ids">A collection of unique identifiers.</param>
        /// <returns>An enumerable collection of Workflows matching the specified identifiers.</returns>
        IEnumerable<Workflow> Read(IEnumerable<Guid> ids);
    }
}
