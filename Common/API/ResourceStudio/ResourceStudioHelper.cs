namespace Skyline.DataMiner.MediaOps.API.Common.ResourceStudio
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides helper methods to interact with MediaOps Resource Studio.
    /// </summary>
    public class ResourceStudioHelper
    {
        #region Fields
        private readonly MediaOpsHelpers helpers;

        private ClientMetadata settings;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceStudioHelper"/> class.
        /// </summary>
        /// <param name="helpers">The <see cref="MediaOpsHelpers"/> instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="helpers"/> is <see langword="null" />.</exception>
        internal ResourceStudioHelper(MediaOpsHelpers helpers)
        {
            this.helpers = helpers ?? throw new ArgumentNullException(nameof(helpers));
        }

        #region Methods
        /// <summary>
        /// Sets the client metadata used for configurations.
        /// </summary>
        /// <param name="clientMetadata">The metadata used for configurations.</param>
        /// <exception cref="ArgumentNullException"><paramref name="clientMetadata"/> is <see langword="null" />.</exception>
        public void SetClientMetadata(ClientMetadata clientMetadata)
        {
            settings = clientMetadata ?? throw new ArgumentNullException(nameof(clientMetadata));
        }

        /// <summary>
        /// Retrieves a resource by its unique identifier.
        /// </summary>
		/// <param name="id">The unique identifier of the resource.</param>
		/// <returns>The resource with the specified ID, or null if not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is an empty GUID.</exception>
        public IResource GetResource(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }
        }

        /// <summary>
        /// Retrieves a resource by its name.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        /// <returns>The resource with the specified name, or null if not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null or empty.</exception>
        public IResource GetResource(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
        }

        /// <summary>
        /// Retrieves all resources.
        /// </summary>
        public IEnumerable<IResource> GetAllResources()
        {

        }

        /// <summary>
        /// Retrieves the total number of resources.
        /// </summary>
        /// <returns>The total number of resources.</returns>
        public long GetResourcesCount()
        {

        }

        /// <summary>
        /// Creates a new resource with the specified configuration and metadata.
        /// </summary>
        /// <param name="configuration">The configuration for the new resource.</param>
        /// <param name="objectMetadata">The metadata for the new resource.</param>
        /// <returns>The unique identifier of the newly created resource, or <see cref="Guid.Empty"/> if the creation failed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> or <paramref name="objectMetadata"/> is null.</exception>
        public Guid CreateResource(ResourceConfiguration configuration, ObjectMetadata objectMetadata)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (objectMetadata == null)
            {
                throw new ArgumentNullException(nameof(objectMetadata));
            }
        }
        #endregion
    }
}
