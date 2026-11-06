namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Net.SRM.Capabilities;
	using Skyline.DataMiner.Net.SRM.Capacities;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

	using CoreReservationStatus = Skyline.DataMiner.Net.Messages.ReservationStatus;

	/// <summary>
	/// Reads the core reservation of jobs and translates its resource usage back to the nodes of the job.
	/// </summary>
	internal static class JobBookingReader
	{
		private static readonly CoreReservationStatus[] StatusesUsingResources =
		{
			CoreReservationStatus.Pending,
			CoreReservationStatus.Confirmed,
			CoreReservationStatus.Ongoing,
		};

		public static IReadOnlyDictionary<Guid, ReservationInstance> GetReservationsByJobId(MediaOpsPlanApi planApi, ICollection<Guid> jobIds)
		{
			if (jobIds.Count == 0)
			{
				return new Dictionary<Guid, ReservationInstance>();
			}

			FilterElement<ReservationInstance> Filter(Guid id) => ReservationInstanceExposers.Properties.StringField(CoreJobHandler.JobIdPropertyName).Equal(Convert.ToString(id));

			return planApi.CoreHelpers.ResourceManagerHelper
				.GetReservationInstances(jobIds, Filter)
				.Select(reservation => (Reservation: reservation, JobId: GetJobId(reservation)))
				.Where(item => item.JobId != Guid.Empty)
				.GroupBy(item => item.JobId)
				.ToDictionary(group => group.Key, group => group.First().Reservation);
		}

		public static JobBooking GetBooking(MediaOpsPlanApi planApi, Guid jobId)
		{
			if (!GetReservationsByJobId(planApi, new[] { jobId }).TryGetValue(jobId, out var reservation))
			{
				return null;
			}

			var job = planApi.Jobs.Read(jobId);
			if (job == null)
			{
				return null;
			}

			var nodesByCoreNodeId = job.NodeGraph.Nodes
				.Where(node => node.CoreReservationNodeId.HasValue)
				.GroupBy(node => node.CoreReservationNodeId.Value)
				.ToDictionary(group => group.Key, group => group.First());

			var nodes = new List<JobNodeResourceUsage>();
			foreach (var usage in (reservation.ResourcesInReservationInstance ?? new List<Net.Messages.ResourceUsageDefinition>()).OfType<ServiceResourceUsageDefinition>())
			{
				// A usage of which the node is no longer part of the job cannot be linked to a node.
				if (!nodesByCoreNodeId.TryGetValue(usage.ServiceDefinitionNodeID, out var node) || !node.IsResourceNode(out var resourceNode))
				{
					continue;
				}

				nodes.Add(new JobNodeResourceUsage(
					resourceNode.Id,
					resourceNode.ResourcePoolId,
					resourceNode.ResourceId,
					(usage.RequiredCapabilities ?? new List<ResourceCapabilityUsage>()).Select(ToCapabilitySetting),
					(usage.RequiredCapacities ?? new List<MultiResourceCapacityUsage>()).Select(ToCapacitySetting)));
			}

			return new JobBooking(
				jobId,
				new DateTimeOffset(DateTime.SpecifyKind(reservation.TimeRange.Start, DateTimeKind.Utc)),
				new DateTimeOffset(DateTime.SpecifyKind(reservation.TimeRange.Stop, DateTimeKind.Utc)),
				StatusesUsingResources.Contains(reservation.Status),
				nodes);
		}

		private static Guid GetJobId(ReservationInstance reservation)
		{
			if (reservation.Properties?.Dictionary == null || !reservation.Properties.Dictionary.TryGetValue(CoreJobHandler.JobIdPropertyName, out var value))
			{
				return Guid.Empty;
			}

			return Guid.TryParse(Convert.ToString(value), out var jobId) ? jobId : Guid.Empty;
		}

		private static CapabilitySetting ToCapabilitySetting(ResourceCapabilityUsage usage)
		{
			return new CapabilitySetting(usage.CapabilityProfileID) { Value = usage.RequiredDiscreet ?? usage.RequiredString };
		}

		private static CapacitySetting ToCapacitySetting(MultiResourceCapacityUsage usage)
		{
			if (usage.RangeStart.HasValue)
			{
				return new RangeCapacitySetting(usage.CapacityProfileID)
				{
					MinValue = usage.RangeStart.Value,
					MaxValue = usage.RangeStart.Value + usage.DecimalQuantity,
				};
			}

			return new NumberCapacitySetting(usage.CapacityProfileID) { Value = usage.DecimalQuantity };
		}
	}
}
