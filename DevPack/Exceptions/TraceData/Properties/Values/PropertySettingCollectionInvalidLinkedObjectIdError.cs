namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a property setting collection configuration specifies an invalid linked object ID.
	/// </summary>
	public sealed class PropertySettingCollectionInvalidLinkedObjectIdError : PropertySettingCollectionError
	{
		/// <summary>
		/// Gets the linked object ID of the property setting collection.
		/// </summary>
		public string LinkedObjectId { get; internal set; }
	}
}
