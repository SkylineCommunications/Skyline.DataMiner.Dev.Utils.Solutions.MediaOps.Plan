namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Defines methods to read API objects from a data source.
    /// </summary>
    /// <typeparam name="T">The type of API object.</typeparam>
    public interface IReaderRepository<T> where T : ApiObject
    {
        /// <summary>
        /// Reads all API objects.
        /// </summary>
        /// <returns>An enumerable collection of all API objects.</returns>
        IEnumerable<T> ReadAll();

        /// <summary>
        /// Reads all API objects in pages.
        /// </summary>
        /// <returns>An enumerable collection of pages, where each page is an enumerable collection of API objects.</returns>
        IEnumerable<IEnumerable<T>> ReadAllPaged();

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
        /// Returns an <see cref="IQueryable{T}"/> that can be used to build LINQ queries against the API objects.
        /// </summary>
        /// <returns>An <see cref="IQueryable{T}"/> for querying API objects.</returns>
        IQueryable<T> Query();

        /// <summary>
        /// Gets the total number of API objects in the repository.
        /// </summary>
        /// <returns>The total count of API objects.</returns>
        long CountAll();
    }
}
