namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an error that occurs when a resource pool is referenced by one or multiple workflows.
    /// </summary>
    public class ResourcePoolInUseByWorkflowsError : ResourcePoolInUseError
    {
        /// <summary>
        /// Ids of the workflows referencing the resource pool.
        /// </summary>
        public IReadOnlyCollection<Guid> WorkflowIds { get; internal set; } = new List<Guid>();
    }
}
