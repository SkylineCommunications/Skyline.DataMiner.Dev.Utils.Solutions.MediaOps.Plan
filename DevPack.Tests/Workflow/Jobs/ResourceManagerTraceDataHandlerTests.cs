namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.ResourceManager.Helpers;
	using Skyline.DataMiner.Net.ResponseErrorData;
	using Skyline.DataMiner.Net.SRM.Capabilities;
	using Skyline.DataMiner.Net.SRM.Capacities;
	using Skyline.DataMiner.Net.SRM.Quarantine;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	using ResourcePool = Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool;
	using PlanResource = Skyline.DataMiner.Solutions.MediaOps.Plan.API.Resource;
	using CoreReservation = Skyline.DataMiner.Net.ResourceManager.Objects.ReservationInstance;

	/// <summary>
	/// Deterministic, simulation-backed tests for translating core ResourceManager errors into DevPack job resource errors.
	/// A real Resource Studio resource is created so the handler can resolve the core resource id to its DOM counterpart.
	/// </summary>
	[TestClass]
	public sealed class ResourceManagerTraceDataHandlerTests
	{
		private static (IMediaOpsPlanApi Api, PlanResource Resource) CreateContextWithResource()
		{
			var (api, resources) = CreateContextWithResources(1);

			return (api, resources[0]);
		}

		private static (IMediaOpsPlanApi Api, IReadOnlyList<PlanResource> Resources) CreateContextWithResources(int count)
		{
			var dms = MediaOpsPlanSimulation.Create();
			var connection = dms.CreateConnection();
			var api = connection.GetMediaOpsPlanApi();

			var prefix = Guid.NewGuid();

			var pool = api.ResourcePools.Create(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = api.ResourcePools.Complete(pool);

			var resources = new List<PlanResource>();
			for (var i = 0; i < count; i++)
			{
				var resource = new UnmanagedResource { Name = $"{prefix}_Resource{i}" }.AssignToPool(pool);
				resource = api.Resources.Create(resource);
				resources.Add(api.Resources.Complete(resource));
			}

			return (api, resources);
		}

		private static ResourceManagerErrorData CreateQuarantineError(params QuarantinedUsagesOnSingleReservation[] impactedReservations)
		{
			// The core software leaves SubjectId empty for this reason; everything is reported through MustBeMovedToQuarantine.
			return new ResourceManagerErrorData(
				ResourceManagerErrorData.Reason.ReservationUpdateCausedReservationsToGoToQuarantine,
				(Guid?)null,
				(Guid?)null,
				new List<Guid>())
			{
				MustBeMovedToQuarantine = impactedReservations.ToList(),
			};
		}

		private static QuarantinedUsagesOnSingleReservation CreateQuarantinedReservation(Guid reservationId, params QuarantinedResourceUsageDefinition[] usages)
		{
			return new QuarantinedUsagesOnSingleReservation
			{
				ReservationInstance = new CoreReservation { ID = reservationId },
				QuarantinedUsages = usages.ToList(),
			};
		}

		private static QuarantinedResourceUsageDefinition CreateQuarantinedUsage(Guid coreResourceId, params Guid[] triggerReservationIds)
		{
			return new QuarantinedResourceUsageDefinition
			{
				QuarantinedResourceUsage = new ResourceUsageDefinition(coreResourceId),
				QuarantineTriggers = triggerReservationIds
					.Select(x => new QuarantineTrigger { ReservationUpdateTrigger = new ReservationDifference { ReservationId = x } })
					.ToList(),
			};
		}

		/// <summary>
		/// Covers use cases 1 and 3, where the core software reports each updated reservation as the one going to quarantine.
		/// Both use cases produce an identical payload.
		/// </summary>
		[TestMethod]
		public void Translate_Quarantine_TriggerIsTheQuarantinedReservation_ReportsResourcePerUpdatedReservation()
		{
			var (api, resources) = CreateContextWithResources(2);

			var jobBReservationId = Guid.NewGuid();
			var jobCReservationId = Guid.NewGuid();

			var error = CreateQuarantineError(
				CreateQuarantinedReservation(jobBReservationId, CreateQuarantinedUsage(resources[0].CoreResourceId, jobBReservationId)),
				CreateQuarantinedReservation(jobCReservationId, CreateQuarantinedUsage(resources[1].CoreResourceId, jobCReservationId)));

			var handler = new ResourceManagerTraceDataHandler((MediaOpsPlanApi)api);

			var result = handler.Translate(new[] { error });

			CollectionAssert.AreEquivalent(
				new[] { jobBReservationId, jobCReservationId },
				result.Keys.ToArray(),
				"Expected the translated errors to be keyed by the reservation being updated.");

			Assert.AreEqual(
				resources[0].Id,
				result[jobBReservationId].ErrorData.OfType<JobResourceNotAvailableError>().Single().ResourceId,
				"Expected the DOM resource id to be reported.");
			Assert.AreEqual(
				resources[1].Id,
				result[jobCReservationId].ErrorData.OfType<JobResourceNotAvailableError>().Single().ResourceId,
				"Expected the DOM resource id to be reported.");
		}

		/// <summary>
		/// Covers use case 2, where the core software reports a single overlapping reservation that is not being updated.
		/// The unavailable resources still have to be reported on the reservations that are being updated.
		/// </summary>
		[TestMethod]
		public void Translate_Quarantine_TriggerIsAnotherReservation_ReportsResourcePerUpdatedReservation()
		{
			var (api, resources) = CreateContextWithResources(2);

			var jobAReservationId = Guid.NewGuid();
			var jobBReservationId = Guid.NewGuid();
			var jobCReservationId = Guid.NewGuid();

			var error = CreateQuarantineError(
				CreateQuarantinedReservation(
					jobAReservationId,
					CreateQuarantinedUsage(resources[0].CoreResourceId, jobBReservationId),
					CreateQuarantinedUsage(resources[1].CoreResourceId, jobCReservationId)));

			var handler = new ResourceManagerTraceDataHandler((MediaOpsPlanApi)api);

			var result = handler.Translate(new[] { error });

			CollectionAssert.AreEquivalent(
				new[] { jobBReservationId, jobCReservationId },
				result.Keys.ToArray(),
				"Expected the translated errors to be keyed by the reservations being updated, not by the quarantined one.");

			Assert.AreEqual(
				resources[0].Id,
				result[jobBReservationId].ErrorData.OfType<JobResourceNotAvailableError>().Single().ResourceId,
				"Expected the DOM resource id to be reported.");
			Assert.AreEqual(
				resources[1].Id,
				result[jobCReservationId].ErrorData.OfType<JobResourceNotAvailableError>().Single().ResourceId,
				"Expected the DOM resource id to be reported.");
		}

		[TestMethod]
		public void Translate_QuarantineErrorWithoutTrigger_FallsBackToSubjectId()
		{
			var (api, resource) = CreateContextWithResource();
			var reservationId = Guid.NewGuid();

			var error = new ResourceManagerErrorData(
				ResourceManagerErrorData.Reason.ReservationUpdateCausedReservationsToGoToQuarantine,
				reservationId,
				(Guid?)null,
				new List<Guid>())
			{
				MustBeMovedToQuarantine = new List<QuarantinedUsagesOnSingleReservation>
				{
					new QuarantinedUsagesOnSingleReservation
					{
						QuarantinedUsages = new List<QuarantinedResourceUsageDefinition>
						{
							new QuarantinedResourceUsageDefinition
							{
								QuarantinedResourceUsage = new ResourceUsageDefinition(resource.CoreResourceId),
							},
						},
					},
				},
			};

			var handler = new ResourceManagerTraceDataHandler((MediaOpsPlanApi)api);

			var result = handler.Translate(new[] { error });

			Assert.IsTrue(result.ContainsKey(reservationId), "Expected the translated errors to be keyed by the reservation id.");
			var notAvailable = result[reservationId].ErrorData.OfType<JobResourceNotAvailableError>().Single();
			Assert.AreEqual(resource.Id, notAvailable.ResourceId, "Expected the DOM resource id to be reported.");
		}

		[TestMethod]
		public void Translate_ResourceCapacityInvalid_EmitsInvalidCapacityWithIds()
		{
			var (api, resource) = CreateContextWithResource();
			var reservationId = Guid.NewGuid();
			var capacityId = Guid.NewGuid();

			var error = new ResourceManagerErrorData(
				ResourceManagerErrorData.Reason.ResourceCapacityInvalid,
				reservationId,
				resource.CoreResourceId,
				new MultiResourceCapacityUsage { CapacityProfileID = capacityId });

			var handler = new ResourceManagerTraceDataHandler((MediaOpsPlanApi)api);

			var result = handler.Translate(new[] { error });

			Assert.IsTrue(result.ContainsKey(reservationId), "Expected the translated errors to be keyed by the reservation id.");
			var capacityError = result[reservationId].ErrorData.OfType<JobResourceInvalidCapacityError>().Single();
			Assert.AreEqual(resource.Id, capacityError.ResourceId, "Expected the DOM resource id to be reported.");
			Assert.AreEqual(capacityId, capacityError.CapacityId, "Expected the capacity profile id to be reported.");
		}

		[TestMethod]
		public void Translate_ResourceCapabilityInvalid_EmitsInvalidCapabilityWithIds()
		{
			var (api, resource) = CreateContextWithResource();
			var reservationId = Guid.NewGuid();
			var capabilityId = Guid.NewGuid();

			var error = new ResourceManagerErrorData(
				ResourceManagerErrorData.Reason.ResourceCapabilityInvalid,
				reservationId,
				resource.CoreResourceId,
				new ResourceCapabilityUsage { CapabilityProfileID = capabilityId },
				"Capability not available.");

			var handler = new ResourceManagerTraceDataHandler((MediaOpsPlanApi)api);

			var result = handler.Translate(new[] { error });

			Assert.IsTrue(result.ContainsKey(reservationId), "Expected the translated errors to be keyed by the reservation id.");
			var capabilityError = result[reservationId].ErrorData.OfType<JobResourceInvalidCapabilityError>().Single();
			Assert.AreEqual(resource.Id, capabilityError.ResourceId, "Expected the DOM resource id to be reported.");
			Assert.AreEqual(capabilityId, capabilityError.CapabilityId, "Expected the capability profile id to be reported.");
		}

		[TestMethod]
		public void Translate_UncategorizedError_FallsBackToRawMessage()
		{
			var (api, _) = CreateContextWithResource();
			var reservationId = Guid.NewGuid();

			var error = new ResourceManagerErrorData(
				ResourceManagerErrorData.Reason.UnknownError,
				reservationId,
				(Guid?)null,
				new List<Guid>());

			var handler = new ResourceManagerTraceDataHandler((MediaOpsPlanApi)api);

			var result = handler.Translate(new[] { error });

			Assert.IsTrue(result.ContainsKey(reservationId), "Expected the raw fallback to be keyed by the reservation id.");
			var errorData = result[reservationId].ErrorData.ToList();
			Assert.IsFalse(
				errorData.OfType<JobResourceError>().Any(),
				"Expected no typed job resource error for an uncategorized reason.");
			Assert.IsTrue(
				errorData.Any(x => x.GetType() == typeof(MediaOpsErrorData)),
				"Expected a raw MediaOpsErrorData fallback for an uncategorized reason.");
		}

		[TestMethod]
		public void Translate_KnownAndUnknownErrors_MapsKnownAndAddsRawForUnknown()
		{
			var (api, resource) = CreateContextWithResource();
			var reservationId = Guid.NewGuid();
			var capacityId = Guid.NewGuid();

			var capacityError = new ResourceManagerErrorData(
				ResourceManagerErrorData.Reason.ResourceCapacityInvalid,
				reservationId,
				resource.CoreResourceId,
				new MultiResourceCapacityUsage { CapacityProfileID = capacityId });

			var unknownError = new ResourceManagerErrorData(
				ResourceManagerErrorData.Reason.UnknownError,
				reservationId,
				(Guid?)null,
				new List<Guid>());

			var handler = new ResourceManagerTraceDataHandler((MediaOpsPlanApi)api);

			var result = handler.Translate(new[] { capacityError, unknownError });

			Assert.IsTrue(result.ContainsKey(reservationId), "Expected the translated errors to be keyed by the reservation id.");
			var errorData = result[reservationId].ErrorData.ToList();
			Assert.AreEqual(
				capacityId,
				errorData.OfType<JobResourceInvalidCapacityError>().Single().CapacityId,
				"Expected the known capacity error to be translated.");
			Assert.IsTrue(
				errorData.Any(x => x.GetType() == typeof(MediaOpsErrorData)),
				"Expected the unknown error to be added as a raw default alongside the translated one.");
		}
		[TestMethod]
		public void Translate_WhenCalledMultipleTimes_DoesNotReturnPreviousResults()
		{
			var (api, resource) = CreateContextWithResource();
			var handler = new ResourceManagerTraceDataHandler((MediaOpsPlanApi)api);

			var reservationId1 = Guid.NewGuid();
			var reservationId2 = Guid.NewGuid();

			var first = new ResourceManagerErrorData(
				ResourceManagerErrorData.Reason.ResourceCapacityInvalid,
				reservationId1,
				resource.CoreResourceId,
				new MultiResourceCapacityUsage { CapacityProfileID = Guid.NewGuid() });

			var second = new ResourceManagerErrorData(
				ResourceManagerErrorData.Reason.UnknownError,
				reservationId2,
				(Guid?)null,
				new List<Guid>());

			Assert.IsTrue(handler.Translate(new[] { first }).ContainsKey(reservationId1));

			var secondResult = handler.Translate(new[] { second });
			Assert.IsFalse(secondResult.ContainsKey(reservationId1), "Expected previous translation output not to leak into subsequent calls.");
			Assert.IsTrue(secondResult.ContainsKey(reservationId2));
		}
	}
}
