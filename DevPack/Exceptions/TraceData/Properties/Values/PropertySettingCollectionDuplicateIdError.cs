namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a property setting collection configuration contains duplicate identifiers.
	/// </summary>
	/// <remarks>This can only occur when property setting collections with the same ID are provided to a bulk operation.</remarks>
	public sealed class PropertySettingCollectionDuplicateIdError : PropertySettingCollectionError
	{
	}
}
