namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a resource pool configuration references an invalid or non-existent pool link.
	/// </summary>
	/// <seealso cref="ResourcePoolEmptyPoolLinkError"/>
	/// <seealso cref="ResourcePoolInvalidPoolLinkStateError"/>
	/// <seealso cref="ResourcePoolPoolLinkNotFoundError"/>
	/// <seealso cref="ResourcePoolSelfReferencingPoolLinkError"/>
	public class ResourcePoolInvalidPoolLinkError : ResourcePoolError
	{
	}
}
