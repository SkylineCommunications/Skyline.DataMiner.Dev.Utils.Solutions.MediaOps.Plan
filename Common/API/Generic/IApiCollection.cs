namespace Skyline.DataMiner.MediaOps.API.Common.API.Generic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Defines a collection of API objects that can be queried.
    /// </summary>
    /// <typeparam name="T">The type of API object in the collection.</typeparam>
    /// <typeparam name="TConfig">The type of configuration for the API objects.</typeparam>
    public interface IApiCollection<out T, TConfig> : IOrderedQueryable<T>
        where T : IApiObject
        where TConfig : IConfiguration
    {
        /// <summary>
        /// Adds a new object to the collection.
        /// </summary>
        /// <param name="config">The configuration of the object to add.</param>
        void Add(TConfig config);

        /// <summary>
        /// Adds a range of objects to the collection.
        /// </summary>
        /// <param name="configs">The configuration of the objects to add.</param>
        void AddRange(IEnumerable<TConfig> configs);

        /// <summary>
        /// Adds a new object to the collection.
        /// </summary>
        /// <param name="config">The configuration changes that have to be applied.</param>
        void Update(TConfig config);

        /// <summary>
        /// Updates multiple objects in the collection.
        /// </summary>
        /// <param name="configs">The configuration changes that have to be applied.</param>
        void UpdateRange(IEnumerable<TConfig> configs);

        /// <summary>
        /// Bulk updates multiple objects in the collection.
        /// </summary>
        /// <param name="ids">The ids of all instances that have to be updated.</param>
        /// <param name="config">The configuration changes to apply on all objects.</param>
        void BulkUpdate(IEnumerable<Guid> ids, TConfig config);

        /// <summary>
        /// Deletes an object from the collection.
        /// </summary>
        /// <param name="id">The ID of the object to delete.</param>
        void Delete(Guid id);

        /// <summary>
        /// Deletes multiple objects from the collection.
        /// </summary>
        /// <param name="ids">The IDs of the objects to delete.</param>
        void Delete(IEnumerable<Guid> ids);
    }
}