namespace Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Stores
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	using Parameter = Skyline.DataMiner.Net.Profiles.Parameter;

	/// <summary>
	/// In-memory store that handles ManagerStore messages for profile <see cref="Parameter"/> objects,
	/// mirroring how a real DataMiner Agent would respond.
	/// </summary>
	internal sealed class ProfileParameterStore
	{
		private const string Author = "ProfileParameterStore";

		private readonly ConcurrentDictionary<Guid, Parameter> _parameters = new ConcurrentDictionary<Guid, Parameter>();
		private readonly ConcurrentDictionary<PagingCookie, InMemoryPagingHandler<Parameter>> _pagingHandlers = new ConcurrentDictionary<PagingCookie, InMemoryPagingHandler<Parameter>>();

		public bool TryHandleMessage(DMSMessage message, out DMSMessage response)
		{
			switch (message)
			{
				case ManagerStoreReadRequest<Parameter> request:
					{
						var filtered = CoercingFilterEvaluator.Apply(request.Query.Filter, _parameters.Values);
						var parameters = request.Query.WithFilter(new TRUEFilterElement<Parameter>()).ExecuteInMemory(filtered).ToList();
						response = new ManagerStoreCrudResponse<Parameter>(parameters);
						return true;
					}

				case ManagerStoreCreateRequest<Parameter> request:
					{
						Stamp(request.Object, isNew: true);
						_parameters[request.Object.ID] = request.Object;
						response = new ManagerStoreCrudResponse<Parameter>(request.Object);
						return true;
					}

				case ManagerStoreUpdateRequest<Parameter> request:
					{
						Stamp(request.Object, isNew: false);
						_parameters[request.Object.ID] = request.Object;
						response = new ManagerStoreCrudResponse<Parameter>(request.Object);
						return true;
					}

				case ManagerStoreDeleteRequest<Parameter> request:
					{
						_parameters.TryRemove(request.Object.ID, out _);
						response = new ManagerStoreCrudResponse<Parameter>(request.Object);
						return true;
					}

				case ManagerStoreCountRequest<Parameter> request:
					{
						var count = CoercingFilterEvaluator.Apply(request.Query.Filter, _parameters.Values).LongCount();
						response = new ManagerStoreCountResponse<Parameter>(count);
						return true;
					}

				case ManagerStoreBulkCreateOrUpdateRequest<Parameter> request:
					{
						var objects = request.Objects.ToList();

						foreach (var obj in objects)
						{
							var isNew = !_parameters.ContainsKey(obj.ID);
							Stamp(obj, isNew);
							_parameters[obj.ID] = obj;
						}

						var traceData = objects.ToDictionary(x => x.ID, x => new TraceData());
						var unsuccessfulIds = new List<Guid>();
						var result = new BulkCreateOrUpdateResult<Parameter, Guid>(objects, unsuccessfulIds, traceData);

						response = new ManagerStoreCrudResponse<Parameter>(result);
						return true;
					}

				case ManagerStoreBulkDeleteRequest<Parameter> request:
					{
						var objects = request.Objects.ToList();

						var successfulIds = new List<Guid>();
						var unsuccessfulIds = new List<Guid>();

						foreach (var obj in objects)
						{
							_parameters.TryRemove(obj.ID, out _);
							successfulIds.Add(obj.ID);
						}

						var traceData = objects.ToDictionary(x => x.ID, x => new TraceData());
						var result = new BulkDeleteResult<Guid>(successfulIds, unsuccessfulIds, traceData);

						response = new ManagerStoreCrudResponse<Parameter>(result);
						return true;
					}

				case ProfileManagerBulkRequest<Parameter> request:
					{
						var objects = request.Objects.ToList();

						if (request.IsDelete)
						{
							foreach (var obj in objects)
							{
								_parameters.TryRemove(obj.ID, out _);
							}
						}
						else
						{
							foreach (var obj in objects)
							{
								var isNew = !_parameters.ContainsKey(obj.ID);
								Stamp(obj, isNew);
								_parameters[obj.ID] = obj;
							}
						}

						response = new ProfileManagerBulkResponse<Parameter>
						{
							Objects = objects,
						};
						return true;
					}

				case ManagerStoreStartPagingRequest<Parameter> request:
					{
						var filtered = CoercingFilterEvaluator.Apply(request.Filter.Filter, _parameters.Values);
						var parameters = request.Filter.WithFilter(new TRUEFilterElement<Parameter>()).ExecuteInMemory(filtered).ToList();
						var pagingHandler = new InMemoryPagingHandler<Parameter>(parameters);
						_pagingHandlers.TryAdd(pagingHandler.Cookie, pagingHandler);

						var nextPage = pagingHandler.GetNextPage(request.PreferredPageSize, out var isLast);

						if (isLast)
						{
							_pagingHandlers.TryRemove(pagingHandler.Cookie, out pagingHandler);
							pagingHandler.Dispose();
						}

						response = new ManagerStorePagingResponse<Parameter>(nextPage, isLast, pagingHandler.Cookie);
						return true;
					}

				case ManagerStoreNextPagingRequest<Parameter> request:
					{
						if (!_pagingHandlers.TryGetValue(request.PagingCookie, out var pagingHandler))
						{
							throw new InvalidOperationException($"Invalid paging cookie: {request.PagingCookie}");
						}

						var nextPage = pagingHandler.GetNextPage(request.PreferredPageSize, out var isLast);

						if (isLast)
						{
							_pagingHandlers.TryRemove(pagingHandler.Cookie, out pagingHandler);
							pagingHandler.Dispose();
						}

						response = new ManagerStorePagingResponse<Parameter>(nextPage, isLast, pagingHandler.Cookie);
						return true;
					}

				default:
					response = default;
					return false;
			}
		}

		private static void Stamp(Parameter parameter, bool isNew)
		{
			var utcNow = DateTime.UtcNow;

			if (isNew)
			{
				if (parameter is ITrackCreatedAt createdAt)
				{
					createdAt.CreatedAt = utcNow;
				}

				if (parameter is ITrackCreatedBy createdBy)
				{
					createdBy.CreatedBy = Author;
				}
			}

			if (parameter is ITrackLastModified lastModified)
			{
				lastModified.LastModified = utcNow;
			}

			if (parameter is ITrackLastModifiedBy lastModifiedBy)
			{
				lastModifiedBy.LastModifiedBy = Author;
			}
		}
	}
}
