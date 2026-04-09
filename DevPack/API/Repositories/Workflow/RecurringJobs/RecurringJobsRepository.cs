namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;

	using SLDataGateway.API.Types.Querying;

	internal class RecurringJobsRepository : Repository, IRecurringJobsRepository
	{
		private readonly RecurringJobFilterTranslator filterTranslator = new RecurringJobFilterTranslator();

		public RecurringJobsRepository(MediaOpsPlanApi planApi) : base(planApi)
		{
		}

		public IEnumerable<RecurringJob> Read(FilterElement<RecurringJob> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return ActivityHelper.Track(nameof(RecurringJobsRepository), nameof(Read), act =>
			{
				var domFilter = filterTranslator.Translate(filter);
				IEnumerable<RecurringJob> Iterator()
				{
					foreach (var domRecurringJob in PlanApi.DomHelpers.SlcWorkflowHelper.GetRecurringJobs(domFilter))
					{
						yield return new RecurringJob(domRecurringJob);
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
				var resourcePool = Read(RecurringJobExposers.Id.Equal(id)).FirstOrDefault();

				if (resourcePool == null)
				{
					act?.AddTag("Hit", false);
					return null;
				}

				act?.AddTag("Hit", true);
				return resourcePool;
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
	}
}
