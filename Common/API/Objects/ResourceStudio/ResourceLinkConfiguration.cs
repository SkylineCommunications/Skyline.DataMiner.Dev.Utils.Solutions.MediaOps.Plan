namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

	/// <summary>
	/// Represents the base configuration for a resource link.
	/// </summary>
	public abstract class ResourceLinkConfiguration
	{
		/// <summary>
		/// Gets or sets the agent ID associated with the resource link.
		/// </summary>
		public int AgentId { get; set; }
	}

	/// <summary>
	/// Represents the configuration for a resource element link.
	/// </summary>
	public class ResourceElementLinkConfiguration : ResourceLinkConfiguration
	{
		/// <summary>
		/// Gets or sets the element ID associated with the resource link.
		/// </summary>
		public int ElementId { get; set; }
	}

	/// <summary>
	/// Represents the configuration for a resource service link.
	/// </summary>
	public class ResourceServiceLinkConfiguration : ResourceLinkConfiguration
	{
		/// <summary>
		/// Gets or sets the service ID associated with the resource link.
		/// </summary>
		public int ServiceId { get; set; }
	}

	/// <summary>
	/// Represents the configuration for a resource virtual function link.
	/// </summary>
	public class ResourceVirtualFunctionLinkConfiguration : ResourceLinkConfiguration
	{
		/// <summary>
		/// Gets or sets the element ID associated with the resource link.
		/// </summary>
		public int ElementId { get; set; }

		/// <summary>
		/// Gets or sets the function ID associated with the resource link.
		/// </summary>
		public Guid FunctionId { get; set; }

		/// <summary>
		/// Gets or sets the function table index associated with the resource link.
		/// </summary>
		public string FunctionTableIndex { get; set; }
	}
}
