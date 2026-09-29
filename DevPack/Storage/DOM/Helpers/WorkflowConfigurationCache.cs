namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Caches the workflow configuration instances that are read while a scope is active.
	/// </summary>
	/// <remarks>
	/// Orchestration settings are stored as their own DOM instances, so parsing a job reads one configuration per node
	/// on top of the job's own configuration. A scope turns that N+1 pattern into a single batched read, and lets a
	/// caller make configuration instances that are not persisted yet visible to the parse path.
	/// <para>
	/// The cache is deliberately scoped rather than ambient: outside a scope every read goes to storage, so a caller
	/// can never observe a stale configuration.
	/// </para>
	/// </remarks>
	internal sealed class WorkflowConfigurationCache
	{
		private readonly Dictionary<Guid, ConfigurationInstance> configurationsById = new Dictionary<Guid, ConfigurationInstance>();

		// Identifiers that were looked up and do not exist. Remembered so a missing configuration is not re-read for
		// every node that points at it.
		private readonly HashSet<Guid> missingIds = new HashSet<Guid>();

		private int depth;

		internal bool IsActive => depth > 0;

		/// <summary>
		/// Opens a caching scope. Nested scopes share the same cache and only the outermost scope clears it.
		/// </summary>
		/// <returns>A handle that closes the scope when disposed.</returns>
		internal IDisposable BeginScope()
		{
			depth++;
			return new Scope(this);
		}

		/// <summary>
		/// Adds configuration instances to the cache, overruling whatever is stored for them.
		/// </summary>
		/// <param name="configurations">The configuration instances to cache.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="configurations"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when no scope is active.</exception>
		internal void Seed(IEnumerable<ConfigurationInstance> configurations)
		{
			if (configurations == null)
			{
				throw new ArgumentNullException(nameof(configurations));
			}

			if (!IsActive)
			{
				throw new InvalidOperationException("Configurations can only be seeded while a scope is active.");
			}

			foreach (var configuration in configurations.Where(x => x?.ID != null && x.ID.Id != Guid.Empty))
			{
				configurationsById[configuration.ID.Id] = configuration;
				missingIds.Remove(configuration.ID.Id);
			}
		}

		/// <summary>
		/// Returns the cached configurations for the requested identifiers and reports the ones that still have to be read.
		/// </summary>
		/// <param name="ids">The identifiers to resolve.</param>
		/// <param name="missing">The identifiers that are not cached yet.</param>
		/// <returns>The configurations that are already cached.</returns>
		internal ICollection<ConfigurationInstance> Resolve(IEnumerable<Guid> ids, out ICollection<Guid> missing)
		{
			var resolved = new List<ConfigurationInstance>();
			var toRead = new HashSet<Guid>();

			foreach (var id in ids.Distinct())
			{
				if (configurationsById.TryGetValue(id, out var configuration))
				{
					resolved.Add(configuration);
				}
				else if (!missingIds.Contains(id))
				{
					toRead.Add(id);
				}
			}

			missing = toRead;
			return resolved;
		}

		/// <summary>
		/// Stores the result of a read so the same identifiers are not read again within the scope.
		/// </summary>
		/// <param name="requestedIds">The identifiers that were requested.</param>
		/// <param name="configurations">The configurations that were returned.</param>
		internal void Store(ICollection<Guid> requestedIds, ICollection<ConfigurationInstance> configurations)
		{
			foreach (var configuration in configurations.Where(x => x?.ID != null && x.ID.Id != Guid.Empty))
			{
				configurationsById[configuration.ID.Id] = configuration;
			}

			foreach (var id in requestedIds.Where(x => !configurationsById.ContainsKey(x)))
			{
				missingIds.Add(id);
			}
		}

		private void EndScope()
		{
			if (depth == 0)
			{
				return;
			}

			depth--;

			if (depth == 0)
			{
				configurationsById.Clear();
				missingIds.Clear();
			}
		}

		private sealed class Scope : IDisposable
		{
			private WorkflowConfigurationCache cache;

			internal Scope(WorkflowConfigurationCache cache)
			{
				this.cache = cache;
			}

			public void Dispose()
			{
				// Guard against a double dispose closing a scope that is not ours.
				cache?.EndScope();
				cache = null;
			}
		}
	}
}
