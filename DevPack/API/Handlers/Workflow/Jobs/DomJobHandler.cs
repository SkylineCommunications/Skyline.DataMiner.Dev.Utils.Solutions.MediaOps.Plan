namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Remoting.Metadata.W3cXsd2001;

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
			var toUpdate = apiJobs.Except(toCreate).ToList();

			ValidateIdsNotInUse(toCreate);
			ValidateKeys(toCreate);
			AssignKeys(toCreate);
			AssignNames(toCreate);
			ValidateStateForUpdateAction(toUpdate);

			ValidateNames(apiJobs);
			ValidateTimings(apiJobs);
			ValidatePreRoll(apiJobs);
			ValidatePostRoll(apiJobs);
			ValidateNodeGraph(apiJobs);
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

			var jobIdByOrchestrationSettingsId = new Dictionary<Guid, Guid>();
			var orchestrationSettings = new List<OrchestrationSettings>();

			foreach (var job in apiJobs)
			{
				jobIdByOrchestrationSettingsId[job.OrchestrationSettings.Id] = job.Id;
				orchestrationSettings.Add(job.OrchestrationSettings);

				foreach (var node in job.NodeGraph.Nodes)
				{
					jobIdByOrchestrationSettingsId[node.OrchestrationSettings.Id] = job.Id;
					orchestrationSettings.Add(node.OrchestrationSettings);
				}
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

			foreach (var job in apiJobs.Where(x => x.State != JobState.Draft))
			{
				var error = new JobInvalidStateError
				{
					ErrorMessage = "Not allowed to update a job that is not in Draft state.",
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

			foreach (var job in apiJobs.Where(x => x.End < x.Start))
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

			foreach (var job in apiJobs.Where(x => x.PreRoll < TimeSpan.Zero))
			{
				var error = new JobInvalidPreRollError
				{
					ErrorMessage = "Pre-roll cannot be negative.",
					Id = job.Id,
					PreRoll = job.PreRoll,
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

			foreach (var job in apiJobs.Where(x => x.PostRoll < TimeSpan.Zero))
			{
				var error = new JobInvalidPostRollError
				{
					ErrorMessage = "Post-roll cannot be negative.",
					Id = job.Id,
					PostRoll = job.PostRoll,
				};

				ReportError(job.Id, error);
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

			var resourceIds = new HashSet<Guid>();
			var resourcePoolIds = new HashSet<Guid>();

			foreach (var job in apiJobs)
			{
				foreach (var node in job.NodeGraph.Nodes)
				{
					switch (node)
					{
						case IResourceNode r:
							if (r.ResourceId != Guid.Empty) resourceIds.Add(r.ResourceId);
							if (r.ResourcePoolId != Guid.Empty) resourcePoolIds.Add(r.ResourcePoolId);
							break;
						case IResourcePoolNode p:
							if (p.ResourcePoolId != Guid.Empty) resourcePoolIds.Add(p.ResourcePoolId);
							break;
					}
				}
			}

			var resourcesById = planApi.Resources.Read(resourceIds).ToDictionary(x => x.Id);
			var resourcePoolsById = planApi.ResourcePools.Read(resourcePoolIds).ToDictionary(x => x.Id);

			foreach (var job in apiJobs)
			{
				PassTraceData(JobNodeGraphValidator.Validate(job.Id, job.NodeGraph, resourcesById, resourcePoolsById));
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
