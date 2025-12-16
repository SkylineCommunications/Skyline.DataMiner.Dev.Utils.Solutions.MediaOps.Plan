namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using SLDataGateway.API.Types.Querying;

    public interface ICountableRepository<T> where T : ApiObject
    {
        /// <summary>
        /// Gets the total number of API objects in the repository.
        /// </summary>
        /// <returns>The total count of API objects.</returns>
        long Count();

        long Count(FilterElement<T> filter);

        long Count(IQuery<T> query);
    }
}
