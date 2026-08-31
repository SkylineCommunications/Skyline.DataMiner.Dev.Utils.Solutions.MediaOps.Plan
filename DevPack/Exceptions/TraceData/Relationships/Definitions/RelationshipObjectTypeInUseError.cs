namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when attempting to delete a relationship object type that is currently in use.
	/// </summary>
	public sealed class RelationshipObjectTypeInUseError : RelationshipObjectTypeError
	{
		/// <summary>
		/// Gets the collection of unique identifiers of the relationships using the object type.
		/// </summary>
		public IReadOnlyCollection<Guid> RelationshipIds { get; internal set; } = [];
	}
}
