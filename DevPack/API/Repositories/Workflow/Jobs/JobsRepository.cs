namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;

	internal class JobsRepository : Repository, IJobsRepository
	{
		private readonly JobFilterTranslator filterTranslator;

		public JobsRepository(MediaOpsPlanApi planApi) : base(planApi)
		{
			filterTranslator = new JobFilterTranslator(planApi);
		}

		public JobTypes JobTypes { get; } = new JobTypes();

		public Job SaveAsTentative(Job job)
		{
			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			SaveChanges([job]);

			return SaveAsTentative(job.Id);
		}

		public Job SaveAsTentative(Guid jobId)
		{
			var job = Read(jobId);
			if (job == null)
			{
				return null;
			}

			if (!DomJobHandler.TrySaveAsTentative(PlanApi, [job], out var result))
			{
				result.ThrowSingleException(job.Id);
			}

			return new Job(PlanApi, result.SuccessfulItems.Single());
		}

		public IReadOnlyCollection<Job> SaveAsTentative(IEnumerable<Job> jobs)
		{
			if (jobs == null)
			{
				throw new ArgumentNullException(nameof(jobs));
			}

			var list = jobs.ToList();

			SaveChanges(list);

			return SaveAsTentative(list.Select(x => x.Id).ToArray());
		}

		public IReadOnlyCollection<Job> SaveAsTentative(IEnumerable<Guid> jobIds)
		{
			if (jobIds == null)
			{
				throw new ArgumentNullException(nameof(jobIds));
			}

			var jobs = Read(jobIds);
			if (!DomJobHandler.TrySaveAsTentative(PlanApi, jobs?.ToList(), out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new Job(PlanApi, x)).ToList();
		}

		public Job Confirm(Job job)
		{
			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			return Confirm(job.Id);
		}

		public Job Confirm(Guid jobId)
		{
			var job = Read(jobId);
			if (job == null)
			{
				return null;
			}

			if (!DomJobHandler.TryConfirm(PlanApi, [job], out var result))
			{
				result.ThrowSingleException(job.Id);
			}

			return new Job(PlanApi, result.SuccessfulItems.Single());
		}

		public IReadOnlyCollection<Job> Confirm(IEnumerable<Job> jobs)
		{
			if (jobs == null)
			{
				throw new ArgumentNullException(nameof(jobs));
			}

			return Confirm(jobs.Select(x => x.Id).ToArray());
		}

		public IReadOnlyCollection<Job> Confirm(IEnumerable<Guid> jobIds)
		{
			if (jobIds == null)
			{
				throw new ArgumentNullException(nameof(jobIds));
			}

			var jobs = Read(jobIds);
			if (!DomJobHandler.TryConfirm(PlanApi, jobs?.ToList(), out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new Job(PlanApi, x)).ToList();
		}

		public Job TransitionToRunning(Job job)
		{
			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			return TransitionToRunning(job.Id);
		}

		public Job TransitionToRunning(Guid jobId)
		{
			var job = Read(jobId);
			if (job == null)
			{
				return null;
			}

			if (!DomJobHandler.TryTransitionToRunning(PlanApi, [job], out var result))
			{
				result.ThrowSingleException(job.Id);
			}

			return new Job(PlanApi, result.SuccessfulItems.Single());
		}

		public IReadOnlyCollection<Job> TransitionToRunning(IEnumerable<Job> jobs)
		{
			if (jobs == null)
			{
				throw new ArgumentNullException(nameof(jobs));
			}

			return TransitionToRunning(jobs.Select(x => x.Id).ToArray());
		}

		public IReadOnlyCollection<Job> TransitionToRunning(IEnumerable<Guid> jobIds)
		{
			if (jobIds == null)
			{
				throw new ArgumentNullException(nameof(jobIds));
			}

			var jobs = Read(jobIds);
			if (!DomJobHandler.TryTransitionToRunning(PlanApi, jobs?.ToList(), out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new Job(PlanApi, x)).ToList();
		}

		public Job TransitionToCompleted(Job job)
		{
			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			return TransitionToCompleted(job.Id);
		}

		public Job TransitionToCompleted(Guid jobId)
		{
			var job = Read(jobId);
			if (job == null)
			{
				return null;
			}

			if (!DomJobHandler.TryTransitionToCompleted(PlanApi, [job], out var result))
			{
				result.ThrowSingleException(job.Id);
			}

			return new Job(PlanApi, result.SuccessfulItems.Single());
		}

		public IReadOnlyCollection<Job> TransitionToCompleted(IEnumerable<Job> jobs)
		{
			if (jobs == null)
			{
				throw new ArgumentNullException(nameof(jobs));
			}

			return TransitionToCompleted(jobs.Select(x => x.Id).ToArray());
		}

		public IReadOnlyCollection<Job> TransitionToCompleted(IEnumerable<Guid> jobIds)
		{
			if (jobIds == null)
			{
				throw new ArgumentNullException(nameof(jobIds));
			}

			var jobs = Read(jobIds);
			if (!DomJobHandler.TryTransitionToCompleted(PlanApi, jobs?.ToList(), out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new Job(PlanApi, x)).ToList();
		}

		public Job Cancel(Job job)
		{
			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			return Cancel(job.Id);
		}

		public Job Cancel(Guid jobId)
		{
			var job = Read(jobId);
			if (job == null)
			{
				return null;
			}

			if (!DomJobHandler.TryCancel(PlanApi, [job], out var result))
			{
				result.ThrowSingleException(job.Id);
			}

			return new Job(PlanApi, result.SuccessfulItems.Single());
		}

		public IReadOnlyCollection<Job> Cancel(IEnumerable<Job> jobs)
		{
			if (jobs == null)
			{
				throw new ArgumentNullException(nameof(jobs));
			}

			return Cancel(jobs.Select(x => x.Id).ToArray());
		}

		public IReadOnlyCollection<Job> Cancel(IEnumerable<Guid> jobIds)
		{
			if (jobIds == null)
			{
				throw new ArgumentNullException(nameof(jobIds));
			}

			var jobs = Read(jobIds);
			if (!DomJobHandler.TryCancel(PlanApi, jobs?.ToList(), out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new Job(PlanApi, x)).ToList();
		}

		public Job ReturnToTentative(Job job)
		{
			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			return ReturnToTentative(job.Id);
		}

		public Job ReturnToTentative(Guid jobId)
		{
			var job = Read(jobId);
			if (job == null)
			{
				return null;
			}

			if (!DomJobHandler.TryReturnToTentative(PlanApi, [job], out var result))
			{
				result.ThrowSingleException(job.Id);
			}

			return new Job(PlanApi, result.SuccessfulItems.Single());
		}

		public IReadOnlyCollection<Job> ReturnToTentative(IEnumerable<Job> jobs)
		{
			if (jobs == null)
			{
				throw new ArgumentNullException(nameof(jobs));
			}

			return ReturnToTentative(jobs.Select(x => x.Id).ToArray());
		}

		public IReadOnlyCollection<Job> ReturnToTentative(IEnumerable<Guid> jobIds)
		{
			if (jobIds == null)
			{
				throw new ArgumentNullException(nameof(jobIds));
			}

			var jobs = Read(jobIds);
			if (!DomJobHandler.TryReturnToTentative(PlanApi, jobs?.ToList(), out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new Job(PlanApi, x)).ToList();
		}

		public Job MarkAsCompleted(Job job)
		{
			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			return MarkAsCompleted(job.Id);
		}

		public Job MarkAsCompleted(Guid jobId)
		{
			var job = Read(jobId);
			if (job == null)
			{
				return null;
			}

			if (!DomJobHandler.TryMarkAsCompleted(PlanApi, [job], out var result))
			{
				result.ThrowSingleException(job.Id);
			}

			return new Job(PlanApi, result.SuccessfulItems.Single());
		}

		public IReadOnlyCollection<Job> MarkAsCompleted(IEnumerable<Job> jobs)
		{
			if (jobs == null)
			{
				throw new ArgumentNullException(nameof(jobs));
			}

			return MarkAsCompleted(jobs.Select(x => x.Id).ToArray());
		}

		public IReadOnlyCollection<Job> MarkAsCompleted(IEnumerable<Guid> jobIds)
		{
			if (jobIds == null)
			{
				throw new ArgumentNullException(nameof(jobIds));
			}

			var jobs = Read(jobIds);
			if (!DomJobHandler.TryMarkAsCompleted(PlanApi, jobs?.ToList(), out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new Job(PlanApi, x)).ToList();
		}

		public Job Start(Job job)
		{
			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			return Start(job.Id, new JobStartOptions());
		}

		public Job Start(Guid jobId)
		{
			return Start(jobId, new JobStartOptions());
		}

		public IReadOnlyCollection<Job> Start(IEnumerable<Job> jobs)
		{
			if (jobs == null)
			{
				throw new ArgumentNullException(nameof(jobs));
			}

			return Start(jobs.Select(x => x.Id).ToArray(), new JobStartOptions());
		}

		public IReadOnlyCollection<Job> Start(IEnumerable<Guid> jobIds)
		{
			if (jobIds == null)
			{
				throw new ArgumentNullException(nameof(jobIds));
			}

			return Start(jobIds, new JobStartOptions());
		}

		public Job Start(Job job, JobStartOptions options)
		{
			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			return Start(job.Id, options);
		}

		public Job Start(Guid jobId, JobStartOptions options)
		{
			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			var job = Read(jobId);
			if (job == null)
			{
				return null;
			}

			if (!DomJobHandler.TryStart(PlanApi, [job], out var result, options))
			{
				result.ThrowSingleException(job.Id);
			}

			return new Job(PlanApi, result.SuccessfulItems.Single());
		}

		public IReadOnlyCollection<Job> Start(IEnumerable<Job> jobs, JobStartOptions options)
		{
			if (jobs == null)
			{
				throw new ArgumentNullException(nameof(jobs));
			}

			return Start(jobs.Select(x => x.Id).ToArray(), options);
		}

		public IReadOnlyCollection<Job> Start(IEnumerable<Guid> jobIds, JobStartOptions options)
		{
			if (jobIds == null)
			{
				throw new ArgumentNullException(nameof(jobIds));
			}

			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			var jobs = Read(jobIds);
			if (!DomJobHandler.TryStart(PlanApi, jobs?.ToList(), out var result, options))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new Job(PlanApi, x)).ToList();
		}

		public Job Stop(Job job)
		{
			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			return Stop(job.Id, new JobStopOptions());
		}

		public Job Stop(Guid jobId)
		{
			return Stop(jobId, new JobStopOptions());
		}

		public IReadOnlyCollection<Job> Stop(IEnumerable<Job> jobs)
		{
			if (jobs == null)
			{
				throw new ArgumentNullException(nameof(jobs));
			}

			return Stop(jobs.Select(x => x.Id).ToArray(), new JobStopOptions());
		}

		public IReadOnlyCollection<Job> Stop(IEnumerable<Guid> jobIds)
		{
			if (jobIds == null)
			{
				throw new ArgumentNullException(nameof(jobIds));
			}

			return Stop(jobIds, new JobStopOptions());
		}

		public Job Stop(Job job, JobStopOptions options)
		{
			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			return Stop(job.Id, options);
		}

		public Job Stop(Guid jobId, JobStopOptions options)
		{
			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			var job = Read(jobId);
			if (job == null)
			{
				return null;
			}

			if (!DomJobHandler.TryStop(PlanApi, [job], out var result, options))
			{
				result.ThrowSingleException(job.Id);
			}

			return new Job(PlanApi, result.SuccessfulItems.Single());
		}

		public IReadOnlyCollection<Job> Stop(IEnumerable<Job> jobs, JobStopOptions options)
		{
			if (jobs == null)
			{
				throw new ArgumentNullException(nameof(jobs));
			}

			return Stop(jobs.Select(x => x.Id).ToArray(), options);
		}

		public IReadOnlyCollection<Job> Stop(IEnumerable<Guid> jobIds, JobStopOptions options)
		{
			if (jobIds == null)
			{
				throw new ArgumentNullException(nameof(jobIds));
			}

			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			var jobs = Read(jobIds);
			if (!DomJobHandler.TryStop(PlanApi, jobs?.ToList(), out var result, options))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new Job(PlanApi, x)).ToList();
		}

		public long Count()
		{
			return Count(new TRUEFilterElement<Job>());
		}

		public long Count(FilterElement<Job> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (filter.isEmpty())
			{
				return 0;
			}

			var domFilter = filterTranslator.TranslateFilter(filter);
			return PlanApi.DomHelpers.SlcWorkflowHelper.CountWorkflowInstances(domFilter);
		}

		public long Count(IQuery<Job> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (query.Filter.isEmpty())
			{
				return 0;
			}

			var domFilter = filterTranslator.TranslateFilter(query.Filter);
			var domOrderBy = filterTranslator.TranslateFullOrderBy(query.Order);

			var domQuery = query
				.WithFilter(domFilter)
				.WithOrder(domOrderBy)
				.WithLimit(query.Limit);

			return PlanApi.DomHelpers.SlcWorkflowHelper.CountWorkflowInstances(domQuery);
		}

		public IReadOnlyCollection<Job> Create(IEnumerable<Job> oToCreate)
		{
			if (oToCreate == null)
			{
				throw new ArgumentNullException(nameof(oToCreate));
			}

			var list = oToCreate.ToList();

			var existing = list.Where(x => !x.IsNew);
			if (existing.Any())
			{
				throw new InvalidOperationException("Not possible to use method Create for existing jobs. Use CreateOrUpdate or Update instead.");
			}

			if (!DomJobHandler.TryCreateOrUpdate(PlanApi, list, out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new Job(PlanApi, x)).ToList();
		}

		public Job Create(Job oToCreate)
		{
			if (oToCreate == null)
			{
				throw new ArgumentNullException(nameof(oToCreate));
			}

			if (!oToCreate.IsNew)
			{
				throw new InvalidOperationException("Not possible to use method Create for existing job. Use CreateOrUpdate or Update instead.");
			}

			if (!DomJobHandler.TryCreateOrUpdate(PlanApi, [oToCreate], out var result))
			{
				result.ThrowSingleException(oToCreate.Id);
			}

			return new Job(PlanApi, result.SuccessfulItems.Single());
		}

		public IReadOnlyCollection<Job> CreateOrUpdate(IEnumerable<Job> oToCreateOrUpdate)
		{
			if (oToCreateOrUpdate == null)
			{
				throw new ArgumentNullException(nameof(oToCreateOrUpdate));
			}

			var list = oToCreateOrUpdate.ToList();

			if (!DomJobHandler.TryCreateOrUpdate(PlanApi, list, out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new Job(PlanApi, x)).ToList();
		}

		public void Delete(Guid apiObjectId)
		{
			Delete(apiObjectId, new JobDeleteOptions());
		}

		public void Delete(IEnumerable<Guid> apiObjectIds)
		{
			if (apiObjectIds == null)
			{
				throw new ArgumentNullException(nameof(apiObjectIds));
			}

			Delete(apiObjectIds, new JobDeleteOptions());
		}

		public void Delete(IEnumerable<Job> oToDelete)
		{
			if (oToDelete == null)
			{
				throw new ArgumentNullException(nameof(oToDelete));
			}

			Delete(oToDelete.Select(x => x.Id).ToArray(), new JobDeleteOptions());
		}

		public void Delete(Job oToDelete)
		{
			if (oToDelete == null)
			{
				throw new ArgumentNullException(nameof(oToDelete));
			}

			Delete(oToDelete.Id, new JobDeleteOptions());
		}

		public void Delete(Guid jobId, JobDeleteOptions options)
		{
			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			var toDelete = Read(jobId);
			if (toDelete == null)
			{
				return;
			}

			if (!DomJobHandler.TryDelete(PlanApi, [toDelete], out var result, options))
			{
				result.ThrowSingleException(toDelete.Id);
			}
		}

		public void Delete(IEnumerable<Guid> jobIds, JobDeleteOptions options)
		{
			if (jobIds == null)
			{
				throw new ArgumentNullException(nameof(jobIds));
			}

			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			var toDelete = Read(jobIds.ToArray());

			if (!DomJobHandler.TryDelete(PlanApi, toDelete?.ToList(), out var result, options))
			{
				result.ThrowBulkException();
			}
		}

		public void Delete(Job job, JobDeleteOptions options)
		{
			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			Delete(job.Id, options);
		}

		public void Delete(IEnumerable<Job> jobs, JobDeleteOptions options)
		{
			if (jobs == null)
			{
				throw new ArgumentNullException(nameof(jobs));
			}

			Delete(jobs.Select(x => x.Id).ToArray(), options);
		}

		public IEnumerable<Job> Read(FilterElement<Job> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (filter.isEmpty())
			{
				return Enumerable.Empty<Job>();
			}

			return ActivityHelper.Track(nameof(JobsRepository), nameof(Read), act =>
			{
				var domFilter = filterTranslator.TranslateFilter(filter);
				IEnumerable<Job> Iterator()
				{
					foreach (var domJob in PlanApi.DomHelpers.SlcWorkflowHelper.GetJobs(domFilter))
					{
						yield return new Job(PlanApi, domJob);
					}
				}

				return Iterator();
			});
		}

		public IEnumerable<Job> Read(IQuery<Job> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (query.Filter.isEmpty())
			{
				return Enumerable.Empty<Job>();
			}

			var domFilter = filterTranslator.TranslateFilter(query.Filter);
			var domOrderBy = filterTranslator.TranslateFullOrderBy(query.Order);

			var domQuery = query
				.WithFilter(domFilter)
				.WithOrder(domOrderBy)
				.WithLimit(query.Limit);

			IEnumerable<Job> Iterator()
			{
				foreach (var domJob in PlanApi.DomHelpers.SlcWorkflowHelper.GetJobs(domQuery))
				{
					yield return new Job(PlanApi, domJob);
				}
			}

			return Iterator();
		}

		public IEnumerable<Job> Read()
		{
			return Read(new TRUEFilterElement<Job>());
		}

		public Job Read(Guid id)
		{
			if (id == Guid.Empty)
			{
				throw new ArgumentNullException(nameof(id));
			}

			return ActivityHelper.Track(nameof(JobsRepository), nameof(Read), act =>
			{
				act?.AddTag("JobId", id);
				var job = Read(JobExposers.Id.Equal(id)).FirstOrDefault();

				if (job == null)
				{
					act?.AddTag("Hit", false);
					return null;
				}

				act?.AddTag("Hit", true);
				return job;
			});
		}

		public IEnumerable<Job> Read(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Array.Empty<Job>();
			}

			return Read(new ORFilterElement<Job>(ids.Select(x => JobExposers.Id.Equal(x)).ToArray()));
		}

		public IEnumerable<SDM.IPagedResult<Job>> ReadPaged()
		{
			return ReadPaged(new TRUEFilterElement<Job>());
		}

		public IEnumerable<SDM.IPagedResult<Job>> ReadPaged(int pageSize)
		{
			return ReadPaged(new TRUEFilterElement<Job>(), pageSize);
		}

		public IEnumerable<SDM.IPagedResult<Job>> ReadPaged(FilterElement<Job> filter)
		{
			return ReadPaged(filter, MediaOpsPlanApi.DefaultPageSize);
		}

		public IEnumerable<SDM.IPagedResult<Job>> ReadPaged(IQuery<Job> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			return ReadPaged(query, MediaOpsPlanApi.DefaultPageSize);
		}

		public IEnumerable<SDM.IPagedResult<Job>> ReadPaged(FilterElement<Job> filter, int pageSize)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (pageSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
			}

			if (filter.isEmpty())
			{
				return Enumerable.Empty<SDM.IPagedResult<Job>>();
			}

			return ReadPagedIterator(filter, pageSize);
		}

		public IEnumerable<SDM.IPagedResult<Job>> ReadPaged(IQuery<Job> query, int pageSize)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (pageSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
			}

			if (query.Filter.isEmpty())
			{
				return Enumerable.Empty<SDM.IPagedResult<Job>>();
			}

			return ReadPagedIterator(query, pageSize);
		}

		private IEnumerable<SDM.IPagedResult<Job>> ReadPagedIterator(FilterElement<Job> filter, int pageSize)
		{
			var pageNumber = 0;
			var domFilter = filterTranslator.TranslateFilter(filter);
			var pages = PlanApi.DomHelpers.SlcWorkflowHelper.GetJobsPaged(domFilter, pageSize);
			var enumerator = pages.GetEnumerator();
			var hasNext = enumerator.MoveNext();

			while (hasNext)
			{
				var page = enumerator.Current;
				hasNext = enumerator.MoveNext();
				yield return new PagedResult<Job>(page.Select(x => new Job(PlanApi, x)), pageNumber++, pageSize, hasNext);
			}
		}

		private IEnumerable<SDM.IPagedResult<Job>> ReadPagedIterator(IQuery<Job> query, int pageSize)
		{
			var domFilter = filterTranslator.TranslateFilter(query.Filter);
			var domOrderBy = filterTranslator.TranslateFullOrderBy(query.Order);

			var domQuery = query
				.WithFilter(domFilter)
				.WithOrder(domOrderBy)
				.WithLimit(query.Limit);

			var pageNumber = 0;
			var pages = PlanApi.DomHelpers.SlcWorkflowHelper.GetJobsPaged(domQuery, pageSize);
			var enumerator = pages.GetEnumerator();
			var hasNext = enumerator.MoveNext();

			while (hasNext)
			{
				var page = enumerator.Current;
				hasNext = enumerator.MoveNext();
				yield return new PagedResult<Job>(page.Select(x => new Job(PlanApi, x)), pageNumber++, pageSize, hasNext);
			}
		}

		public void SetOrchestrationState(Guid id, OrchestrationUpdateDetails updateDetails)
		{
			if (id == Guid.Empty)
			{
				throw new ArgumentNullException(nameof(id));
			}

			if (updateDetails == null)
			{
				throw new ArgumentNullException(nameof(updateDetails));
			}

			if (updateDetails.EventState == OrchestrationEventState.Succeeded)
			{
				return;
			}

			var job = Read(id)
				?? throw new MediaOpsException(
					new JobNotFoundError()
					{
						ErrorMessage = $"Unable to find job with ID {id}.",
						Id = id,
					});

			var errorCode = string.Empty;
			switch (updateDetails.Event)
			{
				case OrchestrationEventType.PrerollStart:
					errorCode = "LIV101";
					break;
				case OrchestrationEventType.PrerollStop:
					errorCode = "LIV102";
					break;
				case OrchestrationEventType.PostrollStart:
					errorCode = "LIV103";
					break;
				case OrchestrationEventType.PostrollStop:
					errorCode = "LIV104";
					break;
				default:
					throw new NotSupportedException($"Unsupported Orchestration Event Type {updateDetails.Event}");
			}

			job.AddError(new JobError(errorCode, updateDetails.Message));
			PlanApi.Jobs.Update(job);
		}

		public IReadOnlyCollection<Job> Update(IEnumerable<Job> oToUpdate)
		{
			if (oToUpdate == null)
			{
				throw new ArgumentNullException(nameof(oToUpdate));
			}

			var list = oToUpdate.ToList();

			var newRoles = list.Where(x => x.IsNew);
			if (newRoles.Any())
			{
				throw new InvalidOperationException("Not possible to use method Update for new jobs. Use Create or CreateOrUpdate instead.");
			}

			if (!DomJobHandler.TryCreateOrUpdate(PlanApi, list, out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new Job(PlanApi, x)).ToList();
		}

		public Job Update(Job oToUpdate)
		{
			if (oToUpdate == null)
			{
				throw new ArgumentNullException(nameof(oToUpdate));
			}

			if (oToUpdate.IsNew)
			{
				throw new InvalidOperationException("Not possible to use method Update for new job. Use Create or CreateOrUpdate instead.");
			}

			if (!DomJobHandler.TryCreateOrUpdate(PlanApi, [oToUpdate], out var result))
			{
				result.ThrowSingleException(oToUpdate.Id);
			}

			return new Job(PlanApi, result.SuccessfulItems.Single());
		}

		/// <summary>
		/// Persists the jobs that are new or that carry unsaved changes. A state transition operates on the stored job, so
		/// without this the pending changes of the supplied objects (for example the node graph of a job that was built
		/// with <see cref="Job.FromWorkflow(IMediaOpsPlanApi, Guid)"/>) would silently be lost.
		/// </summary>
		private void SaveChanges(ICollection<Job> jobs)
		{
			var toSave = jobs.Where(x => x != null && (x.IsNew || x.HasChanges)).ToList();
			if (toSave.Count == 0)
			{
				return;
			}

			CreateOrUpdate(toSave);
		}
	}
}
