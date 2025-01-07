namespace Skyline.DataMiner.MediaOps.API.Common.ResourceStudio
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// MediaOps API Resource Pool interface
    /// </summary>
    public interface IResourcePool
    {
        /// <summary>
        /// Gets the MediaOps Resource Pool ID.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets the name of the resource pool.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the status of the resource Pool.
        /// </summary>
        ResourcePoolStatus Status { get; }

        /// <summary>
        /// Gets the collection of resources assigned to the resource pool.
        /// </summary>
        IReadOnlyCollection<IResource> Resources { get; }

        /// <summary>
        /// Assigns resources to the resource pool.
        /// </summary>
        /// <param name="resources">A collection of resources to be assigned to the resource pool.</param>
        void AssignResources(ICollection<IResource> resources);

        /// <summary>
        /// Unassigns resources from the resource pool.
        /// </summary>
        /// <param name="resources">A collection of resources to be unassigned from the resource pool.</param>
        void UnassignResources(ICollection<IResource> resources);
    }
}
