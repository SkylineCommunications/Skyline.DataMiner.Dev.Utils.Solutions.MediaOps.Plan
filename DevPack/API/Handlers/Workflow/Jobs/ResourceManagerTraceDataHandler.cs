namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.ManagerStore.Select;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResponseErrorData;
	using Skyline.DataMiner.Net.SRM.Quarantine;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

	using CoreReservation = Net.ResourceManager.Objects.ReservationInstance;

	// Translates core ResourceManager errors into DevPack job resource errors keyed per reservation (SubjectId). Only the
	// resource, capacity and capability ids are surfaced; the consumer is responsible for resolving them to names.
	internal sealed class ResourceManagerTraceDataHandler : ITraceDataHandler<ResourceManagerErrorData>
	{
		private readonly MediaOpsPlanApi planApi;

		private readonly Dictionary<Guid, MediaOpsTraceData> traceDataPerReservationId = new Dictionary<Guid, MediaOpsTraceData>();

		public ResourceManagerTraceDataHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		public IReadOnlyDictionary<Guid, MediaOpsTraceData> Translate(ICollection<ResourceManagerErrorData> resourceManagerErrors)
		{
			if (resourceManagerErrors == null)
			{
				throw new ArgumentNullException(nameof(resourceManagerErrors));
			}

			traceDataPerReservationId.Clear();

			if (resourceManagerErrors.Count == 0)
			{
				return traceDataPerReservationId;
			}

			var reservationUpdateCausedReservationsToGoToQuarantineErrors = resourceManagerErrors.Where(x => x.ErrorReason == ResourceManagerErrorData.Reason.ReservationUpdateCausedReservationsToGoToQuarantine).ToList();
			var resourceCapacityInvalidErrors = resourceManagerErrors.Where(x => x.ErrorReason == ResourceManagerErrorData.Reason.ResourceCapacityInvalid).ToList();
			var resourceCapabilityInvalidErrors = resourceManagerErrors.Where(x => x.ErrorReason == ResourceManagerErrorData.Reason.ResourceCapabilityInvalid).ToList();

			// The DevPack surfaces resources by their Resource Studio (DOM) id, so the core resource ids in the errors are
			// resolved to their DOM counterparts in a batched query (one or more backend calls, depending on filter size).
			var domResourceIdByCoreId = BuildDomResourceIdByCoreId(
				reservationUpdateCausedReservationsToGoToQuarantineErrors,
				resourceCapacityInvalidErrors,
				resourceCapabilityInvalidErrors);

			HandleReservationUpdateCausedReservationsToGoToQuarantine(reservationUpdateCausedReservationsToGoToQuarantineErrors, domResourceIdByCoreId);
			HandleResourceCapacityInvalid(resourceCapacityInvalidErrors, domResourceIdByCoreId);
			HandleResourceCapabilityInvalid(resourceCapabilityInvalidErrors, domResourceIdByCoreId);

			// Errors that are not one of the known types are added to their reservation's trace data as raw defaults.
			AddDefaultTraceData(GetUnknownErrors(resourceManagerErrors));

			return traceDataPerReservationId;
		}

		private static List<ResourceManagerErrorData> GetUnknownErrors(ICollection<ResourceManagerErrorData> resourceManagerErrors)
		{
			return resourceManagerErrors
				.Where(x => x.ErrorReason != ResourceManagerErrorData.Reason.ReservationUpdateCausedReservationsToGoToQuarantine
					&& x.ErrorReason != ResourceManagerErrorData.Reason.ResourceCapacityInvalid
					&& x.ErrorReason != ResourceManagerErrorData.Reason.ResourceCapabilityInvalid)
				.ToList();
		}

		private Dictionary<Guid, Guid> BuildDomResourceIdByCoreId(
			IEnumerable<ResourceManagerErrorData> quarantineErrors,
			IEnumerable<ResourceManagerErrorData> capacityErrors,
			IEnumerable<ResourceManagerErrorData> capabilityErrors)
		{
			var coreResourceIds = new HashSet<Guid>();

			foreach (var error in quarantineErrors)
			{
				foreach (var coreResourceId in GetQuarantinedCoreResourceIds(error))
				{
					coreResourceIds.Add(coreResourceId);
				}
			}

			foreach (var error in capacityErrors.Concat(capabilityErrors))
			{
				if (error.ResourceId.HasValue)
				{
					coreResourceIds.Add(error.ResourceId.Value);
				}
			}

			if (coreResourceIds.Count == 0)
			{
				return new Dictionary<Guid, Guid>();
			}

			var selectedFields = new SelectedFields<DomInstance>()
				.Add(SlcResource_StudioIds.Sections.ResourceInternalProperties.Resource_Id);

			FilterElement<DomInstance> Filter(Guid coreResourceId) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.Resource_Id).Equal(coreResourceId));

			var domResourceIdByCoreId = new Dictionary<Guid, Guid>();
			foreach (var partialObject in planApi.DomHelpers.SlcResourceStudioHelper.GetResourceStudioFields(coreResourceIds, Filter, selectedFields))
			{
				if (!partialObject.TryGetValue<Guid>(SlcResource_StudioIds.Sections.ResourceInternalProperties.Resource_Id, out var coreId)
					|| coreId == Guid.Empty)
				{
					continue;
				}

				domResourceIdByCoreId[coreId] = partialObject.Id.Id;
			}

			return domResourceIdByCoreId;
		}

		private void HandleReservationUpdateCausedReservationsToGoToQuarantine(
			IReadOnlyCollection<ResourceManagerErrorData> quarantineErrors,
			IReadOnlyDictionary<Guid, Guid> domResourceIdByCoreId)
		{
			foreach (var error in quarantineErrors)
			{
				// The same resource can be reported by multiple quarantined reservations for the same reservation update.
				var emitted = new HashSet<Tuple<Guid, Guid>>();

				foreach (var quarantinedUsagesOnReservation in error.MustBeMovedToQuarantine)
				{
					foreach (var usage in quarantinedUsagesOnReservation.QuarantinedUsages ?? new List<QuarantinedResourceUsageDefinition>())
					{
						var coreResourceId = usage.QuarantinedResourceUsage.GUID;
						if (!domResourceIdByCoreId.TryGetValue(coreResourceId, out var domResourceId))
						{
							planApi.Logger.Error(this, $"Could not resolve a Resource Studio resource for core resource {coreResourceId}.");
							continue;
						}

						foreach (var reservationId in GetTraceDataKeys(error, quarantinedUsagesOnReservation.ReservationInstance, usage))
						{
							if (!emitted.Add(Tuple.Create(reservationId, domResourceId)))
							{
								continue;
							}

							GetOrCreateTraceData(reservationId).Add(new JobResourceNotAvailableError
							{
								ResourceId = domResourceId,
							});
						}
					}
				}

				if (emitted.Count > 0)
				{
					continue;
				}

				var fallbackKeys = GetFallbackKeys(error).ToList();
				if (fallbackKeys.Count == 0)
				{
					planApi.Logger.Error(this, $"Error with reason {error.ErrorReason} could not be linked to a reservation. Error message: {error.Message}");
					continue;
				}

				foreach (var reservationId in fallbackKeys)
				{
					AddRawFallback(GetOrCreateTraceData(reservationId), error);
				}
			}
		}

		// The quarantine is reported against the reservation update that triggered it, which is the reservation the DevPack is
		// creating or updating. SubjectId is not filled in for this error reason, so it is only used as a fallback.
		private IEnumerable<Guid> GetTraceDataKeys(ResourceManagerErrorData error, CoreReservation impactedReservation, QuarantinedResourceUsageDefinition usage)
		{
			var triggerReservationIds = (usage.QuarantineTriggers ?? new List<QuarantineTrigger>())
				.Where(x => x.ReservationUpdateTrigger != null && x.ReservationUpdateTrigger.ReservationId != Guid.Empty)
				.Select(x => x.ReservationUpdateTrigger.ReservationId)
				.Distinct()
				.ToList();

			if (triggerReservationIds.Count > 0)
			{
				return triggerReservationIds;
			}

			planApi.Logger.Error(this, $"Error with reason {error.ErrorReason} has no reservation update trigger. Falling back to the subject or impacted reservation.");

			return GetFallbackKeys(error, impactedReservation);
		}

		private IEnumerable<Guid> GetFallbackKeys(ResourceManagerErrorData error)
		{
			return error.MustBeMovedToQuarantine
				.Select(x => x.ReservationInstance)
				.DefaultIfEmpty(null)
				.SelectMany(x => GetFallbackKeys(error, x))
				.Distinct();
		}

		private IEnumerable<Guid> GetFallbackKeys(ResourceManagerErrorData error, CoreReservation impactedReservation)
		{
			var subjectId = error.SubjectId.GetValueOrDefault();
			if (subjectId != Guid.Empty)
			{
				yield return subjectId;
				yield break;
			}

			var impactedReservationId = impactedReservation?.ID ?? Guid.Empty;
			if (impactedReservationId != Guid.Empty)
			{
				yield return impactedReservationId;
			}
		}

		private void HandleResourceCapacityInvalid(
			IReadOnlyCollection<ResourceManagerErrorData> capacityErrors,
			IReadOnlyDictionary<Guid, Guid> domResourceIdByCoreId)
		{
			foreach (var error in capacityErrors)
			{
				if (!TryGetReservationId(error, out var reservationId))
				{
					continue;
				}

				var traceData = GetOrCreateTraceData(reservationId);

				if (!TryGetDomResourceId(error, domResourceIdByCoreId, out var domResourceId))
				{
					AddRawFallback(traceData, error);
					continue;
				}

				traceData.Add(new JobResourceInvalidCapacityError
				{
					ErrorMessage = error.Message,
					ResourceId = domResourceId,
					CapacityId = error.ResourceCapacityUsage.CapacityProfileID,
				});
			}
		}

		private void HandleResourceCapabilityInvalid(
			IReadOnlyCollection<ResourceManagerErrorData> capabilityErrors,
			IReadOnlyDictionary<Guid, Guid> domResourceIdByCoreId)
		{
			foreach (var error in capabilityErrors)
			{
				if (!TryGetReservationId(error, out var reservationId))
				{
					continue;
				}

				var traceData = GetOrCreateTraceData(reservationId);

				if (!TryGetDomResourceId(error, domResourceIdByCoreId, out var domResourceId))
				{
					AddRawFallback(traceData, error);
					continue;
				}

				traceData.Add(new JobResourceInvalidCapabilityError
				{
					ErrorMessage = error.Message,
					ResourceId = domResourceId,
					CapabilityId = error.ResourceCapabilityUsage.CapabilityProfileID,
				});
			}
		}

		private static IEnumerable<Guid> GetQuarantinedCoreResourceIds(ResourceManagerErrorData error)
		{
			return error.MustBeMovedToQuarantine
				.SelectMany(x => x.QuarantinedUsages)
				.Select(x => x.QuarantinedResourceUsage.GUID);
		}

		private bool TryGetDomResourceId(ResourceManagerErrorData error, IReadOnlyDictionary<Guid, Guid> domResourceIdByCoreId, out Guid domResourceId)
		{
			domResourceId = Guid.Empty;

			if (!error.ResourceId.HasValue || !domResourceIdByCoreId.TryGetValue(error.ResourceId.Value, out domResourceId))
			{
				planApi.Logger.Error(this, $"Could not resolve a Resource Studio resource for core resource {error.ResourceId}.");
				return false;
			}

			return true;
		}

		private bool TryGetReservationId(ResourceManagerErrorData error, out Guid reservationId)
		{
			reservationId = error.SubjectId.GetValueOrDefault();
			if (reservationId == Guid.Empty)
			{
				planApi.Logger.Error(this, $"Error with reason {error.ErrorReason} has empty SubjectId. This should not happen. Error message: {error.Message}");
				return false;
			}

			return true;
		}

		private void AddRawFallback(MediaOpsTraceData traceData, ResourceManagerErrorData error)
		{
			planApi.Logger.Error(this, $"Falling back to the raw error data (ToString) for an error with reason {error.ErrorReason}.");
			traceData.Add(new MediaOpsErrorData { ErrorMessage = error.ToString() });
		}

		private MediaOpsTraceData GetOrCreateTraceData(Guid id)
		{
			if (!traceDataPerReservationId.TryGetValue(id, out var traceData))
			{
				traceData = new MediaOpsTraceData();
				traceDataPerReservationId[id] = traceData;
			}

			return traceData;
		}

		private void AddDefaultTraceData(ICollection<ResourceManagerErrorData> resourceManagerErrors)
		{
			foreach (var error in resourceManagerErrors)
			{
				if (!TryGetReservationId(error, out var reservationId))
				{
					continue;
				}

				GetOrCreateTraceData(reservationId).Add(new MediaOpsErrorData
				{
					ErrorMessage = error.ToString(),
				});
			}
		}
	}
}
