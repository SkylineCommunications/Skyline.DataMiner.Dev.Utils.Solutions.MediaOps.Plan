namespace Skyline.DataMiner.MediaOps.API.Common.ResourceStudio
{
    using System;

    /// <summary>
    /// MediaOps API Resource interface
    /// </summary>
    public interface IResource
    {
        /// <summary>
        /// Gets the MediaOps Resource ID.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets the name of the resource.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the concurrency of the resource.
        /// </summary>
        long Concurrency { get; }

        /// <summary>
        /// Gets the status of the resource.
        /// </summary>
        ResourceStatus Status { get; }
    }
}
