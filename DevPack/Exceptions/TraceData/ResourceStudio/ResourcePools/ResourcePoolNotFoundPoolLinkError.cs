namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	/// <summary>
	/// Represents an error that occurs when a resource pool configuration references a pool link that does not exist.
	/// </summary>
	public class ResourcePoolNotFoundPoolLinkError : ResourcePoolInvalidPoolLinkError
	{
		/// <summary>
		/// Gets the unique identifier of the linked resource pool.
		/// </summary>
		public Guid LinkedResourcePoolId { get; internal set; }
	}
}
