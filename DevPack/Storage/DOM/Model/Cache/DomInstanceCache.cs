namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;

	internal class DomInstanceCache : ITemporaryCache<DomInstanceBase>
	{
		private readonly ConcurrentDictionary<Type, IReadOnlyList<DomInstanceBase>> cache = new();

		public void SetCache<T>(IEnumerable<T> objects)
		{
			var type = typeof(T);
			if (type == typeof(DomInstanceBase))
			{
				throw new InvalidOperationException("Cannot use DomInstanceBase directly. Use a derived type.");
			}

			if (objects == null)
			{
				throw new ArgumentNullException(nameof(objects));
			}

			var instanceList = objects.ToList();
			if (instanceList.Count == 0)
			{
				return;
			}

			if (instanceList.Any(x => x == null))
			{
				throw new ArgumentException("instances collection contains null values", nameof(objects));
			}

			cache[type] = instanceList.Cast<DomInstanceBase>().ToList().AsReadOnly();
		}

		public void AddToCache<T>(IEnumerable<T> objects)
		{
			var type = typeof(T);
			if (type == typeof(DomInstanceBase))
			{
				throw new InvalidOperationException("Cannot use DomInstanceBase directly. Use a derived type.");
			}

			if (objects == null)
			{
				throw new ArgumentNullException(nameof(objects));
			}

			var instanceList = objects.Cast<DomInstanceBase>().ToList();

			if (instanceList.Count == 0)
			{
				return;
			}

			if (instanceList.Any(x => x == null))
			{
				throw new ArgumentException("instances collection contains null values", nameof(objects));
			}

			cache.AddOrUpdate(
				type,
				addValueFactory: _ => instanceList.AsReadOnly(),
				updateValueFactory: (_, existing) =>
				{
					var result = new List<DomInstanceBase>(existing);
					foreach (var instance in instanceList)
					{
						var idx = result.FindIndex(o => o.ID.Id == instance.ID.Id);
						if (idx >= 0)
						{
							result[idx] = instance;
						}
						else
						{
							result.Add(instance);
						}
					}

					return result.AsReadOnly();
				});
		}

		public IEnumerable<T> GetFromCache<T>()
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
