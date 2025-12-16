namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System.Collections.Generic;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using SLDataGateway.API.Types.Querying;

    /// <summary>
    /// Defines a repository that supports paginated reading operations for API objects.
    /// </summary>
    /// <typeparam name="T">The type of API object managed by this repository.</typeparam>
    public interface IPageableRepository<T> where T : ApiObject
    {
        /// <summary>
        /// Reads all API objects in pages.
        /// </summary>
        /// <returns>An enumerable collection of pages, where each page is an enumerable collection of API objects.</returns>
        IEnumerable<IPagedResult<T>> ReadPaged();

        /// <summary>
        /// Reads API objects that match the specified filter in pages.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading objects.</param>
        /// <returns>An enumerable collection of pages, where each page contains API objects matching the filter.</returns>
        IEnumerable<IPagedResult<T>> ReadPaged(FilterElement<T> filter);

        /// <summary>
        /// Reads API objects that match the specified query in pages.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading objects.</param>
        /// <returns>An enumerable collection of pages, where each page contains API objects matching the query.</returns>
        IEnumerable<IPagedResult<T>> ReadPaged(IQuery<T> query);

        /// <summary>
        /// Reads API objects that match the specified filter in pages with a custom page size.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading objects.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains up to the specified number of API objects matching the filter.</returns>
        IEnumerable<IPagedResult<T>> ReadPaged(FilterElement<T> filter, int pageSize);

        /// <summary>
        /// Reads API objects that match the specified query in pages with a custom page size.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading objects.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains up to the specified number of API objects matching the query.</returns>
        IEnumerable<IPagedResult<T>> ReadPaged(IQuery<T> query, int pageSize);
    }
}
