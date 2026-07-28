namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a node in the node graph of a recurring job has an invalid alias.
	/// </summary>
	public sealed class RecurringJobNodeGraphInvalidNodeAliasError : RecurringJobNodeGraphInvalidNodeError
	{
		/// <summary>
		/// Gets the alias of the recurring job node.
		/// </summary>
		public string Alias { get; internal set; }
	}
}
