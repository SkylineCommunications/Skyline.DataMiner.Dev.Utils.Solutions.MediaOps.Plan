namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using Skyline.DataMiner.SDM;

    /// <summary>
    /// Defines a repository for MediaOps Plan.
    /// </summary>
    public interface IRepository<T> : IBulkRepository<T>
    {
        /// <summary>
        /// Gets the total number of API objects in the repository.
        /// </summary>
        /// <returns>The total count of API objects.</returns>
        long Count();

        /// <summary>
        /// Deletes the API object with the specified unique identifier from the repository.
        /// </summary>
        /// <param name="apiObjectId">The unique identifier of the API object to delete.</param>
        void Delete(Guid apiObjectId);

        /// <summary>
        /// Deletes the API objects with the specified unique identifiers from the repository.
        /// </summary>
        /// <param name="apiObjectIds">The unique identifiers of the API objects to delete.</param>
        void Delete(IEnumerable<Guid> apiObjectIds);

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
        /// <returns>An enumerable collection of API objects matching the specified identifiers.</returns>
        IEnumerable<T> Read(IEnumerable<Guid> ids);

        /// <summary>
        /// Reads all API objects in pages.
        /// </summary>
        /// <returns>An enumerable collection of pages, where each page is an enumerable collection of API objects.</returns>
        IEnumerable<IPagedResult<T>> ReadPaged();

        /// <summary>
        /// Reads all API objects in pages.
        /// </summary>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page is an enumerable collection of API objects.</returns>
        IEnumerable<IPagedResult<T>> ReadPaged(int pageSize);
    }
}