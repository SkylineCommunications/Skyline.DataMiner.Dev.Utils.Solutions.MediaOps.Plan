namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when a capacity configuration contains duplicate identifiers.
    /// </summary>
    /// <remarks>This can only occur when capacities with the same ID are provided to a bulk operation.</remarks>
    public class CapacityDuplicateIdError : CapacityError
    {
    }
}
