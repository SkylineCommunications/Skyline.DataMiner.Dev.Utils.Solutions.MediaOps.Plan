namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    /// <summary>
    /// Defines the current selection state of resources for nodes in job instances.
    /// </summary>
    public enum ResourceSelectionState
    {
		/// <summary>
		/// The resource selection state is unknown.
		/// 
		/// </summary>
		Unknown = -1,

		/// <summary>
		/// Resource has been selected.
		/// </summary>
		Selected = 0,

        /// <summary>
        /// Resource is pending automatic selection.
        /// </summary>
        PendingAutoSelection = 1,

        /// <summary>
        /// Resource is pending manual selection.
        /// </summary>
        PendingManualSelection = 2,

        /// <summary>
        /// Resource has been requested.
        /// </summary>
        RequestedResource = 3,

        /// <summary>
        /// An error occurred during resource selection.
        /// </summary>
        Error = 4,
    }
}
