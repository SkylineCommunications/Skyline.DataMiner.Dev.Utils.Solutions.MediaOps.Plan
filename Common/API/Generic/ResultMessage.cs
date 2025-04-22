namespace Skyline.DataMiner.MediaOps.API.Common.API.Generic
{
    using System;

    /// <summary>
    /// Result message for requests made.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    public struct ResultMessage<TRequest>
        where TRequest : IRequest
    {
        /// <summary>
        /// Whether or not the request was successful.
        /// </summary>
        public bool Succeeded { get; set;  }

        /// <summary>
        /// The requests that failed.
        /// </summary>
        public TRequest[] FailedRequests { get; set; }
    }
}