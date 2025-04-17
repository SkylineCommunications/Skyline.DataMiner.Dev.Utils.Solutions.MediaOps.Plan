namespace Skyline.DataMiner.MediaOps.API.Common.API.Generic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using DataMinerMessageBroker.API.Configuration;

    using Skyline.DataMiner.MediaOps.Common.Tools;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using SLDataGateway.API.Querying;

    using SLDataGateway.API.Types.Querying;

    internal abstract class DomDefinition<T, TConfig> : IApiCollection<T, TConfig>
        where T : DomObject
        where TConfig : DomConfiguration<T>
    {
        private readonly DomQueryProvider<T> _queryProvider;

        protected DomDefinition(DomHelper helper)
        {
            Helper = helper ?? throw new ArgumentNullException(nameof(helper));

            _queryProvider = new ApiRepositoryQueryProvider<T>(this);
        }

        protected DomHelper Helper { get; }

        protected internal abstract DomDefinitionId DomDefinition { get; }

        protected abstract T CreateInstance(DomInstance domInstance);

        public virtual void Add(TConfig config)
        {
            ValidateConfigForCreate(new[] { config });
            Helper.DomInstances.Create(config.TranslateToDomInstance());
        }

        public virtual void AddRange(IEnumerable<TConfig> configs)
        {
            ValidateConfigForCreate(configs);
            foreach (var config in configs)
            {
                Helper.DomInstances.Create(config.TranslateToDomInstance());
            }
        }

        public virtual void Update(TConfig config)
        {
            ValidateConfigForUpdate(new[] { config });

            // TODO: Get instance from cache or from DOM
            var prevObject = Read(config.ObjectId.Value);
            Helper.DomInstances.Update(config.TranslateToDomInstance(prevObject));
        }

        public virtual void UpdateRange(IEnumerable<TConfig> configs)
        {
            ValidateConfigForUpdate(configs);
            foreach (var config in configs)
            {
                // TODO: Get instance from cache or from DOM
                var prevObject = Read(config.ObjectId.Value);
                Helper.DomInstances.Update(config.TranslateToDomInstance(prevObject));
            }
        }

        public virtual void BulkUpdate(IEnumerable<Guid> ids, TConfig config)
        {
            ValidateConfigForBulkUpdate(config);
            foreach (var id in ids)
            {
                // TODO: Get instance from cache or from DOM
                var prevObject = Read(id);
                Helper.DomInstances.Update(config.TranslateToDomInstance(prevObject));
            }
        }

        public virtual void CreateOrUpdate(IEnumerable<T> instances)
        {
            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            var domInstances = instances.Select(x => x.DomInstance.ToInstance()).ToList();
            Helper.DomInstances.CreateOrUpdateInBatches(domInstances).ThrowOnFailure();
        }

        public virtual void Delete(Guid id)
        {
            // TODO: Get instance from cache or from DOM and check if we can avoid a read all together?
            var prevObject = Read(id);
            Helper.DomInstances.Delete(prevObject.DomInstance);
        }

        public virtual void Delete(IEnumerable<T> instances)
        {
            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            var domInstances = instances.Select(x => x.DomInstance.ToInstance()).ToList();
            Helper.DomInstances.DeleteInBatches(domInstances).ThrowOnFailure();
        }

        public virtual long CountAll()
        {
            var filter = DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id);
            return Helper.DomInstances.Count(filter);
        }

        public virtual long Count(FilterElement<T> filter)
        {
            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            var domFilter = TranslateFullFilter(filter);
            domFilter = domFilter.AND(DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id));

            return Helper.DomInstances.Count(domFilter);
        }

        internal long Count(FilterElement<DomInstance> domFilter)
        {
            if (domFilter is null)
            {
                throw new ArgumentNullException(nameof(domFilter));
            }

            domFilter = domFilter.AND(DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id));

            return Helper.DomInstances.Count(domFilter);
        }

        public virtual IEnumerable<T> ReadAll()
        {
            var filter = DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id);
            return Helper.DomInstances.Read(filter).Select(CreateInstance);
        }

        public virtual IEnumerable<IEnumerable<T>> ReadAllPaged()
        {
            var filter = DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id);
            return Helper.DomInstances.ReadPaged(filter).Select(x => x.Select(CreateInstance));
        }

        public virtual T Read(Guid id)
        {
            var filter = DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id)
                .AND(DomInstanceExposers.Id.Equal(id));

            var domInstance = Helper.DomInstances.Read(filter).SingleOrDefault();

            return domInstance != null ? CreateInstance(domInstance) : null;
        }

        public virtual IDictionary<Guid, T> Read(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            FilterElement<DomInstance> CreateFilter(Guid id) =>
                DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id)
                .AND(DomInstanceExposers.Id.Equal(id));

            return FilterQueryExecutor.RetrieveFilteredItems(
                    ids,
                    x => CreateFilter(x),
                    x => Helper.DomInstances.Read(x))
                .Select(CreateInstance)
                .SafeToDictionary(x => x.ID);
        }

        public virtual IEnumerable<T> Read(FilterElement<T> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            var domFilter = TranslateFullFilter(filter);

            return Read(domFilter);
        }

        /// <summary>
        /// This method is used to validate the configuration for creating a new object.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <exception cref="ArgumentException">If the configuration is invalid.</exception>
        protected abstract void ValidateConfigForCreate(IEnumerable<TConfig> configuration);

        /// <summary>
        /// This method is used to validate the configuration for updating an existing  object.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <exception cref="ArgumentException">If the configuration is invalid.</exception>
        protected abstract void ValidateConfigForUpdate(IEnumerable<TConfig> configuration);

        /// <summary>
        /// This method is used to validate the configuration for updating multiple objects with the same configuration.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <exception cref="ArgumentException">If the configuration is invalid.</exception>
        protected abstract void ValidateConfigForBulkUpdate(TConfig configuration);

        internal IEnumerable<T> Read(FilterElement<DomInstance> domFilter)
        {
            if (domFilter == null)
            {
                throw new ArgumentNullException(nameof(domFilter));
            }

            domFilter = domFilter.AND(DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id));

            var domInstances = Helper.DomInstances.Read(domFilter);

            return domInstances.Select(CreateInstance);
        }

        public virtual IEnumerable<T> Read(IQuery<T> query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var domFilter = TranslateFullFilter(query.Filter);
            var domOrder = TranslateFullOrderBy(query.Order);

            var domQuery = query
                .WithFilter(domFilter)
                .WithOrder(domOrder);

            return Read(domQuery);
        }

        internal IEnumerable<T> Read(IQuery<DomInstance> domQuery)
        {
            if (domQuery == null)
            {
                throw new ArgumentNullException(nameof(domQuery));
            }

            var domFilter = domQuery.Filter.AND(DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id));

            domQuery = domQuery.WithFilter(domFilter);

            var domInstances = Helper.DomInstances.Read(domQuery);

            return domInstances.Select(CreateInstance);
        }

        public virtual IQueryable<T> Query()
        {
            return new ApiRepositoryQuery<T>(_queryProvider);
        }

        protected internal virtual FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
        {
            switch (fieldName)
            {
                case nameof(ApiObject<T>.ID):
                    return FilterElementFactory.Create(DomInstanceExposers.Id, comparer, (Guid)value);
                default:
                    throw new NotImplementedException();
            }
        }

        protected internal virtual IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
        {
            switch (fieldName)
            {
                case nameof(ApiObject<T>.ID):
                    return OrderByElementFactory.Create(DomInstanceExposers.Id, sortOrder, naturalSort);
                default:
                    throw new NotImplementedException();
            }
        }

        protected virtual FilterElement<DomInstance> TranslateFullFilter(FilterElement<T> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            FilterElement<DomInstance> translated;

            if (filter is ANDFilterElement<T> and)
            {
                translated = new ANDFilterElement<DomInstance>(and.subFilters.Select(TranslateFullFilter).ToArray());
            }
            else if (filter is ORFilterElement<T> or)
            {
                translated = new ORFilterElement<DomInstance>(or.subFilters.Select(TranslateFullFilter).ToArray());
            }
            else if (filter is NOTFilterElement<T> not)
            {
                translated = new NOTFilterElement<DomInstance>(TranslateFullFilter(not));
            }
            else if (filter is TRUEFilterElement<T>)
            {
                translated = new TRUEFilterElement<DomInstance>();
            }
            else if (filter is FALSEFilterElement<T>)
            {
                translated = new FALSEFilterElement<DomInstance>();
            }
            else if (filter is ManagedFilterIdentifier managedFilter)
            {
                translated = TranslateFilter(managedFilter);
            }
            else
            {
                throw new NotSupportedException($"Unsupported filter: {filter}");
            }

            return translated;
        }

        protected virtual IOrderBy TranslateFullOrderBy(IOrderBy order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            var translatedElements = new List<IOrderByElement>();

            foreach (var orderByElement in order.Elements)
            {
                var translated = TranslateOrderBy(orderByElement);
                translatedElements.Add(translated);
            }

            return new OrderBy(translatedElements);
        }

        protected virtual FilterElement<DomInstance> TranslateFilter(ManagedFilterIdentifier managedFilter)
        {
            if (managedFilter == null)
            {
                throw new ArgumentNullException(nameof(managedFilter));
            }

            var fieldName = managedFilter.getFieldName().fieldName;
            var comparer = managedFilter.getComparer();
            var value = managedFilter.getValue();

            var translated = CreateFilter(fieldName, comparer, value);

            return translated;
        }

        protected virtual IOrderByElement TranslateOrderBy(IOrderByElement orderByElement)
        {
            if (orderByElement == null)
            {
                throw new ArgumentNullException(nameof(orderByElement));
            }

            var fieldName = orderByElement.Exposer.fieldName;
            var sortOrder = orderByElement.SortOrder;
            var naturalSort = orderByElement.Options.NaturalSort;

            var translated = CreateOrderBy(fieldName, sortOrder, naturalSort);

            return translated;
        }
    }
}
