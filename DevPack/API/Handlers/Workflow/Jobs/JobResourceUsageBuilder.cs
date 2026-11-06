namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Determines the resource usage of the nodes of a job as it is booked when the job is saved, following the rules of the
	/// core reservation synchronization: ended nodes are not booked, references are resolved and range capacities are only
	/// booked with literal values.
	/// </summary>
	internal static class JobResourceUsageBuilder
	{
		public static IReadOnlyCollection<JobNodeResourceUsage> Build(IMediaOpsPlanApi planApi, Job job, DateTimeOffset now)
		{
			if (planApi == null)
			{
				throw new ArgumentNullException(nameof(planApi));
			}

			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			var resolver = new Lazy<JobReferenceResolver>(() => new JobReferenceResolver(planApi, job));
			var result = new List<JobNodeResourceUsage>();

			foreach (var node in job.NodeGraph.Nodes)
			{
				Guid resourcePoolId;
				var resourceId = Guid.Empty;
				if (node.IsResourceNode(out var resourceNode))
				{
					resourcePoolId = resourceNode.ResourcePoolId;
					resourceId = resourceNode.ResourceId;
				}
				else if (node.IsResourcePoolNode(out var resourcePoolNode))
				{
					resourcePoolId = resourcePoolNode.ResourcePoolId;
				}
				else
				{
					continue;
				}

				if (GetNodeEnd(job, node) <= now)
				{
					continue;
				}

				var settings = node.OrchestrationSettings;
				result.Add(new JobNodeResourceUsage(
					node.Id,
					resourcePoolId,
					resourceId,
					BuildCapabilities(settings?.Capabilities, node.Id, resolver),
					BuildCapacities(settings?.Capacities, node.Id, resolver)));
			}

			return result.AsReadOnly();
		}

		// Scheduled jobs align all node times with the job window when they are saved; an unsaved node has no end yet.
		private static DateTimeOffset GetNodeEnd(Job job, JobNode node)
		{
			var followsJobWindow = job.State == JobState.Draft || job.State == JobState.Tentative || job.State == JobState.Confirmed;
			return followsJobWindow || node.End == default ? job.PostRollEnd : node.End;
		}

		private static IEnumerable<CapabilitySetting> BuildCapabilities(IEnumerable<CapabilitySetting> capabilities, string nodeId, Lazy<JobReferenceResolver> resolver)
		{
			foreach (var capability in capabilities ?? Enumerable.Empty<CapabilitySetting>())
			{
				if (capability.HasValue)
				{
					yield return new CapabilitySetting(capability.Id) { Value = capability.Value };
					continue;
				}

				// A capability option has no separate display name, so it is booked on the display value of the referenced value.
				if (TryResolve(capability, nodeId, resolver, out var resolvedValue) && !String.IsNullOrEmpty(resolvedValue.DisplayValue))
				{
					yield return new CapabilitySetting(capability.Id) { Value = resolvedValue.DisplayValue };
				}
			}
		}

		private static IEnumerable<CapacitySetting> BuildCapacities(IEnumerable<CapacitySetting> capacities, string nodeId, Lazy<JobReferenceResolver> resolver)
		{
			foreach (var capacity in capacities ?? Enumerable.Empty<CapacitySetting>())
			{
				switch (capacity)
				{
					case NumberCapacitySetting numberCapacity when numberCapacity.Value.HasValue:
						yield return new NumberCapacitySetting(numberCapacity.Id) { Value = numberCapacity.Value };
						break;

					case NumberCapacitySetting numberCapacity:
						if (TryResolve(numberCapacity, nodeId, resolver, out var resolvedValue) && ResolvedValueConverter.TryGetNumber(resolvedValue, out var quantity))
						{
							yield return new NumberCapacitySetting(numberCapacity.Id) { Value = quantity };
						}

						break;

					case RangeCapacitySetting rangeCapacity when rangeCapacity.HasValue:
						yield return new RangeCapacitySetting(rangeCapacity.Id) { MinValue = rangeCapacity.MinValue, MaxValue = rangeCapacity.MaxValue };
						break;
				}
			}
		}

		private static bool TryResolve(Setting setting, string nodeId, Lazy<JobReferenceResolver> resolver, out ResolvedValue resolvedValue)
		{
			resolvedValue = null;
			if (!setting.HasReference)
			{
				return false;
			}

			try
			{
				resolvedValue = resolver.Value.ResolveValue(setting.Reference, nodeId);
			}
			catch (CircularReferenceException)
			{
				return false;
			}

			return resolvedValue != null && resolvedValue.IsResolved;
		}
	}
}
