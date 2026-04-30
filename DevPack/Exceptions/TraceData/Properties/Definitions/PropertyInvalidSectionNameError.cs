namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a property configuration specifies an invalid property section name.
	/// </summary>
	public sealed class PropertyInvalidSectionNameError : PropertyError
	{

		/// <summary>
		/// Gets the section name of the property.
		/// </summary>
		public string Name { get; internal set; }
	}
}
