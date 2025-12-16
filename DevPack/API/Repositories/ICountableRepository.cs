namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using SLDataGateway.API.Types.Querying;

    /// <summary>
    /// Defines a repository that supports counting operations for API objects.
    /// </summary>
    /// <typeparam name="T">The type of API object managed by this repository.</typeparam>
    public interface ICountableRepository<T> where T : ApiObject
    {
        /// <summary>
        /// Gets the total number of API objects in the repository.
        /// </summary>
        /// <returns>The total count of API objects.</returns>
        long Count();

        /// <summary>
        /// Gets the number of API objects in the repository that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when counting objects.</param>
        /// <returns>The count of API objects matching the filter.</returns>
        long Count(FilterElement<T> filter);

        /// <summary>
        /// Gets the number of API objects in the repository that match the specified query.
        /// </summary>
        /// <param name="query">The query criteria to apply when counting objects.</param>
        /// <returns>The count of API objects matching the query.</returns>
        long Count(IQuery<T> query);
    }
}
