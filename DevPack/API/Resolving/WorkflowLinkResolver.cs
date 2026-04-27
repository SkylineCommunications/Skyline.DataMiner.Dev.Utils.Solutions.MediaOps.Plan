namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// A <see cref="LinkResolver"/> bound to a specific <see cref="Workflow"/> or <see cref="Job"/>.
    /// </summary>
    /// <remarks>
    /// Use this resolver when references must be resolved against a known workflow or job. It avoids round-trips
    /// to <see cref="IMediaOpsPlanApi.Workflows"/> / <see cref="IMediaOpsPlanApi.Jobs"/> by exposing the supplied
    /// instance's name and orchestration settings to the base resolution logic.
    /// </remarks>
    public class WorkflowLinkResolver : LinkResolver
    {
        private readonly string workflowName;
        private readonly OrchestrationSettings orchestrationSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowLinkResolver"/> class for a workflow.
        /// </summary>
        /// <param name="planApi">The MediaOps Plan API used as the default data source.</param>
        /// <param name="workflow">The workflow to resolve references against.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="planApi"/> or <paramref name="workflow"/> is <c>null</c>.</exception>
        public WorkflowLinkResolver(IMediaOpsPlanApi planApi, Workflow workflow)
            : base(planApi)
        {
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));

            workflowName = workflow.Name;
            orchestrationSettings = workflow.OrchestrationSettings;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowLinkResolver"/> class for a job.
        /// </summary>
        /// <param name="planApi">The MediaOps Plan API used as the default data source.</param>
        /// <param name="job">The job to resolve references against.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="planApi"/> or <paramref name="job"/> is <c>null</c>.</exception>
        public WorkflowLinkResolver(IMediaOpsPlanApi planApi, Job job)
            : base(planApi)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            workflowName = job.Name;
            orchestrationSettings = job.OrchestrationSettings;
        }

        /// <inheritdoc/>
        protected override string GetWorkflowName(ResolveContext context)
        {
            return workflowName ?? base.GetWorkflowName(context);
        }

        /// <inheritdoc/>
        protected override OrchestrationSettings GetWorkflowOrchestrationSettings(ResolveContext context)
        {
            return orchestrationSettings ?? base.GetWorkflowOrchestrationSettings(context);
        }

        /// <inheritdoc/>
        protected override string TryGetCapabilityDisplayName(CapabilityParameterReference reference, ResolveContext context)
            => TryGetParameterNameFrom(orchestrationSettings, reference) ?? base.TryGetCapabilityDisplayName(reference, context);

        /// <inheritdoc/>
        protected override string TryGetCapacityDisplayName(CapacityParameterReference reference, ResolveContext context)
            => TryGetParameterNameFrom(orchestrationSettings, reference) ?? base.TryGetCapacityDisplayName(reference, context);

        /// <inheritdoc/>
        protected override string TryGetConfigurationDisplayName(ConfigurationParameterReference reference, ResolveContext context)
            => TryGetParameterNameFrom(orchestrationSettings, reference) ?? base.TryGetConfigurationDisplayName(reference, context);
    }
}
