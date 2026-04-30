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
        /// Gets or sets the identifier of the workflow being resolved against. Used to read the workflow name
        /// and workflow-level configuration parameters from <see cref="IMediaOpsPlanApi.Workflows"/>.
        /// </summary>
        /// <remarks>
        /// A <see cref="DataReference"/> with a <see langword="null"/> or empty <see cref="DataReference.NodeId"/>
        /// targets the workflow / job itself rather than any specific node.
        /// </remarks>
        public Guid? WorkflowId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the job being resolved against. Used to read the job name
        /// and workflow-level configuration parameters from <see cref="IMediaOpsPlanApi.Jobs"/>.
        /// </summary>
        /// <remarks>
        /// A <see cref="DataReference"/> with a <see langword="null"/> or empty <see cref="DataReference.NodeId"/>
        /// targets the workflow / job itself rather than any specific node.
        /// </remarks>
        public Guid? JobId { get; set; }
    }
}
