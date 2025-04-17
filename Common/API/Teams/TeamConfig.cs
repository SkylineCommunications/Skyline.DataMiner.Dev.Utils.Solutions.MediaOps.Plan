namespace Skyline.DataMiner.MediaOps.API.Common.API.Teams
{
    using System;

    using Skyline.DataMiner.MediaOps.API.Common.API.Generic;

    /// <summary>
    /// Represents the configuration of a team in the MediaOps API.
    /// </summary>
    public class TeamConfig : IConfiguration
    {
        /// <summary>
        /// Gets or sets the unique identifier of the team.
        /// </summary>
        public Guid? ObjectId { get; set; }

        /// <summary>
        /// Gets or sets the name of the team.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the team.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets whether the team is bookable.
        /// </summary>
        public bool IsBookable { get; set; }
    }
}
