namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a node group of a recurring job references a node that is not part of the node graph.
	/// </summary>
	public sealed class RecurringJobNodeGraphGroupWithInvalidNodeError : RecurringJobNodeGraphError
	{
		/// <summary>
		/// Gets the name of the node group.
		/// </summary>
		public string GroupName { get; internal set; }

		/// <summary>
		/// Gets the unique identifier of the recurring job node.
		/// </summary>
		public string NodeId { get; internal set; }
	}
}
