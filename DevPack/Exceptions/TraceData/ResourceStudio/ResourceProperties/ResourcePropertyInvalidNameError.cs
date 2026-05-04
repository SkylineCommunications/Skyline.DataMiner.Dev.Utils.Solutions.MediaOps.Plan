namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a resource property is configured with an invalid name.
	/// </summary>
	public sealed class ResourcePropertyInvalidNameError : ResourcePropertyError
	{
		/// <summary>
		/// Gets the name of the resource property.
		/// </summary>
		public string Name { get; internal set; }
	}
}
