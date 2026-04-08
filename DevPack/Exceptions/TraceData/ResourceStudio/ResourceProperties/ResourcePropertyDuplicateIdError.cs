namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when multiple resource properties are configured with the same identifier.
	/// </summary>
	/// <remarks>This can only occur when resource properties with the same ID are provided to a bulk operation.</remarks>
	public class ResourcePropertyDuplicateIdError : ResourcePropertyError
	{
	}
}
