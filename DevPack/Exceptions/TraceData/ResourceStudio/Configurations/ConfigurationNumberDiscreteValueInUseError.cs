namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

    /// <summary>
    /// Represents an error that occurs when attempting to delete a configuration Number discrete value that is currently in use.
    /// </summary>
    public class ConfigurationNumberDiscreteValueInUseError : ConfigurationError
    {
        /// <summary>
        /// The discrete value that is in use.
        /// </summary>
        public NumberDiscreet DiscreteValue { get; set; }
    }
}
