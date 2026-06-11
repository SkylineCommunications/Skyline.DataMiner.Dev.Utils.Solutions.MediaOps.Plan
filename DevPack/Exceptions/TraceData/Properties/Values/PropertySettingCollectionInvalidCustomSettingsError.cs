namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when property setting collection custom settings are invalid.
	/// </summary>
	public sealed class PropertySettingCollectionInvalidCustomSettingsError : PropertySettingCollectionError
	{
		/// <summary>
		/// Gets the name of the custom setting.
		/// </summary>
		public string Name { get; internal set; }
	}
}
