namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a capacity with invalid configuration.
	/// </summary>
	/// <seealso cref="CapacityDuplicateIdError"/>
	/// <seealso cref="CapacityDuplicateNameError"/>
	/// <seealso cref="CapacityIdInUseError"/>
	/// <seealso cref="CapacityInUseError"/>
	/// <seealso cref="CapacityInvalidDecimalsError"/>
	/// <seealso cref="CapacityInvalidNameError"/>
	/// <seealso cref="CapacityInvalidRangeError"/>
	/// <seealso cref="CapacityInvalidRangeMaxError"/>
	/// <seealso cref="CapacityInvalidRangeMinError"/>
	/// <seealso cref="CapacityInvalidStateError"/>
	/// <seealso cref="CapacityInvalidStepSizeError"/>
	/// <seealso cref="CapacityNameExistsError"/>
	public class CapacityError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the capacity.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
