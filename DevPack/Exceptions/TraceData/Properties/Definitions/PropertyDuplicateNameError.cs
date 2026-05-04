namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a property configuration contains a duplicate property name.
	/// </summary>
	/// <remarks>This can only occur when properties with the same name are provided to a bulk operation.</remarks>
	public sealed class PropertyDuplicateNameError : PropertyError
	{
		/// <summary>
		/// Gets the name of the property.
		/// </summary>
		public string Name { get; internal set; }
	}
}
