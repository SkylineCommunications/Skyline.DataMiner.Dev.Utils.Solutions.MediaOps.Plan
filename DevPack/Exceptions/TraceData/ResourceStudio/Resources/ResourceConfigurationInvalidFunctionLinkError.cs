namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;

    /// <summary>
    /// Represents an error that occurs when a resource configuration contains an invalid function link.
    /// </summary>
    public class ResourceConfigurationInvalidFunctionLinkError : ResourceConfigurationError
    {
        /// <summary>
        /// Gets or sets the function ID associated with the resource link.
        /// </summary>
        public Guid FunctionId { get; set; }
    }
}
