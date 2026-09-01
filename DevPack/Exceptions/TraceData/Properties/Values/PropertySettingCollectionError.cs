namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a property setting collection with invalid configuration.
	/// </summary>
	/// <seealso cref="PropertySettingCollectionDuplicateIdError"/>
	/// <seealso cref="PropertySettingCollectionDuplicateLinkedObjectIdAndSubIdError"/>
	/// <seealso cref="PropertySettingCollectionIdInUseError"/>
	/// <seealso cref="PropertySettingCollectionInvalidCustomSettingsError"/>
	/// <seealso cref="PropertySettingCollectionInvalidLinkedObjectIdError"/>
	/// <seealso cref="PropertySettingCollectionInvalidPropertySettingsError"/>
	/// <seealso cref="PropertySettingCollectionInvalidScopeError"/>
	/// <seealso cref="PropertySettingCollectionInvalidStateError"/>
	/// <seealso cref="PropertySettingCollectionNotFoundError"/>
	/// <seealso cref="PropertySettingCollectionValueAlreadyChangedError"/>
	public class PropertySettingCollectionError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the property setting collection.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
