namespace Skyline.DataMiner.MediaOps.Plan.API
{
    public interface IMediaOpsPlanApi
	{
        //IResourcesRepository Resources { get; }

        IResourcePoolsRepository ResourcePools { get; }

        /*ICapabilitiesRepository Capabilities { get; }

        ICapacitiesRepository Capacities { get; }

        IConfigurationsRepository Configurations { get; }

        IResourcePropertiesRepository Properties { get; }*/
    }
}
