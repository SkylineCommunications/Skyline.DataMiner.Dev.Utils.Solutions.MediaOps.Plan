namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when property value collection custom settings are invalid.
	/// </summary>
	public sealed class PropertyValueCollectionInvalidCustomSettingsError : PropertyValueCollectionError
	{
		/// <summary>
		/// Gets the name of the custom setting.
		/// </summary>
		public string Name { get; internal set; }
	}
}
