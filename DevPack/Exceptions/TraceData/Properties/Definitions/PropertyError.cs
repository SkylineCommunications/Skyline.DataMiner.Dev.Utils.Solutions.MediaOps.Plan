namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a property with invalid configuration.
	/// </summary>
	public class PropertyError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the property.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
