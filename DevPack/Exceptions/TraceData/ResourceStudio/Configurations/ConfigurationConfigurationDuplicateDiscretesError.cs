namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents an error that occurs when a configuration configuration contains duplicate discrete values.
    /// </summary>
    public class ConfigurationConfigurationDuplicateDiscretesError : ConfigurationConfigurationInvalidDiscretesError
    {
        /// <summary>
        /// Gets or sets the collection of discrete string values.
        /// </summary>
        public List<string> Discretes { get; set; }
    }
}
