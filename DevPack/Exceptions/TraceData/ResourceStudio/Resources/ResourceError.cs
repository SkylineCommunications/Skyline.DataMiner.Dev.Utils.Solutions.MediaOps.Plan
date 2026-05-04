namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a resource with invalid configuration.
	/// </summary>
	public class ResourceError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the resource.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
