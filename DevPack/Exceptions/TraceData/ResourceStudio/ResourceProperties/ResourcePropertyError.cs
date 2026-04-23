namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a resource property with invalid configuration.
	/// </summary>
	public class ResourcePropertyError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the resource property.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
