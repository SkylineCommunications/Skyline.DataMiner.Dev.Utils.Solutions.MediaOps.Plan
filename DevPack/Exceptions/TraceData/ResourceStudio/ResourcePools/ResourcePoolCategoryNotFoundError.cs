namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when no category can be found in the system with a given ID.
	/// </summary>
	public sealed class ResourcePoolCategoryNotFoundError : ResourcePoolError
	{
		/// <summary>
		/// Gets the unique identifier for the category.
		/// </summary>
		public string CategoryId { get; internal set; }
	}
}
