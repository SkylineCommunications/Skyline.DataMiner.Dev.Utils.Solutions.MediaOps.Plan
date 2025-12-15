namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when a capability configuration contains duplicate identifiers.
    /// </summary>
    /// <remarks>This can only occur when capabilities with the same ID are provided to a bulk operation.</remarks>
    public class CapabilityConfigurationDuplicateIdError : CapabilityConfigurationError
    {
    }
}
