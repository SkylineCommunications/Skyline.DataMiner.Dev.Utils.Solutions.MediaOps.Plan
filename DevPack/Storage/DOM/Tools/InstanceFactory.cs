namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Utils.DOM.Extensions;

    using SLDataGateway.API.Types.Querying;

    internal static class InstanceFactory
    {
        public static T CreateInstance<T>(DomInstance domInstance, Func<DomInstance, T> createInstance)
            where T : DomInstanceBase
        {
            if (domInstance == null)
            {
                throw new ArgumentNullException(nameof(domInstance));
            }

            if (createInstance == null)
            {
                throw new ArgumentNullException(nameof(createInstance));
            }

            return createInstance(domInstance);
        }

        public static IEnumerable<T> CreateInstances<T>(IEnumerable<DomInstance> domInstances, Func<DomInstance, T> createInstance)
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

        private static IEnumerable<IEnumerable<T>> CreateInstancesPagedIterator<T>(IEnumerable<IEnumerable<DomInstance>> pages, Func<DomInstance, T> createInstance) where T : DomInstanceBase
        {
            foreach (var page in pages)
            {
                yield return CreateInstancesIterator(page, createInstance);
            }
        }

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

            var pages = domHelper.DomInstances.ReadPaged(query);
            var instances = pages.SelectMany(page => page);

            return CreateInstances(instances, createInstance);
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

                var instance = createInstance(domInstance);

                yield return instance;
            }
        }
    }
}
