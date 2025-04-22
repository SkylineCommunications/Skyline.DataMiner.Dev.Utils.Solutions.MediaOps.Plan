namespace Skyline.DataMiner.MediaOps.API.Common.API.Generic
{
    using System.Collections.Generic;

    /// <summary>
    /// Defines that the implementing class can create new objects.
    /// </summary>
    /// <typeparam name="TCreate">The type to request a create.</typeparam>
    public interface IApiCreate<TCreate>
        where TCreate : IRequest
    {
        /// <summary>
        /// Request to create new instances. The method will not wait until the instances are created. No feedback will be given if there is a failure.
        /// </summary>
        /// <param name="requests">The create requests.</param>
        void CreateAsync(IEnumerable<TCreate> requests);

        /// <summary>
        /// Request to add new instances. The method will be blocking until all requests are handled.
        /// </summary>
        /// <param name="requests">The create requests.</param>
        ResultMessage<TCreate> Create(IEnumerable<TCreate> requests);
    }
}