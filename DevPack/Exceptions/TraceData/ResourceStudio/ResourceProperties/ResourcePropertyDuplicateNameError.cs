namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a resource property configuration has a duplicate name.
	/// </summary>
	/// <remarks>This can only occur when resource properties with the same name are provided to a bulk operation.</remarks>
	public sealed class ResourcePropertyDuplicateNameError : ResourcePropertyError
	{
		/// <summary>
		/// Gets the name of the resource property.
		/// </summary>
		public string Name { get; internal set; }
	}
}
