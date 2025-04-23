namespace Skyline.DataMiner.MediaOps.API.Common.API
{
    using Skyline.DataMiner.MediaOps.API.Common.API.People;

    public interface IMediaOpsPlanApi
    {
        IPeopleApi People { get; }

        //IApiCollection<ITeam, TeamConfig> Teams { get; }

        //IApiCollection<IOrganization, OrganizationConfig> Organizations { get; }
    }
}
