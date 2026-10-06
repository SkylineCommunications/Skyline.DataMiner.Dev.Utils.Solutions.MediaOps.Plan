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
		private readonly WorkflowConfigurationCache configurationCache = new WorkflowConfigurationCache();

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

		/// <summary>
		/// Opens a scope in which the configurations that are read are cached and can be pre-seeded.
		/// </summary>
		/// <param name="idsToPrefetch">Identifiers to read in a single batch when the scope is opened. Optional.</param>
		/// <param name="configurationsToSeed">Configurations to make available without reading them. Optional.</param>
		/// <returns>A handle that closes the scope when disposed.</returns>
		public IDisposable BeginConfigurationScope(IEnumerable<Guid> idsToPrefetch = null, IEnumerable<ConfigurationInstance> configurationsToSeed = null)
		{
			var scope = configurationCache.BeginScope();

			try
			{
				if (idsToPrefetch != null)
				{
					// Materialized so the batched read is performed now rather than on first enumeration.
					GetConfigurations(idsToPrefetch).ToList();
				}

				if (configurationsToSeed != null)
				{
					// Seeded after the prefetch so an in-memory configuration always wins from the stored one.
					configurationCache.Seed(configurationsToSeed);
				}
			}
			catch
			{
				scope.Dispose();
				throw;
			}

			return scope;
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

			if (!configurationCache.IsActive)
			{
				return ReadConfigurations(ids);
			}

			var cached = configurationCache.Resolve(ids, out var missing);
			if (missing.Count == 0)
			{
				return cached;
			}

			var read = ReadConfigurations(missing).ToList();
			configurationCache.Store(missing, read);

			return cached.Concat(read).ToList();
		}

		private IEnumerable<ConfigurationInstance> ReadConfigurations(IEnumerable<Guid> ids)
		{
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

		public IEnumerable<RecurringJobsInstance> GetRecurringJobs(IQuery<DomInstance> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			return GetRecurringJobIterator(query);
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

		public IEnumerable<WorkflowsInstance> GetWorkflows(IQuery<DomInstance> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			return GetWorkflowIterator(query);
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

			return InstanceFactory.ReadAndCreateInstancesPaged(DomHelper, query, pageSize, instance => new JobsInstance(instance));
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

		public IEnumerable<IEnumerable<RecurringJobsInstance>> GetRecurringJobsPaged(IQuery<DomInstance> query, int pageSize)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (pageSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(pageSize));
			}

			return InstanceFactory.ReadAndCreateInstancesPaged(DomHelper, query, pageSize, instance => new RecurringJobsInstance(instance));
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

		public IEnumerable<IEnumerable<WorkflowsInstance>> GetWorkflowsPaged(IQuery<DomInstance> query, int pageSize)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (pageSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(pageSize));
			}

			return InstanceFactory.ReadAndCreateInstancesPaged(DomHelper, query, pageSize, instance => new WorkflowsInstance(instance));
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

		private IEnumerable<RecurringJobsInstance> GetRecurringJobIterator(IQuery<DomInstance> query)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, query, instance => new RecurringJobsInstance(instance));
		}

		private IEnumerable<WorkflowsInstance> GetWorkflowIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new WorkflowsInstance(instance));
		}

		private IEnumerable<WorkflowsInstance> GetWorkflowIterator(IQuery<DomInstance> query)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, query, instance => new WorkflowsInstance(instance));
		}

		private IEnumerable<AppSettingsInstance> GetAppSettingIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new AppSettingsInstance(instance));
		}
	}
}
