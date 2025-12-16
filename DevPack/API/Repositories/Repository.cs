namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using SLDataGateway.API.Types.Querying;

    internal abstract class Repository<T, TFilterElement>
        where T : ApiObject
        where TFilterElement : DataType
    {
        private readonly MediaOpsPlanApi planApi;

        private readonly MediaOpsTraceData traceData = new MediaOpsTraceData();

        protected Repository(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        public MediaOpsPlanApi PlanApi => planApi;

        public MediaOpsTraceData TraceData => traceData;
    }
}
