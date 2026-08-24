namespace RT_MediaOps.Plan.Storage.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.ResourceManager;
	using Skyline.DataMiner.Net.ResourceManager.Helpers;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Net.ResponseErrorData;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

	/// <summary>
	/// Tests for the batched reservation operations, using a stubbed Agent response so a refused reservation can be
	/// reproduced deterministically.
	/// </summary>
	[TestClass]
	public sealed class ReservationInstanceBatchTests
	{
		[TestMethod]
		public void TryCreateOrUpdateReservationInstances_ReservationPersisted_ReportsSuccess()
		{
			var reservation = CreateReservation();

			var helper = CreateHelper(message => CreateResponse(message.ResourceManagerObjects, new TraceData()));

			var success = helper.TryCreateOrUpdateReservationInstancesInBatches(new[] { reservation }, out var result);

			Assert.IsTrue(success, "Expected the operation to succeed when the Agent persisted the reservation.");
			CollectionAssert.AreEquivalent(new[] { reservation.ID }, result.SuccessfulIds.ToArray(), "Expected the persisted reservation to be reported as successful.");
		}

		/// <summary>
		/// The core software refuses a reservation that would exceed the concurrency of a resource, but reports the
		/// conflict on the overlapping booking that would have to go to quarantine. That booking is not necessarily
		/// part of the request, so the error cannot always be linked to the refused reservation.
		/// </summary>
		[TestMethod]
		public void TryCreateOrUpdateReservationInstances_ReservationRefusedWithErrorOnAnotherReservation_ReportsFailure()
		{
			var reservation = CreateReservation();
			var overlappingReservationId = Guid.NewGuid();

			var helper = CreateHelper(message => CreateResponse(
				new List<ReservationInstance>(),
				CreateQuarantineTraceData(overlappingReservationId)));

			var success = helper.TryCreateOrUpdateReservationInstancesInBatches(new[] { reservation }, out var result);

			Assert.IsFalse(success, "Expected the operation to fail when the Agent did not persist the reservation.");
			CollectionAssert.Contains(result.UnsuccessfulIds.ToArray(), reservation.ID, "Expected the refused reservation to be reported as unsuccessful.");
			Assert.AreEqual(0, result.SuccessfulItems.Count, "Expected no reservation to be reported as successful.");
			Assert.IsTrue(result.TraceDataPerItem.ContainsKey(reservation.ID), "Expected trace data to be reported for the refused reservation.");
		}

		/// <summary>
		/// The trace data of the last call is kept on the <see cref="ResourceManagerHelper"/> itself, so a concurrent
		/// call on the same helper can overwrite the errors of a refused reservation. The reservations returned by the
		/// Agent must therefore remain the source of truth.
		/// </summary>
		[TestMethod]
		public void TryCreateOrUpdateReservationInstances_ReservationRefusedWithoutErrors_ReportsFailure()
		{
			var reservation = CreateReservation();

			var helper = CreateHelper(message => CreateResponse(new List<ReservationInstance>(), new TraceData()));

			var success = helper.TryCreateOrUpdateReservationInstancesInBatches(new[] { reservation }, out var result);

			Assert.IsFalse(success, "Expected the operation to fail when the Agent did not persist the reservation.");
			CollectionAssert.Contains(result.UnsuccessfulIds.ToArray(), reservation.ID, "Expected the refused reservation to be reported as unsuccessful.");
			Assert.AreEqual(0, result.SuccessfulItems.Count, "Expected no reservation to be reported as successful.");
			Assert.IsTrue(result.TraceDataPerItem.ContainsKey(reservation.ID), "Expected trace data to be reported for the refused reservation.");
		}

		[TestMethod]
		public void TryCreateOrUpdateReservationInstances_OneOfTwoReservationsRefused_ReportsOnlyRefusedAsUnsuccessful()
		{
			var persistedReservation = CreateReservation();
			var refusedReservation = CreateReservation();

			var helper = CreateHelper(message => CreateResponse(
				message.ResourceManagerObjects.Where(x => x.ID == persistedReservation.ID).ToList(),
				CreateQuarantineTraceData(Guid.NewGuid())));

			var success = helper.TryCreateOrUpdateReservationInstancesInBatches(new[] { persistedReservation, refusedReservation }, out var result);

			Assert.IsFalse(success, "Expected the operation to fail when one of the reservations was not persisted.");
			CollectionAssert.Contains(result.UnsuccessfulIds.ToArray(), refusedReservation.ID, "Expected the refused reservation to be reported as unsuccessful.");
			CollectionAssert.DoesNotContain(result.UnsuccessfulIds.ToArray(), persistedReservation.ID, "Expected the persisted reservation not to be reported as unsuccessful.");
			CollectionAssert.AreEquivalent(new[] { persistedReservation.ID }, result.SuccessfulIds.ToArray(), "Expected the persisted reservation to be reported as successful.");
		}

		private static ResourceManagerHelper CreateHelper(Func<SetReservationInstanceMessage, ResourceManagerResponseMessage> handler)
		{
			return new ResourceManagerHelper(message =>
			{
				if (message is SetReservationInstanceMessage setMessage)
				{
					return handler(setMessage);
				}

				throw new InvalidOperationException($"Unexpected message of type {message.GetType().Name}.");
			});
		}

		private static ResourceManagerResponseMessage CreateResponse(IEnumerable<ReservationInstance> reservationInstances, TraceData traceData)
		{
			return new ResourceManagerResponseMessage
			{
				Success = true,
				ReservationInstances = reservationInstances.ToList(),
				TraceData = traceData,
			};
		}

		private static TraceData CreateQuarantineTraceData(Guid impactedReservationId)
		{
			var traceData = new TraceData();
			traceData.Add(new ResourceManagerErrorData(ResourceManagerErrorData.Reason.ReservationUpdateCausedReservationsToGoToQuarantine, impactedReservationId)
			{
				Message = "The reservation cannot be saved because an overlapping reservation would go to quarantine.",
				MustBeMovedToQuarantine = new List<QuarantinedUsagesOnSingleReservation>(),
			});

			return traceData;
		}

		private static ReservationInstance CreateReservation()
		{
			var start = DateTime.UtcNow.AddHours(1);

			return new ReservationInstance(new Skyline.DataMiner.Net.Time.TimeRangeUtc(start, start.AddHours(1)))
			{
				ID = Guid.NewGuid(),
				Name = "Reservation",
				Status = ReservationStatus.Pending,
			};
		}
	}
}
