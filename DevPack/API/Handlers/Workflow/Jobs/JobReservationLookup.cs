namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

	/// <summary>
	/// Looks up the core reservations of jobs, which hold the job ID in the 'Job ID' property.
	/// </summary>
	internal static class JobReservationLookup
	{
		/// <summary>
		/// Gets the reservations per job ID. A job normally has one reservation; jobs without a reservation are not included.
		/// </summary>
		public static IReadOnlyDictionary<Guid, IReadOnlyList<ReservationInstance>> GetReservationsByJobId(MediaOpsPlanApi planApi, IEnumerable<Guid> jobIds)
		{
			if (planApi == null)
			{
				throw new ArgumentNullException(nameof(planApi));
			}

			if (jobIds == null)
			{
				throw new ArgumentNullException(nameof(jobIds));
			}

			var ids = jobIds.Where(x => x != Guid.Empty).Distinct().ToList();
			if (ids.Count == 0)
			{
				return new Dictionary<Guid, IReadOnlyList<ReservationInstance>>();
			}

			FilterElement<ReservationInstance> Filter(Guid id) => ReservationInstanceExposers.Properties.StringField(CoreJobHandler.JobIdPropertyName).Equal(Convert.ToString(id));

			return planApi.CoreHelpers.ResourceManagerHelper
				.GetReservationInstances(ids, Filter)
				.Select(reservation => (Reservation: reservation, JobId: GetJobId(reservation)))
				.Where(item => item.JobId != Guid.Empty)
				.GroupBy(item => item.JobId)
				.ToDictionary(group => group.Key, group => (IReadOnlyList<ReservationInstance>)group.Select(item => item.Reservation).ToList());
		}

		private static Guid GetJobId(ReservationInstance reservation)
		{
			if (reservation.Properties?.Dictionary == null || !reservation.Properties.Dictionary.TryGetValue(CoreJobHandler.JobIdPropertyName, out var value))
			{
				return Guid.Empty;
			}

			return Guid.TryParse(Convert.ToString(value), out var jobId) ? jobId : Guid.Empty;
		}
	}
}
