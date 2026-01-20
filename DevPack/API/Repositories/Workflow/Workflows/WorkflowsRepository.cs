namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using SLDataGateway.API.Types.Querying;

    internal class WorkflowsRepository : Repository, IWorkflowsRepository
    {
        private readonly WorkflowFilterTranslator filterTranslator = new WorkflowFilterTranslator();

        public WorkflowsRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        public IEnumerable<Workflow> Read(FilterElement<Workflow> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return ActivityHelper.Track(nameof(WorkflowsRepository), nameof(Read), act =>
            {
                var domFilter = filterTranslator.Translate(filter);
                IEnumerable<Workflow> Iterator()
                {
                    foreach (var domWorkflow in PlanApi.DomHelpers.SlcWorkflowHelper.GetWorkflows(domFilter))
                    {
                        yield return new Workflow(domWorkflow);
                    }
                }

                return Iterator();
            });
        }

        public IEnumerable<Workflow> Read(IQuery<Workflow> query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return Read(query.Filter);
        }

        public IEnumerable<Workflow> Read()
        {
            return Read(new TRUEFilterElement<Workflow>());
        }

        public Workflow Read(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return ActivityHelper.Track(nameof(WorkflowsRepository), nameof(Read), act =>
            {
                act?.AddTag("WorkflowId", id);
                var resourcePool = Read(WorkflowExposers.Id.Equal(id)).FirstOrDefault();

                if (resourcePool == null)
                {
                    act?.AddTag("Hit", false);
                    return null;
                }

                act?.AddTag("Hit", true);
                return resourcePool;
            });
        }

        public IEnumerable<Workflow> Read(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            if (!ids.Any())
            {
                return Array.Empty<Workflow>();
            }

            return Read(new ORFilterElement<Workflow>(ids.Select(x => WorkflowExposers.Id.Equal(x)).ToArray()));
        }
    }
}
