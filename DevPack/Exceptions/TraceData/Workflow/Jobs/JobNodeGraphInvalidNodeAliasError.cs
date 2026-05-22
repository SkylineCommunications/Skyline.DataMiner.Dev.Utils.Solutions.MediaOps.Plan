namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a node in the node graph of a job has an invalid alias.
	/// </summary>
	public sealed class JobNodeGraphInvalidNodeAliasError : JobNodeGraphInvalidNodeError
	{
		/// <summary>
		/// Gets the alias of the job node.
		/// </summary>
		public string Alias { get; internal set; }
	}
}
