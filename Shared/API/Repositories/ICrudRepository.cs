namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines methods to create, read, update, and delete API objects in the repository.
    /// </summary>
    /// <typeparam name="T">The type of API object. Must inherit from <see cref="ApiObject"/>.</typeparam>
	public interface ICrudRepository<T> : ICreatorRepository<T>, IReaderRepository<T>, IUpdaterRepository<T>, IDeleterRepository<T> where T : ApiObject
    {
        /// <summary>
        /// Creates new API objects or updates existing ones in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of API objects to create or update.</param>
        /// <returns>A collection of unique identifiers for the created or updated API objects.</returns>
        IEnumerable<Guid> CreateOrUpdate(IEnumerable<T> apiObjects);
    }
}
