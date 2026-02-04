namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Logging;

    /// <summary>
    /// Defines the contract for the MediaOps Plan API.
    /// </summary>
    public interface IMediaOpsPlanApi
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
        IResourcePropertiesRepository ResourceProperties { get; }

        /// <summary>
        /// Gets the repository for managing jobs.
        /// </summary>
        IJobsRepository Jobs { get; }

        /// <summary>
        /// Gets the repository for managing workflows.
        /// </summary>
        IWorkflowsRepository Workflows { get; }

        /// <summary>
        /// Gets the repository for managing recurring jobs.
        /// </summary>
        IRecurringJobsRepository RecurringJobs { get; }

        /// <summary>
        /// Determines whether the MediaOps.PLAN application is installed on the DataMiner System.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the application is installed; otherwise, <c>false</c>.
        /// </returns>
        bool IsInstalled();

        /// <summary>
        /// Determines whether the MediaOps.PLAN application is installed on the DataMiner System.
        /// </summary>
        /// <param name="version">
        /// When this method returns <c>true</c>, contains the version of the installed application;
        /// otherwise, <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the application is installed; otherwise, <c>false</c>.
        /// </returns>
        bool IsInstalled(out string version);

        /// <summary>
        /// Sets the logger to be used by the MediaOps Plan API.
        /// </summary>
        /// <param name="logger">The logger instance to use for logging operations.</param>
        void SetLogger(ILogger logger);
    }
}
