namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating, updating or deleting a relationship object type.
	/// </summary>
	public class RelationshipObjectTypeError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the relationship object type.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
