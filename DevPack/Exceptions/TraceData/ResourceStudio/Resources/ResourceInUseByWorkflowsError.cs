namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an error that occurs when a resource is referenced by one or multiple workflows.
    /// </summary>
    public class ResourceInUseByWorkflowsError : ResourceInUseError
    {
        /// <summary>
        /// Ids of the workflows referencing the resource.
        /// </summary>
        public IReadOnlyCollection<Guid> WorkflowIds { get; internal set; } = new List<Guid>();
    }
}
