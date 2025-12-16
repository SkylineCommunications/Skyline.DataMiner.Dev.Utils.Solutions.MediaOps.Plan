namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    /// <summary>
    /// Provides the base implementation for repository operations on API objects.
    /// </summary>
    internal abstract class Repository
    {
        private readonly MediaOpsPlanApi planApi;

        private readonly MediaOpsTraceData traceData = new MediaOpsTraceData();

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository"/> class.
        /// </summary>
        /// <param name="planApi">The MediaOps Plan API instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="planApi"/> is <c>null</c>.</exception>
        protected Repository(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        /// <summary>
        /// Gets the MediaOps Plan API instance associated with this repository.
        /// </summary>
        public MediaOpsPlanApi PlanApi => planApi;

        /// <summary>
        /// Gets the trace data for operations performed by this repository.
        /// </summary>
        public MediaOpsTraceData TraceData => traceData;
    }
}
