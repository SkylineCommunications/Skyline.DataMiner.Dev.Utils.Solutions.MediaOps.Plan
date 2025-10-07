namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using SLDataGateway.API.Types.Querying;

    internal abstract class Repository<T, TFilterElement>
        where T : ApiObject
        where TFilterElement : DataType
    {
        private readonly MediaOpsPlanApi planApi;

        private readonly MediaOpsTraceData traceData = new MediaOpsTraceData();
        private readonly ApiRepositoryQueryProvider<T, TFilterElement> _queryProvider;

        protected Repository(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
            _queryProvider = new ApiRepositoryQueryProvider<T, TFilterElement>(this);
        }

        public MediaOpsPlanApi PlanApi => planApi;

        public MediaOpsTraceData TraceData => traceData;

        protected internal ApiRepositoryQueryProvider<T, TFilterElement> QueryProvider => _queryProvider;

        protected internal abstract FilterElement<TFilterElement> CreateFilter(string fieldName, Comparer comparer, object value);

        protected internal abstract FilterElement<TFilterElement> CreateFilter(Type type, Comparer comparer);

        protected internal abstract IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false);

        internal abstract IEnumerable<T> Read(IQuery<TFilterElement> query);

        internal abstract long Count(FilterElement<TFilterElement> filter);
    }
}
