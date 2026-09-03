namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a relationship object type has an invalid name.
	/// </summary>
	public sealed class RelationshipObjectTypeInvalidNameError : RelationshipObjectTypeError
	{
		/// <summary>
		/// Gets the name of the relationship object type.
		/// </summary>
		public string Name { get; internal set; }
	}
}
