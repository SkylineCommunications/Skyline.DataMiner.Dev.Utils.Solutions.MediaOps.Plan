namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a property with invalid configuration.
	/// </summary>
	/// <seealso cref="PropertyDuplicateIdError"/>
	/// <seealso cref="PropertyDuplicateNameError"/>
	/// <seealso cref="PropertyIdInUseError"/>
	/// <seealso cref="PropertyInUseError"/>
	/// <seealso cref="PropertyInvalidDiscretesError"/>
	/// <seealso cref="PropertyInvalidFileSizeLimitError"/>
	/// <seealso cref="PropertyInvalidNameError"/>
	/// <seealso cref="PropertyInvalidScopeError"/>
	/// <seealso cref="PropertyInvalidSectionNameError"/>
	/// <seealso cref="PropertyInvalidStateError"/>
	/// <seealso cref="PropertyInvalidStringDefaultValueError"/>
	/// <seealso cref="PropertyInvalidStringSizeLimitError"/>
	/// <seealso cref="PropertyNameExistsError"/>
	/// <seealso cref="PropertyNotFoundError"/>
	/// <seealso cref="PropertyValueAlreadyChangedError"/>
	public class PropertyError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the property.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
