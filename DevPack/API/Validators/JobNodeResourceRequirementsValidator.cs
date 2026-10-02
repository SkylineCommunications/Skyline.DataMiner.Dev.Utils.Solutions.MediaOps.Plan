namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	/// <summary>
	/// Validates that the capability and capacity requirements configured on the resource nodes of a job can actually be
	/// met by the resource that is assigned to those nodes.
	/// </summary>
	/// <remarks>
	/// Without this validation the violation is only detected by the Service &amp; Resource Management when the reservation
	/// is booked, which happens after the orchestration settings of the nodes have already been persisted. Validating the
	/// requirements up front keeps the misconfiguration out of the storage and reports it on the job itself.
	/// <para>
	/// Only requirements that carry a literal value are validated. Requirements that carry a reference are resolved later
	/// in the pipeline and are therefore not known here.
	/// </para>
	/// </remarks>
	internal sealed class JobNodeResourceRequirementsValidator : ApiObjectValidator
	{
		private readonly Guid jobId;
		private readonly NodeGraph<JobNode> nodeGraph;
		private readonly IReadOnlyDictionary<Guid, Resource> resourcesById;

		private JobNodeResourceRequirementsValidator(Guid jobId, NodeGraph<JobNode> nodeGraph, IReadOnlyDictionary<Guid, Resource> resourcesById)
		{
			if (jobId == Guid.Empty)
			{
				throw new ArgumentException("Job ID cannot be an empty GUID.", nameof(jobId));
			}

			this.jobId = jobId;
			this.nodeGraph = nodeGraph ?? throw new ArgumentNullException(nameof(nodeGraph));
			this.resourcesById = resourcesById ?? throw new ArgumentNullException(nameof(resourcesById));

			Validate();
		}

		/// <summary>
		/// Validates the resource requirements of every resource node of the specified node graph.
		/// </summary>
		/// <param name="jobId">The unique identifier of the job that owns the node graph.</param>
		/// <param name="nodeGraph">The node graph to validate.</param>
		/// <param name="resourcesById">The resources referenced by the node graph, indexed by their unique identifier.</param>
		/// <returns>The validator holding the trace data of the validation.</returns>
		public static ApiObjectValidator Validate(Guid jobId, NodeGraph<JobNode> nodeGraph, IReadOnlyDictionary<Guid, Resource> resourcesById)
		{
			return new JobNodeResourceRequirementsValidator(jobId, nodeGraph, resourcesById);
		}

		private static string Describe(NodeBase node)
		{
			return InputValidator.IsNonEmptyText(node.Alias) ? $"{node.Alias} ({node.Id})" : node.Id;
		}

		private static string Format(decimal value)
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}

		private static Dictionary<Guid, T> IndexById<T>(IEnumerable<T> settings, Func<T, Guid> idSelector)
		{
			var indexed = new Dictionary<Guid, T>();
			foreach (var setting in settings)
			{
				indexed[idSelector(setting)] = setting;
			}

			return indexed;
		}

		private static bool TryGetBounds(CapacitySetting resourceCapacity, out decimal? minimum, out decimal? maximum)
		{
			if (resourceCapacity.IsRangeCapacity(out var range))
			{
				minimum = range.MinValue;
				maximum = range.MaxValue;
				return minimum.HasValue || maximum.HasValue;
			}

			minimum = null;
			maximum = resourceCapacity.IsNumberCapacity(out var number) ? number.Value : null;
			return maximum.HasValue;
		}

		private void Validate()
		{
			foreach (var node in nodeGraph.Nodes.OfType<JobResourceNode>())
			{
				// A node that references an unknown resource is already reported by the node graph validation.
				if (!resourcesById.TryGetValue(node.ResourceId, out var resource) || node.OrchestrationSettings == null)
				{
					continue;
				}

				ValidateCapabilities(node, resource);
				ValidateCapacities(node, resource);
			}
		}

		private void ValidateCapabilities(JobResourceNode node, Resource resource)
		{
			var resourceCapabilitiesById = IndexById(resource.Capabilities, x => x.Id);

			foreach (var capability in node.OrchestrationSettings.Capabilities)
			{
				// References are resolved later in the pipeline, so their value cannot be validated here.
				if (capability.HasReference || !capability.HasValue)
				{
					continue;
				}

				if (!resourceCapabilitiesById.TryGetValue(capability.Id, out var resourceCapability))
				{
					ReportCapabilityError(resource, capability.Id, $"Capability '{capability.Id}' configured on node '{Describe(node)}' is not assigned to resource '{resource.Name}'.");
					continue;
				}

				if (!resourceCapability.Discretes.Contains(capability.Value))
				{
					var supported = resourceCapability.Discretes.Count == 0
						? "none"
						: String.Join(", ", resourceCapability.Discretes.OrderBy(x => x, StringComparer.Ordinal).Select(x => $"'{x}'"));

					ReportCapabilityError(resource, capability.Id, $"Value '{capability.Value}' configured for capability '{capability.Id}' on node '{Describe(node)}' is not supported by resource '{resource.Name}'. Supported values: {supported}.");
				}
			}
		}

		private void ValidateCapacities(JobResourceNode node, Resource resource)
		{
			var resourceCapacitiesById = IndexById(resource.Capacities, x => x.Id);

			foreach (var capacity in node.OrchestrationSettings.Capacities)
			{
				// References are resolved later in the pipeline, so their value cannot be validated here.
				if (capacity.HasReference || !capacity.HasValue)
				{
					continue;
				}

				if (!resourceCapacitiesById.TryGetValue(capacity.Id, out var resourceCapacity))
				{
					ReportCapacityError(resource, capacity.Id, $"Capacity '{capacity.Id}' configured on node '{Describe(node)}' is not assigned to resource '{resource.Name}'.");
					continue;
				}

				// A resource capacity without bounds does not restrict what can be requested.
				if (!TryGetBounds(resourceCapacity, out var minimum, out var maximum))
				{
					continue;
				}

				if (capacity.IsRangeCapacity(out var requestedRange))
				{
					ValidateRequestedRange(node, resource, requestedRange, minimum, maximum);
				}
				else if (capacity.IsNumberCapacity(out var requestedNumber) && maximum.HasValue && requestedNumber.Value > maximum.Value)
				{
					ReportCapacityError(resource, capacity.Id, $"Quantity {Format(requestedNumber.Value.Value)} configured for capacity '{capacity.Id}' on node '{Describe(node)}' exceeds the maximum of {Format(maximum.Value)} offered by resource '{resource.Name}'.");
				}
			}
		}

		private void ValidateRequestedRange(JobResourceNode node, Resource resource, RangeCapacitySetting requested, decimal? minimum, decimal? maximum)
		{
			if (minimum.HasValue && requested.MinValue < minimum.Value)
			{
				ReportCapacityError(resource, requested.Id, $"Minimum {Format(requested.MinValue.Value)} configured for capacity '{requested.Id}' on node '{Describe(node)}' is below the minimum of {Format(minimum.Value)} offered by resource '{resource.Name}'.");
			}

			if (maximum.HasValue && requested.MaxValue > maximum.Value)
			{
				ReportCapacityError(resource, requested.Id, $"Maximum {Format(requested.MaxValue.Value)} configured for capacity '{requested.Id}' on node '{Describe(node)}' exceeds the maximum of {Format(maximum.Value)} offered by resource '{resource.Name}'.");
			}
		}

		private void ReportCapabilityError(Resource resource, Guid capabilityId, string errorMessage)
		{
			ReportError(jobId, new JobResourceInvalidCapabilityError
			{
				Id = jobId,
				ResourceId = resource.Id,
				CapabilityId = capabilityId,
				ErrorMessage = errorMessage,
			});
		}

		private void ReportCapacityError(Resource resource, Guid capacityId, string errorMessage)
		{
			ReportError(jobId, new JobResourceInvalidCapacityError
			{
				Id = jobId,
				ResourceId = resource.Id,
				CapacityId = capacityId,
				ErrorMessage = errorMessage,
			});
		}
	}
}
