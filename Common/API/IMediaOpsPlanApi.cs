namespace Skyline.DataMiner.MediaOps.Plan.API
{
    /// <summary>
    /// Defines the contract for the MediaOps Plan API.
    /// </summary>
    public interface IMediaOpsPlanApi
    {
        // /// <summary>
        // /// Gets the repository for managing resources.
        // /// </summary>
        // IResourcesRepository Resources { get; }

        /// <summary>
        /// Gets the repository for managing resource pools.
        /// </summary>
        IResourcePoolsRepository ResourcePools { get; }

        /*
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
		IResourcePropertiesRepository Properties { get; }
		*/
    }
}
