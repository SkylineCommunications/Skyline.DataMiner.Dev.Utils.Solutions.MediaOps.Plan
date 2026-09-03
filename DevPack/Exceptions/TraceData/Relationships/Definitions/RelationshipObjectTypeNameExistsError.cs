namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a relationship object type with the same name already exists.
	/// </summary>
	public sealed class RelationshipObjectTypeNameExistsError : RelationshipObjectTypeError
	{
		/// <summary>
		/// Gets the name of the relationship object type.
		/// </summary>
		public string Name { get; internal set; }
	}
}
