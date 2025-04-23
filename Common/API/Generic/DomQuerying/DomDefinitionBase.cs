namespace Skyline.DataMiner.MediaOps.API.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using DomHelpers;

    using Skyline.DataMiner.MediaOps.API.Common.API.Generic;
    using Skyline.DataMiner.MediaOps.API.Common.API.Generic.DomQuerying;
    using Skyline.DataMiner.MediaOps.Common;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Utils.DOM.Extensions;

    using SLDataGateway.API.Querying;
    using SLDataGateway.API.Types.Querying;

    internal abstract class DomDefinitionBase<T>
        where T : IApiObject
    {
        private readonly ApiRepositoryQueryProvider<T> _queryProvider;

        protected DomDefinitionBase(DomHelper helper)
        {
            DomHelper = helper ?? throw new ArgumentNullException(nameof(helper));

            _queryProvider = new ApiRepositoryQueryProvider<T>(this);
        }

        protected DomHelper DomHelper { get; }

        protected internal abstract DomDefinitionId DomDefinition { get; }

        protected abstract T CreateInstance(DomInstance domInstance);

        public virtual void Create(DomInstanceBase domInstance)
        {
            if (domInstance == null)
            {
                throw new ArgumentNullException(nameof(domInstance));
            }

            DomHelper.DomInstances.Create(domInstance);
        }

        public virtual void Update(DomInstanceBase domInstance)
        {
            if (domInstance == null)
            {
                throw new ArgumentNullException(nameof(domInstance));
            }

            DomHelper.DomInstances.Update(domInstance);
        }

        public virtual void CreateOrUpdate(IEnumerable<DomInstanceBase> instances)
        {
            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            var domInstances = instances.Select(x => x.ToInstance()).ToList();
            DomHelper.DomInstances.CreateOrUpdateInBatches(domInstances).ThrowOnFailure();
        }

        public virtual void Delete(DomInstanceBase domInstance)
        {
            if (domInstance == null)
            {
                throw new ArgumentNullException(nameof(domInstance));
            }

            DomHelper.DomInstances.Delete(domInstance);
        }

        public virtual void Delete(IEnumerable<DomInstanceBase> instances)
        {
            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            var domInstances = instances.Select(x => x.ToInstance()).ToList();
            DomHelper.DomInstances.DeleteInBatches(domInstances).ThrowOnFailure();
        }

        public virtual long CountAll()
        {
            var filter = DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id);
            return DomHelper.DomInstances.Count(filter);
        }

        public virtual long Count(FilterElement<T> filter)
        {
            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            var domFilter = TranslateFullFilter(filter);
            domFilter = domFilter.AND(DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id));

            return DomHelper.DomInstances.Count(domFilter);
        }

        internal long Count(FilterElement<DomInstance> domFilter)
        {
            if (domFilter is null)
            {
                throw new ArgumentNullException(nameof(domFilter));
            }

            domFilter = domFilter.AND(DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id));

            return DomHelper.DomInstances.Count(domFilter);
        }

        public virtual IEnumerable<T> ReadAll()
        {
            var filter = DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id);
            return DomHelper.DomInstances.Read(filter).Select(CreateInstance);
        }

        public virtual IEnumerable<IEnumerable<T>> ReadAllPaged()
        {
            var filter = DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id);
            return DomHelper.DomInstances.ReadPaged(filter).Select(x => x.Select(CreateInstance));
        }

        public virtual T Read(Guid id)
        {
            var filter = DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id)
                .AND(DomInstanceExposers.Id.Equal(id));

            var domInstance = DomHelper.DomInstances.Read(filter).SingleOrDefault();

            return domInstance != null ? CreateInstance(domInstance) : default;
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
                    x => DomHelper.DomInstances.Read(x))
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

        internal IEnumerable<T> Read(FilterElement<DomInstance> domFilter)
        {
            if (domFilter == null)
            {
                throw new ArgumentNullException(nameof(domFilter));
            }

            domFilter = domFilter.AND(DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id));

            var domInstances = DomHelper.DomInstances.Read(domFilter);

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

            var domInstances = DomHelper.DomInstances.Read(domQuery);

            return domInstances.Select(CreateInstance);
        }

        public virtual IOrderedQueryable<T> Query()
        {
            return new ApiRepositoryQuery<T>(_queryProvider);
        }

        protected internal virtual FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
        {
            switch (fieldName)
            {
                case nameof(IApiObject.ID):
                    return FilterElementFactory.Create(DomInstanceExposers.Id, comparer, (Guid)value);

                default:
                    throw new NotImplementedException();
            }
        }

        protected internal virtual IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
        {
            switch (fieldName)
            {
                case nameof(IApiObject.ID):
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