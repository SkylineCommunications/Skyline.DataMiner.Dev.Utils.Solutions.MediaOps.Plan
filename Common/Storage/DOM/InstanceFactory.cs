namespace Skyline.DataMiner.MediaOps.API.Common.Storage.DOM
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.MediaOps.Common.DOM;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using SLDataGateway.API.Types.Querying;

    internal class InstanceFactory
    {
        public static IEnumerable<T> CreateInstances<T>(IEnumerable<DomInstance> domInstances, Func<DomInstance, T> createInstance)
            where T : DomHelpers.DomInstanceBase
        {
            if (domInstances == null)
            {
                throw new ArgumentNullException(nameof(domInstances));
            }

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

        public static IEnumerable<T> ReadAndCreateInstances<T>(DomHelper domHelper, FilterElement<DomInstance> filter, Func<DomInstance, T> createInstance)
            where T : DomHelpers.DomInstanceBase
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

            var pages = domHelper.DomInstances.ReadPaged(filter);
            var instances = pages.SelectMany(page => page);

            return CreateInstances(instances, createInstance);
        }

        public static IEnumerable<T> ReadAndCreateInstances<T>(DomHelper domHelper, IQuery<DomInstance> query, Func<DomInstance, T> createInstance)
            where T : DomHelpers.DomInstanceBase
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

            var pages = domHelper.DomInstances.ReadPaged(query);
            var instances = pages.SelectMany(page => page);

            return CreateInstances(instances, createInstance);
        }
    }
}
