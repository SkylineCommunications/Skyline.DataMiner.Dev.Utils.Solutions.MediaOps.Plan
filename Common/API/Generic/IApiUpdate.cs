namespace Skyline.DataMiner.MediaOps.API.Common.API.Generic
{
    using System.Collections.Generic;

    /// <summary>
    /// Defines that the implementing class can update existing objects.
    /// </summary>
    /// <typeparam name="TUpdate">The type to request an update.</typeparam>
    public interface IApiUpdate<TUpdate>
        where TUpdate : IRequest
    {
        /// <summary>
        /// Request to update existing instances. The method will not wait until the instances are updated. No feedback will be given if there is a failure.
        /// </summary>
        /// <param name="requests">The update requests.</param>
        void UpdateAsync(IEnumerable<TUpdate> requests);

        /// <summary>
        /// Request to update new instances. The method will be blocking until all requests are handled.
        /// </summary>
        /// <param name="requests">The update requests.</param>
        ResultMessage<TUpdate> Update(IEnumerable<TUpdate> requests);
    }
}