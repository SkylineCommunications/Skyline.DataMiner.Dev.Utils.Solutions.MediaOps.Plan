namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a resource with invalid configuration.
	/// </summary>
	/// <seealso cref="ResourceDuplicateIdError"/>
	/// <seealso cref="ResourceDuplicateNameError"/>
	/// <seealso cref="ResourceDuplicateTableIndexLinkError"/>
	/// <seealso cref="ResourceIdInUseError"/>
	/// <seealso cref="ResourceInUseError"/>
	/// <seealso cref="ResourceInvalidAssignedPoolError"/>
	/// <seealso cref="ResourceInvalidCapabilitySettingsError"/>
	/// <seealso cref="ResourceInvalidCapacitySettingsError"/>
	/// <seealso cref="ResourceInvalidConcurrencyError"/>
	/// <seealso cref="ResourceInvalidElementLinkError"/>
	/// <seealso cref="ResourceInvalidFunctionLinkError"/>
	/// <seealso cref="ResourceInvalidNameError"/>
	/// <seealso cref="ResourceInvalidPropertySettingsError"/>
	/// <seealso cref="ResourceInvalidServiceLinkError"/>
	/// <seealso cref="ResourceInvalidStateError"/>
	/// <seealso cref="ResourceInvalidTableIndexLinkError"/>
	/// <seealso cref="ResourceInvalidVirtualSignalGroupError"/>
	/// <seealso cref="ResourceNameExistsError"/>
	/// <seealso cref="ResourceNotFoundError"/>
	/// <seealso cref="ResourceValueAlreadyChangedError"/>
	public class ResourceError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the resource.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
