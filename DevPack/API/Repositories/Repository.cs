namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using SLDataGateway.API.Types.Querying;

    /// <summary>
    /// Provides the base implementation for repository operations on API objects.
    /// </summary>
    /// <typeparam name="T">The type of API object managed by this repository.</typeparam>
    /// <typeparam name="TFilterElement">The type of filter element used for querying.</typeparam>
    internal abstract class Repository<T, TFilterElement>
        where T : ApiObject
        where TFilterElement : DataType
    {
        private readonly MediaOpsPlanApi planApi;

        private readonly MediaOpsTraceData traceData = new MediaOpsTraceData();

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository{T, TFilterElement}"/> class.
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
