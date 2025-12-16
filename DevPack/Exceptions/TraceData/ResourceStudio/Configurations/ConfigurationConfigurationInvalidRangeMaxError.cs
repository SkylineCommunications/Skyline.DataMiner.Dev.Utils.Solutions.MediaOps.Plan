namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when the specified maximum value for a configuration configuration range is invalid.
    /// </summary>
    public class ConfigurationConfigurationInvalidRangeMaxError : ConfigurationConfigurationError
    {
        /// <summary>
        /// Gets or sets the maximum allowable range value.
        /// </summary>
        public decimal RangeMax { get; set; }
    }
}
