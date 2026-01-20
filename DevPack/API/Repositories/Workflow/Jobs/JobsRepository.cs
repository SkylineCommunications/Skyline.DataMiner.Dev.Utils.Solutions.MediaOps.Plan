namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using SLDataGateway.API.Types.Querying;

    internal class JobsRepository : Repository, IJobsRepository
    {
        private readonly JobFilterTranslator filterTranslator = new JobFilterTranslator();

        public JobsRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
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
                        yield return new Job(domJob);
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
                var resourcePool = Read(JobExposers.Id.Equal(id)).FirstOrDefault();

                if (resourcePool == null)
                {
                    act?.AddTag("Hit", false);
                    return null;
                }

                act?.AddTag("Hit", true);
                return resourcePool;
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
    }
}
