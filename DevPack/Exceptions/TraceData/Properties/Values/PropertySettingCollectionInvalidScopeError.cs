namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a property setting collection specifies an invalid scope.
	/// </summary>
	public sealed class PropertySettingCollectionInvalidScopeError : PropertySettingCollectionError
	{
		/// <summary>
		/// Gets the scope of the property setting collection.
		/// </summary>
		public string Scope { get; internal set; }
	}
}
