namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when attempting to delete a resource property that is currently in use.
	/// </summary>
	public class ResourcePropertyInUseError : ResourcePropertyError
	{
		/// <summary>
		/// Gets or sets the collection of unique identifiers of the resources having the resource property implemented.
		/// </summary>
		public List<Guid> ResourceIds { get; set; } = [];
	}
}
