namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Linq;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using SLDataGateway.API.Types.Querying;

    /// <summary>
    /// Provides a base repository for managing DOM-based API objects.
    /// </summary>
    /// <typeparam name="T">The type of API object managed by this repository.</typeparam>
    internal abstract class DomRepository<T> : Repository<T, DomInstance> where T : ApiObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DomRepository{T}"/> class.
        /// </summary>
        /// <param name="planApi">The MediaOps Plan API instance.</param>
        protected DomRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        /// <summary>
        /// Adds a DOM definition filter to the provided filter element.
        /// </summary>
        /// <param name="domFilter">The existing DOM instance filter.</param>
        /// <param name="domDefinitionId">The DOM definition ID to filter by.</param>
        /// <returns>A filter element that includes the DOM definition filter.</returns>
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
