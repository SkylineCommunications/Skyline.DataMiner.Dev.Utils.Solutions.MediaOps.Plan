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

	using DomRecurringJob = Storage.DOM.SlcWorkflow.RecurringJobsInstance;
	using DomResource = Storage.DOM.SlcResource_Studio.ResourceInstance;

	internal class DomRecurringJobHandler : DomInstanceApiObjectValidator<DomRecurringJob>
	{
		private readonly MediaOpsPlanApi planApi;

		private readonly Dictionary<Guid, List<OrchestrationSettings>> orchestrationSettingsByJobId = new Dictionary<Guid, List<OrchestrationSettings>>();

		private Dictionary<Guid, Resource> resourcesById = new Dictionary<Guid, Resource>();

		private DomRecurringJobHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<RecurringJob> apiRecurringJobs, out DomInstanceBulkOperationResult<DomRecurringJob> result)
		{
			var handler = new DomRecurringJobHandler(planApi);
			handler.CreateOrUpdate(apiRecurringJobs);

			result = new DomInstanceBulkOperationResult<DomRecurringJob>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<RecurringJob> apiRecurringJobs, out DomInstanceBulkOperationResult<DomRecurringJob> result, JobDeleteOptions options = null)
		{
			var handler = new DomRecurringJobHandler(planApi);
			handler.Delete(apiRecurringJobs);

			result = new DomInstanceBulkOperationResult<DomRecurringJob>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryCancel(MediaOpsPlanApi planApi, ICollection<RecurringJob> apiRecurringJobs, out DomInstanceBulkOperationResult<DomRecurringJob> result)
		{
			var handler = new DomRecurringJobHandler(planApi);
			handler.Cancel(apiRecurringJobs);

			result = new DomInstanceBulkOperationResult<DomRecurringJob>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryComplete(MediaOpsPlanApi planApi, ICollection<RecurringJob> apiRecurringJobs, out DomInstanceBulkOperationResult<DomRecurringJob> result)
		{
			var handler = new DomRecurringJobHandler(planApi);
			handler.Complete(apiRecurringJobs);

			result = new DomInstanceBulkOperationResult<DomRecurringJob>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		private void CreateOrUpdate(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			var toCreate = apiRecurringJobs.Where(x => x.IsNew).ToList();
			var toUpdate = apiRecurringJobs.Except(toCreate).ToList();

			ValidateIdsNotInUse(toCreate);
			ValidateStateForUpdateAction(toUpdate);

			ValidateNames(apiRecurringJobs);
			ValidateStartTime(apiRecurringJobs);
			ValidateCategories(apiRecurringJobs);
			ValidateDesiredJobState(apiRecurringJobs);
			ValidatePattern_RepeatType(apiRecurringJobs);
			ValidatePreRoll(apiRecurringJobs);
			ValidatePostRoll(apiRecurringJobs);

			ValidateNodeGraph(apiRecurringJobs);
			ValidateDescription(apiRecurringJobs);

			var lockResult = planApi.LockManager.LockAndExecute(apiRecurringJobs.Where(IsValid).ToList(), CreateOrUpdateLocked);
			ReportError(lockResult);
		}

		private void CreateOrUpdateLocked(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			if (apiRecurringJobs.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided recurring jobs are valid", nameof(apiRecurringJobs));
			}

			var toCreate = apiRecurringJobs.Where(x => x.IsNew).ToList();
			var toUpdate = apiRecurringJobs.Except(toCreate).ToList();

			var changeResults = GetRecurringJobsWithChanges(toUpdate);

			CreateOrUpdateOrchestrationSettings(apiRecurringJobs.Where(IsValid).ToList());
			CreateOrUpdatePropertySettingCollections(apiRecurringJobs.Where(IsValid).ToList());

			var toCreateDomInstances = toCreate
				.Where(IsValid)
				.Select(x => x.GetInstanceWithChanges())
				.ToList();

			var toUpdateDomInstances = changeResults
				.Where(IsValid)
				.Select(x => new DomRecurringJob(x.Instance))
				.ToList();

			CreateOrUpdateDomRecurringJobs(toCreateDomInstances.Concat(toUpdateDomInstances).ToList());
		}

		private void CreateOrUpdateDomRecurringJobs(ICollection<DomRecurringJob> domRecurringJobs)
		{
			if (domRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(domRecurringJobs));
			}

			if (domRecurringJobs.Count == 0)
			{
				return;
			}

			var domRecurringJobsById = domRecurringJobs.ToDictionary(x => x.ID.Id);

			planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(domRecurringJobsById.Values.Select(x => x.ToInstance()), out var domResult);

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

			ReportSuccess(domResult.SuccessfulItems.Select(x => new DomRecurringJob(x)));
		}

		private void CreateOrUpdateOrchestrationSettings(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			if (apiRecurringJobs.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided recurring jobs are valid", nameof(apiRecurringJobs));
			}

			var jobIdByOrchestrationSettingsId = new Dictionary<Guid, Guid>();
			var orchestrationSettings = new List<OrchestrationSettings>();

			foreach (var recurringJob in apiRecurringJobs)
			{
				CollectOrchestrationSettingsForRecurringJob(recurringJob, jobIdByOrchestrationSettingsId, orchestrationSettings);
			}

			DomWorkflowOrchestrationSettingsHandler.TryCreateOrUpdate(planApi, orchestrationSettings, out var domResult);

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

		private void CollectOrchestrationSettingsForRecurringJob(
			RecurringJob recurringJob,
			Dictionary<Guid, Guid> jobIdByOrchestrationSettingsId,
			List<OrchestrationSettings> orchestrationSettings)
		{
			jobIdByOrchestrationSettingsId[recurringJob.OrchestrationSettings.Id] = recurringJob.Id;
			orchestrationSettings.Add(recurringJob.OrchestrationSettings);

			var jobOrchestrationSettings = new List<OrchestrationSettings> { recurringJob.OrchestrationSettings };

			foreach (var node in recurringJob.NodeGraph.Nodes)
			{
				jobIdByOrchestrationSettingsId[node.OrchestrationSettings.Id] = recurringJob.Id;
				orchestrationSettings.Add(node.OrchestrationSettings);
				jobOrchestrationSettings.Add(node.OrchestrationSettings);
			}

			orchestrationSettingsByJobId[recurringJob.Id] = jobOrchestrationSettings;
		}

		private void Cancel(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			ValidateStateForCancelAction(apiRecurringJobs);

			var lockResult = planApi.LockManager.LockAndExecute(apiRecurringJobs.Where(IsValid).ToList(), CancelLocked);
			ReportError(lockResult);
		}

		private void CancelLocked(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			if (apiRecurringJobs.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided recurring jobs are valid", nameof(apiRecurringJobs));
			}

			// The transition does not modify the job, so the stored instance is used as-is; conflict detection via
			// GetJobsWithChanges adds no value when no changes are applied.
			var domRecurringJobs = apiRecurringJobs.Select(x => x.OriginalInstance).ToList();

			TransitionDomJobsToCanceled(domRecurringJobs);
		}

		private void TransitionDomJobsToCanceled(ICollection<DomRecurringJob> domRecurringJobs)
		{
			if (domRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(domRecurringJobs));
			}

			if (domRecurringJobs.Count == 0)
			{
				return;
			}

			var domRecurringJobsById = domRecurringJobs.ToDictionary(x => x.ID.Id);

			foreach (var domRecurringJob in domRecurringJobsById.Values)
			{
				try
				{
					var transitionedInstance = planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances.DoStatusTransition(domRecurringJob.ID, Storage.DOM.SlcWorkflow.SlcWorkflowIds.Behaviors.Recurringjob_Behavior.Transitions.Active_To_Cancelled);
					ReportSuccess(new DomRecurringJob(transitionedInstance));
				}
				catch (Exception ex)
				{
					ReportError(domRecurringJob.ID.Id, new MediaOpsErrorData() { ErrorMessage = ex.ToString() });
				}
			}
		}

		private void Complete(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			ValidateStateForCompleteAction(apiRecurringJobs);

			var lockResult = planApi.LockManager.LockAndExecute(apiRecurringJobs.Where(IsValid).ToList(), CompleteLocked);
			ReportError(lockResult);
		}

		private void CompleteLocked(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			if (apiRecurringJobs.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided recurring jobs are valid", nameof(apiRecurringJobs));
			}

			// The transition does not modify the job, so the stored instance is used as-is; conflict detection via
			// GetJobsWithChanges adds no value when no changes are applied.
			var domRecurringJobs = apiRecurringJobs.Select(x => x.OriginalInstance).ToList();

			TransitionDomJobsToCompleted(domRecurringJobs);
		}

		private void TransitionDomJobsToCompleted(ICollection<DomRecurringJob> domRecurringJobs)
		{
			if (domRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(domRecurringJobs));
			}

			if (domRecurringJobs.Count == 0)
			{
				return;
			}

			var domRecurringJobsById = domRecurringJobs.ToDictionary(x => x.ID.Id);

			foreach (var domRecurringJob in domRecurringJobsById.Values)
			{
				try
				{
					var transitionedInstance = planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances.DoStatusTransition(domRecurringJob.ID, Storage.DOM.SlcWorkflow.SlcWorkflowIds.Behaviors.Recurringjob_Behavior.Transitions.Active_To_Completed);
					ReportSuccess(new DomRecurringJob(transitionedInstance));
				}
				catch (Exception ex)
				{
					ReportError(domRecurringJob.ID.Id, new MediaOpsErrorData() { ErrorMessage = ex.ToString() });
				}
			}
		}

		private void Delete(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			ValidateStateForDeleteAction(apiRecurringJobs);

			var lockResult = planApi.LockManager.LockAndExecute(apiRecurringJobs.Where(IsValid).ToList(), DeleteLocked);
			ReportError(lockResult);
		}

		private void DeleteLocked(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			if (apiRecurringJobs.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided recurring jobs are valid", nameof(apiRecurringJobs));
			}

			var recurringJobsToDelete = apiRecurringJobs.ToList();

			if (recurringJobsToDelete.Count == 0)
			{
				return;
			}

			DeleteOrchestrationSettings(recurringJobsToDelete);
			DeletePropertySettingCollections(recurringJobsToDelete);

			var domRecurringJobsById = recurringJobsToDelete.ToDictionary(x => x.Id, x => x.OriginalInstance);

			var instancesToDelete = domRecurringJobsById.Values.Select(x => x.ToInstance()).ToArray();
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

			ReportSuccess(instancesToDelete.Where(x => domResult.SuccessfulIds.Contains(x.ID)).Select(x => new DomRecurringJob(x)).ToArray());
		}

		private void DeleteOrchestrationSettings(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			if (apiRecurringJobs.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided recurring jobs are valid", nameof(apiRecurringJobs));
			}

			DomWorkflowOrchestrationSettingsHandler.TryDelete(planApi, apiRecurringJobs.Select(x => x.OrchestrationSettings).ToList(), out _);
		}

		private void CreateOrUpdatePropertySettingCollections(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			if (apiRecurringJobs.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided recurring jobs are valid", nameof(apiRecurringJobs));
			}

			// Make sure every job has a property values context so that newly created scopes
			// (owner and nodes) pick up the correct LinkedObjectId when the user added properties
			// prior to saving.
			foreach (var job in apiRecurringJobs)
			{
				job.EnsureContext();
			}

			var ownerScopes = new List<KeyValuePair<Guid, PropertySettingsScope>>();
			foreach (var job in apiRecurringJobs)
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

		private void DeletePropertySettingCollections(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			if (apiRecurringJobs.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided recurring jobs are valid", nameof(apiRecurringJobs));
			}

			var recurringJobIdByCollectionId = new Dictionary<Guid, Guid>();
			var toDelete = new List<PropertySettingCollection>();

			var jobsRequiringQuery = CollectCachedCollectionsToDelete(apiRecurringJobs, recurringJobIdByCollectionId, toDelete);
			QueryCollectionsToDelete(jobsRequiringQuery, recurringJobIdByCollectionId, toDelete);

			if (toDelete.Count == 0)
			{
				return;
			}

			DomPropertySettingCollectionHandler.TryDelete(planApi, toDelete, out var domResult);
			ReportPropertySettingCollectionFailures(domResult, recurringJobIdByCollectionId);
		}

		private static Dictionary<string, Guid> CollectCachedCollectionsToDelete(ICollection<RecurringJob> apiRecurringJobs, Dictionary<Guid, Guid> recurringJobIdByCollectionId, List<PropertySettingCollection> toDelete)
		{
			var recurringJobsRequiringQuery = new Dictionary<string, Guid>();

			foreach (var job in apiRecurringJobs)
			{
				var cached = job.PropertySettingsContext?.TryGetCachedOriginalCollections();
				if (cached == null)
				{
					recurringJobsRequiringQuery[job.Id.ToString()] = job.Id;
					continue;
				}

				foreach (var collection in cached)
				{
					recurringJobIdByCollectionId[collection.Id] = job.Id;
					toDelete.Add(collection);
				}
			}

			return recurringJobsRequiringQuery;
		}

		private void QueryCollectionsToDelete(Dictionary<string, Guid> recurringJobsRequiringQuery, Dictionary<Guid, Guid> recurringJobIdByCollectionId, List<PropertySettingCollection> toDelete)
		{
			if (recurringJobsRequiringQuery.Count == 0)
			{
				return;
			}

			var linkedObjectIdFilter = new ORFilterElement<PropertySettingCollection>(
				recurringJobsRequiringQuery.Keys.Select(id => PropertySettingCollectionExposers.LinkedObjectId.Equal(id)).ToArray());

			var filter = new ANDFilterElement<PropertySettingCollection>(
				linkedObjectIdFilter,
				PropertySettingCollectionExposers.Scope.Equal(PropertySettingsContext.MediaOpsScope));

			foreach (var collection in planApi.PropertySettingCollections.Read(filter))
			{
				if (collection.LinkedObjectId != null && recurringJobsRequiringQuery.TryGetValue(collection.LinkedObjectId, out var jobId))
				{
					recurringJobIdByCollectionId[collection.Id] = jobId;
					toDelete.Add(collection);
				}
			}
		}

		private void ReportPropertySettingCollectionFailures(
			DomInstanceBulkOperationResult<Storage.DOM.SlcProperties.PropertyValuesInstance> result,
			Dictionary<Guid, Guid> recurringJobIdByCollectionId)
		{
			if (result == null || !result.HasFailures)
			{
				return;
			}

			foreach (var id in result.UnsuccessfulIds)
			{
				if (!recurringJobIdByCollectionId.TryGetValue(id, out var jobId))
				{
					planApi.Logger.Error(this, $"Failed to find recurring job ID for property value collection ID {id}.");
					continue;
				}

				ReportError(jobId);

				if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					PassTraceData(jobId, traceData);
				}
			}
		}

		private void ValidateIdsNotInUse(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			var jobsRequiringValidation = apiRecurringJobs.Where(x => x.IsNew && x.HasUserDefinedId).ToList();
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
				var error = new RecurringJobDuplicateIdError
				{
					ErrorMessage = $"Recurring job '{job.Name}' has a duplicate ID.",
					Id = job.Id,
				};

				ReportError(job.Id, error);

				jobsRequiringValidation.Remove(job);
			}

			foreach (var foundInstance in planApi.DomHelpers.SlcWorkflowHelper.GetWorkflowInstances(jobsRequiringValidation.Select(x => x.Id)))
			{
				planApi.Logger.Information(this, $"ID is already in use by a Workflow instance.", [foundInstance.ID.Id]);

				var error = new RecurringJobIdInUseError
				{
					ErrorMessage = "ID is already in use.",
					Id = foundInstance.ID.Id,
				};

				ReportError(foundInstance.ID.Id, error);
			}
		}

		private void ValidateStateForUpdateAction(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			foreach (var recurringJob in apiRecurringJobs)
			{
				var error = new RecurringJobInvalidStateError
				{
					ErrorMessage = "Not allowed to update a recurring jobs once it has been created.",
					Id = recurringJob.Id,
				};

				ReportError(recurringJob.Id, error);
			}
		}

		private void ValidateStateForDeleteAction(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			var isNew = apiRecurringJobs.Where(x => x.IsNew).ToList();
			foreach (var job in isNew)
			{
				var error = new RecurringJobInvalidStateError
				{
					ErrorMessage = "Not allowed to delete a recurring job that has not been created yet.",
					Id = job.Id,
				};

				ReportError(job.Id, error);
			}

			foreach (var job in apiRecurringJobs
				.Except(isNew)
				.Where(x => x.State != RecurringJobState.Cancelled && x.State != RecurringJobState.Completed))
			{
				var error = new RecurringJobInvalidStateError
				{
					ErrorMessage = "Not allowed to delete a recurring job that is not in Draft, Canceled or Completed state.",
					Id = job.Id,
				};

				ReportError(job.Id, error);
			}
		}

		private void ValidateStateForCancelAction(ICollection<RecurringJob> apiRecurringJobs)
		{
			foreach (var job in apiRecurringJobs.Where(x => x.IsNew || x.State != RecurringJobState.Active))
			{
				ReportError(job.Id, new RecurringJobInvalidStateError
				{
					ErrorMessage = "Only recurring jobs in Active state can be canceled.",
					Id = job.Id,
				});
			}
		}

		private void ValidateStateForCompleteAction(ICollection<RecurringJob> apiRecurringJobs)
		{
			foreach (var job in apiRecurringJobs.Where(x => x.IsNew || x.State != RecurringJobState.Active))
			{
				ReportError(job.Id, new RecurringJobInvalidStateError
				{
					ErrorMessage = "Only recurring jobs in Active state can be marked as completed.",
					Id = job.Id,
				});
			}
		}

		private void ValidateStartTime(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			var now = DateTimeOffset.UtcNow;
			foreach (var recurringJob in apiRecurringJobs.Where(x => x.Start < now).ToArray())
			{
				var error = new RecurringJobInvalidStartTimeError
				{
					ErrorMessage = "Start time must be in the future.",
					Id = recurringJob.Id,
				};

				ReportError(recurringJob.Id, error);
			}
		}

		private void ValidateNames(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			var requiringValidation = apiRecurringJobs.Where(x => !string.IsNullOrEmpty(x.Name)).ToList();

			foreach (var recurringJob in requiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Name)).ToArray())
			{
				var error = new RecurringJobInvalidNameError
				{
					ErrorMessage = $"Name cannot be empty.",
					Id = recurringJob.Id,
				};

				ReportError(recurringJob.Id, error);
				requiringValidation.Remove(recurringJob);
			}

			foreach (var recurringJob in requiringValidation.Where(x => !InputValidator.HasValidTextLength(x.Name)).ToArray())
			{
				var error = new RecurringJobInvalidNameError
				{
					ErrorMessage = $"Name exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
					Name = recurringJob.Name,
					Id = recurringJob.Id,
				};

				ReportError(recurringJob.Id, error);
			}
		}

		private void ValidateCategories(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}
			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			var toValidate = apiRecurringJobs.Where(x => !string.IsNullOrEmpty(x.JobTypeCategoryId)).ToList();
			if (toValidate.Count == 0)
			{
				return;
			}

			var scope = planApi.Categories.Scopes.Read(ScopeExposers.Name.Equal("Job Types")).FirstOrDefault();
			if (scope == null)
			{
				foreach (var recurringJob in toValidate)
				{
					var error = new JobCategoryScopeNotFoundError
					{
						ErrorMessage = "Category with scope 'Job Types' not found.",
						Id = recurringJob.Id,
					};

					ReportError(recurringJob.Id, error);
				}

				return;
			}

			var categoryIds = planApi.Categories.Categories.GetByScope(scope).Select(x => x.ID.ToString()).ToList();

			foreach (var recurringJob in toValidate)
			{
				if (!categoryIds.Contains(recurringJob.JobTypeCategoryId))
				{
					if (recurringJob.JobTypeCategoryId.Equals("Scheduling", StringComparison.InvariantCultureIgnoreCase))
					{
						// Translate previous fixed source to new fixed category id.
						recurringJob.JobTypeCategoryId = Convert.ToString(JobTypes.Scheduled);
						continue;
					}

					var error = new JobCategoryNotFoundError
					{
						ErrorMessage = $"Category with ID '{recurringJob.JobTypeCategoryId}' not found in Scope 'Job Types'.",
						CategoryId = recurringJob.JobTypeCategoryId,
						Id = recurringJob.Id,
					};

					ReportError(recurringJob.Id, error);
				}
			}
		}

		private void ValidateDescription(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			foreach (var job in apiRecurringJobs.Where(x => InputValidator.IsNonEmptyText(x.Description) && !InputValidator.HasValidTextSize(x.Description)))
			{
				var error = new RecurringJobInvalidDescriptionError
				{
					ErrorMessage = $"Description exceeds maximum size of {InputValidator.DefaultMaxTextSize} bytes.",
					Description = job.Description,
					Id = job.Id,
				};

				ReportError(job.Id, error);
			}
		}

		private void ValidateDesiredJobState(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			foreach (var job in apiRecurringJobs)
			{
				if (job.DesiredJobState == JobState.Draft || job.DesiredJobState == JobState.Tentative)
				{
					continue;
				}

				ReportError(new RecurringJobInvalidDesiredJobStateError
				{
					ErrorMessage = $"The desired job state '{job.DesiredJobState}' is not valid for a recurring job.",
					DesiredJobState = job.DesiredJobState,
					Id = job.Id,
				});
			}
		}

		private void ValidatePattern_RepeatType(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			foreach (var recurringJob in apiRecurringJobs)
			{
				if (recurringJob.Pattern.RepeatType == RepeatType.Never)
				{
					var error = new RecurringJobInvalidPatternError
					{
						ErrorMessage = "The RepeatType cannot be 'Never' for a recurring pattern.",
						Id = recurringJob.Id,
					};

					ReportError(recurringJob.Id, error);
					continue;
				}

				if (recurringJob.Pattern.RepeatEvery < 1)
				{
					var error = new RecurringJobInvalidPatternError
					{
						ErrorMessage = "The RepeatEvery value must be at least 1.",
						Id = recurringJob.Id,
					};

					ReportError(recurringJob.Id, error);
					continue;
				}

				if (recurringJob.Pattern.RepeatType == RepeatType.Weekly && recurringJob.Pattern.WeekDays == WeekDays.None)
				{
					var error = new RecurringJobInvalidPatternError
					{
						ErrorMessage = "At least one day of the week should be included in a weekly pattern.",
						Id = recurringJob.Id,
					};

					ReportError(recurringJob.Id, error);
					continue;
				}
			}
		}

		private void ValidatePreRoll(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			var toValidate = apiRecurringJobs.ToList();

			foreach (var job in toValidate.Where(x => x.PreRollDuration.Ticks % TimeSpan.TicksPerSecond != 0).ToArray())
			{
				var error = new RecurringJobInvalidPreRollError
				{
					ErrorMessage = "Pre-roll duration must not have sub-second precision.",
					Id = job.Id,
					PreRollDuration = job.PreRollDuration,
				};

				ReportError(job.Id, error);
				toValidate.Remove(job);
			}

			foreach (var job in toValidate.Where(x => x.PreRollDuration < TimeSpan.Zero))
			{
				var error = new RecurringJobInvalidPreRollError
				{
					ErrorMessage = $"The pre-roll duration must be at least 0 seconds.",
					Id = job.Id,
					PreRollDuration = job.PreRollDuration,
				};

				ReportError(job.Id, error);
			}
		}

		private void ValidatePostRoll(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			var toValidate = apiRecurringJobs.ToList();

			foreach (var job in toValidate.Where(x => x.PostRollDuration.Ticks % TimeSpan.TicksPerSecond != 0).ToArray())
			{
				var error = new RecurringJobInvalidPostRollError
				{
					ErrorMessage = "Post-roll duration must not have sub-second precision.",
					Id = job.Id,
					PostRollDuration = job.PostRollDuration,
				};

				ReportError(job.Id, error);
				toValidate.Remove(job);
			}

			foreach (var job in toValidate.Where(x => x.PostRollDuration < TimeSpan.Zero))
			{
				var error = new RecurringJobInvalidPostRollError
				{
					ErrorMessage = $"The post-roll duration must be at least 0 seconds.",
					Id = job.Id,
					PostRollDuration = job.PostRollDuration,
				};

				ReportError(job.Id, error);
			}
		}

		private void ValidateNodeGraph(ICollection<RecurringJob> apiRecurringJobs)
		{
			if (apiRecurringJobs == null)
			{
				throw new ArgumentNullException(nameof(apiRecurringJobs));
			}

			if (apiRecurringJobs.Count == 0)
			{
				return;
			}

			CollectReferencedIds(apiRecurringJobs, out var resourceIds, out var resourcePoolIds);

			resourcesById = planApi.Resources.Read(resourceIds).ToDictionary(x => x.Id);
			var resourcePoolsById = planApi.ResourcePools.Read(resourcePoolIds).ToDictionary(x => x.Id);

			foreach (var job in apiRecurringJobs)
			{
				PassTraceData(RecurringJobNodeGraphValidator.Validate(job.Id, job.NodeGraph, resourcesById, resourcePoolsById));
			}
		}

		private static void CollectReferencedIds(ICollection<RecurringJob> apiRecurringJobs, out HashSet<Guid> resourceIds, out HashSet<Guid> resourcePoolIds)
		{
			resourceIds = new HashSet<Guid>();
			resourcePoolIds = new HashSet<Guid>();

			var allNodes = apiRecurringJobs.SelectMany(j => j.NodeGraph.Nodes);
			foreach (var node in allNodes)
			{
				CollectNodeIds(node, resourceIds, resourcePoolIds);
			}
		}

		private static void CollectNodeIds(RecurringJobNode node, HashSet<Guid> resourceIds, HashSet<Guid> resourcePoolIds)
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

		private ICollection<DomChangeResults> GetRecurringJobsWithChanges(ICollection<RecurringJob> apiRecurringJobs)
		{
			return GetItemsWithChanges<RecurringJob, DomRecurringJob>(
				apiRecurringJobs,
				j => j.OriginalInstance,
				j => j.GetInstanceWithChanges(),
				ids => planApi.DomHelpers.SlcWorkflowHelper.GetRecurringJobs(ids),
				j => new RecurringJobNotFoundError { ErrorMessage = $"Recurring job with ID '{j.Id}' no longer exists.", Id = j.Id },
				(j, msg) => new RecurringJobValueAlreadyChangedError { ErrorMessage = msg, Id = j.Id })
				.ToList();
		}
	}
}
