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
		/// Gets the repository for reading flow rules and evaluating connections against them.
		/// </summary>
		IFlowRuleCollectionsRepository FlowRuleCollections { get; }

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
		/// Gets the repository for managing properties.
		/// </summary>
		IPropertiesRepository Properties { get; }

		/// <summary>
		/// Gets the repository for managing scheduling properties.
		/// </summary>
		ISchedulingPropertiesRepository SchedulingProperties { get; }

		/// <summary>
		/// Gets the repository for managing property setting collections.
		/// </summary>
		IPropertySettingCollectionsRepository PropertySettingCollections { get; }

		/// <summary>
		/// Gets the repository for managing relationships.
		/// </summary>
		IRelationshipsRepository Relationships { get; }

		/// <summary>
		/// Gets the repository for managing relationship object types.
		/// </summary>
		IRelationshipObjectTypesRepository RelationshipObjectTypes { get; }

		/// <summary>
		/// Gets the global application settings.
		/// </summary>
		IGlobalSettings GlobalSettings { get; }

		/// <summary>
		/// Installs or reconciles the DOM schemas owned by MediaOps Plan, including People and Organizations.
		/// </summary>
		/// <remarks>
		/// Requires DataMiner 10.6.11 or later and permission to configure module settings.
		/// Module settings and declared schema metadata are authoritative. Removed fields and section links
		/// are retained as soft-deleted metadata; unrelated definitions and sections are not removed.
		/// This method does not install scripts, migrate instance data, or register the solution.
		/// Each module's DOM Manager is reinitialized after reconciliation. Exceptions propagate and
		/// previously completed writes are not rolled back.
		/// </remarks>
		void InstallDomModules();

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
