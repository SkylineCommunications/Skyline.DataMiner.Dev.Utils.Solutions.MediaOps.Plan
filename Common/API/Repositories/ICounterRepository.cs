namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    /// <summary>
    /// Defines methods to count API objects in a repository.
    /// </summary>
    /// <typeparam name="T">The type of API object.</typeparam>
    public interface ICounterRepository<T> where T : ApiObject
    {
        /// <summary>
        /// Gets the total number of API objects in the repository.
        /// </summary>
        /// <returns>The total count of API objects.</returns>
        long CountAll();

        /// <summary>
        /// Gets the number of API objects in the repository that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter to apply to the API objects.</param>
        /// <returns>The count of API objects matching the filter.</returns>
        long Count(FilterElement<T> filter);
    }
}
