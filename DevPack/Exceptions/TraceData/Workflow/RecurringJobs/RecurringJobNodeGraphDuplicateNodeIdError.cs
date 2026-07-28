namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when multiple nodes in the node graph of a recurring job share the same ID.
	/// </summary>
	public sealed class RecurringJobNodeGraphDuplicateNodeIdError : RecurringJobNodeGraphInvalidNodeError
	{
	}
}
