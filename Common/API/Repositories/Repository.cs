namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using SLDataGateway.API.Types.Querying;

    internal abstract class Repository<T> where T : ApiObject
    {
        private readonly MediaOpsPlanApi planApi;

        private readonly MediaOpsTraceData traceData = new MediaOpsTraceData();
        private readonly ApiRepositoryQueryProvider<T> _queryProvider;

        protected Repository(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
            _queryProvider = new ApiRepositoryQueryProvider<T>(this);
        }

        public MediaOpsPlanApi PlanApi => planApi;

        public MediaOpsTraceData TraceData => traceData;

        protected internal ApiRepositoryQueryProvider<T> QueryProvider => _queryProvider;

        protected internal virtual FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
        {
            switch (fieldName)
            {
                case nameof(ApiObject.Id):
                    return FilterElementFactory.Create<Guid>(DomInstanceExposers.Id, comparer, value);
                case nameof(ApiObject.Name):
                    return FilterElementFactory.Create(DomInstanceExposers.Name, comparer, value);
                default:
                    throw new NotImplementedException();
            }
        }

        protected internal virtual FilterElement<DomInstance> CreateFilter(Type type, Comparer comparer)
        {
            throw new NotImplementedException();
        }

        protected internal virtual IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
        {
            switch (fieldName)
            {
                case nameof(ApiObject.Id):
                    return OrderByElementFactory.Create(DomInstanceExposers.Id, sortOrder, naturalSort);
                case nameof(Resource.Name):
                    return OrderByElementFactory.Create(DomInstanceExposers.Name, sortOrder, naturalSort);
                default:
                    throw new NotImplementedException();
            }
        }

        internal abstract IEnumerable<T> Read(IQuery<DomInstance> query);

        internal abstract long Count(FilterElement<DomInstance> domFilter);

        protected static FilterElement<DomInstance> AddDomDefinitionFilter(FilterElement<DomInstance> domFilter, DomDefinitionId domDefinitionId)
        {
            var _domDefinitionFilter = DomInstanceExposers.DomDefinitionId.Equal(domDefinitionId.Id);
            if (domFilter == _domDefinitionFilter)
            {
                return domFilter;
            }

            if (domFilter is TRUEFilterElement<DomInstance>)
            {
                return _domDefinitionFilter;
            }

            if (domFilter is ANDFilterElement<DomInstance> andFilter)
            {
                return !andFilter.subFilters.Contains(_domDefinitionFilter)
                    ? andFilter.AND(_domDefinitionFilter)
                    : domFilter;
            }

            return new ANDFilterElement<DomInstance>(_domDefinitionFilter, domFilter);
        }
    }
}
