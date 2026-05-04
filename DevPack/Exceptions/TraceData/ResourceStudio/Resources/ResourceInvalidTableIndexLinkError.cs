namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a resource configuration contains an invalid table index link.
	/// </summary>
	public sealed class ResourceInvalidTableIndexLinkError : ResourceError
	{
		/// <summary>
		/// Gets or sets the agent ID associated with the resource link.
		/// </summary>
		public int AgentId { get; internal set; }

		/// <summary>
		/// Gets or sets the element ID associated with the resource link.
		/// </summary>
		public int ElementId { get; internal set; }

		/// <summary>
		/// Gets or sets the function ID associated with the resource link.
		/// </summary>
		public Guid FunctionId { get; internal set; }

		/// <summary>
		/// Gets or sets the function table index associated with the resource link.
		/// </summary>
		public string FunctionTableIndex { get; internal set; }
	}
}
