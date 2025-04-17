namespace Skyline.DataMiner.MediaOps.API.Common.API.Teams
{
    using Skyline.DataMiner.MediaOps.API.Common.API.People;

    public interface ITeamMember
    {
        ITeam Team { get; }

        IPerson Person { get; }

        string Role { get; }
    }
}
