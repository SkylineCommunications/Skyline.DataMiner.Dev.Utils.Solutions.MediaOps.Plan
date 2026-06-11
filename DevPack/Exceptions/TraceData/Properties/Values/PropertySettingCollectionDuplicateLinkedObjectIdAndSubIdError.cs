namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a property setting collection has a combination of <see cref="LinkedObjectId"/> and <see cref="SubId"/> that is already used by another property setting collection.
	/// </summary>
	public sealed class PropertySettingCollectionDuplicateLinkedObjectIdAndSubIdError : PropertySettingCollectionError
	{
		/// <summary>
		/// Gets the linked object ID of the property value collection.
		/// </summary>
		public string LinkedObjectId { get; internal set; }

		/// <summary>
		/// Gets the sub-identifier of the property value collection.
		/// </summary>
		public string SubId { get; internal set; }
	}
}
