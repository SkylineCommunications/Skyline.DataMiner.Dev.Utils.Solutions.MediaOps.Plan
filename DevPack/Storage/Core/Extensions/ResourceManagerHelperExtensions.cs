namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Net.ResponseErrorData;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	internal static class ResourceManagerHelperExtensions
	{
		public static IReadOnlyDictionary<Guid, ResourcePool> GetResourcePoolsInBatches(this ResourceManagerHelper helper, IEnumerable<Guid> resourcePoolIds)
		{
			if (helper == null)
			{
				throw new ArgumentNullException(nameof(helper));
			}

			if (resourcePoolIds == null)
			{
				throw new ArgumentNullException(nameof(resourcePoolIds));
			}

			if (!resourcePoolIds.Any())
			{
				return new Dictionary<Guid, ResourcePool>();
			}

			var coreResourcePools = new List<ResourcePool>();
			foreach (var batch in resourcePoolIds.Where(x => x != Guid.Empty).Distinct().Batch(500))
			{
				coreResourcePools.AddRange(helper.GetResourcePools(batch.Select(x => new ResourcePool(x)).ToArray()));
			}

			return coreResourcePools.ToDictionary(x => x.ID);
		}

		public static IReadOnlyDictionary<string, IReadOnlyCollection<ResourcePool>> GetResourcePoolsInBatches(this ResourceManagerHelper helper, IEnumerable<string> resourcePoolNames)
		{
			if (helper == null)
			{
				throw new ArgumentNullException(nameof(helper));
			}

			if (resourcePoolNames == null)
			{
				throw new ArgumentNullException(nameof(resourcePoolNames));
			}

			if (!resourcePoolNames.Any())
			{
				return new Dictionary<string, IReadOnlyCollection<ResourcePool>>();
			}

			var coreResourcePools = new List<ResourcePool>();
			foreach (var batch in resourcePoolNames.Where(x => !string.IsNullOrEmpty(x)).Distinct().Batch(500))
			{
				coreResourcePools.AddRange(helper.GetResourcePools(batch.Select(x => new ResourcePool { Name = x }).ToArray()));
			}

			return coreResourcePools
				.GroupBy(x => x.Name)
				.ToDictionary(x => x.Key, x => (IReadOnlyCollection<ResourcePool>)x.ToList());
		}

		public static bool TryCreateOrUpdateResourcePoolsInBatches(this ResourceManagerHelper helper, IEnumerable<ResourcePool> resourcePools, out ResourcePoolBulkOperationResult result)
		{
			if (helper == null)
			{
				throw new ArgumentNullException(nameof(helper));
			}

			if (resourcePools == null)
			{
				throw new ArgumentNullException(nameof(resourcePools));
			}

			result = InnerCreateOrUpdateResourcePoolsInBatches(helper, resourcePools);

			return !result.HasFailures;
		}

		public static bool TryDeleteResourcePoolsInBatches(this ResourceManagerHelper helper, IEnumerable<ResourcePool> resourcePools, out ResourcePoolBulkOperationResult result)
		{
			if (helper == null)
			{
				throw new ArgumentNullException(nameof(helper));
			}

			if (resourcePools == null)
			{
				throw new ArgumentNullException(nameof(resourcePools));
			}

			result = InnerDeleteResourcePoolsInBatches(helper, resourcePools);

			return !result.HasFailures;
		}

		public static IEnumerable<Resource> GetResources<T>(this ResourceManagerHelper helper, IEnumerable<T> values, Func<T, FilterElement<Resource>> filter)
		{
			if (helper == null)
			{
				throw new ArgumentNullException(nameof(helper));
			}

			if (values == null)
			{
				throw new ArgumentNullException(nameof(values));
			}

			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return FilterQueryExecutor.RetrieveFilteredItems(
				values.Distinct(),
				x => filter(x),
				x => helper.GetResources(x));
		}

		public static bool TryCreateOrUpdateResourcesInBatches(this ResourceManagerHelper helper, IEnumerable<Resource> resources, out ResourceBulkOperationResult result)
		{
			if (helper == null)
			{
				throw new ArgumentNullException(nameof(helper));
			}

			if (resources == null)
			{
				throw new ArgumentNullException(nameof(resources));
			}

			result = InnerCreateOrUpdateResourcesInBatches(helper, resources);

			return !result.HasFailures;
		}

		public static bool TryDeleteResourcesInBatches(this ResourceManagerHelper helper, IEnumerable<Resource> resources, ResourceDeleteOptions options, out ResourceBulkOperationResult result)
		{
			if (helper == null)
			{
				throw new ArgumentNullException(nameof(helper));
			}

			if (resources == null)
			{
				throw new ArgumentNullException(nameof(resources));
			}

			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			result = InnerDeleteResourcesInBatches(helper, resources, options);

			return !result.HasFailures;
		}

		public static IEnumerable<ReservationInstance> GetReservationInstances<T>(this ResourceManagerHelper helper, IEnumerable<T> values, Func<T, FilterElement<ReservationInstance>> filter)
		{
			if (helper == null)
			{
				throw new ArgumentNullException(nameof(helper));
			}

			if (values == null)
			{
				throw new ArgumentNullException(nameof(values));
			}

			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return FilterQueryExecutor.RetrieveFilteredItems(
				values.Distinct(),
				x => filter(x),
				x => helper.GetReservationInstances(x));
		}

		public static bool TryCreateOrUpdateReservationInstancesInBatches(this ResourceManagerHelper helper, IEnumerable<ReservationInstance> reservationInstances, out ReservationInstanceBulkOperationResult result, ITraceDataHandler<ResourceManagerErrorData> traceDataHandler = null)
		{
			if (helper == null)
			{
				throw new ArgumentNullException(nameof(helper));
			}

			if (reservationInstances == null)
			{
				throw new ArgumentNullException(nameof(reservationInstances));
			}

			result = InnerCreateOrUpdateReservationInstancesInBatches(helper, reservationInstances, traceDataHandler);

			return !result.HasFailures;
		}

		public static bool TryDeleteReservationInstancesInBatches(this ResourceManagerHelper helper, IEnumerable<ReservationInstance> reservationInstances, out ReservationInstanceBulkOperationResult result)
		{
			if (helper == null)
			{
				throw new ArgumentNullException(nameof(helper));
			}

			if (reservationInstances == null)
			{
				throw new ArgumentNullException(nameof(reservationInstances));
			}

			result = InnerDeleteReservationInstancesInBatches(helper, reservationInstances);

			return !result.HasFailures;
		}

		private static ResourcePoolBulkOperationResult InnerCreateOrUpdateResourcePoolsInBatches(ResourceManagerHelper helper, IEnumerable<ResourcePool> resourcePools)
		{
			var successfulItems = new List<ResourcePool>();
			var unsuccessfulIds = new List<Guid>();
			var traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();

			foreach (var batch in resourcePools.Batch(100))
			{
				var pools = helper.AddOrUpdateResourcePools(batch.ToArray());

				successfulItems.AddRange(pools);

				var traceData = helper.GetTraceDataLastCall();
				foreach (var error in traceData.ErrorData.OfType<ResourceManagerErrorData>())
				{
					if (!error.SubjectId.HasValue)
					{
						continue;
					}

					if (!traceDataPerItem.TryGetValue(error.SubjectId.Value, out var mediaOpsTraceData))
					{
						mediaOpsTraceData = new MediaOpsTraceData();
						traceDataPerItem.Add(error.SubjectId.Value, mediaOpsTraceData);

						unsuccessfulIds.Add(error.SubjectId.Value);
					}

					mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = error.ToString() });
				}
			}

			return new ResourcePoolBulkOperationResult(successfulItems, unsuccessfulIds, traceDataPerItem);
		}

		private static ResourcePoolBulkOperationResult InnerDeleteResourcePoolsInBatches(ResourceManagerHelper helper, IEnumerable<ResourcePool> resourcePools)
		{
			var successfulItems = new List<ResourcePool>();
			var unsuccessfulIds = new List<Guid>();
			var traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();

			ActivityHelper.Track(nameof(ResourceManagerHelperExtensions), nameof(InnerDeleteResourcePoolsInBatches), act =>
			{
				foreach (var batch in resourcePools.Batch(100))
				{
					var res = helper.RemoveResourcePools(batch.ToArray());

					successfulItems.AddRange(res);

					var traceData = helper.GetTraceDataLastCall();
					foreach (var error in traceData.ErrorData.OfType<ResourceManagerErrorData>())
					{
						if (!error.SubjectId.HasValue)
						{
							continue;
						}

						if (!traceDataPerItem.TryGetValue(error.SubjectId.Value, out var mediaOpsTraceData))
						{
							mediaOpsTraceData = new MediaOpsTraceData();
							traceDataPerItem.Add(error.SubjectId.Value, mediaOpsTraceData);

							unsuccessfulIds.Add(error.SubjectId.Value);
						}

						mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = error.ToString() });
					}
				}
			});

			return new ResourcePoolBulkOperationResult(successfulItems, unsuccessfulIds, traceDataPerItem);
		}

		private static ResourceBulkOperationResult InnerCreateOrUpdateResourcesInBatches(ResourceManagerHelper helper, IEnumerable<Resource> resources)
		{
			var successfulItems = new List<Resource>();
			var unsuccessfulIds = new List<Guid>();
			var traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();

			ActivityHelper.Track(nameof(ResourceManagerHelperExtensions), nameof(InnerCreateOrUpdateResourcesInBatches), act =>
			{
				foreach (var batch in resources.Batch(100))
				{
					var res = helper.AddOrUpdateResources(batch.ToArray());
					successfulItems.AddRange(res);

					var traceData = helper.GetTraceDataLastCall();
					foreach (var error in traceData.ErrorData.OfType<ResourceManagerErrorData>())
					{
						if (!error.SubjectId.HasValue)
						{
							continue;
						}

						if (!traceDataPerItem.TryGetValue(error.SubjectId.Value, out var mediaOpsTraceData))
						{
							mediaOpsTraceData = new MediaOpsTraceData();
							traceDataPerItem.Add(error.SubjectId.Value, mediaOpsTraceData);

							unsuccessfulIds.Add(error.SubjectId.Value);
						}

						mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = error.ToString() });
					}
				}
			});

			return new ResourceBulkOperationResult(successfulItems, unsuccessfulIds, traceDataPerItem);
		}

		private static ResourceBulkOperationResult InnerDeleteResourcesInBatches(ResourceManagerHelper helper, IEnumerable<Resource> resources, ResourceDeleteOptions options)
		{
			var successfulItems = new List<Resource>();
			var unsuccessfulIds = new List<Guid>();
			var traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();

			ActivityHelper.Track(nameof(ResourceManagerHelperExtensions), nameof(InnerDeleteResourcesInBatches), act =>
			{
				foreach (var batch in resources.Batch(100))
				{
					var res = helper.RemoveResources(batch.ToArray(), options);

					successfulItems.AddRange(res);

					var traceData = helper.GetTraceDataLastCall();
					foreach (var error in traceData.ErrorData.OfType<ResourceManagerErrorData>())
					{
						if (!error.SubjectId.HasValue)
						{
							continue;
						}

						if (!traceDataPerItem.TryGetValue(error.SubjectId.Value, out var mediaOpsTraceData))
						{
							mediaOpsTraceData = new MediaOpsTraceData();
							traceDataPerItem.Add(error.SubjectId.Value, mediaOpsTraceData);

							unsuccessfulIds.Add(error.SubjectId.Value);
						}

						mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = error.ToString() });
					}
				}
			});

			return new ResourceBulkOperationResult(successfulItems, unsuccessfulIds, traceDataPerItem);
		}

		private static ReservationInstanceBulkOperationResult InnerCreateOrUpdateReservationInstancesInBatches(ResourceManagerHelper helper, IEnumerable<ReservationInstance> reservationInstances, ITraceDataHandler<ResourceManagerErrorData> traceDataHandler)
		{
			var successfulItems = new List<ReservationInstance>();
			var unsuccessfulIds = new HashSet<Guid>();
			var traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();

			ActivityHelper.Track(nameof(ResourceManagerHelperExtensions), nameof(InnerCreateOrUpdateReservationInstancesInBatches), act =>
			{
				// Batch of 1 is used instead of 100 because of issue in core software [DCP296627]
				foreach (var batch in reservationInstances.Batch(1))
				{
					var res = helper.AddOrUpdateReservationInstances(batch.ToArray());
					successfulItems.AddRange(res);

					var traceData = helper.GetTraceDataLastCall();
					var resourceManagerErrors = traceData.ErrorData.OfType<ResourceManagerErrorData>().ToList();
					if (resourceManagerErrors.Count == 0)
					{
						continue;
					}

					if (traceDataHandler != null)
					{
						foreach (var kvp in traceDataHandler.Translate(resourceManagerErrors))
						{
							if (traceDataPerItem.TryGetValue(kvp.Key, out var mediaOpsTraceData))
							{
								foreach (var error in kvp.Value.ErrorData)
								{
									mediaOpsTraceData.Add(error);
								}
							}
							else
							{
								traceDataPerItem.Add(kvp.Key, kvp.Value);
							}

							unsuccessfulIds.Add(kvp.Key);
						}

						continue;
					}

					var resourceManagerErrorsBySubjectId = resourceManagerErrors
					.Where(x => x.SubjectId.HasValue)
					.GroupBy(x => x.SubjectId.Value)
					.ToDictionary(x => x.Key, x => x.ToList());

					foreach (var resourceSpecificErrors in resourceManagerErrorsBySubjectId)
					{
						var subjectId = resourceSpecificErrors.Key;
						var errors = resourceSpecificErrors.Value;

						var mediaOpsTraceData = new MediaOpsTraceData();
						foreach (var error in errors)
						{
							mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = error.ToString() });
						}

						traceDataPerItem.Add(subjectId, mediaOpsTraceData);
						unsuccessfulIds.Add(subjectId);
					}
				}
			});

			return new ReservationInstanceBulkOperationResult(successfulItems, unsuccessfulIds, traceDataPerItem);
		}

		private static ReservationInstanceBulkOperationResult InnerDeleteReservationInstancesInBatches(ResourceManagerHelper helper, IEnumerable<ReservationInstance> reservationInstances)
		{
			var successfulItems = new List<ReservationInstance>();
			var unsuccessfulIds = new List<Guid>();
			var traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();

			ActivityHelper.Track(nameof(ResourceManagerHelperExtensions), nameof(InnerDeleteReservationInstancesInBatches), act =>
			{
				foreach (var batch in reservationInstances.Batch(100))
				{
					var reservations = helper.RemoveReservationInstances(batch.ToArray());

					successfulItems.AddRange(reservations);

					var traceData = helper.GetTraceDataLastCall();
					foreach (var error in traceData.ErrorData.OfType<ResourceManagerErrorData>())
					{
						if (!error.SubjectId.HasValue)
						{
							continue;
						}

						if (!traceDataPerItem.TryGetValue(error.SubjectId.Value, out var mediaOpsTraceData))
						{
							mediaOpsTraceData = new MediaOpsTraceData();
							traceDataPerItem.Add(error.SubjectId.Value, mediaOpsTraceData);

							unsuccessfulIds.Add(error.SubjectId.Value);
						}

						mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = error.ToString() });
					}
				}
			});

			return new ReservationInstanceBulkOperationResult(successfulItems, unsuccessfulIds, traceDataPerItem);
		}
	}
}
