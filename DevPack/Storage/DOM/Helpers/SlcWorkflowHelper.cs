namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
    using System;
    using System.Collections.Generic;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

    internal class SlcWorkflowHelper : DomModuleHelperBase
    {
        public SlcWorkflowHelper(IConnection connection) : base(SlcWorkflowIds.ModuleId, connection)
        {
        }

        public long CountWorkflowInstances(FilterElement<DomInstance> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return DomHelper.DomInstances.Count(filter);
        }

        public IEnumerable<ConfigurationInstance> GetConfigurations(FilterElement<DomInstance> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return GetConfigurationIterator(filter);
        }

        public IEnumerable<JobsInstance> GetJobs(FilterElement<DomInstance> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return GetJobIterator(filter);
        }

        public IEnumerable<RecurringJobsInstance> GetRecurringJobs(FilterElement<DomInstance> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return GetRecurringJobIterator(filter);
        }

        public IEnumerable<WorkflowsInstance> GetWorkflows(FilterElement<DomInstance> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return GetWorkflowIterator(filter);
        }

        private IEnumerable<ConfigurationInstance> GetConfigurationIterator(FilterElement<DomInstance> filter)
        {
            return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new ConfigurationInstance(instance));
        }

        private IEnumerable<JobsInstance> GetJobIterator(FilterElement<DomInstance> filter)
        {
            return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new JobsInstance(instance));
        }

        private IEnumerable<RecurringJobsInstance> GetRecurringJobIterator(FilterElement<DomInstance> filter)
        {
            return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new RecurringJobsInstance(instance));
        }

        private IEnumerable<WorkflowsInstance> GetWorkflowIterator(FilterElement<DomInstance> filter)
        {
            return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new WorkflowsInstance(instance));
        }
    }
}
