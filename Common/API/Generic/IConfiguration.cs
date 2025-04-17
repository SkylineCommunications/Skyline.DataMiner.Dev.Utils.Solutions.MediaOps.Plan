namespace Skyline.DataMiner.MediaOps.API.Common.API.Generic
{
    using System;

    /// <summary>
    /// The base interface for all configuration objects.
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// The ID of the object to update/create. Can be left empty when creating a new object.
        /// </summary>
        Guid? ObjectId { get; set; }
    }
}
