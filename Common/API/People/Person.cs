namespace Skyline.DataMiner.MediaOps.API.Common.API.People
{
    using System;
    using System.Collections.Generic;

    using Skyline.DataMiner.MediaOps.API.Common.API.Generic;
    using Skyline.DataMiner.MediaOps.API.Common.API.Teams;

    /// <summary>
    /// Represents a person in the system.
    /// </summary>
    public class Person : IApiObject
    {
        /// <summary>
        /// Unique ID of the person.
        /// </summary>
        public Guid ID { get; internal set; }

        /// <summary>
        /// Gets the name of the person.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the email address of the person.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets the phone number of the person.
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets the team membership of the person.
        /// </summary>
        public IEnumerable<ITeamMember> Membership { get; internal set; } = new List<ITeamMember>();
    }
}