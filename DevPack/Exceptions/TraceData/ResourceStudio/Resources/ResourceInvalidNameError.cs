namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a resource configuration specifies an invalid resource name.
	/// </summary>
	public sealed class ResourceInvalidNameError : ResourceError
	{
		/// <summary>
		/// Gets the name of the resource.
		/// </summary>
		public string Name { get; internal set; }
	}
}
