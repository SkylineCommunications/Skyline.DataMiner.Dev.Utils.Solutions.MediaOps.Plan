namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a node in the node graph of a recurring job cannot be swapped to the requested node type.
	/// </summary>
	public sealed class RecurringJobNodeGraphSwapNotAllowedError : RecurringJobNodeGraphInvalidNodeError
	{
		/// <summary>
		/// Gets the identifier of the node that is being swapped to.
		/// </summary>
		public string TargetNodeId { get; internal set; }
	}
}
