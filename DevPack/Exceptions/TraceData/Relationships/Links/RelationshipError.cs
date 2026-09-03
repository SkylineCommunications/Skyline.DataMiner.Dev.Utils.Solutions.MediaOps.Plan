namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating, updating or deleting a relationship.
	/// </summary>
	public class RelationshipError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the relationship.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
