namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Defines methods to delete API objects or their identifiers from a repository.
    /// </summary>
    /// <typeparam name="T">The type of API object, derived from <see cref="ApiObject"/>.</typeparam>
    public interface IDeleterRepository<in T> where T : ApiObject
    {
        /// <summary>
        /// Deletes the specified API objects from the repository.
        /// </summary>
        /// <param name="apiObjects">The API objects to delete.</param>
        void Delete(params T[] apiObjects);

        /// <summary>
        /// Deletes the API objects with the specified unique identifiers from the repository.
        /// </summary>
        /// <param name="apiObjectIds">The unique identifiers of the API objects to delete.</param>
        void Delete(params Guid[] apiObjectIds);
    }
}
