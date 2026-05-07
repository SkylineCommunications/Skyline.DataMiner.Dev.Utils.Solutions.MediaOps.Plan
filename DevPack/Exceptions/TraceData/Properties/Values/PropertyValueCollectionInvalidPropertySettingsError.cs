namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when property value collection property settings are invalid.
	/// </summary>
	public sealed class PropertyValueCollectionInvalidPropertySettingsError : PropertyValueCollectionError
	{
		/// <summary>
		/// Gets the unique identifier for the property.
		/// </summary>
		public Guid PropertyId { get; internal set; }
	}
}
