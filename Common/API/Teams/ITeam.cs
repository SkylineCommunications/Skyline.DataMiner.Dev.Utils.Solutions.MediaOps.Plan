namespace Skyline.DataMiner.MediaOps.API.Common.API.Teams
{
    using System.Collections.Generic;

    using Skyline.DataMiner.MediaOps.API.Common.API.Generic;

    /// <summary>
    /// Represents a team.
    /// </summary>
    public interface ITeam : IApiObject<TeamConfig>
    {
        /// <summary>
        /// Gets the name of the team.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description of the team.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the members of the team.
        /// </summary>
        IEnumerable<ITeamMember> Members { get; }

        /// <summary>
        /// Gets the skills of the team.
        /// </summary>
        IEnumerable<ISkill> Skills { get; }
    }
}
