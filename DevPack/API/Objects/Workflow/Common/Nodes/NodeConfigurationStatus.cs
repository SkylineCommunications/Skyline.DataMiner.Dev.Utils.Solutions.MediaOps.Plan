namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    /// <summary>
    /// Defines the configuration status of nodes.
    /// </summary>
    public enum NodeConfigurationStatus
    {
		/// <summary>
		/// The configuration status of the node is unknown.
		/// </summary>
		Unknown = -1,

		/// <summary>
		/// No configuration values are needed for this node.
		/// </summary>
		NoValuesNeeded = 0,

        /// <summary>
        /// Mandatory configuration values are missing.
        /// </summary>
        MandatoryValuesMissing = 1,

        /// <summary>
        /// Non-mandatory configuration values are missing.
        /// </summary>
        NonMandatoryValuesMissing = 2,

        /// <summary>
        /// All required configuration values have been provided.
        /// </summary>
        AllValuesProvided = 3,

        /// <summary>
        /// No parameters are defined for this node.
        /// </summary>
        NoParametersDefined = 4,
    }
}
