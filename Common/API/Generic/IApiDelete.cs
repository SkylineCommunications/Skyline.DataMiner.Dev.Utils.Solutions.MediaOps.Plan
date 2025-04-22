namespace Skyline.DataMiner.MediaOps.API.Common.API.Generic
{
    using System.Collections.Generic;

    /// <summary>
    /// Defines that the implementing class can create new objects.
    /// </summary>
    public interface IApiDelete<TDelete>
        where TDelete : IRequest
    {
        /// <summary>
        /// Request to delete instances. The method will not wait until the instances are deleted. No feedback will be given if there is a failure.
        /// </summary>
        /// <param name="requests">The requests to delete.</param>
        void DeleteAsync(IEnumerable<TDelete> requests);

        /// <summary>
        /// Request to delete instances. The method will be blocking until all requests are handled.
        /// </summary>
        /// <param name="requests">The requests to delete.</param>
        ResultMessage<TDelete> Delete(IEnumerable<TDelete> requests);
    }
}