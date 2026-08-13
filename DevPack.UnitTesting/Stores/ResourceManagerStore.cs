namespace Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Stores
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.ResourceManager;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Net.ResponseErrorData;
	using Skyline.DataMiner.Net.SRM.Capabilities;
	using Skyline.DataMiner.Net.SRM.Capacities;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	/// <summary>
	/// In-memory store that handles Resource Manager messages (resources, resource pools and
	/// reservation instances), mirroring how a real DataMiner Agent would respond.
	/// </summary>
	internal sealed class ResourceManagerStore
	{
		private readonly ConcurrentDictionary<Guid, Resource> _resources = new ConcurrentDictionary<Guid, Resource>();
		private readonly ConcurrentDictionary<Guid, ResourcePool> _resourcePools = new ConcurrentDictionary<Guid, ResourcePool>();
		private readonly ConcurrentDictionary<Guid, ReservationInstance> _reservationInstances = new ConcurrentDictionary<Guid, ReservationInstance>();
		private readonly ConcurrentDictionary<PagingCookie, InMemoryPagingHandler<ReservationInstance>> _reservationPagingHandlers = new ConcurrentDictionary<PagingCookie, InMemoryPagingHandler<ReservationInstance>>();

		public bool TryHandleMessage(DMSMessage message, out DMSMessage response)
		{
			switch (message)
			{
				#region Resources

				case GetResourceMessage request:
					{
						IEnumerable<Resource> result;

						if (request.Filter != null)
						{
							result = _resources.Values.Where(request.Filter.getLambda());
						}
						else if (request.ResourceManagerObjects != null && request.ResourceManagerObjects.Count > 0)
						{
							var ids = new HashSet<Guid>(request.ResourceManagerObjects.Select(x => x.GUID));
							result = _resources.Values.Where(x => ids.Contains(x.GUID));
						}
						else
						{
							result = _resources.Values;
						}

						response = new ResourceResponseMessage(result.ToArray()) { Success = true };
						return true;
					}

				case GetEligibleResourcesMessage request:
					{
						response = HandleGetEligibleResources(request);
						return true;
					}

				case SetResourceMessage request:
					{
						var objects = request.ResourceManagerObjects ?? new List<Resource>();

						foreach (var resource in objects)
						{
							if (request.isDelete)
							{
								_resources.TryRemove(resource.GUID, out _);
							}
							else
							{
								// A real DataMiner Agent provisions the DVE row for a function resource and
								// assigns its primary key. Mirror that by assigning a primary key so callers
								// that enable the DVE afterwards have a valid key to work with.
								if (resource is FunctionResource functionResource && String.IsNullOrWhiteSpace(functionResource.PK))
								{
									functionResource.PK = functionResource.GUID.ToString();
								}

								_resources[resource.GUID] = resource;
							}
						}

						response = new ResourceResponseMessage(objects.ToArray()) { Success = true };
						return true;
					}

				#endregion

				#region Resource pools

				case GetResourcePoolMessage request:
					{
						IEnumerable<ResourcePool> result;

						if (request.ResourceManagerObjects != null && request.ResourceManagerObjects.Count > 0)
						{
							var ids = new HashSet<Guid>(request.ResourceManagerObjects.Select(x => x.GUID));
							result = _resourcePools.Values.Where(x => ids.Contains(x.GUID));
						}
						else
						{
							result = _resourcePools.Values;
						}

						response = new ResourcePoolResponseMessage(result.ToArray()) { Success = true };
						return true;
					}

				case SetResourcePoolMessage request:
					{
						var objects = request.ResourceManagerObjects ?? new List<ResourcePool>();

						foreach (var pool in objects)
						{
							if (request.isDelete)
							{
								_resourcePools.TryRemove(pool.GUID, out _);
							}
							else
							{
								_resourcePools[pool.GUID] = pool;
							}
						}

						response = new ResourcePoolResponseMessage(objects.ToArray()) { Success = true };
						return true;
					}

				#endregion

				#region Reservation instances (dedicated set message)

				case SetReservationInstanceMessage request:
					{
						var objects = request.ResourceManagerObjects ?? new List<ReservationInstance>();
						var successfulObjects = new List<ReservationInstance>();
						var traceData = new TraceData();

						foreach (var reservation in objects)
						{
							if (request.isDelete)
							{
								_reservationInstances.TryRemove(reservation.ID, out _);
								successfulObjects.Add(reservation);
							}
							else if (HasReservationConflict(reservation, _reservationInstances.Values.Concat(successfulObjects)))
							{
								traceData.Add(new ResourceManagerErrorData(ResourceManagerErrorData.Reason.UnknownError, reservation.ID)
								{
									Message = "Reservation has conflicting resource usage.",
								});
							}
							else
							{
								_reservationInstances[reservation.ID] = reservation;
								successfulObjects.Add(reservation);
							}
						}

						response = new ResourceManagerResponseMessage
						{
							Success = true,
							ReservationInstances = successfulObjects,
							TraceData = traceData,
						};
						return true;
					}

				#endregion

				#region Reservation instances (ManagerStore CRUD / paging)

				case ManagerStoreReadRequest<ReservationInstance> request:
					{
						var instances = request.Query.ExecuteInMemory(_reservationInstances.Values).ToList();
						response = new ManagerStoreCrudResponse<ReservationInstance>(instances);
						return true;
					}

				case ManagerStoreCountRequest<ReservationInstance> request:
					{
						var count = request.Query.ExecuteInMemory(_reservationInstances.Values).LongCount();
						response = new ManagerStoreCountResponse<ReservationInstance>(count);
						return true;
					}

				case ManagerStoreStartPagingRequest<ReservationInstance> request:
					{
						var instances = request.Filter.ExecuteInMemory(_reservationInstances.Values).ToList();
						var pagingHandler = new InMemoryPagingHandler<ReservationInstance>(instances);
						_reservationPagingHandlers.TryAdd(pagingHandler.Cookie, pagingHandler);

						var nextPage = pagingHandler.GetNextPage(request.PreferredPageSize, out var isLast);

						if (isLast)
						{
							_reservationPagingHandlers.TryRemove(pagingHandler.Cookie, out pagingHandler);
							pagingHandler.Dispose();
						}

						response = new ManagerStorePagingResponse<ReservationInstance>(nextPage, isLast, pagingHandler.Cookie);
						return true;
					}

				case ManagerStoreNextPagingRequest<ReservationInstance> request:
					{
						if (!_reservationPagingHandlers.TryGetValue(request.PagingCookie, out var pagingHandler))
						{
							throw new InvalidOperationException($"Invalid paging cookie: {request.PagingCookie}");
						}

						var nextPage = pagingHandler.GetNextPage(request.PreferredPageSize, out var isLast);

						if (isLast)
						{
							_reservationPagingHandlers.TryRemove(pagingHandler.Cookie, out pagingHandler);
							pagingHandler.Dispose();
						}

						response = new ManagerStorePagingResponse<ReservationInstance>(nextPage, isLast, pagingHandler.Cookie);
						return true;
					}

				#endregion

				#region Legacy reservations

				case GetReservationMessage request:
					{
						response = new ReservationResponseMessage(Array.Empty<Reservation>()) { Success = true };
						return true;
					}

				#endregion

				default:
					response = default;
					return false;
			}
		}

		private EligibleResourcesResponseMessage HandleGetEligibleResources(GetEligibleResourcesMessage request)
		{
			if (request.MultipleContexts != null && request.MultipleContexts.Count > 0)
			{
				var results = request.MultipleContexts
					.Select(context => new EligibleResourceResult(context.ContextId, GetEligibleResources(context).ToList(), new List<ResourceUsageDetails>()))
					.ToList();

				var response = new EligibleResourcesResponseMessage { Success = true };
				response.MultipleResults.AddRange(results);
				return response;
			}

			var singleContext = request.Context ?? new EligibleResourceContext(request.RequestedTimeRange)
			{
				RequiredCapabilities = request.RequiredCapabilities,
				RequiredCapacities = request.RequiredCapacities,
				ReservationIdToIgnore = request.ReservationToIgnore,
				NodeIdToIgnore = request.NodeIdToIgnore,
			};

			var eligibleResources = GetEligibleResources(singleContext).ToList();
			return new EligibleResourcesResponseMessage(new EligibleResourceResult(singleContext.ContextId, eligibleResources, new List<ResourceUsageDetails>()))
			{
				EligibleResources = eligibleResources.ToArray(),
				Success = true,
			};
		}

		private IEnumerable<Resource> GetEligibleResources(EligibleResourceContext context)
		{
			IEnumerable<Resource> resources = _resources.Values;

			if (context?.ResourceFilter != null && !context.ResourceFilter.isEmpty())
			{
				resources = resources.Where(context.ResourceFilter.getLambda());
			}

			foreach (var resource in resources)
			{
				if (!HasRequiredCapabilities(resource, context?.RequiredCapabilities) ||
					!HasRequiredCapacities(resource, context))
				{
					continue;
				}

				yield return resource;
			}
		}

		private static bool HasRequiredCapabilities(Resource resource, IReadOnlyCollection<ResourceCapabilityUsage> requiredCapabilities)
		{
			if (requiredCapabilities == null || requiredCapabilities.Count == 0)
			{
				return true;
			}

			foreach (var required in requiredCapabilities)
			{
				if (required == null)
				{
					continue;
				}

				var capability = resource.Capabilities?.FirstOrDefault(x => x.CapabilityProfileID == required.CapabilityProfileID);
				if (capability == null)
				{
					return false;
				}

				if (!String.IsNullOrEmpty(required.RequiredDiscreet) &&
					!(capability.Value?.Discreets?.Contains(required.RequiredDiscreet) ?? false))
				{
					return false;
				}
			}

			return true;
		}

		private bool HasRequiredCapacities(Resource resource, EligibleResourceContext context)
		{
			var requiredCapacities = context?.RequiredCapacities;
			if (requiredCapacities == null || requiredCapacities.Count == 0)
			{
				return true;
			}

			foreach (var required in requiredCapacities)
			{
				if (required == null)
				{
					continue;
				}

				var capacity = resource.Capacities?.FirstOrDefault(x => x.CapacityProfileID == required.CapacityProfileID);
				if (capacity == null || capacity.Value == null)
				{
					return false;
				}

				if (required.RangeStart.HasValue)
				{
					var resourceMin = capacity.Value.MinDecimalQuantity ?? Decimal.MinValue;
					var resourceMax = capacity.Value.MaxDecimalQuantity;
					var requiredStart = required.RangeStart.Value;
					var requiredEnd = requiredStart + required.DecimalQuantity;

					if (requiredStart < resourceMin || requiredEnd > resourceMax || HasOverlappingRangeUsage(resource.GUID, required, context))
					{
						return false;
					}

					continue;
				}

				var used = GetUsedQuantity(resource.GUID, required, context);
				if (capacity.Value.MaxDecimalQuantity - used < required.DecimalQuantity)
				{
					return false;
				}
			}

			return true;
		}

		private bool HasReservationConflict(ReservationInstance reservation, IEnumerable<ReservationInstance> existingReservations)
		{
			if (!ConsumesCapacity(reservation.Status))
			{
				return false;
			}

			var existingUsages = existingReservations
				.Where(x => x.ID != reservation.ID)
				.Where(x => ConsumesCapacity(x.Status) && RangesOverlap(reservation.Start, reservation.End, x.Start, x.End))
				.SelectMany(x => x.ResourcesInReservationInstance.OfType<ServiceResourceUsageDefinition>())
				.ToList();

			var reservationUsages = reservation.ResourcesInReservationInstance.OfType<ServiceResourceUsageDefinition>().ToList();
			foreach (var usage in reservationUsages)
			{
				if (!_resources.TryGetValue(usage.GUID, out var resource))
				{
					continue;
				}

				var otherUsages = existingUsages
					.Concat(reservationUsages.Where(x => !ReferenceEquals(x, usage)))
					.Where(x => x.GUID == usage.GUID)
					.ToList();

				if (UsesCompleteResource(usage))
				{
					if (otherUsages.Any())
					{
						return true;
					}

					continue;
				}

				foreach (var requiredCapacity in usage.RequiredCapacities ?? Enumerable.Empty<MultiResourceCapacityUsage>())
				{
					if (!HasCapacityAvailable(resource, requiredCapacity, otherUsages))
					{
						return true;
					}
				}
			}

			return false;
		}

		private static bool UsesCompleteResource(ServiceResourceUsageDefinition usage)
		{
			return usage.UsesCompleteCapacity || usage.RequiredCapacities == null || usage.RequiredCapacities.Count == 0;
		}

		private static bool HasCapacityAvailable(Resource resource, MultiResourceCapacityUsage requiredCapacity, IReadOnlyCollection<ServiceResourceUsageDefinition> otherUsages)
		{
			var capacity = resource.Capacities?.FirstOrDefault(x => x.CapacityProfileID == requiredCapacity.CapacityProfileID);
			if (capacity?.Value == null)
			{
				return false;
			}

			var usedCapacities = otherUsages
				.SelectMany(x => x.RequiredCapacities ?? Enumerable.Empty<MultiResourceCapacityUsage>())
				.Where(x => x.CapacityProfileID == requiredCapacity.CapacityProfileID)
				.ToList();

			if (requiredCapacity.RangeStart.HasValue)
			{
				var requiredStart = requiredCapacity.RangeStart.Value;
				var requiredEnd = requiredStart + requiredCapacity.DecimalQuantity;
				var resourceMin = capacity.Value.MinDecimalQuantity ?? Decimal.MinValue;
				var resourceMax = capacity.Value.MaxDecimalQuantity;

				return requiredStart >= resourceMin &&
					requiredEnd <= resourceMax &&
					!usedCapacities
						.Where(x => x.RangeStart.HasValue)
						.Any(x => RangesOverlap(requiredStart, requiredEnd, x.RangeStart.Value, x.RangeStart.Value + x.DecimalQuantity));
			}

			var usedQuantity = usedCapacities
				.Where(x => !x.RangeStart.HasValue)
				.Sum(x => x.DecimalQuantity);

			return capacity.Value.MaxDecimalQuantity - usedQuantity >= requiredCapacity.DecimalQuantity;
		}

		private decimal GetUsedQuantity(Guid resourceId, MultiResourceCapacityUsage requiredCapacity, EligibleResourceContext context)
		{
			return GetOverlappingResourceUsages(resourceId, context)
				.SelectMany(x => x.RequiredCapacities ?? Enumerable.Empty<MultiResourceCapacityUsage>())
				.Where(x => x.CapacityProfileID == requiredCapacity.CapacityProfileID && !x.RangeStart.HasValue)
				.Sum(x => x.DecimalQuantity);
		}

		private bool HasOverlappingRangeUsage(Guid resourceId, MultiResourceCapacityUsage requiredCapacity, EligibleResourceContext context)
		{
			var requiredStart = requiredCapacity.RangeStart.Value;
			var requiredEnd = requiredStart + requiredCapacity.DecimalQuantity;

			return GetOverlappingResourceUsages(resourceId, context)
				.SelectMany(x => x.RequiredCapacities ?? Enumerable.Empty<MultiResourceCapacityUsage>())
				.Where(x => x.CapacityProfileID == requiredCapacity.CapacityProfileID && x.RangeStart.HasValue)
				.Any(x => RangesOverlap(requiredStart, requiredEnd, x.RangeStart.Value, x.RangeStart.Value + x.DecimalQuantity));
		}

		private IEnumerable<ServiceResourceUsageDefinition> GetOverlappingResourceUsages(Guid resourceId, EligibleResourceContext context)
		{
			if (context?.TimeRange == null)
			{
				yield break;
			}

			foreach (var reservation in _reservationInstances.Values)
			{
				if (!ConsumesCapacity(reservation.Status) ||
					IsIgnoredReservation(reservation, context) ||
					!RangesOverlap(context.TimeRange.Start, context.TimeRange.Stop, reservation.Start, reservation.End))
				{
					continue;
				}

				foreach (var usage in reservation.ResourcesInReservationInstance.OfType<ServiceResourceUsageDefinition>())
				{
					if (usage.GUID == resourceId)
					{
						yield return usage;
					}
				}
			}
		}

		private static bool ConsumesCapacity(ReservationStatus status)
		{
			return status == ReservationStatus.Pending ||
				status == ReservationStatus.Confirmed ||
				status == ReservationStatus.Ongoing;
		}

		private static bool IsIgnoredReservation(ReservationInstance reservation, EligibleResourceContext context)
		{
			if (context.ReservationIdToIgnore == null || context.ReservationIdToIgnore.Id == Guid.Empty)
			{
				return false;
			}

			return reservation.ID == context.ReservationIdToIgnore.Id;
		}

		private static bool RangesOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
		{
			return start1 < end2 && start2 < end1;
		}

		private static bool RangesOverlap(decimal start1, decimal end1, decimal start2, decimal end2)
		{
			return start1 < end2 && start2 < end1;
		}
	}
}
