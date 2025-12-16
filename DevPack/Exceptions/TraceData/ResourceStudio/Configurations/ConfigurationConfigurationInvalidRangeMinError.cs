namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when the specified minimum value for a configuration configuration range is invalid.
    /// </summary>
    public class ConfigurationConfigurationInvalidRangeMinError : ConfigurationConfigurationError
    {
        /// <summary>
        /// Gets or sets the minimum allowable range value.
        /// </summary>
        public decimal RangeMin { get; set; }
    }
}
