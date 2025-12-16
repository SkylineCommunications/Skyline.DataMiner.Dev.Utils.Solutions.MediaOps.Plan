namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using SLDataGateway.API.Types.Querying;

    /// <summary>
    /// Defines methods to read API objects from a data source.
    /// </summary>
    /// <typeparam name="T">The type of API object.</typeparam>
    public interface IReadableRepository<T> where T : ApiObject
    {
        /// <summary>
        /// Reads all API objects.
        /// </summary>
        /// <returns>An enumerable collection of all API objects.</returns>
        IEnumerable<T> Read();

        /// <summary>
        /// Reads a single API object by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the API object.</param>
        /// <returns>The API object with the specified identifier, or <c>null</c> if not found.</returns>
        T Read(Guid id);

        /// <summary>
        /// Reads multiple API objects by their unique identifiers.
        /// </summary>
        /// <param name="ids">A collection of unique identifiers.</param>
        /// <returns>A dictionary mapping each identifier to its corresponding API object.</returns>
        IDictionary<Guid, T> Read(IEnumerable<Guid> ids);

        /// <summary>
        /// Reads API objects that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading objects.</param>
        /// <returns>An enumerable collection of API objects matching the filter.</returns>
        IEnumerable<T> Read(FilterElement<T> filter);

        /// <summary>
        /// Reads API objects that match the specified query.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading objects.</param>
        /// <returns>An enumerable collection of API objects matching the query.</returns>
        IEnumerable<T> Read(IQuery<T> query);
    }
}
