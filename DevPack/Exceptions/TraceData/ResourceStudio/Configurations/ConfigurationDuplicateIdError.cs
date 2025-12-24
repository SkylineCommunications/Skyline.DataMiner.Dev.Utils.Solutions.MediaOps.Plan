namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when a configurations configuration contains duplicate identifiers.
    /// </summary>
    /// <remarks>This can only occur when configurations with the same ID are provided to a bulk operation.</remarks>
    public class ConfigurationDuplicateIdError : ConfigurationError
    {
    }
}
