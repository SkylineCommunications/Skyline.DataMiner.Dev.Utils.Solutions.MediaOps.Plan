namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a reference to the linked object ID of a resource.
	/// </summary>
	public sealed class ResourceLinkedObjectIdReference : DataReference
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ResourceLinkedObjectIdReference"/> class.
		/// </summary>
		/// <param name="nodeId">
		/// Optional identifier of the workflow node whose resource linked object ID is referenced.
		/// When <see langword="null"/> the reference targets the resource of the current node.
		/// </param>
		public ResourceLinkedObjectIdReference(string nodeId = null) : base(DataReferenceType.ResourceLinkedObjectID, nodeId)
		{
		}
	}
}
