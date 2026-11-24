namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using DomResource = Storage.DOM.SlcResource_Studio.ResourceInstance;
	using DomResourcePool = Storage.DOM.SlcResource_Studio.ResourcepoolInstance;
	using LiveEndpointRole = Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums.EndpointRole;
	using LiveVirtualSignalGroup = Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement.VirtualSignalGroup;

	internal class DomResourceHandler : DomInstanceApiObjectValidator<DomResource>
	{
		internal const string FunctionInstancesLockFailedMessage = "Failed to lock the function instances of the linked element. Try again later.";

		private readonly MediaOpsPlanApi planApi;

		private readonly HashSet<Guid> resourceIdsWithCoreChanges = new HashSet<Guid>();

		private readonly HashSet<Guid> resourceIdsRequiringConnectionManagementValidation = new HashSet<Guid>();

		private Dictionary<Guid, ResourcePool> resourcePoolsById = new Dictionary<Guid, ResourcePool>();

		private bool validatePoolAssignmentUsage = true;

		private DomResourceHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<Resource> apiResources, out DomInstanceBulkOperationResult<DomResource> result, bool allowDeprecatedUpdates = false, bool validatePoolAssignmentUsage = true)
		{
			var handler = new DomResourceHandler(planApi)
			{
				validatePoolAssignmentUsage = validatePoolAssignmentUsage,
			};
			handler.CreateOrUpdate(apiResources, allowDeprecatedUpdates);

			result = new DomInstanceBulkOperationResult<DomResource>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryComplete(MediaOpsPlanApi planApi, ICollection<Resource> apiResources, out DomInstanceBulkOperationResult<DomResource> result)
		{
			var handler = new DomResourceHandler(planApi);
			handler.TransitionToCompleteFromDraft(apiResources);

			result = new DomInstanceBulkOperationResult<DomResource>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
			return !result.HasFailures;
		}

		internal static bool TryDeprecate(MediaOpsPlanApi planApi, ICollection<Resource> apiResources, out DomInstanceBulkOperationResult<DomResource> result)
		{
			var handler = new DomResourceHandler(planApi);
			handler.TransitionToDeprecated(apiResources);

			result = new DomInstanceBulkOperationResult<DomResource>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
			return !result.HasFailures;
		}

		internal static bool TryRestore(MediaOpsPlanApi planApi, ICollection<Resource> apiResources, out DomInstanceBulkOperationResult<DomResource> result)
		{
			var handler = new DomResourceHandler(planApi);
			handler.TransitionToCompleteFromDeprecated(apiResources);

			result = new DomInstanceBulkOperationResult<DomResource>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
			return !result.HasFailures;
		}

		internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<Resource> apiResources, out DomInstanceBulkOperationResult<DomResource> result)
		{
			var handler = new DomResourceHandler(planApi);
			handler.Delete(apiResources);

			result = new DomInstanceBulkOperationResult<DomResource>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
			return !result.HasFailures;
		}

		internal static bool TryConvertToUnmanagedResource(MediaOpsPlanApi planApi, Resource resource, out DomInstanceBulkOperationResult<DomResource> result)
		{
			var handler = new DomResourceHandler(planApi);
			handler.ConvertToUnmanagedResource(resource);

			result = new DomInstanceBulkOperationResult<DomResource>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
			return !result.HasFailures;
		}

		internal static bool TryConvertToVirtualFunctionResource(MediaOpsPlanApi planApi, Resource resource, ResourceVirtualFunctionLinkSetting setting, out DomInstanceBulkOperationResult<DomResource> result)
		{
			var handler = new DomResourceHandler(planApi);
			handler.ConvertToVirtualFunctionResource(resource, setting);

			result = new DomInstanceBulkOperationResult<DomResource>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
			return !result.HasFailures;
		}

		internal static bool TryConvertToServiceResource(MediaOpsPlanApi planApi, Resource resource, ResourceServiceLinkSetting setting, out DomInstanceBulkOperationResult<DomResource> result)
		{
			var handler = new DomResourceHandler(planApi);
			handler.ConvertToServiceResource(resource, setting);

			result = new DomInstanceBulkOperationResult<DomResource>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
			return !result.HasFailures;
		}

		internal static bool TryConvertToElementResource(MediaOpsPlanApi planApi, Resource resource, ResourceElementLinkSetting setting, out DomInstanceBulkOperationResult<DomResource> result)
		{
			var handler = new DomResourceHandler(planApi);
			handler.ConvertToElementResource(resource, setting);

			result = new DomInstanceBulkOperationResult<DomResource>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
			return !result.HasFailures;
		}

		private void ClearErrors(ICollection<DomResource> domResources, ErrorDefinition errorDefinition)
		{
			if (domResources == null)
			{
				throw new ArgumentNullException(nameof(domResources));
			}

			if (domResources.Count == 0)
			{
				return;
			}

			foreach (var domResource in domResources)
			{
				domResource.ClearError(errorDefinition.ErrorCode);
			}
		}

		private void CreateOrUpdate(ICollection<Resource> apiResources, bool allowDeprecatedUpdates)
		{
			if (apiResources == null)
			{
				throw new ArgumentNullException(nameof(apiResources));
			}

			if (apiResources.Count == 0)
			{
				return;
			}

			var toValidate = allowDeprecatedUpdates
				? apiResources.Where(x => x.State != ResourceState.Deprecated).ToList()
				: apiResources.ToList();
			var toCreate = apiResources.Where(x => x.IsNew).ToList();
			var toUpdate = toValidate.Except(toCreate).ToList();

			ValidateIdsNotInUse(toCreate);
			ValidateStateForUpdateAction(toUpdate);

			ValidateCapacities(toValidate);
			ValidateCapabilities(toValidate);
			ValidateResourceProperties(toValidate);
			ValidateNames(toValidate);
			ValidateConcurrency(toValidate);
			resourceIdsRequiringConnectionManagementValidation.UnionWith(toValidate.Select(x => x.Id));

			var validResources = apiResources.Where(IsValid).ToList();
			var lockResult = planApi.LockManager.LockAndExecute(validResources, CreateOrUpdateCoreResources);
			ReportError(lockResult);
		}

		private void CreateOrUpdateCoreResources(ICollection<Resource> resources)
		{
			if (resources == null)
			{
				throw new ArgumentNullException(nameof(resources));
			}

			if (resources.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided resources are valid", nameof(resources));
			}

			var resourcesToCreate = resources.Where(x => x.IsNew).ToList();

			var storedResourcesById = ValidateStoredStateForUpdateAction(resources.Except(resourcesToCreate).ToList());
			var resourcesToUpdate = resources.Except(resourcesToCreate).Where(IsValid).ToList();

			// The change detection merges the changes into the stored instances, so the stored pools are captured first.
			var storedPoolIdsByResourceId = GetStoredPoolIdsOfCompleteResources(storedResourcesById.Values);

			var changeResults = ActivityHelper.Track(nameof(DomResourceHandler), nameof(GetResourcesWithChanges), act => GetResourcesWithChanges(resourcesToUpdate, storedResourcesById));

			var toUpdateNameValidation = resourcesToUpdate.Where(x => changeResults.Any(y => y.Instance.ID.Id == x.Id && y.ChangedFields.Select(z => z.FieldDescriptorId).Contains(SlcResource_StudioIds.Sections.ResourceInfo.Name.Id)));
			ActivityHelper.Track(nameof(DomResourceHandler), nameof(ValidateDomNames), act => ValidateDomNames(resourcesToCreate.Concat(toUpdateNameValidation).ToList()));

			var toCreatePoolValidation = resourcesToCreate.Where(IsValid);
			var toUpdatePoolValidation = resourcesToUpdate.Where(x => changeResults.Any(y => y.Instance.ID.Id == x.Id && y.ChangedFields.Select(z => z.FieldDescriptorId).Contains(SlcResource_StudioIds.Sections.ResourceInternalProperties.Pool_Ids.Id)));
			ValidatePoolAssignments(toCreatePoolValidation.Concat(toUpdatePoolValidation).ToList());

			if (validatePoolAssignmentUsage)
			{
				ValidateRemovedPoolAssignmentUsage(toUpdatePoolValidation.Where(IsValid).ToList(), changeResults, storedPoolIdsByResourceId);
			}

			var resourcesWithCapabilityChanges = resourcesToUpdate.Where(x =>
				IsValid(x)
				&& x.State != ResourceState.Deprecated
				&& changeResults.Any(y => y.Instance.ID.Id == x.Id
					&& (y.AddedSections.Select(z => z.SectionDefinitionId).Contains(SlcResource_StudioIds.Sections.ResourceCapabilities.Id.Id)
							|| y.RemovedSections.Select(z => z.SectionDefinitionId).Contains(SlcResource_StudioIds.Sections.ResourceCapabilities.Id.Id)
							|| y.ChangedFields.Select(z => z.SectionDefinitionId).Contains(SlcResource_StudioIds.Sections.ResourceCapabilities.Id.Id))));

			foreach (var resource in resourcesWithCapabilityChanges)
			{
				MarkAsResourceWithCoreChanges(resource);
			}

			var resourcesWithCapacityChanges = resourcesToUpdate.Where(x =>
				IsValid(x)
				&& x.State != ResourceState.Deprecated
				&& changeResults.Any(y => y.Instance.ID.Id == x.Id
				&& (y.AddedSections.Select(z => z.SectionDefinitionId).Contains(SlcResource_StudioIds.Sections.ResourceCapacities.Id.Id)
						|| y.RemovedSections.Select(z => z.SectionDefinitionId).Contains(SlcResource_StudioIds.Sections.ResourceCapacities.Id.Id)
						|| y.ChangedFields.Select(z => z.SectionDefinitionId).Contains(SlcResource_StudioIds.Sections.ResourceCapacities.Id.Id))));

			foreach (var resource in resourcesWithCapacityChanges)
			{
				MarkAsResourceWithCoreChanges(resource);
			}

			var resourcesWithConcurencyChanges = resourcesToUpdate.Where(x =>
				IsValid(x)
				&& changeResults.Any(y => y.Instance.ID.Id == x.Id
				&& y.ChangedFields.Select(z => z.FieldDescriptorId).Contains(SlcResource_StudioIds.Sections.ResourceInfo.Concurrency.Id)));

			foreach (var resource in resourcesWithConcurencyChanges)
			{
				MarkAsResourceWithCoreChanges(resource);
			}

			// The VSGs are validated on the merged instances, so a stale snapshot cannot validate VSGs that are no longer linked.
			var createdInstancesById = resourcesToCreate
				.Where(IsValid)
				.ToDictionary(x => x.Id, x => x.GetInstanceWithChanges());
			var mergedInstances = changeResults
				.Where(IsValid)
				.Select(x => new DomResource(x.Instance))
				.ToList();

			ValidateConnectionManagement(createdInstancesById.Values.Concat(mergedInstances).Where(x => resourceIdsRequiringConnectionManagementValidation.Contains(x.ID.Id)).ToList());

			var toCreateDomInstances = createdInstancesById.Values
				.Where(x => !TraceDataPerItem.ContainsKey(x.ID.Id))
				.ToList();

			var toUpdateDomInstances = mergedInstances
				.Where(x => !TraceDataPerItem.ContainsKey(x.ID.Id))
				.ToList();

			CreateOrUpdateDomResources(toCreateDomInstances.Concat(toUpdateDomInstances).ToList());
		}

		private void CreateOrUpdateDomResources(ICollection<DomResource> domResources)
		{
			if (domResources == null)
			{
				throw new ArgumentNullException(nameof(domResources));
			}

			if (domResources.Count == 0)
			{
				return;
			}

			var domResourcesById = domResources.ToDictionary(x => x.ID.Id);

			if (resourceIdsWithCoreChanges.Count != 0)
			{
				var domResourcesWithCoreChanges = domResources.Where(x => resourceIdsWithCoreChanges.Contains(x.ID.Id)).ToList();
				UpdateCaches(domResourcesWithCoreChanges);

				CoreResourceHandler.TryCreateOrUpdate(planApi, domResourcesWithCoreChanges, out var coreResult);

				foreach (var id in coreResult.UnsuccessfulIds)
				{
					ReportError(id);

					if (coreResult.TraceDataPerItem.TryGetValue(id, out var traceData))
					{
						PassTraceData(id, traceData);
					}

					domResourcesById.Remove(id);
				}
			}

			planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(domResourcesById.Values.Select(x => x.ToInstance()), out var domResult);

			foreach (var id in domResult.UnsuccessfulIds)
			{
				ReportError(id.Id);

				if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					var mediaOpsTraceData = new MediaOpsTraceData();
					mediaOpsTraceData.Add(new MediaOpsErrorData { ErrorMessage = traceData.ToString() });

					PassTraceData(id.Id, mediaOpsTraceData);
				}
			}

			ReportSuccess(domResult.SuccessfulItems.Select(x => new DomResource(x)));
		}

		private void TransitionToCompleteFromDraft(ICollection<Resource> apiResources)
		{
			if (apiResources == null)
			{
				throw new ArgumentNullException(nameof(apiResources));
			}

			if (apiResources.Count == 0)
			{
				return;
			}

			ValidateStateForCompleteFromDraftAction(apiResources);

			var lockResult = planApi.LockManager.LockAndExecute(apiResources.Where(IsValid).ToList(), TransitionToCompleteFromDraftLocked);
			ReportError(lockResult);
		}

		private void TransitionToCompleteFromDraftLocked(ICollection<Resource> apiResources)
		{
			// The resources were read before the lock was granted. The CORE resource is created and linked to the stored
			// resource, so a resource that was completed in the meantime does not get a second CORE resource, and the
			// link is saved on the stored resource instead of on a copy that would overwrite later changes.
			var resourcesToCreate = GetStoredDomResources(
				apiResources,
				[SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum.Draft],
				[SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum.Complete],
				"Not allowed to complete a resource that is not in Draft state.");
			if (resourcesToCreate.Count == 0)
			{
				return;
			}

			ClearErrors(resourcesToCreate, ResourceErrors.ExecuteAction_MarkCompleteException);

			// Counting the function instances on an element and creating the CORE resource must not interleave with another writer.
			var coreSuccessfulIds = new HashSet<Guid>();
			var notLockedResources = CoreResourceHandler.LockFunctionInstanceSlotsAndExecute(planApi, resourcesToCreate, subset =>
			{
				CoreResourceHandler.TryCreateOrUpdate(planApi, subset, out var coreResult);
				coreSuccessfulIds.UnionWith(coreResult.SuccessfulIds);

				foreach (var id in coreResult.UnsuccessfulIds)
				{
					ReportError(id);

					if (coreResult.TraceDataPerItem.TryGetValue(id, out var traceData))
					{
						PassTraceData(id, traceData);
					}
				}
			});

			foreach (var resource in notLockedResources)
			{
				ReportError(resource.ID.Id, new MediaOpsErrorData { ErrorMessage = FunctionInstancesLockFailedMessage });
			}

			// Save link with CORE Resources
			var resourcesToSave = resourcesToCreate.Where(x => coreSuccessfulIds.Contains(x.ID.Id)).ToList();
			planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(resourcesToSave.Select(x => x.ToInstance()), out var domResult);
			foreach (var id in domResult.UnsuccessfulIds)
			{
				ReportError(id.Id);

				if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					var mediaOpsTraceData = new MediaOpsTraceData();
					mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = traceData.ToString() });

					PassTraceData(id.Id, mediaOpsTraceData);
				}
			}

			// Transition DOM resources to Complete state
			foreach (var domInstanceId in domResult.SuccessfulIds)
			{
				try
				{
					var transitionedInstance = planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.DoStatusTransition(domInstanceId, SlcResource_StudioIds.Behaviors.Resource_Behavior.Transitions.Draft_To_Complete);
					ReportSuccess(new DomResource(transitionedInstance));
				}
				catch (Exception ex)
				{
					ReportError(domInstanceId.Id, new MediaOpsErrorData() { ErrorMessage = ex.ToString() });
				}
			}
		}

		private void TransitionToCompleteFromDeprecated(ICollection<Resource> apiResources)
		{
			if (apiResources == null)
			{
				throw new ArgumentNullException(nameof(apiResources));
			}

			if (apiResources.Count == 0)
			{
				return;
			}

			ValidateStateForCompleteFromDeprecatedAction(apiResources);

			var lockResult = planApi.LockManager.LockAndExecute(apiResources.Where(IsValid).ToList(), TransitionToCompleteFromDeprecatedLocked);
			ReportError(lockResult);
		}

		private void TransitionToCompleteFromDeprecatedLocked(ICollection<Resource> apiResources)
		{
			// The resources were read before the lock was granted. The CORE resource is made available before the DOM
			// transition, so the stored state is validated first.
			var resourcesToRestore = GetStoredDomResources(
				apiResources,
				[SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum.Deprecated],
				[SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum.Complete],
				"Not allowed to restore a resource that is not in Deprecated state.");
			if (resourcesToRestore.Count == 0)
			{
				return;
			}

			CoreResourceHandler.TryRestore(planApi, resourcesToRestore, out var coreResult);

			foreach (var id in coreResult.UnsuccessfulIds)
			{
				ReportError(id);

				if (coreResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					PassTraceData(id, traceData);
				}
			}

			// Transition DOM resources to Complete state
			foreach (var id in coreResult.SuccessfulIds)
			{
				try
				{
					var transitionedInstance = planApi.DomHelpers.SlcResourceStudioHelper.TransitionResourceToCompleteFromDeprecated(id);
					ReportSuccess(new DomResource(transitionedInstance));
				}
				catch (Exception ex)
				{
					ReportError(id, new MediaOpsErrorData() { ErrorMessage = ex.ToString() });
				}
			}
		}

		private void TransitionToDeprecated(ICollection<Resource> apiResources)
		{
			if (apiResources == null)
			{
				throw new ArgumentNullException(nameof(apiResources));
			}

			if (apiResources.Count == 0)
			{
				return;
			}

			ValidateStateForDeprecateAction(apiResources);
			ValidateWorkflowUsage(apiResources.Where(IsValid).ToArray());

			var lockResult = planApi.LockManager.LockAndExecute(apiResources.Where(IsValid).ToList(), TransitionToDeprecatedLocked);
			ReportError(lockResult);
		}

		private void TransitionToDeprecatedLocked(ICollection<Resource> apiResources)
		{
			// The resources were read before the lock was granted. The CORE resource is made unavailable before the DOM
			// transition, so the stored state is validated first.
			var resourcesToDeprecate = GetStoredDomResources(
				apiResources,
				[SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum.Complete],
				[SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum.Deprecated],
				"Not allowed to deprecate a resource that is not in Completed state.");
			if (resourcesToDeprecate.Count == 0)
			{
				return;
			}

			CoreResourceHandler.TryDeprecate(planApi, resourcesToDeprecate, out var coreResult);

			foreach (var id in coreResult.UnsuccessfulIds)
			{
				ReportError(id);

				if (coreResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					PassTraceData(id, traceData);
				}
			}

			// Transition DOM resources to Deprecate state
			foreach (var id in coreResult.SuccessfulIds)
			{
				try
				{
					var transitionedInstance = planApi.DomHelpers.SlcResourceStudioHelper.TransitionResourceToDeprecated(id);
					ReportSuccess(new DomResource(transitionedInstance));
				}
				catch (Exception ex)
				{
					ReportError(id, new MediaOpsErrorData() { ErrorMessage = ex.ToString() });
				}
			}
		}

		private void Delete(ICollection<Resource> apiResources)
		{
			if (apiResources == null)
			{
				throw new ArgumentNullException(nameof(apiResources));
			}

			if (apiResources.Count == 0)
			{
				return;
			}

			var newResources = apiResources.Where(x => x.IsNew).ToList();
			newResources.ForEach(x =>
			{
				var error = new ResourceInvalidStateError
				{
					ErrorMessage = $"A resource that was not saved cannot be removed.",
					Id = x.Id,
				};

				ReportError(x.Id, error);
			});

			apiResources = apiResources.Except(newResources).ToList();

			ValidateStateForDeleteAction(apiResources);

			var resourcesToDelete = apiResources.Where(IsValid).ToList();
			var lockResult = planApi.LockManager.LockAndExecute(resourcesToDelete, DeleteCoreResources);
			ReportError(lockResult);
		}

		private void DeleteCoreResources(ICollection<Resource> resources)
		{
			// The resources were read before the lock was granted. The stored resources are validated and deleted instead,
			// so a resource that was completed in the meantime is not deleted, and the CORE resource that is deleted
			// along with it is the one that is linked now.
			var storedResources = GetStoredDomResources(
				resources,
				[SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum.Draft, SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum.Deprecated],
				[],
				"Not allowed to delete a resource that is not in Draft or Deprecated state.",
				isDelete: true);
			if (storedResources.Count == 0)
			{
				return;
			}

			CoreResourceHandler.TryDelete(planApi, storedResources, out var coreResult);

			foreach (var id in coreResult.UnsuccessfulIds)
			{
				ReportError(id);

				if (coreResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					PassTraceData(id, traceData);
				}
			}

			var resourceInstancesToDelete = storedResources.Where(x => !UnsuccessfulItems.Contains(x.ID.Id)).Select(x => x.ToInstance()).ToList();
			planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryDeleteInBatches(resourceInstancesToDelete, out var domResult);

			foreach (var id in domResult.UnsuccessfulIds)
			{
				ReportError(id.Id);

				if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					var mediaOpsTraceData = new MediaOpsTraceData();
					mediaOpsTraceData.Add(new MediaOpsErrorData { ErrorMessage = traceData.ToString() });

					PassTraceData(id.Id, mediaOpsTraceData);
				}
			}

			ReportSuccess(resourceInstancesToDelete.Where(x => domResult.SuccessfulIds.Contains(x.ID)).Select(x => new DomResource(x)));
		}

		private void UpdateCaches(ICollection<DomResource> domResources)
		{
			if (domResources == null)
			{
				throw new ArgumentNullException(nameof(domResources));
			}

			if (domResources.Count == 0)
			{
				return;
			}

			foreach (var domResource in domResources)
			{
				UpdateResourcePoolCache(domResource);
			}
		}

		private void UpdateResourcePoolCache(DomResource domResource)
		{
			if (resourcePoolsById == null)
			{
				return;
			}

			var domResourcePools = new List<DomResourcePool>();
			foreach (var poolId in domResource.ResourceInternalProperties.PoolIds)
			{
				if (!resourcePoolsById.TryGetValue(poolId, out var resourcePool))
				{
					continue;
				}

				domResourcePools.Add(resourcePool.OriginalInstance);
			}

			if (domResourcePools.Count > 0)
			{
				domResource.DomInstanceCache.SetCache(domResourcePools);
			}
		}

		private void ValidateIdsNotInUse(ICollection<Resource> apiResources)
		{
			if (apiResources == null)
			{
				throw new ArgumentNullException(nameof(apiResources));
			}

			if (apiResources.Count == 0)
			{
				return;
			}

			var resourcesRequiringValidation = apiResources.Where(x => x.IsNew && x.HasUserDefinedId).ToList();
			if (resourcesRequiringValidation.Count == 0)
			{
				return;
			}

			var resourcesWithDuplicateIds = resourcesRequiringValidation
				.GroupBy(resource => resource.Id)
				.Where(g => g.Count() > 1)
				.SelectMany(x => x)
				.ToList();

			foreach (var resource in resourcesWithDuplicateIds)
			{
				var error = new ResourceDuplicateIdError
				{
					ErrorMessage = $"Resource '{resource.Name}' has a duplicate ID.",
					Id = resource.Id,
				};

				ReportError(resource.Id, error);

				resourcesRequiringValidation.Remove(resource);
			}

			foreach (var foundInstance in planApi.DomHelpers.SlcResourceStudioHelper.GetResourceStudioInstances(resourcesRequiringValidation.Select(x => x.Id)))
			{
				planApi.Logger.Information(this, $"ID is already in use by a Resource Studio instance.", [foundInstance.ID.Id]);

				var error = new ResourceIdInUseError
				{
					ErrorMessage = "ID is already in use.",
					Id = foundInstance.ID.Id,
				};

				ReportError(foundInstance.ID.Id, error);
			}
		}

		// The state rules, the state-dependent validations and the decision which changes go to the CORE resource ran
		// against the resources as they were read, before the lock was granted. A resource whose stored state changed
		// since then is rejected. The stored resources are returned so they are not read twice.
		private Dictionary<Guid, DomResource> ValidateStoredStateForUpdateAction(ICollection<Resource> apiResources)
		{
			if (apiResources.Count == 0)
			{
				return new Dictionary<Guid, DomResource>();
			}

			var storedResourcesById = planApi.DomHelpers.SlcResourceStudioHelper.GetResources(apiResources.Select(x => x.Id)).ToDictionary(x => x.ID.Id);
			foreach (var resource in apiResources)
			{
				// A resource that no longer exists is reported by the change detection.
				if (!storedResourcesById.TryGetValue(resource.Id, out var storedResource) || storedResource.Status == resource.OriginalInstance.Status)
				{
					continue;
				}

				ReportError(resource.Id, new ResourceInvalidStateError
				{
					ErrorMessage = $"The state of the resource changed from {resource.State} to {ToResourceState(storedResource.Status)} after the resource was read. Read the resource again and retry the update.",
					Id = resource.Id,
				});
			}

			return storedResourcesById;
		}

		// Re-reads the resources while holding the lock. The resources in one of the expected statuses are returned; a
		// resource that already reached one of the target statuses is reported as successful and any other one as an
		// error. A resource that no longer exists is reported as successful when deleting it, and as an error otherwise.
		private List<DomResource> GetStoredDomResources(
			ICollection<Resource> apiResources,
			ICollection<SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum> expectedStatuses,
			ICollection<SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum> targetStatuses,
			string invalidStateMessage,
			bool isDelete = false)
		{
			var storedResourcesById = planApi.DomHelpers.SlcResourceStudioHelper.GetResources(apiResources.Select(x => x.Id)).ToDictionary(x => x.ID.Id);

			var domResources = new List<DomResource>();
			foreach (var resource in apiResources)
			{
				if (!storedResourcesById.TryGetValue(resource.Id, out var storedResource))
				{
					if (isDelete)
					{
						ReportSuccess(resource.OriginalInstance);
					}
					else
					{
						ReportError(resource.Id, new ResourceNotFoundError { ErrorMessage = $"Resource with ID '{resource.Id}' no longer exists.", Id = resource.Id });
					}
				}
				else if (expectedStatuses.Contains(storedResource.Status))
				{
					domResources.Add(storedResource);
				}
				else if (targetStatuses.Contains(storedResource.Status))
				{
					ReportSuccess(storedResource);
				}
				else
				{
					ReportError(resource.Id, new ResourceInvalidStateError
					{
						ErrorMessage = invalidStateMessage,
						Id = resource.Id,
					});
				}
			}

			return domResources;
		}

		private static ResourceState ToResourceState(SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum status)
		{
			return EnumExtensions.MapEnum<SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum, ResourceState>(status);
		}

		private void ValidateStateForUpdateAction(ICollection<Resource> apiResources)
		{
			if (apiResources == null)
			{
				throw new ArgumentNullException(nameof(apiResources));
			}

			if (apiResources.Count == 0)
			{
				return;
			}

			foreach (var resource in apiResources.Where(x => x.State == ResourceState.Deprecated))
			{
				var error = new ResourceInvalidStateError
				{
					ErrorMessage = "Not allowed to update a resource in Deprecated state.",
					Id = resource.Id,
				};
				ReportError(resource.Id, error);
			}
		}

		private void ValidateStateForCompleteFromDraftAction(ICollection<Resource> apiResources)
		{
			if (apiResources == null)
			{
				throw new ArgumentNullException(nameof(apiResources));
			}

			if (apiResources.Count == 0)
			{
				return;
			}

			foreach (var resource in apiResources.Where(x => x.State != ResourceState.Draft))
			{
				var error = new ResourceInvalidStateError
				{
					ErrorMessage = "Not allowed to complete a resource that is not in Draft state.",
					Id = resource.Id,
				};
				ReportError(resource.Id, error);
			}
		}

		private void ValidateStateForCompleteFromDeprecatedAction(ICollection<Resource> apiResources)
		{
			if (apiResources == null)
			{
				throw new ArgumentNullException(nameof(apiResources));
			}

			if (apiResources.Count == 0)
			{
				return;
			}

			foreach (var resource in apiResources.Where(x => x.State != ResourceState.Deprecated))
			{
				var error = new ResourceInvalidStateError
				{
					ErrorMessage = "Not allowed to restore a resource that is not in Deprecated state.",
					Id = resource.Id,
				};
				ReportError(resource.Id, error);
			}
		}

		private void ValidateStateForDeprecateAction(ICollection<Resource> apiResources)
		{
			if (apiResources == null)
			{
				throw new ArgumentNullException(nameof(apiResources));
			}

			if (apiResources.Count == 0)
			{
				return;
			}

			foreach (var resource in apiResources.Where(x => x.State != ResourceState.Complete))
			{
				var error = new ResourceInvalidStateError
				{
					ErrorMessage = "Not allowed to deprecate a resource that is not in Completed state.",
					Id = resource.Id,
				};
				ReportError(resource.Id, error);
			}
		}

		private void ValidateStateForDeleteAction(ICollection<Resource> apiResources)
		{
			if (apiResources == null)
			{
				throw new ArgumentNullException(nameof(apiResources));
			}

			if (apiResources.Count == 0)
			{
				return;
			}

			foreach (var resource in apiResources.Where(x => !new[] { ResourceState.Draft, ResourceState.Deprecated }.Contains(x.State)))
			{
				var error = new ResourceInvalidStateError
				{
					ErrorMessage = "Not allowed to delete a resource that is not in Draft or Deprecated state.",
					Id = resource.Id,
				};
				ReportError(resource.Id, error);
			}
		}

		private void ValidateNames(ICollection<Resource> apiResources)
		{
			if (apiResources == null)
			{
				throw new ArgumentNullException(nameof(apiResources));
			}

			if (apiResources.Count == 0)
			{
				return;
			}

			var resourcesRequiringValidation = apiResources.ToList();

			foreach (var resource in resourcesRequiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Name)).ToArray())
			{
				var error = new ResourceInvalidNameError
				{
					ErrorMessage = "Name cannot be empty.",
					Id = resource.Id,
				};

				ReportError(resource.Id, error);

				resourcesRequiringValidation.Remove(resource);
			}

			foreach (var resource in resourcesRequiringValidation.Where(x => !InputValidator.HasValidTextLength(x.Name)).ToArray())
			{
				var error = new ResourceInvalidNameError
				{
					ErrorMessage = $"Name exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
					Id = resource.Id,
					Name = resource.Name,
				};

				ReportError(resource.Id, error);

				resourcesRequiringValidation.Remove(resource);
			}

			var resourcesWithDuplicateNames = resourcesRequiringValidation
				.GroupBy(resource => resource.Name)
				.Where(g => g.Count() > 1)
				.SelectMany(x => x)
				.ToList();

			foreach (var resource in resourcesWithDuplicateNames)
			{
				var error = new ResourceDuplicateNameError
				{
					ErrorMessage = $"Resource '{resource.Name}' has a duplicate name.",
					Id = resource.Id,
					Name = resource.Name,
				};

				ReportError(resource.Id, error);
			}
		}

		private void ValidateDomNames(ICollection<Resource> apiResources)
		{
			if (apiResources == null)
			{
				throw new ArgumentNullException(nameof(apiResources));
			}

			if (apiResources.Count == 0)
			{
				return;
			}

			FilterElement<DomInstance> Filter(string name) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Name).Equal(name));

			var domResourcesbyName = planApi.DomHelpers.SlcResourceStudioHelper.GetResources(apiResources.Select(x => x.Name), Filter)
				.GroupBy(x => x.Name)
				.ToDictionary(x => x.Key, x => (IReadOnlyCollection<DomResource>)x.ToList());

			foreach (var resource in apiResources)
			{
				if (!domResourcesbyName.TryGetValue(resource.Name, out var domResources))
				{
					MarkAsResourceWithCoreChanges(resource);
					continue;
				}

				var existingResources = domResources.Where(x => x.ID.Id != resource.Id).ToList();
				if (existingResources.Count == 0)
				{
					MarkAsResourceWithCoreChanges(resource);
					continue;
				}

				planApi.Logger.Information(this, $"Name '{resource.Name}' is already in use by DOM resource(s) with ID(s)", [existingResources.Select(x => x.ID.Id).ToArray()]);

				var error = new ResourceNameExistsError
				{
					ErrorMessage = "Name is already in use.",
					Id = resource.Id,
					Name = resource.Name,
				};

				ReportError(resource.Id, error);
			}
		}

		private void ValidateConcurrency(ICollection<Resource> apiResources)
		{
			if (apiResources == null)
			{
				throw new ArgumentNullException(nameof(apiResources));
			}

			foreach (var apiResource in apiResources)
			{
				if (apiResource.Concurrency < 1)
				{
					var error = new ResourceInvalidConcurrencyError
					{
						ErrorMessage = "Concurrency must be greater than or equal to 1.",
						Id = apiResource.Id,
					};

					ReportError(apiResource.Id, error);
				}
			}
		}

		private void ValidatePoolAssignments(ICollection<Resource> apiResources)
		{
			if (apiResources == null)
			{
				throw new ArgumentNullException(nameof(apiResources));
			}

			if (apiResources.Count == 0)
			{
				return;
			}

			var poolIds = apiResources
				.SelectMany(x => x.ResourcePoolIds)
				.Distinct()
				.ToList();
			resourcePoolsById = planApi.ResourcePools.Read(poolIds).ToDictionary(x => x.Id);

			foreach (var resource in apiResources)
			{
				var hasError = false;

				foreach (var poolId in resource.ResourcePoolIds)
				{
					if (!resourcePoolsById.TryGetValue(poolId, out _))
					{
						var error = new ResourceInvalidAssignedPoolError
						{
							ErrorMessage = $"Resource Pool with ID '{poolId}' not found.",
							Id = resource.Id,
							ResourcePoolId = poolId,
						};

						ReportError(resource.Id, error);
						hasError = true;
					}
				}

				if (!hasError)
				{
					MarkAsResourceWithCoreChanges(resource);
				}
			}
		}

		// Jobs and workflows validate that each resource node's resource is still part of the node's pool, so a resource
		// that is unassigned from a pool it is used from would make those jobs and workflows impossible to update.
		// The removed pools are taken from the stored resource, so a stale snapshot cannot hide a removal.
		private void ValidateRemovedPoolAssignmentUsage(ICollection<Resource> apiResources, ICollection<DomChangeResults> changeResults, IReadOnlyDictionary<Guid, List<Guid>> storedPoolIdsByResourceId)
		{
			if (apiResources.Count == 0)
			{
				return;
			}

			var mergedInstancesById = changeResults.ToDictionary(x => x.Instance.ID.Id, x => new DomResource(x.Instance));
			var removedAssignments = new List<(Resource Resource, Guid ResourcePoolId)>();

			foreach (var resource in apiResources)
			{
				if (!storedPoolIdsByResourceId.TryGetValue(resource.Id, out var storedPoolIds)
					|| !mergedInstancesById.TryGetValue(resource.Id, out var mergedResource))
				{
					continue;
				}

				var mergedPoolIds = mergedResource.ResourceInternalProperties?.PoolIds ?? Enumerable.Empty<Guid>();

				removedAssignments.AddRange(storedPoolIds.Except(mergedPoolIds).Select(x => (resource, x)));
			}

			PassTraceData(SlcWorkflowResourcePoolAssignmentUsageValidator.Validate(planApi, removedAssignments));
		}

		// Only resources in Complete state can be used by jobs and workflows.
		private static Dictionary<Guid, List<Guid>> GetStoredPoolIdsOfCompleteResources(IEnumerable<DomResource> storedResources)
		{
			return storedResources
				.Where(x => x.Status == SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum.Complete)
				.ToDictionary(x => x.ID.Id, x => x.ResourceInternalProperties?.PoolIds?.ToList() ?? new List<Guid>());
		}

		private void ValidateCapacities(ICollection<Resource> apiResources)
		{
			if (apiResources == null)
			{
				throw new ArgumentNullException(nameof(apiResources));
			}

			if (apiResources.Count == 0)
			{
				return;
			}

			var capacityIds = apiResources
				.SelectMany(x => x.Capacities)
				.Select(x => x.Id)
				.Distinct()
				.ToList();
			var capacitiesById = planApi.Capacities.Read(capacityIds).ToDictionary(x => x.Id);

			foreach (var resource in apiResources)
			{
				var duplicateSettings = resource.Capacities
					.GroupBy(x => x.Id)
					.Where(g => g.Count() > 1)
					.ToDictionary(x => x.Key, x => x.Count());

				foreach (var kvp in duplicateSettings)
				{
					var error = new ResourceInvalidCapacitySettingsError
					{
						Id = resource.Id,
						CapacityId = kvp.Key,
						ErrorMessage = $"Capacity with ID '{kvp.Key}' is defined {kvp.Value} times. Duplicate capacity settings are not allowed.",
					};

					ReportError(resource.Id, error);
				}

				if (duplicateSettings.Count > 0)
				{
					continue;
				}

				foreach (var capacitySetting in resource.Capacities)
				{
					if (capacitySetting.Id == Guid.Empty)
					{
						var error = new ResourceInvalidCapacitySettingsError
						{
							ErrorMessage = "Capacity ID cannot be empty.",
							Id = resource.Id,
						};

						ReportError(resource.Id, error);
						continue;
					}

					if (!capacitiesById.TryGetValue(capacitySetting.Id, out var capacity))
					{
						var error = new ResourceInvalidCapacitySettingsError
						{
							ErrorMessage = $"Capacity with ID '{capacitySetting.Id}' not found.",
							CapacityId = capacitySetting.Id,
							Id = resource.Id,
						};

						ReportError(resource.Id, error);
						continue;
					}

					PassTraceData(ResourceCapacitySettingValidator.Validate(resource.Id, capacity, capacitySetting));
				}
			}
		}

		private void ValidateCapabilities(ICollection<Resource> apiResources)
		{
			if (apiResources == null)
			{
				throw new ArgumentNullException(nameof(apiResources));
			}

			if (apiResources.Count == 0)
			{
				return;
			}

			var capabilityIds = apiResources
				.SelectMany(x => x.Capabilities)
				.Select(x => x.Id)
				.Distinct()
				.ToList();
			var capabilitiesById = planApi.Capabilities.Read(capabilityIds).ToDictionary(x => x.Id);

			foreach (var resource in apiResources)
			{
				var duplicateSettings = resource.Capabilities
					.GroupBy(x => x.Id)
					.Where(g => g.Count() > 1)
					.ToDictionary(x => x.Key, x => x.Count());

				foreach (var kvp in duplicateSettings)
				{
					var error = new ResourceInvalidCapabilitySettingsError
					{
						Id = resource.Id,
						CapabilityId = kvp.Key,
						ErrorMessage = $"Capability with ID '{kvp.Key}' is defined {kvp.Value} times. Duplicate capability settings are not allowed.",
					};

					ReportError(resource.Id, error);
				}

				if (duplicateSettings.Count > 0)
				{
					continue;
				}

				foreach (var capabilitySetting in resource.Capabilities)
				{
					if (capabilitySetting.Id == Guid.Empty)
					{
						var error = new ResourceInvalidCapabilitySettingsError
						{
							ErrorMessage = "Capability ID cannot be empty.",
							Id = resource.Id,
						};

						ReportError(resource.Id, error);
						continue;
					}

					if (!capabilitiesById.TryGetValue(capabilitySetting.Id, out var capability))
					{
						var error = new ResourceInvalidCapabilitySettingsError
						{
							ErrorMessage = $"Capability with ID '{capabilitySetting.Id}' not found.",
							CapabilityId = capabilitySetting.Id,
							Id = resource.Id,
						};

						ReportError(resource.Id, error);
						continue;
					}

					if (capabilitySetting.Discretes.Count == 0)
					{
						var error = new ResourceInvalidCapabilitySettingsError
						{
							ErrorMessage = "At least one discrete value must be specified for the capability.",
							CapabilityId = capabilitySetting.Id,
							Id = resource.Id,
						};

						ReportError(resource.Id, error);
						continue;
					}

					foreach (var discreteValue in capabilitySetting.Discretes)
					{
						if (!capability.Discretes.Contains(discreteValue))
						{
							var error = new ResourceInvalidCapabilitySettingsError
							{
								ErrorMessage = $"Discrete value '{discreteValue}' is not valid for capability '{capability.Name}'.",
								CapabilityId = capabilitySetting.Id,
								Id = resource.Id,
							};

							ReportError(resource.Id, error);
						}
					}
				}
			}
		}

		private void ValidateResourceProperties(ICollection<Resource> apiResources)
		{
			if (apiResources == null)
			{
				throw new ArgumentNullException(nameof(apiResources));
			}

			if (apiResources.Count == 0)
			{
				return;
			}

			var propertyIds = apiResources
				.SelectMany(x => x.Properties)
				.Select(x => x.Id)
				.Distinct()
				.ToList();
			var propertiesById = planApi.ResourceProperties.Read(propertyIds).ToDictionary(x => x.Id);

			foreach (var resource in apiResources)
			{
				var duplicateSettings = resource.Properties
					.GroupBy(x => x.Id)
					.Where(g => g.Count() > 1)
					.ToDictionary(x => x.Key, x => x.Count());

				foreach (var kvp in duplicateSettings)
				{
					var error = new ResourceInvalidPropertySettingsError
					{
						Id = resource.Id,
						PropertyId = kvp.Key,
						ErrorMessage = $"Property with ID '{kvp.Key}' is defined {kvp.Value} times. Duplicate property settings are not allowed.",
					};

					ReportError(resource.Id, error);
				}

				if (duplicateSettings.Count > 0)
				{
					continue;
				}

				foreach (var propertySetting in resource.Properties)
				{
					if (propertySetting.Id == Guid.Empty)
					{
						var error = new ResourceInvalidPropertySettingsError
						{
							Id = resource.Id,
							PropertyId = propertySetting.Id,
							ErrorMessage = "Property ID cannot be empty.",
						};

						ReportError(resource.Id, error);
						continue;
					}

					if (!propertiesById.TryGetValue(propertySetting.Id, out _))
					{
						var error = new ResourceInvalidPropertySettingsError
						{
							Id = resource.Id,
							PropertyId = propertySetting.Id,
							ErrorMessage = $"Property with ID '{propertySetting.Id}' not found.",
						};

						ReportError(resource.Id, error);
						continue;
					}

					if (!InputValidator.HasValidTextLength(propertySetting.Value))
					{
						var error = new ResourceInvalidPropertySettingsError
						{
							Id = resource.Id,
							PropertyId = propertySetting.Id,
							ErrorMessage = $"Property value length is limited to {InputValidator.DefaultMaxTextLength} characters.",
						};

						ReportError(resource.Id, error);
					}
				}
			}
		}

		private void ValidateConnectionManagement(ICollection<DomResource> domResources)
		{
			if (domResources == null)
			{
				throw new ArgumentNullException(nameof(domResources));
			}

			if (domResources.Count == 0)
			{
				return;
			}

			if (!planApi.LiveApi.IsInstalled())
			{
				planApi.Logger.Information(this, "Skipping connection management validation because MediaOps Live is not installed.");
				return;
			}

			var virtualSignalGroupIds = domResources
				.SelectMany(x => new[] { x.ResourceConnectionManagement.VirtualSignalGroupInputId, x.ResourceConnectionManagement.VirtualSignalGroupOutputId })
				.Where(x => x != Guid.Empty)
				.Distinct()
				.ToList();

			if (virtualSignalGroupIds.Count == 0)
			{
				return;
			}

			var virtualSignalGroupsById = planApi.LiveApi.VirtualSignalGroups.Read(virtualSignalGroupIds);

			foreach (var domResource in domResources)
			{
				ValidateVirtualSignalGroupLink(domResource.ID.Id, domResource.ResourceConnectionManagement.VirtualSignalGroupInputId, LiveEndpointRole.Destination, "input", virtualSignalGroupsById);
				ValidateVirtualSignalGroupLink(domResource.ID.Id, domResource.ResourceConnectionManagement.VirtualSignalGroupOutputId, LiveEndpointRole.Source, "output", virtualSignalGroupsById);
			}
		}

		private void ValidateVirtualSignalGroupLink(Guid resourceId, Guid virtualSignalGroupId, LiveEndpointRole expectedRole, string direction, IDictionary<Guid, LiveVirtualSignalGroup> virtualSignalGroupsById)
		{
			if (virtualSignalGroupId == Guid.Empty)
			{
				return;
			}

			if (!virtualSignalGroupsById.TryGetValue(virtualSignalGroupId, out var virtualSignalGroup))
			{
				ReportError(resourceId, new ResourceInvalidVirtualSignalGroupError
				{
					ErrorMessage = $"Virtual Signal Group with ID '{virtualSignalGroupId}' not found.",
					Id = resourceId,
					VirtualSignalGroupId = virtualSignalGroupId,
				});

				return;
			}

			if (virtualSignalGroup.Role != expectedRole)
			{
				ReportError(resourceId, new ResourceInvalidVirtualSignalGroupRoleError
				{
					ErrorMessage = $"Virtual Signal Group '{virtualSignalGroup.Name}' ({virtualSignalGroupId}) has role '{virtualSignalGroup.Role}' and cannot be used as {direction}: the {direction} virtual signal group must have role '{expectedRole}'.",
					Id = resourceId,
					VirtualSignalGroupId = virtualSignalGroupId,
				});
			}
		}

		private void ValidateWorkflowUsage(ICollection<Resource> apiResources)
		{
			PassTraceData(SlcWorkflowResourceUsageValidator.Validate(planApi, apiResources));
		}

		private ICollection<DomChangeResults> GetResourcesWithChanges(ICollection<Resource> apiResources, IReadOnlyDictionary<Guid, DomResource> storedResourcesById)
		{
			return GetItemsWithChanges<Resource, DomResource>(
				apiResources,
				p => p.OriginalInstance,
				p => p.GetInstanceWithChanges(),
				ids => ids.Where(storedResourcesById.ContainsKey).Select(id => storedResourcesById[id]),
				p => new ResourceNotFoundError { ErrorMessage = $"Resource with ID '{p.Id}' no longer exists.", Id = p.Id },
				(p, msg) => new ResourceValueAlreadyChangedError { ErrorMessage = msg, Id = p.Id })
				.ToList();
		}

		private void MarkAsResourceWithCoreChanges(Resource resource)
		{
			if (resource == null)
			{
				throw new ArgumentNullException(nameof(resource));
			}

			if (resource.State == ResourceState.Draft)
			{
				return;
			}

			resourceIdsWithCoreChanges.Add(resource.Id);
		}

		private void ConvertToUnmanagedResource(Resource resource)
		{
			var domResource = planApi.DomHelpers.SlcResourceStudioHelper.GetResources([resource.Id]).FirstOrDefault();
			if (domResource == null)
			{
				ReportError(resource.Id, new ResourceNotFoundError
				{
					ErrorMessage = $"Resource with ID '{resource.Id}' not found.",
					Id = resource.Id,
				});

				return;
			}

			domResource.ResourceInfo.Type = SlcResource_StudioIds.Enums.Type.Unmanaged;
			domResource.ResourceInternalProperties.ResourceMetadata = String.Empty;

			CreateOrUpdateDomResources([domResource]);
		}

		private void ConvertToVirtualFunctionResource(Resource resource, ResourceVirtualFunctionLinkSetting setting)
		{
			if (!CoreResourceHandler.TryValidateVirtualFunctionConfiguration(planApi, setting, out var error, resource.CoreResourceId))
			{
				error.Id = resource.Id;
				ReportError(resource.Id, error);
				return;
			}

			var domResource = planApi.DomHelpers.SlcResourceStudioHelper.GetResources([resource.Id]).FirstOrDefault();
			if (domResource == null)
			{
				ReportError(resource.Id, new ResourceNotFoundError
				{
					ErrorMessage = $"Resource with ID '{resource.Id}' not found.",
					Id = resource.Id,
				});

				return;
			}

			domResource.ResourceInfo.Type = SlcResource_StudioIds.Enums.Type.VirtualFunction;
			domResource.ResourceInternalProperties.Metadata = new ResourceMetadata
			{
				LinkedElementInfo = new DmsElementId(setting.AgentId, setting.ElementId).Value,
				LinkedFunctionId = setting.FunctionId,
				LinkedFunctionTableIndex = setting.FunctionTableIndex,
			};

			CreateOrUpdateDomResources([domResource]);
		}

		private void ConvertToServiceResource(Resource resource, ResourceServiceLinkSetting setting)
		{
			if (!CoreResourceHandler.TryValidateServiceConfiguration(planApi, setting, out var error))
			{
				error.Id = resource.Id;
				ReportError(resource.Id, error);
				return;
			}

			var domResource = planApi.DomHelpers.SlcResourceStudioHelper.GetResources([resource.Id]).FirstOrDefault();
			if (domResource == null)
			{
				ReportError(resource.Id, new ResourceNotFoundError
				{
					ErrorMessage = $"Resource with ID '{resource.Id}' not found.",
					Id = resource.Id,
				});

				return;
			}

			domResource.ResourceInfo.Type = SlcResource_StudioIds.Enums.Type.Service;
			domResource.ResourceInternalProperties.Metadata = new ResourceMetadata
			{
				LinkedServiceInfo = new DmsServiceId(setting.AgentId, setting.ServiceId).Value,
			};

			CreateOrUpdateDomResources([domResource]);
		}

		private void ConvertToElementResource(Resource resource, ResourceElementLinkSetting setting)
		{
			if (!CoreResourceHandler.TryValidateElementConfiguration(planApi, setting, out var error))
			{
				error.Id = resource.Id;
				ReportError(resource.Id, error);
				return;
			}

			var domResource = planApi.DomHelpers.SlcResourceStudioHelper.GetResources([resource.Id]).FirstOrDefault();
			if (domResource == null)
			{
				ReportError(resource.Id, new ResourceNotFoundError
				{
					ErrorMessage = $"Resource with ID '{resource.Id}' not found.",
					Id = resource.Id,
				});

				return;
			}

			domResource.ResourceInfo.Type = SlcResource_StudioIds.Enums.Type.Element;
			domResource.ResourceInternalProperties.Metadata = new ResourceMetadata
			{
				LinkedElementInfo = new DmsElementId(setting.AgentId, setting.ElementId).Value,
			};

			CreateOrUpdateDomResources([domResource]);
		}
	}
}
