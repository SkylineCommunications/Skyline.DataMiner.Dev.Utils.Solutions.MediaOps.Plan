namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System.Collections.Generic;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using SLDataGateway.API.Types.Querying;

    public interface IPageableRepository<T> where T : ApiObject
    {
        /// <summary>
        /// Reads all API objects in pages.
        /// </summary>
        /// <returns>An enumerable collection of pages, where each page is an enumerable collection of API objects.</returns>
        IEnumerable<IPagedResult<T>> ReadPaged();

        IEnumerable<IPagedResult<T>> ReadPaged(FilterElement<T> filter);

        IEnumerable<IPagedResult<T>> ReadPaged(IQuery<T> query);

        IEnumerable<IPagedResult<T>> ReadPaged(FilterElement<T> filter, int pageSize);

        IEnumerable<IPagedResult<T>> ReadPaged(IQuery<T> query, int pageSize);
    }
}
