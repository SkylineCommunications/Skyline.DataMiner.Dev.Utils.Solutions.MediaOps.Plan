namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Jobs;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;

	using SLDataGateway.API.Types.Querying;

	internal class RecurringJobsRepository : Repository, IRecurringJobsRepository
	{
		private readonly RecurringJobFilterTranslator filterTranslator = new RecurringJobFilterTranslator();

		public RecurringJobsRepository(MediaOpsPlanApi planApi) : base(planApi)
		{
		}

		public RecurringJob Cancel(RecurringJob recurringJob)
		{
			if (recurringJob == null)
			{
				throw new ArgumentNullException(nameof(recurringJob));
			}

			return Cancel(recurringJob.Id);
		}

		public RecurringJob Cancel(Guid recurringJobId)
		{
			var recurringJob = Read(recurringJobId);
			if (recurringJob == null)
			{
				return null;
			}

			if (!DomRecurringJobHandler.TryCancel(PlanApi, [recurringJob], out var result))
			{
				result.ThrowSingleException(recurringJob.Id);
			}

			return new RecurringJob(PlanApi, result.SuccessfulItems.Single());
		}

		public IReadOnlyCollection<RecurringJob> Cancel(IEnumerable<RecurringJob> recurringJobs)
		{
			if (recurringJobs == null)
			{
				throw new ArgumentNullException(nameof(recurringJobs));
			}

			return Cancel(recurringJobs.Select(x => x.Id).ToArray());
		}

		public IReadOnlyCollection<RecurringJob> Cancel(IEnumerable<Guid> recurringJobIds)
		{
			if (recurringJobIds == null)
			{
				throw new ArgumentNullException(nameof(recurringJobIds));
			}

			var recurringJobs = Read(recurringJobIds);
			if (!DomRecurringJobHandler.TryCancel(PlanApi, recurringJobs?.ToList(), out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new RecurringJob(PlanApi, x)).ToList();
		}

		public RecurringJob Complete(RecurringJob recurringJob)
		{
			if (recurringJob == null)
			{
				throw new ArgumentNullException(nameof(recurringJob));
			}

			return Complete(recurringJob.Id);
		}

		public RecurringJob Complete(Guid recurringJobId)
		{
			var recurringJob = Read(recurringJobId);
			if (recurringJob == null)
			{
				return null;
			}

			if (!DomRecurringJobHandler.TryComplete(PlanApi, [recurringJob], out var result))
			{
				result.ThrowSingleException(recurringJob.Id);
			}

			return new RecurringJob(PlanApi, result.SuccessfulItems.Single());
		}

		public IReadOnlyCollection<RecurringJob> Complete(IEnumerable<RecurringJob> recurringJobs)
		{
			if (recurringJobs == null)
			{
				throw new ArgumentNullException(nameof(recurringJobs));
			}

			return Complete(recurringJobs.Select(x => x.Id).ToArray());
		}

		public IReadOnlyCollection<RecurringJob> Complete(IEnumerable<Guid> recurringJobIds)
		{
			if (recurringJobIds == null)
			{
				throw new ArgumentNullException(nameof(recurringJobIds));
			}

			var recurringJobs = Read(recurringJobIds);
			if (!DomRecurringJobHandler.TryComplete(PlanApi, recurringJobs?.ToList(), out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new RecurringJob(PlanApi, x)).ToList();
		}

		public long Count()
		{
			return Count(new TRUEFilterElement<RecurringJob>());
		}

		public long Count(FilterElement<RecurringJob> filter)
		{
			if (filter.isEmpty())
			{
				return 0;
			}

			var domFilter = filterTranslator.Translate(filter);
			return PlanApi.DomHelpers.SlcWorkflowHelper.CountWorkflowInstances(domFilter);
		}

		public long Count(IQuery<RecurringJob> query)
		{
			return Count(query.Filter);
		}

		public IReadOnlyCollection<RecurringJob> Create(IEnumerable<RecurringJob> oToCreate)
		{
			if (oToCreate == null)
			{
				throw new ArgumentNullException(nameof(oToCreate));
			}

			var list = oToCreate.ToList();

			var existing = list.Where(x => !x.IsNew);
			if (existing.Any())
			{
				throw new InvalidOperationException("Not possible to use method Create for existing recurring jobs. Use CreateOrUpdate or Update instead.");
			}

			if (!DomRecurringJobHandler.TryCreateOrUpdate(PlanApi, list, out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new RecurringJob(PlanApi, x)).ToList();
		}

		public RecurringJob Create(RecurringJob oToCreate)
		{
			if (oToCreate == null)
			{
				throw new ArgumentNullException(nameof(oToCreate));
			}

			if (!oToCreate.IsNew)
			{
				throw new InvalidOperationException("Not possible to use method Create for existing recurring job. Use CreateOrUpdate or Update instead.");
			}

			if (!DomRecurringJobHandler.TryCreateOrUpdate(PlanApi, [oToCreate], out var result))
			{
				result.ThrowSingleException(oToCreate.Id);
			}

			return new RecurringJob(PlanApi, result.SuccessfulItems.Single());
		}

		public IReadOnlyCollection<RecurringJob> CreateOrUpdate(IEnumerable<RecurringJob> oToCreateOrUpdate)
		{
			if (oToCreateOrUpdate == null)
			{
				throw new ArgumentNullException(nameof(oToCreateOrUpdate));
			}

			var list = oToCreateOrUpdate.ToList();

			if (!DomRecurringJobHandler.TryCreateOrUpdate(PlanApi, list, out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new RecurringJob(PlanApi, x)).ToList();
		}

		public void Delete(Guid apiObjectId)
		{
			var toDelete = Read(apiObjectId);
			if (toDelete == null)
			{
				return;
			}

			if (!DomRecurringJobHandler.TryDelete(PlanApi, [toDelete], out var result))
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

			if (!DomRecurringJobHandler.TryDelete(PlanApi, toDelete?.ToList(), out var result))
			{
				result.ThrowBulkException();
			}
		}

		public void Delete(IEnumerable<RecurringJob> oToDelete)
		{
			if (oToDelete == null)
			{
				throw new ArgumentNullException(nameof(oToDelete));
			}

			Delete(oToDelete.Select(x => x.Id).ToArray());
		}

		public void Delete(RecurringJob oToDelete)
		{
			if (oToDelete == null)
			{
				throw new ArgumentNullException(nameof(oToDelete));
			}

			Delete(oToDelete.Id);
		}

		public IEnumerable<RecurringJob> Read(FilterElement<RecurringJob> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (filter.isEmpty())
			{
				return Enumerable.Empty<RecurringJob>();
			}

			return ActivityHelper.Track(nameof(RecurringJobsRepository), nameof(Read), act =>
			{
				var domFilter = filterTranslator.Translate(filter);
				IEnumerable<RecurringJob> Iterator()
				{
					foreach (var domRecurringJob in PlanApi.DomHelpers.SlcWorkflowHelper.GetRecurringJobs(domFilter))
					{
						yield return new RecurringJob(PlanApi, domRecurringJob);
					}
				}

				return Iterator();
			});
		}

		public IEnumerable<RecurringJob> Read(IQuery<RecurringJob> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			return Read(query.Filter);
		}

		public IEnumerable<RecurringJob> Read()
		{
			return Read(new TRUEFilterElement<RecurringJob>());
		}

		public RecurringJob Read(Guid id)
		{
			if (id == Guid.Empty)
			{
				throw new ArgumentNullException(nameof(id));
			}

			return ActivityHelper.Track(nameof(RecurringJobsRepository), nameof(Read), act =>
			{
				act?.AddTag("RecurringJobId", id);
				var recurringJob = Read(RecurringJobExposers.Id.Equal(id)).FirstOrDefault();

				if (recurringJob == null)
				{
					act?.AddTag("Hit", false);
					return null;
				}

				act?.AddTag("Hit", true);
				return recurringJob;
			});
		}

		public IEnumerable<RecurringJob> Read(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Array.Empty<RecurringJob>();
			}

			return Read(new ORFilterElement<RecurringJob>(ids.Select(x => RecurringJobExposers.Id.Equal(x)).ToArray()));
		}

		public IEnumerable<SDM.IPagedResult<RecurringJob>> ReadPaged()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<SDM.IPagedResult<RecurringJob>> ReadPaged(int pageSize)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<SDM.IPagedResult<RecurringJob>> ReadPaged(FilterElement<RecurringJob> filter)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<SDM.IPagedResult<RecurringJob>> ReadPaged(IQuery<RecurringJob> query)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<SDM.IPagedResult<RecurringJob>> ReadPaged(FilterElement<RecurringJob> filter, int pageSize)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<SDM.IPagedResult<RecurringJob>> ReadPaged(IQuery<RecurringJob> query, int pageSize)
		{
			throw new NotImplementedException();
		}

		public IReadOnlyCollection<RecurringJob> Update(IEnumerable<RecurringJob> oToUpdate)
		{
			if (oToUpdate == null)
			{
				throw new ArgumentNullException(nameof(oToUpdate));
			}

			var list = oToUpdate.ToList();

			var newRecurringJobs = list.Where(x => x.IsNew);
			if (newRecurringJobs.Any())
			{
				throw new InvalidOperationException("Not possible to use method Update for new recurring jobs. Use Create or CreateOrUpdate instead.");
			}

			if (!DomRecurringJobHandler.TryCreateOrUpdate(PlanApi, list, out var result))
			{
				result.ThrowBulkException();
			}

			return result.SuccessfulItems.Select(x => new RecurringJob(PlanApi, x)).ToList();
		}

		public RecurringJob Update(RecurringJob oToUpdate)
		{
			if (oToUpdate == null)
			{
				throw new ArgumentNullException(nameof(oToUpdate));
			}

			if (oToUpdate.IsNew)
			{
				throw new InvalidOperationException("Not possible to use method Update for new recurring job. Use Create or CreateOrUpdate instead.");
			}

			if (!DomRecurringJobHandler.TryCreateOrUpdate(PlanApi, [oToUpdate], out var result))
			{
				result.ThrowSingleException(oToUpdate.Id);
			}

			return new RecurringJob(PlanApi, result.SuccessfulItems.Single());
		}
	}
}
