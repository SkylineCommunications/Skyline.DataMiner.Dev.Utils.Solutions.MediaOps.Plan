namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Owner-scoped context that lazily reads the orchestration settings of an owning object (e.g. a job) and of all its
	/// nodes in a single backend call, the first time any of them is accessed.
	/// </summary>
	/// <typeparam name="TSettings">The type of the orchestration settings.</typeparam>
	/// <typeparam name="TInstance">The type of the stored configuration instance.</typeparam>
	/// <remarks>
	/// Every settings instance that is handed out is parsed from the stored configuration on request, so owners never
	/// share settings objects. The profile parameters of all loaded configurations are read in one call as well.
	/// </remarks>
	internal abstract class OrchestrationSettingsContext<TSettings, TInstance>
		where TSettings : OrchestrationSettings
		where TInstance : class
	{
		private readonly object sync = new object();
		private readonly HashSet<Guid> ids;

		private Dictionary<Guid, TInstance> instancesById;

		private protected OrchestrationSettingsContext(MediaOpsPlanApi planApi, ProfileParameterLookup parameters, IEnumerable<Guid?> ids)
		{
			PlanApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
			Parameters = parameters ?? new ProfileParameterLookup(planApi);
			this.ids = new HashSet<Guid>((ids ?? []).Where(x => x.HasValue && x.Value != Guid.Empty).Select(x => x.Value));
		}

		/// <summary>
		/// Gets a value indicating whether the stored configurations were already read.
		/// </summary>
		internal bool IsLoaded
		{
			get
			{
				lock (sync)
				{
					return instancesById != null;
				}
			}
		}

		private protected MediaOpsPlanApi PlanApi { get; }

		private protected ProfileParameterLookup Parameters { get; }

		/// <summary>
		/// Reads the orchestration settings of every specified context that was not loaded yet, using one call for the
		/// stored configurations and one call for their profile parameters.
		/// </summary>
		/// <param name="contexts">The contexts to load.</param>
		internal static void LoadAll(IEnumerable<OrchestrationSettingsContext<TSettings, TInstance>> contexts)
		{
			if (contexts == null)
			{
				throw new ArgumentNullException(nameof(contexts));
			}

			var pending = new List<OrchestrationSettingsContext<TSettings, TInstance>>();
			var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
			foreach (var context in contexts)
			{
				if (context != null && !context.IsLoaded && seen.Add(context))
				{
					pending.Add(context);
				}
			}

			if (pending.Count == 0)
			{
				return;
			}

			var reader = pending[0];
			var idsToRead = pending.SelectMany(x => x.GetIds()).Distinct().ToList();
			var read = idsToRead.Count > 0 ? reader.Read(idsToRead).Where(x => x != null).ToList() : new List<TInstance>();

			var parameters = new ProfileParameterLookup(reader.PlanApi);

			// Parameters the owners already resolved (for example through another owner of the same read) are not read again.
			var seenLookups = new HashSet<object>(ReferenceEqualityComparer.Instance);
			foreach (var context in pending.Where(x => seenLookups.Add(x.Parameters)))
			{
				parameters.SeedFrom(context.Parameters);
			}

			parameters.Prefetch(read.SelectMany(reader.CollectProfileParameterIds));

			var readById = new Dictionary<Guid, TInstance>();
			foreach (var instance in read)
			{
				readById[reader.GetId(instance)] = instance;
			}

			foreach (var context in pending)
			{
				context.Prime(readById, parameters);
			}
		}

		/// <summary>
		/// Creates a holder for the settings with the specified stored ID that reads them through this context.
		/// </summary>
		/// <param name="storedId">The ID of the stored settings.</param>
		/// <returns>The holder.</returns>
		internal OrchestrationSettingsHolder<TSettings> CreateHolder(Guid? storedId)
		{
			return OrchestrationSettingsHolder<TSettings>.Deferred(storedId, Resolve, CreateEmpty);
		}

		/// <summary>
		/// Returns newly parsed settings for the stored configuration with the specified ID, reading the configurations
		/// of the whole owner on first use.
		/// </summary>
		/// <param name="id">The ID of the stored configuration.</param>
		/// <returns>The settings, or <see langword="null"/> when the configuration does not exist.</returns>
		internal TSettings Resolve(Guid id)
		{
			TInstance instance;
			lock (sync)
			{
				if (instancesById == null)
				{
					// Only marked as loaded once the read succeeded, so a failed read is retried on the next access
					// instead of reporting stored settings as missing (which would make a save replace them).
					instancesById = ReadInstances(ids.ToList());
				}

				if (!instancesById.TryGetValue(id, out instance) && !ids.Contains(id))
				{
					// Not part of the owner when the context was created, so it is read on its own.
					var read = ReadInstances([id]);
					ids.Add(id);

					foreach (var entry in read)
					{
						instancesById[entry.Key] = entry.Value;
					}

					read.TryGetValue(id, out instance);
				}
			}

			return instance == null ? null : Create(instance, Parameters);
		}

		private protected abstract IEnumerable<TInstance> Read(ICollection<Guid> idsToRead);

		private protected abstract Guid GetId(TInstance instance);

		private protected abstract IEnumerable<Guid> CollectProfileParameterIds(TInstance instance);

		private protected abstract TSettings Create(TInstance instance, ProfileParameterLookup parameters);

		private protected abstract TSettings CreateEmpty();

		private List<Guid> GetIds()
		{
			lock (sync)
			{
				return ids.ToList();
			}
		}

		private void Prime(IReadOnlyDictionary<Guid, TInstance> readById, ProfileParameterLookup parameters)
		{
			lock (sync)
			{
				if (instancesById != null)
				{
					return;
				}

				instancesById = new Dictionary<Guid, TInstance>();
				foreach (var id in ids)
				{
					if (readById.TryGetValue(id, out var instance))
					{
						instancesById[id] = instance;
					}
				}

				Parameters.SeedFrom(parameters);
			}
		}

		private Dictionary<Guid, TInstance> ReadInstances(ICollection<Guid> idsToRead)
		{
			var result = new Dictionary<Guid, TInstance>();
			if (idsToRead.Count == 0)
			{
				return result;
			}

			var read = Read(idsToRead).Where(x => x != null).ToList();
			Parameters.Prefetch(read.SelectMany(CollectProfileParameterIds));

			foreach (var instance in read)
			{
				result[GetId(instance)] = instance;
			}

			return result;
		}

		private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
		{
			internal static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

			public new bool Equals(object x, object y) => ReferenceEquals(x, y);

			public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
		}
	}
}
