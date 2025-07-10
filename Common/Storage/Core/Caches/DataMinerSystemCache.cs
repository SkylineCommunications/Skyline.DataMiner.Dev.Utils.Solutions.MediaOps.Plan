namespace Skyline.DataMiner.MediaOps.Plan.Storage.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Core.DataMinerSystem.Common;

    internal class DataMinerSystemCache
    {
        private readonly IDms dms;

        private readonly Dictionary<DmsElementId, IDmsElement> elementsById = [];

        private readonly Dictionary<DmsServiceId, IDmsService> servicesById = [];

        public DataMinerSystemCache(IDms dms)
        {
            this.dms = dms ?? throw new ArgumentNullException(nameof(dms));
        }

        public IReadOnlyDictionary<DmsElementId, IDmsElement> GetElements(IEnumerable<DmsElementId> elementIds, bool forceGet = false)
        {
            if (elementIds == null)
            {
                throw new ArgumentNullException(nameof(elementIds));
            }

            var result = new Dictionary<DmsElementId, IDmsElement>();
            var idsToRetrieve = new List<DmsElementId>();

            if (forceGet)
            {
                idsToRetrieve.AddRange(elementIds.Distinct());
            }
            else
            {
                foreach (var elementId in elementIds.Distinct())
                {
                    if (elementsById.TryGetValue(elementId, out var element))
                    {
                        result[elementId] = element;
                    }
                    else
                    {
                        idsToRetrieve.Add(elementId);
                    }
                }
            }

            if (idsToRetrieve.Count > 0)
            {
                foreach (var elementId in idsToRetrieve)
                {
                    if (!dms.ElementExists(elementId))
                    {
                        continue;
                    }

                    var element = dms.GetElement(elementId);
                    result[element.DmsElementId] = element;
                    elementsById[element.DmsElementId] = element;
                }
            }

            return result;
        }

        public IReadOnlyDictionary<DmsServiceId, IDmsService> GetServices(IEnumerable<DmsServiceId> serviceIds, bool forceGet = false)
        {
            if (serviceIds == null)
            {
                throw new ArgumentNullException(nameof(serviceIds));
            }

            var result = new Dictionary<DmsServiceId, IDmsService>();
            var idsToRetrieve = new List<DmsServiceId>();

            if (forceGet)
            {
                idsToRetrieve.AddRange(serviceIds.Distinct());
            }
            else
            {
                foreach (var serviceId in serviceIds.Distinct())
                {
                    if (servicesById.TryGetValue(serviceId, out var service))
                    {
                        result[serviceId] = service;
                    }
                    else
                    {
                        idsToRetrieve.Add(serviceId);
                    }
                }
            }

            if (idsToRetrieve.Count > 0)
            {
                foreach (var serviceId in idsToRetrieve)
                {
                    if (!dms.ServiceExists(serviceId))
                    {
                        continue;
                    }

                    var service = dms.GetService(serviceId);
                    result[service.DmsServiceId] = service;
                    servicesById[service.DmsServiceId] = service;
                }
            }

            return result;
        }
    }
}
