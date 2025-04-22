namespace Skyline.DataMiner.MediaOps.API.Common.API.People
{
    /// <summary>
    /// The states that people can have.
    /// </summary>
    public enum PeopleStates
    {
        /// <summary>
        /// The person is in draft.
        /// </summary>
        Draft,

        /// <summary>
        /// The person is active.
        /// </summary>
        Active,

        /// <summary>
        /// The person is no longer active. But is kept to ensure all references keep working.
        /// Once deleted references to this person can no longer be resolved.
        /// </summary>
        Deprecated,
    }
}