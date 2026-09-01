namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a resource pool with invalid configuration.
	/// </summary>
	/// <seealso cref="ResourcePoolCategoryNotFoundError"/>
	/// <seealso cref="ResourcePoolCategoryScopeNotFoundError"/>
	/// <seealso cref="ResourcePoolDuplicateIdError"/>
	/// <seealso cref="ResourcePoolDuplicateNameError"/>
	/// <seealso cref="ResourcePoolIdInUseError"/>
	/// <seealso cref="ResourcePoolInUseError"/>
	/// <seealso cref="ResourcePoolInvalidCapabilitySettingsError"/>
	/// <seealso cref="ResourcePoolInvalidNameError"/>
	/// <seealso cref="ResourcePoolInvalidPoolLinkError"/>
	/// <seealso cref="ResourcePoolInvalidStateError"/>
	/// <seealso cref="ResourcePoolNameExistsError"/>
	/// <seealso cref="ResourcePoolNotFoundError"/>
	/// <seealso cref="ResourcePoolValueAlreadyChangedError"/>
	public class ResourcePoolError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the resource pool.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
