namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using DomResource = Storage.DOM.SlcResource_Studio.ResourceInstance;
	using DomResourcePool = Storage.DOM.SlcResource_Studio.ResourcepoolInstance;

	internal sealed class SynchronizationHandler
	{
		private readonly MediaOpsPlanApi planApi;

		private SynchronizationHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		public static SynchronizationReport GetReport(MediaOpsPlanApi planApi)
		{
			var handler = new SynchronizationHandler(planApi);

			return ActivityHelper.Track(nameof(SynchronizationHandler), nameof(GetReport), act =>
			{
				var domResourcePools = planApi.ResourcePools.Read(ResourcePoolExposers.State.Equal(ResourcePoolState.Complete)).Select(x => x.OriginalInstance).ToList();
				var domResources = planApi.Resources.Read(ResourceExposers.State.Equal(ResourceState.Complete)).Select(x => x.OriginalInstance).ToList();

				return handler.BuildReport(domResourcePools, domResources);
			});
		}

		public static SynchronizationReport GetReport(MediaOpsPlanApi planApi, ICollection<ResourcePool> resourcePools)
		{
			if (resourcePools == null)
			{
				throw new ArgumentNullException(nameof(resourcePools));
			}

			var handler = new SynchronizationHandler(planApi);

			return ActivityHelper.Track(nameof(SynchronizationHandler), nameof(GetReport), act =>
			{
				var completedResourcePools = resourcePools.Where(x => x != null && x.State == ResourcePoolState.Complete).ToList();
				if (completedResourcePools.Count == 0)
				{
					return SynchronizationReport.Create([], []);
				}

				var domResources = planApi.Resources
					.GetResourcesPerPool(completedResourcePools, ResourceState.Complete)
					.SelectMany(x => x.Value)
					.GroupBy(x => x.Id)
					.Select(x => x.First().OriginalInstance)
					.ToList();

				return handler.BuildReport(completedResourcePools.Select(x => x.OriginalInstance).ToList(), domResources);
			});
		}

		public static SynchronizationResult Synchronize(MediaOpsPlanApi planApi, ICollection<SynchronizationItem> items)
		{
			if (items == null)
			{
				throw new ArgumentNullException(nameof(items));
			}

			var handler = new SynchronizationHandler(planApi);

			return ActivityHelper.Track(nameof(SynchronizationHandler), nameof(Synchronize), act => handler.Synchronize(items));
		}

		private static bool CoreObjectExists(SynchronizationDetectionResult detection, Guid id)
		{
			return !detection.DifferencesPerItem.TryGetValue(id, out var differences)
				|| !differences.Any(x => x is MissingCoreObjectDifference);
		}

		private static List<SynchronizationDifference> GetDifferences(SynchronizationDetectionResult detection, Guid id)
		{
			return detection.DifferencesPerItem.TryGetValue(id, out var differences) ? differences : [];
		}

		private static List<MediaOpsErrorData> GetBlockers(SynchronizationDetectionResult detection, Guid id)
		{
			return detection.BlockersPerItem.TryGetValue(id, out var traceData) ? traceData.ErrorData : [];
		}

		private static void AddFailure(IDictionary<Guid, MediaOpsTraceData> failures, Guid id, MediaOpsErrorData error)
		{
			if (!failures.TryGetValue(id, out var traceData))
			{
				traceData = new MediaOpsTraceData();
				failures.Add(id, traceData);
			}

			traceData.Add(error);
		}

		private SynchronizationReport BuildReport(ICollection<DomResourcePool> domResourcePools, ICollection<DomResource> domResources)
		{
			CoreResourcePoolHandler.TryGetDifferences(planApi, domResourcePools, out var resourcePoolDetection);
			CoreResourceHandler.TryGetDifferences(planApi, domResources, out var resourceDetection);

			var resourcePoolItems = domResourcePools
				.Where(x => !resourcePoolDetection.IsSynchronizedItem(x.ID.Id))
				.Select(x => new ResourcePoolSynchronizationItem(
					x.ID.Id,
					x.ResourcePoolInfo.Name,
					CoreObjectExists(resourcePoolDetection, x.ID.Id),
					GetDifferences(resourcePoolDetection, x.ID.Id),
					GetBlockers(resourcePoolDetection, x.ID.Id)))
				.ToList();

			var resourceItems = domResources
				.Where(x => !resourceDetection.IsSynchronizedItem(x.ID.Id))
				.Select(x => new ResourceSynchronizationItem(
					x.ID.Id,
					x.ResourceInfo.Name,
					CoreObjectExists(resourceDetection, x.ID.Id),
					GetDifferences(resourceDetection, x.ID.Id),
					GetBlockers(resourceDetection, x.ID.Id)))
				.ToList();

			return SynchronizationReport.Create(resourcePoolItems, resourceItems);
		}

		private SynchronizationResult Synchronize(ICollection<SynchronizationItem> items)
		{
			var failures = new Dictionary<Guid, MediaOpsTraceData>();

			// Resource pools go first so that resources can link to a CORE pool that was just created.
			var synchronizedResourcePoolIds = SynchronizeResourcePools(items.OfType<ResourcePoolSynchronizationItem>().Select(x => x.Id).Distinct().ToList(), failures);
			var synchronizedResourceIds = SynchronizeResources(items.OfType<ResourceSynchronizationItem>().Select(x => x.Id).Distinct().ToList(), failures);

			return SynchronizationResult.Create(synchronizedResourcePoolIds, synchronizedResourceIds, failures);
		}

		private List<Guid> SynchronizeResourcePools(ICollection<Guid> ids, IDictionary<Guid, MediaOpsTraceData> failures)
		{
			var synchronizedIds = new List<Guid>();
			if (ids.Count == 0)
			{
				return synchronizedIds;
			}

var resourcePools = planApi.ResourcePools.Read(ids).ToList();
			var domResourcePoolsById = resourcePools
				.Where(x => x.State == ResourcePoolState.Complete)
				.ToDictionary(x => x.Id, x => x.OriginalInstance);
			foreach (var id in ids.Where(x => resourcePools.All(y => y.Id != x)))
			{
				AddFailure(failures, id, new ResourcePoolNotFoundError { ErrorMessage = $"Resource pool with ID '{id}' no longer exists.", Id = id });
			}

			foreach (var resourcePool in resourcePools.Where(x => x.State != ResourcePoolState.Complete))
			{
				AddFailure(failures, resourcePool.Id, new ResourcePoolInvalidStateError { ErrorMessage = "Resource pool is no longer in Completed state.", Id = resourcePool.Id });
			}

			CoreResourcePoolHandler.TryGetDifferences(planApi, domResourcePoolsById.Values.ToList(), out var detection);

			var toSynchronize = new List<DomResourcePool>();
			var idsWithoutCoreObject = new HashSet<Guid>();
			foreach (var entry in domResourcePoolsById)
			{
				if (detection.BlockersPerItem.TryGetValue(entry.Key, out var traceData))
				{
					failures[entry.Key] = traceData;
					continue;
				}

				if (detection.IsSynchronizedItem(entry.Key))
				{
					synchronizedIds.Add(entry.Key);
					continue;
				}

				if (!CoreObjectExists(detection, entry.Key))
				{
					idsWithoutCoreObject.Add(entry.Key);
				}

				toSynchronize.Add(entry.Value);
			}

			if (toSynchronize.Count == 0)
			{
				return synchronizedIds;
			}

			CoreResourcePoolHandler.TryCreateOrUpdate(planApi, toSynchronize, out var result);

			foreach (var id in result.UnsuccessfulIds)
			{
				failures[id] = result.TraceDataPerItem.TryGetValue(id, out var traceData) ? traceData : new MediaOpsTraceData();
			}

			SaveDomInstances(result.SuccessfulItems.Where(x => idsWithoutCoreObject.Contains(x.ID.Id)).Select(x => x.ToInstance()), failures);

			synchronizedIds.AddRange(result.SuccessfulIds.Where(x => !failures.ContainsKey(x)));

			return synchronizedIds;
		}

		private List<Guid> SynchronizeResources(ICollection<Guid> ids, IDictionary<Guid, MediaOpsTraceData> failures)
		{
			var synchronizedIds = new List<Guid>();
			if (ids.Count == 0)
			{
				return synchronizedIds;
			}

var resources = planApi.Resources.Read(ids).ToList();
			var domResourcesById = resources
				.Where(x => x.State == ResourceState.Complete)
				.ToDictionary(x => x.Id, x => x.OriginalInstance);
			foreach (var id in ids.Where(x => resources.All(y => y.Id != x)))
			{
				AddFailure(failures, id, new ResourceNotFoundError { ErrorMessage = $"Resource with ID '{id}' no longer exists.", Id = id });
			}

			foreach (var resource in resources.Where(x => x.State != ResourceState.Complete))
			{
				AddFailure(failures, resource.Id, new ResourceInvalidStateError { ErrorMessage = "Resource is no longer in Completed state.", Id = resource.Id });
			}

			CoreResourceHandler.TryGetDifferences(planApi, domResourcesById.Values.ToList(), out var detection);

			var toSynchronize = new List<DomResource>();
			var idsWithoutCoreObject = new HashSet<Guid>();
			foreach (var entry in domResourcesById)
			{
				if (detection.BlockersPerItem.TryGetValue(entry.Key, out var traceData))
				{
					failures[entry.Key] = traceData;
					continue;
				}

				if (detection.IsSynchronizedItem(entry.Key))
				{
					synchronizedIds.Add(entry.Key);
					continue;
				}

				if (!CoreObjectExists(detection, entry.Key))
				{
					idsWithoutCoreObject.Add(entry.Key);
				}

				toSynchronize.Add(entry.Value);
			}

			if (toSynchronize.Count == 0)
			{
				return synchronizedIds;
			}

			CoreResourceHandler.TryCreateOrUpdate(planApi, toSynchronize, out var result, recreateMissingCoreResources: true);

			foreach (var id in result.UnsuccessfulIds)
			{
				failures[id] = result.TraceDataPerItem.TryGetValue(id, out var traceData) ? traceData : new MediaOpsTraceData();
			}

			SaveDomInstances(result.SuccessfulItems.Where(x => idsWithoutCoreObject.Contains(x.ID.Id)).Select(x => x.ToInstance()), failures);

			synchronizedIds.AddRange(result.SuccessfulIds.Where(x => !failures.ContainsKey(x)));

			return synchronizedIds;
		}

		private void SaveDomInstances(IEnumerable<DomInstance> instances, IDictionary<Guid, MediaOpsTraceData> failures)
		{
			var instancesToSave = instances.ToList();
			if (instancesToSave.Count == 0)
			{
				return;
			}

			planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(instancesToSave, out var domResult);

			foreach (var id in domResult.UnsuccessfulIds)
			{
				var errorMessage = domResult.TraceDataPerItem.TryGetValue(id, out var traceData) ? traceData.ToString() : "Failed to store the link with the CORE object.";
				AddFailure(failures, id.Id, new MediaOpsErrorData { ErrorMessage = errorMessage });
			}
		}
	}
}
