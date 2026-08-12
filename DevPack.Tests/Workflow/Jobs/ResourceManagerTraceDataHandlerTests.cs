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

	/// <summary>
	/// Deterministic, simulation-backed tests for translating core ResourceManager errors into DevPack job resource errors.
	/// A real Resource Studio resource is created so the handler can resolve the core resource id to its DOM counterpart.
	/// </summary>
	[TestClass]
	public sealed class ResourceManagerTraceDataHandlerTests
	{
		private static (IMediaOpsPlanApi Api, PlanResource Resource) CreateContextWithResource()
		{
			var dms = MediaOpsPlanSimulation.Create();
			var connection = dms.CreateConnection();
			var api = connection.GetMediaOpsPlanApi();

			var prefix = Guid.NewGuid();

			var pool = api.ResourcePools.Create(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = api.Resources.Create(resource);
			resource = api.Resources.Complete(resource);

			return (api, resource);
		}

		[TestMethod]
		public void Translate_QuarantineError_EmitsResourceNotAvailableWithDomResourceId()
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
	}
}
