namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when attempting to delete a capability discrete value that is currently in use.
    /// </summary>
    public class CapabilityDiscreteValueInUseError : CapabilityError
    {
        /// <summary>
        /// The discrete value that is in use.
        /// </summary>
        public string DiscreteValue { get; set; }
    }
}
