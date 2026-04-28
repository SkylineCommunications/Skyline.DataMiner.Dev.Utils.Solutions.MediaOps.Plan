namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a property configuration specifies an invalid property scope.
	/// </summary>
	public sealed class PropertyInvalidScopeError : PropertyError
	{
		/// <summary>
		/// Gets the scope of the property.
		/// </summary>
		public string Scope { get; internal set; }
	}
}
