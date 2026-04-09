namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	using SLDataGateway.API.Types.Querying;

	internal class JobsRepository : Repository, IJobsRepository
	{
		private readonly JobFilterTranslator filterTranslator = new JobFilterTranslator();

		public JobsRepository(MediaOpsPlanApi planApi) : base(planApi)
		{
		}

		public long Count()
		{
			throw new NotImplementedException();
		}

		public long Count(FilterElement<Job> filter)
		{
			throw new NotImplementedException();
		}

		public long Count(IQuery<Job> query)
		{
			throw new NotImplementedException();
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
			var toDelete = Read(apiObjectId);
			if (toDelete == null)
			{
				return;
			}

			if (!DomJobHandler.TryDelete(PlanApi, [toDelete], out var result))
			{
				result.ThrowSingleException(toDelete.Id);
			}
		}

		public void Delete(IEnumerable<Guid> apiObjectIds)
		{
			if (apiObjectIds == null)
			{
				throw new ArgumentNullException(nameof(apiObjectIds));
			}

			var toDelete = Read(apiObjectIds.ToArray());

			if (!DomJobHandler.TryDelete(PlanApi, toDelete?.ToList(), out var result))
			{
				result.ThrowBulkException();
			}
		}

		public void Delete(IEnumerable<Job> oToDelete)
		{
			if (oToDelete == null)
			{
				throw new ArgumentNullException(nameof(oToDelete));
			}

			Delete(oToDelete.Select(x => x.Id).ToArray());
		}

		public void Delete(Job oToDelete)
		{
			if (oToDelete == null)
			{
				throw new ArgumentNullException(nameof(oToDelete));
			}

			Delete(oToDelete.Id);
		}

		public IEnumerable<Job> Read(FilterElement<Job> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return ActivityHelper.Track(nameof(JobsRepository), nameof(Read), act =>
            {
                var domFilter = filterTranslator.Translate(filter);
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

			return Read(query.Filter);
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
			throw new NotImplementedException();
		}

		public IEnumerable<SDM.IPagedResult<Job>> ReadPaged(int pageSize)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<SDM.IPagedResult<Job>> ReadPaged(FilterElement<Job> filter)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<SDM.IPagedResult<Job>> ReadPaged(IQuery<Job> query)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<SDM.IPagedResult<Job>> ReadPaged(FilterElement<Job> filter, int pageSize)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<SDM.IPagedResult<Job>> ReadPaged(IQuery<Job> query, int pageSize)
		{
			throw new NotImplementedException();
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
					break;
			}

			var error = job.OriginalInstance.Errors.FirstOrDefault(e => e.ErrorCode == errorCode);
			if (error == null)
			{
				error = new Storage.DOM.SlcWorkflow.ErrorsSection
				{
					ErrorCode = errorCode,
				};

				job.OriginalInstance.Errors.Add(error);
			}

			error.ErrorMessage = updateDetails.Message;

            job.OriginalInstance.Save(PlanApi.DomHelpers.SlcWorkflowHelper.DomHelper);
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
	}
}
