namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a property configuration specifies an invalid property name.
	/// </summary>
	public sealed class PropertyInvalidNameError : PropertyError
	{
		/// <summary>
		/// Gets the name of the property.
		/// </summary>
		public string Name { get; internal set; }
	}
}
