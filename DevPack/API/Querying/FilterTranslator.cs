namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal abstract class FilterTranslator<T, K> where T : ApiObject
    {
        protected FilterTranslator()
        {
        }

        protected abstract Dictionary<string, Func<Comparer, object, FilterElement<K>>> Handlers { get; }

        /// <summary>
        /// Translates a filter element of type <typeparamref name="T"/> into a filter element for <see cref="DomInstance"/>.
        /// </summary>
        /// <param name="filter">The filter element to translate.</param>
        /// <returns>A <see cref="FilterElement{DomInstance}"/> representing the translated filter.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filter"/> is null.</exception>
        /// <exception cref="NotSupportedException">Thrown when the filter type is not supported.</exception>
        public virtual FilterElement<K> Translate(FilterElement<T> filter)
        {
            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            FilterElement<K> translated;
            if (filter is ANDFilterElement<T> and)
            {
                translated = new ANDFilterElement<K>(and.subFilters.Select(Translate).ToArray());
            }
            else if (filter is ORFilterElement<T> or)
            {
                translated = new ORFilterElement<K>(or.subFilters.Select(Translate).ToArray());
            }
            else if (filter is NOTFilterElement<T> not)
            {
                translated = new NOTFilterElement<K>(Translate(not));
            }
            else if (filter is TRUEFilterElement<T>)
            {
                translated = new TRUEFilterElement<K>();
            }
            else if (filter is FALSEFilterElement<T>)
            {
                translated = new FALSEFilterElement<K>();
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

        private FilterElement<K> TranslateFilter(ManagedFilterIdentifier managedFilter)
        {
            if (managedFilter is null)
            {
                throw new ArgumentNullException(nameof(managedFilter));
            }

            var fieldName = managedFilter.getFieldName().fieldName;
            var comparer = managedFilter.getComparer();
            var value = managedFilter.getValue();
            var translated = CreateFilter(fieldName, comparer, value);
            return translated;
        }

        private FilterElement<K> CreateFilter(string fieldName, Comparer comparer, object value)
        {
            if (!Handlers.ContainsKey(fieldName))
            {
                throw new NotSupportedException(fieldName);
            }

            return Handlers[fieldName].Invoke(comparer, value);
        }
    }
}
