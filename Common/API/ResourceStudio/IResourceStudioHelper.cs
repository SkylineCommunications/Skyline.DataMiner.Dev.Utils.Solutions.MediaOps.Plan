namespace Skyline.DataMiner.MediaOps.API.Common.ResourceStudio
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// MediaOps API Resource Studio Helper interface
    /// </summary>
    public interface IResourceStudioHelper
    {
        /// <summary>
        /// Creates a new element resource with the specified configuration and metadata.
        /// </summary>
        /// <param name="configuration">The configuration for the new resource.</param>
        /// <param name="objectMetadata">The metadata for the new resource.</param>
        /// <returns>The unique identifier of the newly created resource, or <see cref="Guid.Empty"/> if the creation failed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> or <paramref name="objectMetadata"/> is null.</exception>
        Guid CreateElementResource(ElementResourceConfiguration configuration, ObjectMetadata objectMetadata);

        /// <summary>
        /// Creates a new unmanaged resource with the specified configuration and metadata.
        /// </summary>
        /// <param name="configuration">The configuration for the new resource.</param>
        /// <param name="objectMetadata">The metadata for the new resource.</param>
        /// <returns>The unique identifier of the newly created resource, or <see cref="Guid.Empty"/> if the creation failed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> or <paramref name="objectMetadata"/> is null.</exception>
        Guid CreateResource(ResourceConfiguration configuration, ObjectMetadata objectMetadata);

        /// <summary>
        /// Creates a new service resource with the specified configuration and metadata.
        /// </summary>
        /// <param name="configuration">The configuration for the new resource.</param>
        /// <param name="objectMetadata">The metadata for the new resource.</param>
        /// <returns>The unique identifier of the newly created resource, or <see cref="Guid.Empty"/> if the creation failed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> or <paramref name="objectMetadata"/> is null.</exception>
        Guid CreateServiceResource(ServiceResourceConfiguration configuration, ObjectMetadata objectMetadata);

        /// <summary>
        /// Retrieves all resources.
        /// </summary>
        IEnumerable<IResource> GetAllResources();

        /// <summary>
        /// Retrieves a resource by its unique identifier.
        /// </summary>
		/// <param name="id">The unique identifier of the resource.</param>
		/// <returns>The resource with the specified ID, or null if not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is an empty GUID.</exception>
        IResource GetResource(Guid id);

        /// <summary>
        /// Retrieves a resource by its name.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        /// <returns>The resource with the specified name, or null if not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty.</exception>
        IResource GetResource(string name);

        /// <summary>
        /// Retrieves the total number of resources.
        /// </summary>
        /// <returns>The total number of resources.</returns>
        long GetResourcesCount();

        /// <summary>
        /// Sets the client metadata used for configurations.
        /// </summary>
        /// <param name="clientMetadata">The metadata used for configurations.</param>
        /// <exception cref="ArgumentNullException"><paramref name="clientMetadata"/> is <see langword="null" />.</exception>
        void SetClientMetadata(ClientMetadata clientMetadata);
    }
}