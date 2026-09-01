namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a capability with invalid configuration.
	/// </summary>
	/// <seealso cref="CapabilityDiscreteValueInUseError"/>
	/// <seealso cref="CapabilityDuplicateIdError"/>
	/// <seealso cref="CapabilityDuplicateNameError"/>
	/// <seealso cref="CapabilityIdInUseError"/>
	/// <seealso cref="CapabilityInUseError"/>
	/// <seealso cref="CapabilityInvalidDiscretesError"/>
	/// <seealso cref="CapabilityInvalidNameError"/>
	/// <seealso cref="CapabilityInvalidStateError"/>
	/// <seealso cref="CapabilityInvalidTimeDependencyError"/>
	/// <seealso cref="CapabilityNameExistsError"/>
	public class CapabilityError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the capability.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
