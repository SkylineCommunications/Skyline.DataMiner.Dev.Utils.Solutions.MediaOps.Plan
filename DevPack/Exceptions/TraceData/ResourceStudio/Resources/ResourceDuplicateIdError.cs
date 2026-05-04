namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a resources configuration contains duplicate identifiers.
	/// </summary>
	/// <remarks>This can only occur when resources with the same ID are provided to a bulk operation.</remarks>
	public sealed class ResourceDuplicateIdError : ResourceError
	{
	}
}
