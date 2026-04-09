namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
    using System;
    using System.Collections.Generic;
	using System.Linq;

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

		public IEnumerable<ConfigurationInstance> GetConfigurations(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Enumerable.Empty<ConfigurationInstance>();
			}

			FilterElement<DomInstance> Filter(Guid id) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Configuration.Id)
				.AND(DomInstanceExposers.Id.Equal(id));

			return FilterQueryExecutor.RetrieveFilteredItems(
				ids.Distinct(),
				x => Filter(x),
				x => GetConfigurationIterator(x));
		}

		public IEnumerable<JobsInstance> GetJobs(FilterElement<DomInstance> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

			return GetJobIterator(filter);
		}

		public IEnumerable<JobsInstance> GetJobs(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Enumerable.Empty<JobsInstance>();
			}

			FilterElement<DomInstance> Filter(Guid id) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Jobs.Id)
				.AND(DomInstanceExposers.Id.Equal(id));

			return FilterQueryExecutor.RetrieveFilteredItems(
				ids.Distinct(),
				x => Filter(x),
				x => GetJobIterator(x));
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

		public IEnumerable<WorkflowsInstance> GetWorkflows(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Enumerable.Empty<WorkflowsInstance>();
			}

			FilterElement<DomInstance> Filter(Guid id) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Workflows.Id)
				.AND(DomInstanceExposers.Id.Equal(id));

			return FilterQueryExecutor.RetrieveFilteredItems(
				ids.Distinct(),
				x => Filter(x),
				x => GetWorkflowIterator(x));
		}

		public IEnumerable<DomInstance> GetWorkflowInstances(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Enumerable.Empty<DomInstance>();
			}

			return FilterQueryExecutor.RetrieveFilteredItems(
				ids.Distinct(),
				x => DomInstanceExposers.Id.Equal(x),
				x => DomHelper.DomInstances.Read(x));
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
