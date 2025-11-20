namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    /// <summary>
    /// Specifies the method by which resources are selected.
    /// </summary>
    /// <remarks>
    /// Use this enumeration to indicate whether resources should be selected automatically by the
    /// system or manually by the user.
    /// </remarks>
    public enum ResourceSelectionType
    {
        /// <summary>
        /// Resources are selected automatically by the system.
        /// </summary>
        Automatic,

        /// <summary>
        /// Resources are selected manually by the user.
        /// </summary>
        Manual,
    }
}
