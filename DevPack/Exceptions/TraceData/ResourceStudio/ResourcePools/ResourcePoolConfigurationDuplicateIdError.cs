namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when a resource pools configuration contains duplicate identifiers.
    /// </summary>
    /// <remarks>This can only occur when resource pools with the same ID are provided to a bulk operation.</remarks>
    public class ResourcePoolConfigurationDuplicateIdError : ResourcePoolConfigurationError
    {
    }
}
