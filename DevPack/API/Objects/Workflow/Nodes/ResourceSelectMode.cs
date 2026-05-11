namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    /// <summary>
    /// Defines how resources are selected for nodes in job instances.
    /// </summary>
    public enum ResourceSelectMode
    {
        /// <summary>
        /// Resource is selected manually during job setup.
        /// </summary>
        Manual = 0,

        /// <summary>
        /// Resource is automatically selected at booking time.
        /// </summary>
        AutoSelectAtBooking = 1,

        /// <summary>
        /// Resource is automatically selected at runtime.
        /// </summary>
        AutoSelectAtRuntime = 2,
    }
}
