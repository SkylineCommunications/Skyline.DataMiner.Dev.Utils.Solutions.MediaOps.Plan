namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Defines the contract for the MediaOps Plan API.
    /// </summary>
    public interface IMediaOpsPlanApi : IDisposable
    {
        /// <summary>
        /// Gets the repository for managing resources.
        /// </summary>
        IResourcesRepository Resources { get; }

        /// <summary>
        /// Gets the repository for managing resource pools.
        /// </summary>
        IResourcePoolsRepository ResourcePools { get; }

        /// <summary>
        /// Gets the repository for managing capabilities.
        /// </summary>
        ICapabilitiesRepository Capabilities { get; }

        /// <summary>
        /// Gets the repository for managing capacities.
        /// </summary>
        ICapacitiesRepository Capacities { get; }

        /// <summary>
        /// Gets the repository for managing configurations.
        /// </summary>
        IConfigurationsRepository Configurations { get; }

        /// <summary>
        /// Gets the repository for managing resource properties.
        /// </summary>
        IResourcePropertiesRepository Properties { get; } // TODO: should we rename this to ResourceProperties? Just to not run into issues if we introduce other kinds of properties in the future.

    }
}
