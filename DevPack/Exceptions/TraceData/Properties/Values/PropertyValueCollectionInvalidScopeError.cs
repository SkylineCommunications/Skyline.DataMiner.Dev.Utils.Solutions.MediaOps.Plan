namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a property value collection specifies an invalid scope.
	/// </summary>
	public sealed class PropertyValueCollectionInvalidScopeError : PropertyValueCollectionError
	{
		/// <summary>
		/// Gets the scope of the property value collection.
		/// </summary>
		public string Scope { get; internal set; }
	}
}
