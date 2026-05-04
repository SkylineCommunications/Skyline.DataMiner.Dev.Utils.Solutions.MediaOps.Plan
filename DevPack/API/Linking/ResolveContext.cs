namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.ServiceManager.Objects;

	/// <summary>
	/// Provides contextual information used while resolving <see cref="DataReference"/> instances
	/// through a <see cref="LinkResolver"/>.
	/// </summary>
	public sealed class ResolveContext
    {
        /// <summary>Gets a shared empty context, useful when no contextual information is required.</summary>
        public static ResolveContext Empty { get; } = new ResolveContext();

		/// <summary>
		/// Gets or sets the workflow associated with this instance.
		/// </summary>
		public Workflow Workflow { get; set; }

		/// <summary>
		/// Gets or sets the job associated with this instance.
		/// </summary>
		public Job Job { get; set; }

		/// <summary>
		/// Gets or sets a collection that maps node identifiers to their associated resources.
		/// </summary>
		public IDictionary<string, Resource> ResourcesByNode { get; set; }

		/// <summary>
		/// Gets or sets the collection of capabilities.
		/// </summary>
		public IDictionary<Guid, Capability> Capabilities { get; set; }

		/// <summary>
		/// Gets or sets the collection of capacities.
		/// </summary>
		public IDictionary<Guid, Capacity> Capacities { get; set; }

		/// <summary>
		/// Gets or sets the collection of configurations.
		/// </summary>
		public IDictionary<Guid, Configuration> Configurations { get; set; }

		/// <summary>
		/// Gets or sets the collection of resource properties.
		/// </summary>
		public IDictionary<Guid, ResourceProperty> ResourceProperties { get; set; }

		/// <summary>
		/// Gets or sets the collection of workflow properties.
		/// </summary>
		public IDictionary<Guid, Property> WorkflowProperties { get; set; }

		/// <summary>
		/// Gets or sets the collection of workflow property values, indexed by property identifier.
		/// </summary>
		public IDictionary<Guid, string> WorkflowPropertyValues { get; set; }

		/// <summary>
		/// Gets or sets a collection that maps node identifiers to their associated orchestration settings.
		/// </summary>
		public IDictionary<string, OrchestrationSettings> OrchestrationSettingsByNode { get; set; }
    }
}
