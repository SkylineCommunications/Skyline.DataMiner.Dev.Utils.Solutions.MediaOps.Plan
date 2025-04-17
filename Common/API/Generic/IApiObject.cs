namespace Skyline.DataMiner.MediaOps.API.Common.API.Generic
{
    using System;

    /// <summary>
    /// Defines a generic API object interface.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IApiObject
    {
        /// <summary>
        /// Gets the unique identifier of the object.
        /// </summary>
        Guid Id { get; }
    }
}
