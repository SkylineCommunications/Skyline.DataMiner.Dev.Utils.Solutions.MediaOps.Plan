namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a resource pool node in the node graph of a recurring job is invalid.
	/// </summary>
	public sealed class RecurringJobNodeGraphInvalidResourcePoolNodeError : RecurringJobNodeGraphInvalidNodeError
	{
		/// <summary>
		/// Gets the unique identifier of the resource pool.
		/// </summary>
		public Guid ResourcePoolId { get; internal set; }
	}
}
