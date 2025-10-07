namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Linq;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using SLDataGateway.API.Types.Querying;

    internal abstract class DomRepository<T> : Repository<T, DomInstance> where T : ApiObject
    {
        protected DomRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
        {
            switch (fieldName)
            {
                case nameof(ApiObject.Id):
                    return FilterElementFactory<DomInstance>.Create(DomInstanceExposers.Id, comparer, value);
                case nameof(ApiObject.Name):
                    return FilterElementFactory<DomInstance>.Create(DomInstanceExposers.Name, comparer, value);
                default:
                    throw new NotImplementedException();
            }
        }

        protected internal override FilterElement<DomInstance> CreateFilter(Type type, Comparer comparer)
        {
            throw new NotImplementedException();
        }

        protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
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
