namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when the same relationship object type name is provided more than once in a single operation.
	/// </summary>
	public sealed class RelationshipObjectTypeDuplicateNameError : RelationshipObjectTypeError
	{
		/// <summary>
		/// Gets the name of the relationship object type.
		/// </summary>
		public string Name { get; internal set; }
	}
}
