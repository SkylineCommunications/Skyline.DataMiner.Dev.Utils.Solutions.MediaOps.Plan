namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Provides contextual information used while resolving <see cref="DataReference"/> instances
    /// through a <see cref="LinkResolver"/>.
    /// </summary>
    public sealed class ResolveContext
    {
        /// <summary>Gets a shared empty context, useful when no contextual information is required.</summary>
        public static ResolveContext Empty { get; } = new ResolveContext();

        /// <summary>
        /// Gets or sets the identifier of the workflow node from which the resolution is performed.
        /// References whose <see cref="DataReference.NodeId"/> matches this value (or is empty) are
        /// considered to target the current node.
        /// </summary>
        public string CurrentNodeId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the resource assigned to the current node. Used by
        /// <see cref="LinkResolver"/> to resolve <see cref="DataReferenceType.ResourceName"/>,
        /// <see cref="DataReferenceType.ResourceProperty"/> and <see cref="DataReferenceType.ResourceLinkedObjectID"/>
        /// references that target the current node.
        /// </summary>
        public Guid? CurrentResourceId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the workflow being resolved against. Used to read the workflow name
        /// and workflow-level configuration parameters from <see cref="IMediaOpsPlanApi.Workflows"/>.
        /// </summary>
        public Guid? WorkflowId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the job being resolved against. Used to read the job name
        /// and workflow-level configuration parameters from <see cref="IMediaOpsPlanApi.Jobs"/>.
        /// </summary>
        public Guid? JobId { get; set; }
    }
}
