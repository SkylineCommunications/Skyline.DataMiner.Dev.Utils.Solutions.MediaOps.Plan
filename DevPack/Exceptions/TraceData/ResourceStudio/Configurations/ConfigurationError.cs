namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a configuration with invalid configuration.
	/// </summary>
	/// <seealso cref="ConfigurationDuplicateIdError"/>
	/// <seealso cref="ConfigurationDuplicateNameError"/>
	/// <seealso cref="ConfigurationIdInUseError"/>
	/// <seealso cref="ConfigurationInUseError"/>
	/// <seealso cref="ConfigurationInvalidDecimalsError"/>
	/// <seealso cref="ConfigurationInvalidDefaultDiscreetError"/>
	/// <seealso cref="ConfigurationInvalidDefaultValueError"/>
	/// <seealso cref="ConfigurationInvalidDiscretesError"/>
	/// <seealso cref="ConfigurationInvalidNameError"/>
	/// <seealso cref="ConfigurationInvalidRangeError"/>
	/// <seealso cref="ConfigurationInvalidRangeMaxError"/>
	/// <seealso cref="ConfigurationInvalidRangeMinError"/>
	/// <seealso cref="ConfigurationInvalidStateError"/>
	/// <seealso cref="ConfigurationInvalidStepSizeError"/>
	/// <seealso cref="ConfigurationNameExistsError"/>
	/// <seealso cref="ConfigurationNumberDiscreteValueInUseError"/>
	/// <seealso cref="ConfigurationTextDiscreteValueInUseError"/>
	public class ConfigurationError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the configuration.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
