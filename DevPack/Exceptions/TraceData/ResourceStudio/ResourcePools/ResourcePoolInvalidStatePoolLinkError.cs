namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a resource pool configuration references a pool link that has an invalid state.
	/// </summary>
	public class ResourcePoolInvalidStatePoolLinkError : ResourcePoolInvalidPoolLinkError
	{
		/// <summary>
		/// Gets the unique identifier of the linked resource pool.
		/// </summary>
		public Guid LinkedResourcePoolId { get; set; }
	}
}
