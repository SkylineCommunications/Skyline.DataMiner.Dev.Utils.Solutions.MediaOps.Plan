namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a resource pool with invalid configuration.
	/// </summary>
	public class ResourcePoolError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the resource pool.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
