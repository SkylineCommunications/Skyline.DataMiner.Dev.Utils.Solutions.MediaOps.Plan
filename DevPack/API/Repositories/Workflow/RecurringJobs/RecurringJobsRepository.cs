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
			throw new NotImplementedException();
		}

		public IReadOnlyCollection<RecurringJob> Cancel(IEnumerable<Guid> recurringJobIds)
		{
			throw new NotImplementedException();
		}

		public RecurringJob Complete(RecurringJob recurringJob)
		{
			throw new NotImplementedException();
		}

		public RecurringJob Complete(Guid recurringJobId)
		{
			throw new NotImplementedException();
		}

		public IReadOnlyCollection<RecurringJob> Complete(IEnumerable<RecurringJob> recurringJobs)
		{
			throw new NotImplementedException();
		}

		public IReadOnlyCollection<RecurringJob> Complete(IEnumerable<Guid> recurringJobIds)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		public RecurringJob Create(RecurringJob oToCreate)
		{
			throw new NotImplementedException();
		}

		public IReadOnlyCollection<RecurringJob> CreateOrUpdate(IEnumerable<RecurringJob> oToCreateOrUpdate)
		{
			throw new NotImplementedException();
		}

		public void Delete(Guid apiObjectId)
		{
			throw new NotImplementedException();
		}

		public void Delete(IEnumerable<Guid> apiObjectIds)
		{
			throw new NotImplementedException();
		}

		public void Delete(IEnumerable<RecurringJob> oToDelete)
		{
			throw new NotImplementedException();
		}

		public void Delete(RecurringJob oToDelete)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		public RecurringJob Update(RecurringJob oToUpdate)
		{
			throw new NotImplementedException();
		}
	}
}
