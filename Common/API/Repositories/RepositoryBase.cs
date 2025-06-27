namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.MediaOps.Plan.Exceptions;

    internal abstract class RepositoryBase<T> where T : ApiObject
    {
        private readonly MediaOpsPlanApi planApi;

        private readonly MediaOpsTraceData traceData = new MediaOpsTraceData();

        public RepositoryBase(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        public MediaOpsPlanApi PlanApi => planApi;

        public MediaOpsTraceData TraceData => traceData;
    }
}
