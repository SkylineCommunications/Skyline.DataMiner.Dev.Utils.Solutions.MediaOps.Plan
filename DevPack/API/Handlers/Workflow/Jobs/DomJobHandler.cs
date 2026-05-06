namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using DomJob = Storage.DOM.SlcWorkflow.JobsInstance;

	internal class DomJobHandler : DomInstanceApiObjectValidator<DomJob>
	{
		private readonly MediaOpsPlanApi planApi;

		private DomJobHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
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

			ValidateIdsNotInUse(toCreate);

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

			CreateOrUpdateOrchestrationSettings(apiJobs.Where(IsValid).ToList());

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

			planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(domJobs.Select(x => x.ToInstance()), out var domResult);

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

			var jobIdByOrchestrationSettingsId = apiJobs.ToDictionary(x => x.OrchestrationSettings.Id, x => x.Id);

			DomWorkflowOrchestrationSettingsHandler.TryCreateOrUpdate(planApi, apiJobs.Select(x => x.OrchestrationSettings).ToList(), out var domResult);

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
	}
}
