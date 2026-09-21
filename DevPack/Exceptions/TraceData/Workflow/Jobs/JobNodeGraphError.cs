namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when the node graph of a job is invalid.
	/// </summary>
	/// <seealso cref="JobNodeGraphChangedInPostRollNotAllowedError"/>
	/// <seealso cref="JobNodeGraphInvalidGroupNodeError"/>
	/// <seealso cref="JobNodeGraphInvalidConnectionError"/>
	/// <seealso cref="JobNodeGraphInvalidLinkError"/>
	/// <seealso cref="JobNodeGraphInvalidNodeError"/>
	public class JobNodeGraphError : JobError
	{
	}
}
