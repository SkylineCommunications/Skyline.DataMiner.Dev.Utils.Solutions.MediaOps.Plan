namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Represents the resource a job node uses and the capabilities and capacities it requires from it.
	/// </summary>
	public sealed class JobNodeResourceUsage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JobNodeResourceUsage"/> class.
		/// </summary>
		/// <param name="nodeId">The identifier of the node.</param>
		/// <param name="resourcePoolId">The identifier of the resource pool of the node.</param>
		/// <param name="resourceId">The identifier of the resource, or <see cref="Guid.Empty"/> when no resource is assigned.</param>
		/// <param name="capabilities">The required capabilities, each with a value.</param>
		/// <param name="capacities">The required capacities, each with a value.</param>
		public JobNodeResourceUsage(string nodeId, Guid resourcePoolId, Guid resourceId, IEnumerable<CapabilitySetting> capabilities, IEnumerable<CapacitySetting> capacities)
		{
			NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
			ResourcePoolId = resourcePoolId;
			ResourceId = resourceId;
			Capabilities = (capabilities ?? Enumerable.Empty<CapabilitySetting>()).ToList().AsReadOnly();
			Capacities = (capacities ?? Enumerable.Empty<CapacitySetting>()).ToList().AsReadOnly();
		}

		/// <summary>
		/// Gets the identifier of the node.
		/// </summary>
		public string NodeId { get; }

		/// <summary>
		/// Gets the identifier of the resource pool of the node.
		/// </summary>
		public Guid ResourcePoolId { get; }

		/// <summary>
		/// Gets the identifier of the resource, or <see cref="Guid.Empty"/> when no resource is assigned.
		/// </summary>
		public Guid ResourceId { get; }

		/// <summary>
		/// Gets the required capabilities, each with a value.
		/// </summary>
		public IReadOnlyCollection<CapabilitySetting> Capabilities { get; }

		/// <summary>
		/// Gets the required capacities, each with a value.
		/// </summary>
		public IReadOnlyCollection<CapacitySetting> Capacities { get; }
	}
}
