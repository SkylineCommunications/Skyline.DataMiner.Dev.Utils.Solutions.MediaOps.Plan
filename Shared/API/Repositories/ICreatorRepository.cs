namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines methods to create API objects in the repository.
    /// </summary>
    /// <typeparam name="T">The type of API object. Must inherit from <see cref="ApiObject"/>.</typeparam>
    public interface ICreatorRepository<T> where T : ApiObject
    {
        /// <summary>
        /// Creates a new API object in the repository.
        /// </summary>
        /// <param name="apiObject">The API object to create.</param>
        /// <returns>The unique identifier of the created API object.</returns>
        Guid Create(T apiObject);

        /// <summary>
        /// Creates multiple new API objects in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of API objects to create.</param>
        /// <returns>A collection of unique identifiers for the created API objects.</returns>
        IEnumerable<Guid> Create(IEnumerable<T> apiObjects);
    }
}
