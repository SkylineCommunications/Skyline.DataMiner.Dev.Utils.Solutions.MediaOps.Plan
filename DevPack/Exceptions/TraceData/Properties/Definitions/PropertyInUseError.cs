namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when attempting to delete a property that is currently in use.
	/// </summary>
	public sealed class PropertyInUseError : PropertyError
	{
		/// <summary>
		/// Gets or sets the collection of unique identifiers of the collections having the property implemented.
		/// </summary>
		public IReadOnlyCollection<Guid> CollectionIds { get; internal set; } = [];
	}
}
