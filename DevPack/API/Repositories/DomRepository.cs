namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
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
