namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    internal partial class ResourceInstance : IDomTemporaryCache
    {
        private readonly ConcurrentDictionary<Type, List<DomInstanceBase>> cache = new();

        protected override void BeforeToInstance()
        {
            ResourceInternalProperties.ApplyChanges();
        }

        public bool ClearError(string errorCode)
        {
            if (String.IsNullOrWhiteSpace(errorCode))
            {
                throw new ArgumentException("error code can't be empty", nameof(errorCode));
            }

            var errorsToRemove = Errors.Where(x => string.Equals(x.ErrorCode, errorCode)).ToList();
            if (!errorsToRemove.Any()) return false;

            foreach (var errorToRemove in errorsToRemove) Errors.Remove(errorToRemove);
            return true;
        }

        public void SetCache<T>(IEnumerable<T> instances) where T : DomInstanceBase
        {
            var type = typeof(T);
            if (type == typeof(DomInstanceBase))
            {
                throw new InvalidOperationException("Cannot use DomInstanceBase directly. Use a derived type.");
            }

            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            if (!instances.Any())
            {
                return;
            }

            if (instances.Any(x => x == null))
            {
                throw new ArgumentException("instances collection contains null values", nameof(instances));
            }

            if (!cache.TryGetValue(type, out var cachedInstances))
            {
                cachedInstances = new List<DomInstanceBase>();
                cache.TryAdd(type, cachedInstances);
            }

            cachedInstances.Clear();
            cachedInstances.AddRange(instances);
        }

        public void AddToCache<T>(IEnumerable<T> instances) where T : DomInstanceBase
        {
            var type = typeof(T);
            if (type == typeof(DomInstanceBase))
            {
                throw new InvalidOperationException("Cannot use DomInstanceBase directly. Use a derived type.");
            }

            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            if (!instances.Any())
            {
                return;
            }

            if (instances.Any(x => x == null))
            {
                throw new ArgumentException("instances collection contains null values", nameof(instances));
            }

            if (!cache.TryGetValue(type, out var cachedInstances))
            {
                cachedInstances = new List<DomInstanceBase>(instances);
                cache.TryAdd(type, cachedInstances);

                return;
            }

            var cachedInstancesById = cachedInstances.ToDictionary(x => x.ID.Id);
            foreach (var instance in instances)
            {
                if (cachedInstancesById.ContainsKey(instance.ID.Id))
                {
                    cachedInstances.Remove(cachedInstancesById[instance.ID.Id]);
                }

                cachedInstances.Add(instance);
            }
        }

        public IEnumerable<T> GetFromCache<T>() where T : DomInstanceBase
        {
            var type = typeof(T);
            if (type == typeof(DomInstanceBase))
            {
                throw new InvalidOperationException("Cannot use DomInstanceBase directly. Use a derived type.");
            }

            if (!cache.TryGetValue(typeof(T), out var cachedInstances))
            {
                return Enumerable.Empty<T>();
            }

            return cachedInstances.OfType<T>();
        }
    }
}
