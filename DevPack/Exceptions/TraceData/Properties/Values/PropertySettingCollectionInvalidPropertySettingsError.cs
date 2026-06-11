namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when property setting collection property settings are invalid.
	/// </summary>
	public sealed class PropertySettingCollectionInvalidPropertySettingsError : PropertySettingCollectionError
	{
		/// <summary>
		/// Gets the unique identifier for the property.
		/// </summary>
		public Guid PropertyId { get; internal set; }
	}
}
