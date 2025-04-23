namespace Skyline.DataMiner.MediaOps.API.Common.API.Generic
{
    using System;

    /// <summary>
    /// The base interface for all requests made.
    /// </summary>
    internal interface IRequest
    {
        /// <summary>
        /// The ID of the request. This ID is used to track the request in the system.
        /// </summary>
        Guid RequestId { get; set; }

        /// <summary>
        /// The ID of the main object to perform a CRUD action on. Can have default value when creating a new object.
        /// </summary>
        Guid ObjectId { get; set; }
    }
}