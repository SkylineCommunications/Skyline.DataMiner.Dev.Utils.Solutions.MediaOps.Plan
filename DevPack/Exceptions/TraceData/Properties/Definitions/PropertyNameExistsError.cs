namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a property configuration name already exists.
	/// </summary>
	public sealed class PropertyNameExistsError : PropertyError
	{
		/// <summary>
		/// Gets the name of the property.
		/// </summary>
		public string Name { get; internal set; }
	}
}
