namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using SLDataGateway.API.Types.Querying;

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

		public long CountWorkflowInstances(IQuery<DomInstance> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			return DomHelper.DomInstances.Count(query);
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

		public IEnumerable<JobsInstance> GetJobs(IQuery<DomInstance> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			return GetJobIterator(query);
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

		public IEnumerable<RecurringJobsInstance> GetRecurringJobs(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Enumerable.Empty<RecurringJobsInstance>();
			}

			FilterElement<DomInstance> Filter(Guid id) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.RecurringJobs.Id)
				.AND(DomInstanceExposers.Id.Equal(id));

			return FilterQueryExecutor.RetrieveFilteredItems(
				ids.Distinct(),
				x => Filter(x),
				x => GetRecurringJobIterator(x));
		}

		public IEnumerable<WorkflowsInstance> GetWorkflows(FilterElement<DomInstance> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return GetWorkflowIterator(filter);
		}

		public IEnumerable<WorkflowsInstance> GetWorkflows<T>(IEnumerable<T> values, Func<T, FilterElement<DomInstance>> filter)
		{
			if (values == null)
			{
				throw new ArgumentNullException(nameof(values));
			}

			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return FilterQueryExecutor.RetrieveFilteredItems(
				values.Distinct(),
				x => filter(x),
				x => GetWorkflowIterator(x));
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

		public IEnumerable<IEnumerable<JobsInstance>> GetJobsPaged(FilterElement<DomInstance> filter, int pageSize)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (pageSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(pageSize));
			}

			var pages = DomHelper.DomInstances.ReadPaged(filter, pageSize);
			return InstanceFactory.CreateInstances(pages, instance => new JobsInstance(instance));
		}

		public IEnumerable<IEnumerable<JobsInstance>> GetJobsPaged(IQuery<DomInstance> query, int pageSize)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (pageSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(pageSize));
			}

			var pages = DomHelper.DomInstances.ReadPaged(query, pageSize);
			return InstanceFactory.CreateInstances(pages, instance => new JobsInstance(instance));
		}

		public IEnumerable<IEnumerable<RecurringJobsInstance>> GetRecurringJobsPaged(FilterElement<DomInstance> filter, int pageSize)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (pageSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(pageSize));
			}

			var pages = DomHelper.DomInstances.ReadPaged(filter, pageSize);
			return InstanceFactory.CreateInstances(pages, instance => new RecurringJobsInstance(instance));
		}

		public IEnumerable<IEnumerable<WorkflowsInstance>> GetWorkflowsPaged(FilterElement<DomInstance> filter, int pageSize)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (pageSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(pageSize));
			}

			var pages = DomHelper.DomInstances.ReadPaged(filter, pageSize);
			return InstanceFactory.CreateInstances(pages, instance => new WorkflowsInstance(instance));
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

		public IEnumerable<AppSettingsInstance> GetAppSettings(FilterElement<DomInstance> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return GetAppSettingIterator(filter);
		}

		public IEnumerable<AppSettingsInstance> GetAppSettings(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Enumerable.Empty<AppSettingsInstance>();
			}

			FilterElement<DomInstance> Filter(Guid id) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.AppSettings.Id)
				.AND(DomInstanceExposers.Id.Equal(id));

			return FilterQueryExecutor.RetrieveFilteredItems(
				ids.Distinct(),
				x => Filter(x),
				x => GetAppSettingIterator(x));
		}

		private IEnumerable<ConfigurationInstance> GetConfigurationIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new ConfigurationInstance(instance));
		}

		private IEnumerable<JobsInstance> GetJobIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new JobsInstance(instance));
		}

		private IEnumerable<JobsInstance> GetJobIterator(IQuery<DomInstance> query)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, query, instance => new JobsInstance(instance));
		}

		private IEnumerable<RecurringJobsInstance> GetRecurringJobIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new RecurringJobsInstance(instance));
		}

		private IEnumerable<WorkflowsInstance> GetWorkflowIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new WorkflowsInstance(instance));
		}

		private IEnumerable<AppSettingsInstance> GetAppSettingIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new AppSettingsInstance(instance));
		}
	}
}
