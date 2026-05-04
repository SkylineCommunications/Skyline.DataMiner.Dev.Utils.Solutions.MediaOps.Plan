namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a resource configuration contains a duplicate resource name.
	/// </summary>
	/// <remarks>This can only occur when resources with the same name are provided to a bulk operation.</remarks>
	public sealed class ResourceDuplicateNameError : ResourceError
	{
		/// <summary>
		/// Gets the name of the resource.
		/// </summary>
		public string Name { get; internal set; }
	}
}
