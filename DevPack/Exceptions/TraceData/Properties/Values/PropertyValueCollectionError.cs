namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a property value collection with invalid configuration.
	/// </summary>
	public class PropertyValueCollectionError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the property value collection.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
