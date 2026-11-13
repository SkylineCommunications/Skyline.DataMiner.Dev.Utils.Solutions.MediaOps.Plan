namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when linking a virtual function resource to an element would exceed the maximum number of
	/// instances the function definition allows on a single element.
	/// </summary>
	/// <remarks>This check only applies to functions without entry points; table-based functions are bounded by their table rows.</remarks>
	public sealed class ResourceFunctionMaxInstancesExceededError : ResourceError
	{
		/// <summary>
		/// Gets the agent ID associated with the resource link.
		/// </summary>
		public int AgentId { get; internal set; }

		/// <summary>
		/// Gets the element ID associated with the resource link.
		/// </summary>
		public int ElementId { get; internal set; }

		/// <summary>
		/// Gets the function ID associated with the resource link.
		/// </summary>
		public Guid FunctionId { get; internal set; }

		/// <summary>
		/// Gets the maximum number of instances the function definition allows on a single element.
		/// </summary>
		public int MaxInstances { get; internal set; }
	}
}
