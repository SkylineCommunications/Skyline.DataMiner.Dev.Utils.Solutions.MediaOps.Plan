namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a resource pool configuration contains a duplicate resource pool name.
	/// </summary>
	/// <remarks>This can only occur when resource pools with the same name are provided to a bulk operation.</remarks>
	public class ResourcePoolDuplicateNameError : ResourcePoolError
	{
		/// <summary>
		/// Gets the name of the resource pool.
		/// </summary>
		public string Name { get; set; }
	}
}
