namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a property value collection configuration specifies an invalid linked object ID.
	/// </summary>
	public sealed class PropertyValueCollectionInvalidLinkedObjectIdError : PropertyValueCollectionError
	{
		/// <summary>
		/// Gets the linked object ID of the property value collection.
		/// </summary>
		public string LinkedObjectId { get; internal set; }
	}
}
