namespace Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Stores
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.ResourceManager;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
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

						foreach (var reservation in objects)
						{
							if (request.isDelete)
							{
								_reservationInstances.TryRemove(reservation.ID, out _);
							}
							else
							{
								_reservationInstances[reservation.ID] = reservation;
							}
						}

						response = new ResourceManagerResponseMessage
						{
							Success = true,
							ReservationInstances = objects,
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
	}
}
