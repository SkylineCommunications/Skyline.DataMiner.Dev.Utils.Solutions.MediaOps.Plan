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
    }
}
