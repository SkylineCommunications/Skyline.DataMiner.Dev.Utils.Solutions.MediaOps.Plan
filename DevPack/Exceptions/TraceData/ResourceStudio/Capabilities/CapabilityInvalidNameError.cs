namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when a capability configuration specifies an invalid capability name.
    /// </summary>
    public class CapabilityInvalidNameError : CapabilityError
    {
        /// <summary>
        /// Gets the name of the capability.
        /// </summary>
        public string Name { get; set; }
    }
}
