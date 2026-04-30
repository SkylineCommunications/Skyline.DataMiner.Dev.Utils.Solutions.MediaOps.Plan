namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a reference to the name of a resource.
	/// </summary>
	public sealed class ResourceNameReference : DataReference
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ResourceNameReference"/> class.
		/// </summary>
		/// <param name="nodeId">
		/// Optional identifier of the workflow node whose resource name is referenced.
		/// When <see langword="null"/> the reference targets the resource of the current node.
		/// </param>
		public ResourceNameReference(string nodeId = null) : base(DataReferenceType.ResourceName, nodeId)
		{
		}
	}
}
