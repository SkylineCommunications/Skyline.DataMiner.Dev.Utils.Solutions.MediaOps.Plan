namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a relationship object type uses a name that is reserved by the MediaOps solution.
	/// </summary>
	public sealed class RelationshipObjectTypeReservedNameError : RelationshipObjectTypeError
	{
		/// <summary>
		/// Gets the name of the relationship object type.
		/// </summary>
		public string Name { get; internal set; }
	}
}
