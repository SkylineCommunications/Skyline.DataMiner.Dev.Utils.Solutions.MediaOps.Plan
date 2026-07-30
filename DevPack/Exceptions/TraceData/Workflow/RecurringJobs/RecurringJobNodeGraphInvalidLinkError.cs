namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a parent-child link in the node graph of a recurring job is invalid.
	/// </summary>
	public sealed class RecurringJobNodeGraphInvalidLinkError : RecurringJobNodeGraphError
	{
		/// <summary>
		/// Gets the unique identifier of the parent recurring job node.
		/// </summary>
		public string ParentNodeId { get; internal set; }

		/// <summary>
		/// Gets the unique identifier of the child recurring job node.
		/// </summary>
		public string ChildNodeId { get; internal set; }
	}
}
