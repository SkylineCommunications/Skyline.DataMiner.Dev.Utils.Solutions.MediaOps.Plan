namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.Categories.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using DomJob = Storage.DOM.SlcWorkflow.JobsInstance;
	using DomResource = Storage.DOM.SlcResource_Studio.ResourceInstance;

	internal class DomJobHandler : DomInstanceApiObjectValidator<DomJob>
	{
		private readonly MediaOpsPlanApi planApi;
		private readonly DateTimeOffset currentTime;

		// Definition cache scoped to this handler's operation. Created fresh for every create/update so it never
		// outlives a single operation and cannot become stale, while still being shared by every resolver built
		// during the operation so the definitions are read at most once.
		private readonly ReferenceDefinitionCache referenceDefinitions;

		private readonly HashSet<Guid> jobIdsWithCoreChanges = new HashSet<Guid>();

		private readonly Dictionary<Guid, IReadOnlyDictionary<DataReference, ResolvedValue>> resolvedReferencesByJobId = new Dictionary<Guid, IReadOnlyDictionary<DataReference, ResolvedValue>>();

		private readonly Dictionary<Guid, List<OrchestrationSettings>> orchestrationSettingsByJobId = new Dictionary<Guid, List<OrchestrationSettings>>();

		private Dictionary<Guid, Resource> resourcesById = new Dictionary<Guid, Resource>();

		private DomJobHandler(MediaOpsPlanApi planApi)
			: this(planApi, DateTimeOffset.UtcNow)
		{
		}

		private DomJobHandler(MediaOpsPlanApi planApi, DateTimeOffset currentTime)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
			this.currentTime = currentTime;
			this.referenceDefinitions = new ReferenceDefinitionCache(planApi);
		}

		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<Job> apiJobs, out DomInstanceBulkOperationResult<DomJob> result)
		{
			var handler = new DomJobHandler(planApi);
			handler.CreateOrUpdate(apiJobs);

			result = new DomInstanceBulkOperationResult<DomJob>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<Job> apiJobs, out DomInstanceBulkOperationResult<DomJob> result)
		{
			var handler = new DomJobHandler(planApi);
			handler.Delete(apiJobs);

			result = new DomInstanceBulkOperationResult<DomJob>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TrySaveAsTentative(MediaOpsPlanApi planApi, ICollection<Job> apiJobs, out DomInstanceBulkOperationResult<DomJob> result)
		{
			var handler = new DomJobHandler(planApi);
			handler.TransitionToTentativeFromDraft(apiJobs);

			result = new DomInstanceBulkOperationResult<DomJob>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryConfirm(MediaOpsPlanApi planApi, ICollection<Job> apiJobs, out DomInstanceBulkOperationResult<DomJob> result)
		{
			var handler = new DomJobHandler(planApi);
			handler.TransitionToConfirmedFromTentative(apiJobs);

			result = new DomInstanceBulkOperationResult<DomJob>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		private void CreateOrUpdate(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			var toCreate = apiJobs.Where(x => x.IsNew).ToList();
			var toUpdate = apiJobs.Except(toCreate).ToList();

			ValidateIdsNotInUse(toCreate);
			ValidateKeys(toCreate);
			AssignKeys(toCreate);
			AssignNames(toCreate);
			ValidateStateForUpdateAction(toUpdate);

			ValidateNames(apiJobs);
			ValidateCategories(apiJobs);
			ValidateTimings(apiJobs);
			ValidatePreRoll(apiJobs);
			ValidatePostRoll(apiJobs);
			ValidateStateTimings(apiJobs);

			// Apply the node timings before validating the node graph so that restored/added/changed nodes are part of
			// the whole-graph validation, and before the lock's GetJobsWithChanges so the DOM snapshots capture them.
			// Only valid jobs are touched so invalid job-level timings are not propagated onto the nodes. The application
			// is idempotent, so a job whose timings did not change produces no node-timing diff (and no spurious conflict).
			ApplyNodeTimings(apiJobs.Where(IsValid).ToList());

			ValidateNodeGraph(apiJobs);
			ValidateNoResourcePoolNodeForLiveJob(apiJobs);
			ValidateReferences(apiJobs);
			ValidateDescription(apiJobs);
			ValidateNotes(apiJobs);

			var lockResult = planApi.LockManager.LockAndExecute(apiJobs.Where(IsValid).ToList(), CreateOrUpdateLocked);
			ReportError(lockResult);
		}

		private void CreateOrUpdateLocked(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			if (apiJobs.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided jobs are valid", nameof(apiJobs));
			}

			var toCreate = apiJobs.Where(x => x.IsNew).ToList();
			var toUpdate = apiJobs.Except(toCreate).ToList();

			var changeResults = GetJobsWithChanges(toUpdate);

			// Re-validate the timing chain on the merged result while holding the lock. The pre-lock validation ran
			// against this user's own snapshot, but a concurrent user may have changed a different timing field that
			// only became visible after the merge. This check is baseline-independent: it asserts the absolute
			// ordering invariants (PreRollStart <= Start <= End <= PostRollEnd) of the merged window and is only run
			// when this user actually changed a timing field.
			ValidateMergedTimings(toUpdate, changeResults);

			// A reservation only mirrors the job's name, timings and nodes; all other fields are metadata that have no
			// impact on the core reservation. Only mark jobs whose name, timing or node sections actually changed so
			// the CoreJobHandler is not invoked for metadata-only updates.
			var jobsWithReservationChanges = toUpdate.Where(x =>
				IsValid(x)
				&& changeResults.Any(y => y.Id == x.Id && HasReservationImpactingChanges(y)));

			foreach (var job in jobsWithReservationChanges)
			{
				MarkAsJobWithCoreChanges(job);
			}

			CreateOrUpdateOrchestrationSettings(apiJobs.Where(IsValid).ToList());
			CreateOrUpdatePropertySettingCollections(apiJobs.Where(IsValid).ToList());

			var toCreateDomInstances = toCreate
				.Where(IsValid)
				.Select(x => x.GetInstanceWithChanges())
				.ToList();

			var toUpdateDomInstances = changeResults
				.Where(IsValid)
				.Select(x => new DomJob(x.Instance))
				.ToList();

			CreateOrUpdateDomJobs(toCreateDomInstances.Concat(toUpdateDomInstances).ToList());
		}

		private void CreateOrUpdateDomJobs(ICollection<DomJob> domJobs)
		{
			if (domJobs == null)
			{
				throw new ArgumentNullException(nameof(domJobs));
			}

			if (domJobs.Count == 0)
			{
				return;
			}

			var domJobsById = domJobs.ToDictionary(x => x.ID.Id);

			if (jobIdsWithCoreChanges.Count != 0)
			{
				var domJobsWithCoreChanges = domJobs.Where(x => jobIdsWithCoreChanges.Contains(x.ID.Id)).ToList();
				UpdateCaches(domJobsWithCoreChanges);

				CoreJobHandler.TryCreateOrUpdate(planApi, domJobsWithCoreChanges, out var coreResult);

				foreach (var id in coreResult.UnsuccessfulIds)
				{
					ReportError(id);

					if (coreResult.TraceDataPerItem.TryGetValue(id, out var traceData))
					{
						PassTraceData(id, traceData);
					}

					domJobsById.Remove(id);
				}
			}

			planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(domJobsById.Values.Select(x => x.ToInstance()), out var domResult);

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

			ReportSuccess(domResult.SuccessfulItems.Select(x => new DomJob(x)));
		}

		private void CreateOrUpdateOrchestrationSettings(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			if (apiJobs.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided jobs are valid", nameof(apiJobs));
			}

			var jobIdByOrchestrationSettingsId = new Dictionary<Guid, Guid>();
			var orchestrationSettings = new List<OrchestrationSettings>();
			var referenceTargets = new Dictionary<Guid, (ReferenceResolver Resolver, bool ReportErrors)>();

			foreach (var job in apiJobs)
			{
				CollectOrchestrationSettingsForJob(job, jobIdByOrchestrationSettingsId, orchestrationSettings, referenceTargets);
			}

			var referenceValidationContext = referenceTargets.Count > 0
				? new OrchestrationReferenceValidationContext(referenceTargets)
				: null;

			DomWorkflowOrchestrationSettingsHandler.TryCreateOrUpdate(planApi, orchestrationSettings, referenceValidationContext, out var domResult);

			foreach (var id in domResult.UnsuccessfulIds)
			{
				if (!jobIdByOrchestrationSettingsId.TryGetValue(id, out var jobId))
				{
					planApi.Logger.Error(this, $"Failed to find job ID for orchestration settings ID", [id]);
					continue;
				}

				ReportError(jobId);

				if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					PassTraceData(jobId, traceData);
				}
			}
		}

		private void CollectOrchestrationSettingsForJob(
			Job job,
			Dictionary<Guid, Guid> jobIdByOrchestrationSettingsId,
			List<OrchestrationSettings> orchestrationSettings,
			Dictionary<Guid, (ReferenceResolver Resolver, bool ReportErrors)> referenceTargets)
		{
			jobIdByOrchestrationSettingsId[job.OrchestrationSettings.Id] = job.Id;
			orchestrationSettings.Add(job.OrchestrationSettings);

			var jobOrchestrationSettings = new List<OrchestrationSettings> { job.OrchestrationSettings };

			foreach (var node in job.NodeGraph.Nodes)
			{
				jobIdByOrchestrationSettingsId[node.OrchestrationSettings.Id] = job.Id;
				orchestrationSettings.Add(node.OrchestrationSettings);
				jobOrchestrationSettings.Add(node.OrchestrationSettings);
			}

			orchestrationSettingsByJobId[job.Id] = jobOrchestrationSettings;

			// Only Confirmed/Running jobs report unresolved orchestration-event references. Build a single resolver
			// per job and share it across the job's own and its nodes' orchestration settings.
			if (job.State != JobState.Confirmed && job.State != JobState.Running)
			{
				return;
			}

			var resolver = new JobReferenceResolver(planApi, job, referenceDefinitions);
			referenceTargets[job.OrchestrationSettings.Id] = (resolver, true);

			foreach (var node in job.NodeGraph.Nodes)
			{
				referenceTargets[node.OrchestrationSettings.Id] = (resolver, true);
			}
		}

		private void TransitionToTentativeFromDraft(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			ValidateStateForSaveAsTentativeFromDraftAction(apiJobs);
			ValidateEndNotInPast(apiJobs);

			var lockResult = planApi.LockManager.LockAndExecute(apiJobs.Where(IsValid).ToList(), SaveAsTentativeLocked);
			ReportError(lockResult);
		}

		private void SaveAsTentativeLocked(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			if (apiJobs.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided jobs are valid", nameof(apiJobs));
			}

			// Re-read the resources referenced by the node graph so the resource cache used by the reservation sync is
			// populated, and collect (but do not persist) every job's orchestration settings so they can be cached.
			// Settings are not changed during a draft-to-tentative transition, so re-persisting them is not required.
			CollectReferencedIds(apiJobs.ToList(), out var resourceIds, out _);
			resourcesById = planApi.Resources.Read(resourceIds).ToDictionary(x => x.Id);

			foreach (var job in apiJobs.Where(IsValid))
			{
				var jobOrchestrationSettings = new List<OrchestrationSettings> { job.OrchestrationSettings };
				foreach (var node in job.NodeGraph.Nodes)
				{
					jobOrchestrationSettings.Add(node.OrchestrationSettings);
				}

				orchestrationSettingsByJobId[job.Id] = jobOrchestrationSettings;

				// Every job gets a core reservation when it moves to Tentative, even when it has no resource nodes yet.
				// The reservation is created (possibly without bookings) and is populated as resources are assigned.
				jobIdsWithCoreChanges.Add(job.Id);
			}

			// The transition does not modify the job, so the stored instance is used as-is; conflict detection via
			// GetJobsWithChanges adds no value when no changes are applied.
			var domJobs = apiJobs.Where(IsValid).Select(x => x.OriginalInstance).ToList();

			TransitionDomJobsToTentative(domJobs);
		}

		private void TransitionDomJobsToTentative(ICollection<DomJob> domJobs)
		{
			if (domJobs == null)
			{
				throw new ArgumentNullException(nameof(domJobs));
			}

			if (domJobs.Count == 0)
			{
				return;
			}

			var domJobsById = domJobs.ToDictionary(x => x.ID.Id);

			if (jobIdsWithCoreChanges.Count != 0)
			{
				var domJobsWithCoreChanges = domJobs.Where(x => jobIdsWithCoreChanges.Contains(x.ID.Id)).ToList();
				UpdateCaches(domJobsWithCoreChanges);

				CoreJobHandler.TryCreateOrUpdate(planApi, domJobsWithCoreChanges, out var coreResult);

				foreach (var id in coreResult.UnsuccessfulIds)
				{
					ReportError(id);

					if (coreResult.TraceDataPerItem.TryGetValue(id, out var traceData))
					{
						PassTraceData(id, traceData);
					}

					domJobsById.Remove(id);
				}
			}

			// The transition applies no field changes, so the DOM jobs are not re-saved; DoStatusTransition persists the
			// status change on its own. Only jobs whose reservation sync succeeded are transitioned.
			foreach (var domJob in domJobsById.Values)
			{
				try
				{
					var transitionedInstance = planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances.DoStatusTransition(domJob.ID, Storage.DOM.SlcWorkflow.SlcWorkflowIds.Behaviors.Job_Behavior.Transitions.Draft_To_Tentative);
					ReportSuccess(new DomJob(transitionedInstance));
				}
				catch (Exception ex)
				{
					ReportError(domJob.ID.Id, new MediaOpsErrorData() { ErrorMessage = ex.ToString() });
				}
			}
		}

		private void TransitionToConfirmedFromTentative(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			ValidateStateForConfirmFromTentativeAction(apiJobs);
			ValidateEndNotInPast(apiJobs);
			ValidateAllNodesHaveResourceAssigned(apiJobs);
			ValidateNoMandatoryConfigurationMissing(apiJobs);
			ValidateReferencesForConfirm(apiJobs);

			var lockResult = planApi.LockManager.LockAndExecute(apiJobs.Where(IsValid).ToList(), ConfirmLocked);
			ReportError(lockResult);
		}

		private void ConfirmLocked(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			if (apiJobs.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided jobs are valid", nameof(apiJobs));
			}

			// Every Tentative job has a core reservation (created during the draft-to-tentative transition), including
			// resource-less jobs, so every job's reservation must be confirmed.
			foreach (var job in apiJobs)
			{
				jobIdsWithCoreChanges.Add(job.Id);
			}

			// The transition does not modify the job, so the stored instance is used as-is; conflict detection via
			// GetJobsWithChanges adds no value when no changes are applied.
			var domJobs = apiJobs.Select(x => x.OriginalInstance).ToList();

			TransitionDomJobsToConfirmed(domJobs);
		}

		private void TransitionDomJobsToConfirmed(ICollection<DomJob> domJobs)
		{
			if (domJobs == null)
			{
				throw new ArgumentNullException(nameof(domJobs));
			}

			if (domJobs.Count == 0)
			{
				return;
			}

			var domJobsById = domJobs.ToDictionary(x => x.ID.Id);

			if (jobIdsWithCoreChanges.Count != 0)
			{
				// A confirm does not change the reservation contents; the reservation was already synced while the job
				// was edited in Tentative state. Only its status is flipped to Confirmed.
				var domJobsWithCoreChanges = domJobs.Where(x => jobIdsWithCoreChanges.Contains(x.ID.Id)).ToList();

				CoreJobHandler.TryConfirm(planApi, domJobsWithCoreChanges, out var coreResult);

				foreach (var id in coreResult.UnsuccessfulIds)
				{
					ReportError(id);

					if (coreResult.TraceDataPerItem.TryGetValue(id, out var traceData))
					{
						PassTraceData(id, traceData);
					}

					domJobsById.Remove(id);
				}
			}

			// The transition applies no field changes, so the DOM jobs are not re-saved; DoStatusTransition persists the
			// status change on its own. Only jobs whose reservation confirmation succeeded are transitioned.
			foreach (var domJob in domJobsById.Values)
			{
				try
				{
					var transitionedInstance = planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances.DoStatusTransition(domJob.ID, Storage.DOM.SlcWorkflow.SlcWorkflowIds.Behaviors.Job_Behavior.Transitions.Tentative_To_Confirmed);
					ReportSuccess(new DomJob(transitionedInstance));
				}
				catch (Exception ex)
				{
					ReportError(domJob.ID.Id, new MediaOpsErrorData() { ErrorMessage = ex.ToString() });
				}
			}
		}

		private void ValidateEndNotInPast(ICollection<Job> apiJobs)
		{
			foreach (var job in apiJobs.Where(x => IsValid(x) && x.End < currentTime))
			{
				ReportError(job.Id, new JobInvalidEndTimeError
				{
					ErrorMessage = "A job whose end time lies in the past cannot be moved to Tentative state.",
					Id = job.Id,
					End = job.End,
				});
			}
		}

		private void Delete(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			ValidateStateForDeleteAction(apiJobs);

			var lockResult = planApi.LockManager.LockAndExecute(apiJobs.Where(IsValid).ToList(), DeleteLocked);
			ReportError(lockResult);
		}

		private void DeleteLocked(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			if (apiJobs.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided jobs are valid", nameof(apiJobs));
			}

			DeleteOrchestrationSettings(apiJobs);
			DeletePropertySettingCollections(apiJobs);

			var domJobsById = apiJobs.ToDictionary(x => x.Id, x => x.OriginalInstance);

			var instancesToDelete = domJobsById.Values.Select(x => x.ToInstance()).ToArray();
			planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances.TryDeleteInBatches(instancesToDelete, out var domResult);

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

			ReportSuccess(instancesToDelete.Where(x => domResult.SuccessfulIds.Contains(x.ID)).Select(x => new DomJob(x)).ToArray());
		}

		private void DeleteOrchestrationSettings(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			if (apiJobs.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided jobs are valid", nameof(apiJobs));
			}

			DomWorkflowOrchestrationSettingsHandler.TryDelete(planApi, apiJobs.Select(x => x.OrchestrationSettings).ToList(), out _);
		}

		private void CreateOrUpdatePropertySettingCollections(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			if (apiJobs.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided jobs are valid", nameof(apiJobs));
			}

			// Make sure every job has a property values context so that newly created scopes
			// (owner and nodes) pick up the correct LinkedObjectId when the user added properties
			// prior to saving.
			foreach (var job in apiJobs)
			{
				job.EnsureContext();
			}

			var ownerScopes = new List<KeyValuePair<Guid, PropertySettingsScope>>();
			foreach (var job in apiJobs)
			{
				ownerScopes.Add(new KeyValuePair<Guid, PropertySettingsScope>(job.Id, job.PropertySettingsScope));

				foreach (var node in job.NodeGraph.Nodes)
				{
					ownerScopes.Add(new KeyValuePair<Guid, PropertySettingsScope>(job.Id, node.PropertySettingsScope));
				}
			}

			var (toCreateOrUpdate, toDelete, jobIdByCollectionId) = ownerScopes.BuildPersistenceActions();

			if (toCreateOrUpdate.Count > 0)
			{
				DomPropertySettingCollectionHandler.TryCreateOrUpdate(planApi, toCreateOrUpdate, out var result);
				ReportPropertySettingCollectionFailures(result, jobIdByCollectionId);
			}

			if (toDelete.Count > 0)
			{
				DomPropertySettingCollectionHandler.TryDelete(planApi, toDelete, out var result);
				ReportPropertySettingCollectionFailures(result, jobIdByCollectionId);
			}
		}

		private void DeletePropertySettingCollections(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			if (apiJobs.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided jobs are valid", nameof(apiJobs));
			}

			var jobIdByCollectionId = new Dictionary<Guid, Guid>();
			var toDelete = new List<PropertySettingCollection>();

			var jobsRequiringQuery = CollectCachedCollectionsToDelete(apiJobs, jobIdByCollectionId, toDelete);
			QueryCollectionsToDelete(jobsRequiringQuery, jobIdByCollectionId, toDelete);

			if (toDelete.Count == 0)
			{
				return;
			}

			DomPropertySettingCollectionHandler.TryDelete(planApi, toDelete, out var domResult);
			ReportPropertySettingCollectionFailures(domResult, jobIdByCollectionId);
		}

		private static Dictionary<string, Guid> CollectCachedCollectionsToDelete(ICollection<Job> apiJobs, Dictionary<Guid, Guid> jobIdByCollectionId, List<PropertySettingCollection> toDelete)
		{
			var jobsRequiringQuery = new Dictionary<string, Guid>();

			foreach (var job in apiJobs)
			{
				var cached = job.PropertySettingsContext?.TryGetCachedOriginalCollections();
				if (cached == null)
				{
					jobsRequiringQuery[job.Id.ToString()] = job.Id;
					continue;
				}

				foreach (var collection in cached)
				{
					jobIdByCollectionId[collection.Id] = job.Id;
					toDelete.Add(collection);
				}
			}

			return jobsRequiringQuery;
		}

		private void QueryCollectionsToDelete(Dictionary<string, Guid> jobsRequiringQuery, Dictionary<Guid, Guid> jobIdByCollectionId, List<PropertySettingCollection> toDelete)
		{
			if (jobsRequiringQuery.Count == 0)
			{
				return;
			}

			var linkedObjectIdFilter = new ORFilterElement<PropertySettingCollection>(
				jobsRequiringQuery.Keys.Select(id => PropertySettingCollectionExposers.LinkedObjectId.Equal(id)).ToArray());

			var filter = new ANDFilterElement<PropertySettingCollection>(
				linkedObjectIdFilter,
				PropertySettingCollectionExposers.Scope.Equal(PropertySettingsContext.MediaOpsScope));

			foreach (var collection in planApi.PropertySettingCollections.Read(filter))
			{
				if (collection.LinkedObjectId != null && jobsRequiringQuery.TryGetValue(collection.LinkedObjectId, out var jobId))
				{
					jobIdByCollectionId[collection.Id] = jobId;
					toDelete.Add(collection);
				}
			}
		}

		private void ReportPropertySettingCollectionFailures(
			DomInstanceBulkOperationResult<Storage.DOM.SlcProperties.PropertyValuesInstance> result,
			Dictionary<Guid, Guid> jobIdByCollectionId)
		{
			if (result == null || !result.HasFailures)
			{
				return;
			}

			foreach (var id in result.UnsuccessfulIds)
			{
				if (!jobIdByCollectionId.TryGetValue(id, out var jobId))
				{
					planApi.Logger.Error(this, $"Failed to find job ID for property value collection ID {id}.");
					continue;
				}

				ReportError(jobId);

				if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					PassTraceData(jobId, traceData);
				}
			}
		}

		private void UpdateCaches(ICollection<DomJob> domJobs)
		{
			if (domJobs == null)
			{
				throw new ArgumentNullException(nameof(domJobs));
			}

			if (domJobs.Count == 0)
			{
				return;
			}

			foreach (var domJob in domJobs)
			{
				UpdateResourceCache(domJob);
				UpdateResolvedReferenceCache(domJob);
				UpdateOrchestrationSettingsCache(domJob);
			}
		}

		private void UpdateResolvedReferenceCache(DomJob domJob)
		{
			if (resolvedReferencesByJobId.TryGetValue(domJob.ID.Id, out var resolvedReferences) && resolvedReferences.Count > 0)
			{
				domJob.ResolvedReferenceCache.SetCache(resolvedReferences);
			}
		}

		private void UpdateOrchestrationSettingsCache(DomJob domJob)
		{
			if (orchestrationSettingsByJobId.TryGetValue(domJob.ID.Id, out var orchestrationSettings) && orchestrationSettings.Count > 0)
			{
				domJob.OrchestrationSettingsCache.SetCache(orchestrationSettings.ToDictionary(x => x.Id));
			}
		}

		private void UpdateResourceCache(DomJob domJob)
		{
			if (resourcesById == null)
			{
				return;
			}

			var resourceIds = domJob.Nodes
				.Where(x => x.NodeType == Storage.DOM.SlcWorkflow.SlcWorkflowIds.Enums.Nodetype.Resource)
				.Select(x => x.ReferenceId)
				.Distinct()
				.ToList();

			var domResources = new List<DomResource>();
			foreach (var resourceId in resourceIds)
			{
				if (!resourcesById.TryGetValue(resourceId, out var resource))
				{
					continue;
				}

				domResources.Add(resource.OriginalInstance);
			}

			if (domResources.Count > 0)
			{
				domJob.DomInstanceCache.SetCache(domResources);
			}
		}

		private void AssignKeys(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			var toAssign = apiJobs.Where(x => x.IsNew && string.IsNullOrEmpty(x.Key)).ToList();
			if (toAssign.Count == 0)
			{
				return;
			}

			DomJobSettingHandler.TryGetNextKeys(planApi, toAssign.Count, out var keys, out var result);

			if (result.HasFailures)
			{
				var traceData = result.TraceDataPerItem.First().Value;

				foreach (var job in apiJobs)
				{
					ReportError(job.Id, traceData.ErrorData.FirstOrDefault() ?? new MediaOpsErrorData { ErrorMessage = "Failed to generate key." });
				}

				return;
			}

			for (int i = 0; i < toAssign.Count; i++)
			{
				toAssign[i].AssignKey(keys[i]);
			}
		}

		private void AssignNames(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			var toAssign = apiJobs.Where(x => x.IsNew && string.IsNullOrEmpty(x.Name)).ToList();
			if (toAssign.Count == 0)
			{
				return;
			}

			foreach (var job in toAssign)
			{
				job.Name = job.Key;
			}
		}

		private void ApplyNodeTimings(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			foreach (var job in apiJobs)
			{
				var requested = JobTimingWindow.FromJob(job);

				JobNodeTimingResolver.Apply(job.State, requested, currentTime, job.NodeGraph);
			}
		}

		private void ValidateIdsNotInUse(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			var jobsRequiringValidation = apiJobs.Where(x => x.IsNew && x.HasUserDefinedId).ToList();
			if (jobsRequiringValidation.Count == 0)
			{
				return;
			}

			var jobsWithDuplicateIds = jobsRequiringValidation
				.GroupBy(pool => pool.Id)
				.Where(g => g.Count() > 1)
				.SelectMany(x => x)
				.ToList();

			foreach (var job in jobsWithDuplicateIds)
			{
				var error = new JobDuplicateIdError
				{
					ErrorMessage = $"Job '{job.Name}' has a duplicate ID.",
					Id = job.Id,
				};

				ReportError(job.Id, error);

				jobsRequiringValidation.Remove(job);
			}

			foreach (var foundInstance in planApi.DomHelpers.SlcWorkflowHelper.GetWorkflowInstances(jobsRequiringValidation.Select(x => x.Id)))
			{
				planApi.Logger.Information(this, $"ID is already in use by a Workflow instance.", [foundInstance.ID.Id]);

				var error = new JobIdInUseError
				{
					ErrorMessage = "ID is already in use.",
					Id = foundInstance.ID.Id,
				};

				ReportError(foundInstance.ID.Id, error);
			}
		}

		private void ValidateStateForUpdateAction(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			foreach (var job in apiJobs.Where(x => x.State == JobState.Completed || x.State == JobState.Canceled))
			{
				var error = new JobInvalidStateError
				{
					ErrorMessage = "Not allowed to update a job that is completed or canceled.",
					Id = job.Id,
				};

				ReportError(job.Id, error);
			}
		}

		private void ValidateStateForDeleteAction(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			var isNew = apiJobs.Where(x => x.IsNew).ToList();
			foreach (var job in isNew)
			{
				var error = new JobInvalidStateError
				{
					ErrorMessage = "Not allowed to delete a job that has not been created yet.",
					Id = job.Id,
				};

				ReportError(job.Id, error);
			}

			foreach (var job in apiJobs
				.Except(isNew)
				.Where(x => x.State != JobState.Draft))
			{
				var error = new JobInvalidStateError
				{
					ErrorMessage = "Not allowed to delete a job that is not in Draft state.",
					Id = job.Id,
				};

				ReportError(job.Id, error);
			}
		}

		private void ValidateStateForSaveAsTentativeFromDraftAction(ICollection<Job> apiJobs)
		{
			foreach (var job in apiJobs.Where(x => x.IsNew || x.State != JobState.Draft))
			{
				ReportError(job.Id, new JobInvalidStateError
				{
					ErrorMessage = "Only jobs in Draft state can be moved to Tentative state.",
					Id = job.Id,
				});
			}
		}

		private void ValidateStateForConfirmFromTentativeAction(ICollection<Job> apiJobs)
		{
			foreach (var job in apiJobs.Where(x => x.IsNew || x.State != JobState.Tentative))
			{
				ReportError(job.Id, new JobInvalidStateError
				{
					ErrorMessage = "Only jobs in Tentative state can be confirmed.",
					Id = job.Id,
				});
			}
		}

		private void ValidateNoResourcePoolNodeForLiveJob(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			// A Confirmed or Running job is backed by a confirmed reservation, which can only hold concrete resources.
			// A resource pool node represents an unresolved booking, so it is rejected for jobs in those states.
			foreach (var job in apiJobs.Where(x => IsValid(x) && (x.State == JobState.Confirmed || x.State == JobState.Running)))
			{
				foreach (var node in job.NodeGraph.Nodes.Where(x => x.IsResourcePoolNode(out _)))
				{
					ReportError(job.Id, new JobResourcePoolNodeNotAllowedError
					{
						ErrorMessage = "A resource pool node is not allowed on a job in the Confirmed or Running state.",
						Id = job.Id,
						NodeId = node.Id,
					});
				}
			}
		}

		private void ValidateAllNodesHaveResourceAssigned(ICollection<Job> apiJobs)
		{
			foreach (var job in apiJobs.Where(IsValid))
			{
				foreach (var node in job.NodeGraph.Nodes)
				{
					if (node.IsResourcePoolNode(out _) || (node.IsResourceNode(out var resourceNode) && resourceNode.ResourceId == Guid.Empty))
					{
						ReportError(job.Id, new JobNodeResourceNotAssignedError
						{
							ErrorMessage = "A job can only be confirmed when all of its nodes have a concrete resource assigned.",
							Id = job.Id,
							NodeId = node.Id,
						});
					}
				}
			}
		}

		private void ValidateNoMandatoryConfigurationMissing(ICollection<Job> apiJobs)
		{
			foreach (var job in apiJobs.Where(IsValid))
			{
				foreach (var node in job.NodeGraph.Nodes.Where(x => x.NodeConfigurationStatus == NodeConfigurationStatus.MandatoryValuesMissing))
				{
					ReportError(job.Id, new JobMandatoryConfigurationMissingError
					{
						ErrorMessage = "A job can only be confirmed when all mandatory configuration values are provided.",
						Id = job.Id,
						NodeId = node.Id,
					});
				}
			}
		}

		private void ValidateReferencesForConfirm(ICollection<Job> apiJobs)
		{
			foreach (var job in apiJobs.Where(IsValid))
			{
				var resolver = new JobReferenceResolver(planApi, job, referenceDefinitions);
				var resolution = new JobReferenceValidator(resolver).Resolve(job);

				foreach (var reference in resolution.UnresolvedReferences)
				{
					var label = resolver.GetDisplayLabel(reference);
					ReportError(job.Id, new JobUnresolvedReferenceError
					{
						Id = job.Id,
						Reference = label,
						ErrorMessage = $"Reference '{label}' could not be resolved to a value.",
					});
				}
			}
		}

		private void ValidateKeys(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			var requiringValidation = apiJobs.Where(x => !string.IsNullOrEmpty(x.Key)).ToList();

			foreach (var job in requiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Key)).ToArray())
			{
				var error = new JobInvalidKeyError
				{
					ErrorMessage = $"Key cannot be empty.",
					Id = job.Id,
				};

				ReportError(job.Id, error);
				requiringValidation.Remove(job);
			}

			foreach (var job in requiringValidation.Where(x => !InputValidator.HasValidTextLength(x.Key)).ToArray())
			{
				var error = new JobInvalidKeyError
				{
					ErrorMessage = $"Key exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
					Key = job.Key,
					Id = job.Id,
				};

				ReportError(job.Id, error);
			}
		}

		private void ValidateNames(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			var requiringValidation = apiJobs.Where(x => !string.IsNullOrEmpty(x.Name)).ToList();

			foreach (var job in requiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Name)).ToArray())
			{
				var error = new JobInvalidNameError
				{
					ErrorMessage = $"Name cannot be empty.",
					Id = job.Id,
				};

				ReportError(job.Id, error);
				requiringValidation.Remove(job);
			}

			foreach (var job in requiringValidation.Where(x => !InputValidator.HasValidTextLength(x.Name)).ToArray())
			{
				var error = new JobInvalidNameError
				{
					ErrorMessage = $"Name exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
					Name = job.Name,
					Id = job.Id,
				};

				ReportError(job.Id, error);
			}
		}

		private void ValidateCategories(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}
			if (apiJobs.Count == 0)
			{
				return;
			}

			var toValidate = apiJobs.Where(x => !string.IsNullOrEmpty(x.CategoryId)).ToList();
			if (toValidate.Count == 0)
			{
				return;
			}

			var scope = planApi.Categories.Scopes.Read(ScopeExposers.Name.Equal("Job Types")).FirstOrDefault();
			if (scope == null)
			{
				foreach (var job in toValidate)
				{
					var error = new JobCategoryScopeNotFoundError
					{
						ErrorMessage = "Category with scope 'Job Types' not found.",
						Id = job.Id,
					};

					ReportError(job.Id, error);
				}

				return;
			}

			var categoryIds = planApi.Categories.Categories.GetByScope(scope).Select(x => x.ID.ToString()).ToList();

			foreach (var job in toValidate)
			{
				if (!categoryIds.Contains(job.CategoryId))
				{
					if (job.CategoryId.Equals("Scheduling", StringComparison.InvariantCultureIgnoreCase))
					{
						// Translate previous fixed source to new fixed category id.
						job.CategoryId = Convert.ToString(JobTypes.Scheduled);
						continue;
					}

					var error = new JobCategoryNotFoundError
					{
						ErrorMessage = $"Category with ID '{job.CategoryId}' not found in Scope 'Job Types'.",
						CategoryId = job.CategoryId,
						Id = job.Id,
					};

					ReportError(job.Id, error);
				}
			}
		}

		private void ValidateDescription(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			foreach (var job in apiJobs.Where(x => InputValidator.IsNonEmptyText(x.Description) && !InputValidator.HasValidTextSize(x.Description)))
			{
				var error = new JobInvalidDescriptionError
				{
					ErrorMessage = $"Description exceeds maximum size of {InputValidator.DefaultMaxTextSize} bytes.",
					Description = job.Description,
					Id = job.Id,
				};

				ReportError(job.Id, error);
			}
		}

		private void ValidateNotes(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			foreach (var job in apiJobs.Where(x => InputValidator.IsNonEmptyText(x.Notes) && !InputValidator.HasValidTextSize(x.Notes)))
			{
				var error = new JobInvalidNotesError
				{
					ErrorMessage = $"Notes exceeds maximum size of {InputValidator.DefaultMaxTextSize} bytes.",
					Notes = job.Notes,
					Id = job.Id,
				};

				ReportError(job.Id, error);
			}
		}

		private void ValidateTimings(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			var toValidate = apiJobs.ToList();

			foreach (var job in toValidate.Where(x => x.Start == default).ToArray())
			{
				var error = new JobInvalidStartTimeError
				{
					ErrorMessage = "Start time is required.",
					Id = job.Id,
					Start = job.Start,
				};

				ReportError(job.Id, error);
				toValidate.Remove(job);
			}

			foreach (var job in toValidate.Where(x => x.End == default).ToArray())
			{
				var error = new JobInvalidEndTimeError
				{
					ErrorMessage = "End time is required.",
					Id = job.Id,
					End = job.End,
				};

				ReportError(job.Id, error);
				toValidate.Remove(job);
			}

			foreach (var job in toValidate.Where(x => x.Start.Ticks % TimeSpan.TicksPerSecond != 0))
			{
				var error = new JobInvalidStartTimeError
				{
					ErrorMessage = "Start time must not have sub-second precision.",
					Id = job.Id,
					Start = job.Start,
				};

				ReportError(job.Id, error);
			}

			foreach (var job in toValidate.Where(x => x.End.Ticks % TimeSpan.TicksPerSecond != 0))
			{
				var error = new JobInvalidEndTimeError
				{
					ErrorMessage = "End time must not have sub-second precision.",
					Id = job.Id,
					End = job.End,
				};

				ReportError(job.Id, error);
			}

			foreach (var job in toValidate.Where(x => x.End < x.Start))
			{
				var error = new JobInvalidTimingError
				{
					ErrorMessage = "Start time must be before end time.",
					Id = job.Id,
					Start = job.Start,
					End = job.End,
				};

				ReportError(job.Id, error);
			}
		}

		private void ValidatePreRoll(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			var toValidate = apiJobs.ToList();

			foreach (var job in toValidate.Where(x => x.PreRollStart == default).ToArray())
			{
				var error = new JobInvalidPreRollError
				{
					ErrorMessage = "Pre-roll start time is required.",
					Id = job.Id,
					PreRollStart = job.PreRollStart,
					Start = job.Start,
				};

				ReportError(job.Id, error);
				toValidate.Remove(job);
			}

			foreach (var job in toValidate.Where(x => x.PreRollStart.Ticks % TimeSpan.TicksPerSecond != 0).ToArray())
			{
				var error = new JobInvalidPreRollError
				{
					ErrorMessage = "Pre-roll start time must not have sub-second precision.",
					Id = job.Id,
					PreRollStart = job.PreRollStart,
					Start = job.Start,
				};

				ReportError(job.Id, error);
				toValidate.Remove(job);
			}

			foreach (var job in toValidate.Where(x => x.PreRollStart > x.Start))
			{
				var error = new JobInvalidPreRollError
				{
					ErrorMessage = "Pre-roll start cannot be after the job start time.",
					Id = job.Id,
					PreRollStart = job.PreRollStart,
					Start = job.Start,
				};

				ReportError(job.Id, error);
			}
		}

		private void ValidatePostRoll(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			var toValidate = apiJobs.ToList();

			foreach (var job in toValidate.Where(x => x.PostRollEnd == default).ToArray())
			{
				var error = new JobInvalidPostRollError
				{
					ErrorMessage = "Post-roll end time is required.",
					Id = job.Id,
					PostRollEnd = job.PostRollEnd,
					End = job.End,
				};

				ReportError(job.Id, error);
				toValidate.Remove(job);
			}

			foreach (var job in toValidate.Where(x => x.PostRollEnd.Ticks % TimeSpan.TicksPerSecond != 0).ToArray())
			{
				var error = new JobInvalidPostRollError
				{
					ErrorMessage = "Post-roll end time must not have sub-second precision.",
					Id = job.Id,
					PostRollEnd = job.PostRollEnd,
					End = job.End,
				};

				ReportError(job.Id, error);
				toValidate.Remove(job);
			}

			foreach (var job in toValidate.Where(x => x.PostRollEnd < x.End))
			{
				var error = new JobInvalidPostRollError
				{
					ErrorMessage = "Post-roll end cannot be before the job end time.",
					Id = job.Id,
					PostRollEnd = job.PostRollEnd,
					End = job.End,
				};

				ReportError(job.Id, error);
			}
		}

		private void ValidateStateTimings(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			// A new job has no stored baseline, so there are no state-based change restrictions to enforce yet.
			foreach (var job in apiJobs.Where(x => !x.IsNew))
			{
				var requested = JobTimingWindow.FromJob(job);
				var original = JobTimingWindow.FromInstance(job.OriginalInstance);

				var errors = JobNodeTimingResolver.Validate(job.Id, job.State, requested, original, currentTime);
				foreach (var error in errors)
				{
					ReportError(job.Id, error);
				}
			}
		}

		private void ValidateMergedTimings(ICollection<Job> toUpdate, ICollection<DomChangeResults> changeResults)
		{
			if (toUpdate == null)
			{
				throw new ArgumentNullException(nameof(toUpdate));
			}

			if (changeResults == null)
			{
				throw new ArgumentNullException(nameof(changeResults));
			}

			if (changeResults.Count == 0)
			{
				return;
			}

			var jobsById = toUpdate.ToDictionary(x => x.Id);

			foreach (var changeResult in changeResults.Where(IsValid))
			{
				if (!jobsById.TryGetValue(changeResult.Id, out var job))
				{
					continue;
				}

				// The merged window only matters when this user actually changed one of the timing boundaries; an
				// unrelated change (e.g. notes) cannot introduce a timing-ordering violation on its own.
				var requested = JobTimingWindow.FromJob(job);
				var original = JobTimingWindow.FromInstance(job.OriginalInstance);
				if (!requested.GetChanges(original).Any)
				{
					continue;
				}

				var mergedWindow = JobTimingWindow.FromInstance(new DomJob(changeResult.Instance));

				var errors = JobNodeTimingResolver.ValidateTimingChainOrdering(job.Id, mergedWindow);
				foreach (var error in errors)
				{
					ReportError(job.Id, error);
				}
			}
		}

		private void ValidateNodeGraph(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			CollectReferencedIds(apiJobs, out var resourceIds, out var resourcePoolIds);

			resourcesById = planApi.Resources.Read(resourceIds).ToDictionary(x => x.Id);
			var resourcePoolsById = planApi.ResourcePools.Read(resourcePoolIds).ToDictionary(x => x.Id);

			foreach (var job in apiJobs)
			{
				PassTraceData(JobNodeGraphValidator.Validate(job.Id, job.NodeGraph, resourcesById, resourcePoolsById));
			}
		}

		// Resolves the settings references of every job that has reached a state where its references are expected to
		// point at concrete values. Tentative jobs are resolved so the resolved values can be cached for later use, but
		// only Confirmed/Running jobs report unresolved references as errors. Must run after ValidateNodeGraph so the
		// node graph (and therefore the resources referenced by the resolver) is known to be valid.
		private void ValidateReferences(ICollection<Job> apiJobs)
		{
			if (apiJobs == null)
			{
				throw new ArgumentNullException(nameof(apiJobs));
			}

			if (apiJobs.Count == 0)
			{
				return;
			}

			foreach (var job in apiJobs.Where(x => IsValid(x) && RequiresReferenceResolution(x.State)))
			{
				var resolver = new JobReferenceResolver(planApi, job, referenceDefinitions);
				var resolution = new JobReferenceValidator(resolver).Resolve(job);

				resolvedReferencesByJobId[job.Id] = resolution.ResolvedReferences;

				if (job.State != JobState.Confirmed && job.State != JobState.Running)
				{
					continue;
				}

				foreach (var reference in resolution.UnresolvedReferences)
				{
					var label = resolver.GetDisplayLabel(reference);
					ReportError(job.Id, new JobUnresolvedReferenceError
					{
						Id = job.Id,
						Reference = label,
						ErrorMessage = $"Reference '{label}' could not be resolved to a value.",
					});
				}
			}
		}

		private static bool RequiresReferenceResolution(JobState state)
		{
			return state == JobState.Tentative || state == JobState.Confirmed || state == JobState.Running;
		}

		private static void CollectReferencedIds(ICollection<Job> apiJobs, out HashSet<Guid> resourceIds, out HashSet<Guid> resourcePoolIds)
		{
			resourceIds = new HashSet<Guid>();
			resourcePoolIds = new HashSet<Guid>();

			var allNodes = apiJobs.SelectMany(j => j.NodeGraph.Nodes);
			foreach (var node in allNodes)
			{
				CollectNodeIds(node, resourceIds, resourcePoolIds);
			}
		}

		private static void CollectNodeIds(JobNode node, HashSet<Guid> resourceIds, HashSet<Guid> resourcePoolIds)
		{
			switch (node)
			{
				case IResourceNode r:
					AddIfNotEmpty(resourceIds, r.ResourceId);
					AddIfNotEmpty(resourcePoolIds, r.ResourcePoolId);
					break;
				case IResourcePoolNode p:
					AddIfNotEmpty(resourcePoolIds, p.ResourcePoolId);
					break;
			}
		}

		private static void AddIfNotEmpty(HashSet<Guid> set, Guid id)
		{
			if (id != Guid.Empty)
			{
				set.Add(id);
			}
		}

		private ICollection<DomChangeResults> GetJobsWithChanges(ICollection<Job> apiJobs)
		{
			return GetItemsWithChanges<Job, DomJob>(
				apiJobs,
				j => j.OriginalInstance,
				j => j.GetInstanceWithChanges(),
				ids => planApi.DomHelpers.SlcWorkflowHelper.GetJobs(ids),
				j => new JobNotFoundError { ErrorMessage = $"Job with ID '{j.Id}' no longer exists.", Id = j.Id },
				(j, msg) => new JobValueAlreadyChangedError { ErrorMessage = msg, Id = j.Id })
				.ToList();
		}

		private void MarkAsJobWithCoreChanges(Job job)
		{
			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			if (!new[] { JobState.Tentative, JobState.Confirmed, JobState.Running }.Contains(job.State))
			{
				return;
			}

			jobIdsWithCoreChanges.Add(job.Id);
		}

		// The core reservation only mirrors the job's name, timings and nodes. A change is reservation-impacting when
		// one of these JobInfo fields changed or when a node section was added, removed or changed.
		private static bool HasReservationImpactingChanges(DomChangeResults changeResult)
		{
			var reservationFields = new[]
			{
				Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.JobInfo.JobName.Id,
				Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.JobInfo.JobStart.Id,
				Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.JobInfo.JobEnd.Id,
				Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.JobInfo.Preroll.Id,
				Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.JobInfo.Postroll.Id,
			};

			if (changeResult.ChangedFields.Any(x => reservationFields.Contains(x.FieldDescriptorId)))
			{
				return true;
			}

			var nodesSectionId = Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.Nodes.Id.Id;

			return changeResult.AddedSections.Any(x => x.SectionDefinitionId == nodesSectionId)
				|| changeResult.RemovedSections.Any(x => x.SectionDefinitionId == nodesSectionId)
				|| changeResult.ChangedFields.Any(x => x.SectionDefinitionId == nodesSectionId);
		}
	}
}
