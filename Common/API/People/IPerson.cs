namespace Skyline.DataMiner.MediaOps.API.Common.API.People
{
    using System.Collections.Generic;

    using Skyline.DataMiner.MediaOps.API.Common.API.Generic;
    using Skyline.DataMiner.MediaOps.API.Common.API.Teams;

    /// <summary>
    /// Represents a person in the system.
    /// </summary>
    public interface IPerson : IApiObject
    {
        /// <summary>
        /// Gets the name of the person.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the email address of the person.
        /// </summary>
        string Email { get; }

        /// <summary>
        /// Gets the phone number of the person.
        /// </summary>
        string PhoneNumber { get; }

        /// <summary>
        /// Gets the team membership of the person.
        /// </summary>
        IEnumerable<ITeamMember> Membership { get; }
    }
}