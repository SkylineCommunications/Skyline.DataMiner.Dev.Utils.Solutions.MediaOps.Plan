namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;

	internal static class InstanceFactory
	{
		/// <summary>
		/// The limit used by <see cref="LimitBy.Default"/>, indicating that the query is not limited.
		/// </summary>
		private const int NoLimit = Int32.MaxValue;

		private static readonly ConcurrentDictionary<string, Dictionary<Guid, Guid[]>> SoftDeletedFieldsPerModule = new ConcurrentDictionary<string, Dictionary<Guid, Guid[]>>();

		public static IEnumerable<T> ReadAndCreateInstances<T>(DomHelper domHelper, FilterElement<DomInstance> filter, Func<DomInstance, T> createInstance)
							where T : DomInstanceBase
		{
			if (domHelper == null)
			{
				throw new ArgumentNullException(nameof(domHelper));
			}

			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (createInstance == null)
			{
				throw new ArgumentNullException(nameof(createInstance));
			}

			if (filter.isEmpty())
			{
				return Enumerable.Empty<T>();
			}

			InitSoftDeletedFields(domHelper);

			var pages = domHelper.DomInstances.ReadPaged(filter);
			var instances = pages.SelectMany(page => page);

			return CreateInstances(instances, createInstance);
		}

		public static IEnumerable<T> ReadAndCreateInstances<T>(DomHelper domHelper, IQuery<DomInstance> query, Func<DomInstance, T> createInstance)
			where T : DomInstanceBase
		{
			if (domHelper == null)
			{
				throw new ArgumentNullException(nameof(domHelper));
			}

			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (createInstance == null)
			{
				throw new ArgumentNullException(nameof(createInstance));
			}

			if (query.Filter.isEmpty())
			{
				return Enumerable.Empty<T>();
			}

			InitSoftDeletedFields(domHelper);

			// A paged read only uses the limit as a hint for the page size: it keeps requesting pages until the server
			// reports the final page, so the limit and offset of the query are not applied to the total result.
			// A regular read applies them, so it must be used whenever the query is limited.
			var instances = IsLimited(query)
				? (IEnumerable<DomInstance>)domHelper.DomInstances.Read(query)
				: domHelper.DomInstances.ReadPaged(query).SelectMany(page => page);

			return CreateInstances(instances, createInstance);
		}

		public static IEnumerable<IEnumerable<T>> ReadAndCreateInstancesPaged<T>(DomHelper domHelper, IQuery<DomInstance> query, int pageSize, Func<DomInstance, T> createInstance)
			where T : DomInstanceBase
		{
			if (domHelper == null)
			{
				throw new ArgumentNullException(nameof(domHelper));
			}

			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (pageSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(pageSize));
			}

			if (createInstance == null)
			{
				throw new ArgumentNullException(nameof(createInstance));
			}

			InitSoftDeletedFields(domHelper);

			// A paged read only uses the limit as a hint for the page size: it keeps requesting pages until the server
			// reports the final page, so the limit and offset of the query are not applied to the total result.
			// A regular read applies them, so a limited query is read at once and split into pages afterwards.
			var pages = IsLimited(query)
				? SplitInPages(domHelper.DomInstances.Read(query), pageSize)
				: domHelper.DomInstances.ReadPaged(query, pageSize);

			return CreateInstances(pages, createInstance);
		}

		public static IEnumerable<IEnumerable<T>> CreateInstances<T>(IEnumerable<IEnumerable<DomInstance>> pages, Func<DomInstance, T> createInstance)
							where T : DomInstanceBase
		{
			if (pages == null)
			{
				throw new ArgumentNullException(nameof(pages));
			}

			if (createInstance == null)
			{
				throw new ArgumentNullException(nameof(createInstance));
			}

			return CreateInstancesPagedIterator(pages, createInstance);
		}

		private static IEnumerable<T> CreateInstances<T>(IEnumerable<DomInstance> domInstances, Func<DomInstance, T> createInstance)
							where T : DomInstanceBase
		{
			if (domInstances == null)
			{
				throw new ArgumentNullException(nameof(domInstances));
			}

			if (createInstance == null)
			{
				throw new ArgumentNullException(nameof(createInstance));
			}

			return CreateInstancesIterator(domInstances, createInstance);
		}

		private static IEnumerable<T> CreateInstancesIterator<T>(IEnumerable<DomInstance> domInstances, Func<DomInstance, T> createInstance)
			where T : DomInstanceBase
		{
			foreach (var domInstance in domInstances)
			{
				if (domInstance == null)
				{
					continue;
				}

				TryDeleteSoftDeletedFields(domInstance);

				var instance = createInstance(domInstance);

				yield return instance;
			}
		}

		private static IEnumerable<IEnumerable<T>> CreateInstancesPagedIterator<T>(IEnumerable<IEnumerable<DomInstance>> pages, Func<DomInstance, T> createInstance) where T : DomInstanceBase
		{
			foreach (var page in pages)
			{
				yield return CreateInstancesIterator(page, createInstance);
			}
		}

		private static IEnumerable<IEnumerable<DomInstance>> SplitInPages(IEnumerable<DomInstance> instances, int pageSize)
		{
			var page = new List<DomInstance>(pageSize);

			foreach (var instance in instances)
			{
				page.Add(instance);

				if (page.Count == pageSize)
				{
					yield return page;
					page = new List<DomInstance>(pageSize);
				}
			}

			if (page.Count > 0)
			{
				yield return page;
			}
		}

		private static bool IsLimited(IQuery<DomInstance> query)
		{
			var limit = query.Limit;
			if (limit == null)
			{
				return false;
			}

			if (limit.Limit != NoLimit)
			{
				return true;
			}

			return limit is LimitBy limitBy && limitBy.Offset > 0;
		}

		private static void InitSoftDeletedFields(DomHelper domHelper)
		{
			if (SoftDeletedFieldsPerModule.ContainsKey(domHelper.ModuleId))
			{
				return;
			}

			Dictionary<Guid, Guid[]> softDeletedFieldsPerSection = new Dictionary<Guid, Guid[]>();

			var sectionDefinitions = domHelper.SectionDefinitions.ReadAll();
			foreach (var sectionDefinition in sectionDefinitions)
			{
				var softDeletedFields = sectionDefinition.GetAllFieldDescriptors().Where(x => x.IsSoftDeleted).Select(x => x.ID.Id).ToArray();
				if (softDeletedFields.Length == 0)
				{
					continue;
				}

				softDeletedFieldsPerSection.Add(sectionDefinition.GetID().Id, softDeletedFields);
			}

			SoftDeletedFieldsPerModule.TryAdd(domHelper.ModuleId, softDeletedFieldsPerSection);
		}

		private static void TryDeleteSoftDeletedField(Net.Sections.Section section)
		{
			if (!SoftDeletedFieldsPerModule.TryGetValue(section.SectionDefinitionID.ModuleId, out var softDeletedFieldsPerSection))
				return;

			if (!softDeletedFieldsPerSection.TryGetValue(section.SectionDefinitionID.Id, out var softDeletedFields))
				return;

			softDeletedFields.ForEach(x => section.RemoveFieldValueById(new Net.Sections.FieldDescriptorID(x)));
		}

		private static void TryDeleteSoftDeletedFields(DomInstance instance)
		{
			foreach (var section in instance.Sections)
			{
				TryDeleteSoftDeletedField(section);
			}
		}
	}
}
