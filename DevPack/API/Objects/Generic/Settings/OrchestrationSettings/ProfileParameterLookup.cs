namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Resolves profile parameters by ID and caches them, so parsing orchestration settings and their events reads every
	/// profile parameter at most once instead of once per settings instance and once per orchestration event.
	/// </summary>
	/// <remarks>
	/// One instance is shared by the objects of a single read batch. Parameters that do not exist are remembered as well,
	/// so they are not read again for every settings instance that refers to them.
	/// </remarks>
	internal sealed class ProfileParameterLookup
	{
		private readonly object sync = new object();
		private readonly MediaOpsPlanApi planApi;
		private readonly Dictionary<Guid, Net.Profiles.Parameter> parametersById = new Dictionary<Guid, Net.Profiles.Parameter>();
		private readonly HashSet<Guid> missingIds = new HashSet<Guid>();

		internal ProfileParameterLookup(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		/// <summary>
		/// Returns the requested profile parameters, reading the ones that are not cached yet in a single call.
		/// </summary>
		/// <param name="ids">The identifiers of the profile parameters.</param>
		/// <returns>The profile parameters that exist, by ID.</returns>
		internal IReadOnlyDictionary<Guid, Net.Profiles.Parameter> GetParametersById(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			var requested = ids.Where(x => x != Guid.Empty).Distinct().ToList();
			var result = new Dictionary<Guid, Net.Profiles.Parameter>();
			if (requested.Count == 0)
			{
				return result;
			}

			lock (sync)
			{
				var toRead = requested.Where(x => !parametersById.ContainsKey(x) && !missingIds.Contains(x)).ToList();
				if (toRead.Count > 0)
				{
					foreach (var parameter in planApi.CoreHelpers.ProfileProvider.GetParametersById(toRead))
					{
						parametersById[parameter.ID] = parameter;
					}

					foreach (var id in toRead.Where(x => !parametersById.ContainsKey(x)))
					{
						missingIds.Add(id);
					}
				}

				foreach (var id in requested)
				{
					if (parametersById.TryGetValue(id, out var parameter))
					{
						result[id] = parameter;
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Reads the requested profile parameters that are not cached yet, so later lookups are served from the cache.
		/// </summary>
		/// <param name="ids">The identifiers of the profile parameters.</param>
		internal void Prefetch(IEnumerable<Guid> ids)
		{
			GetParametersById(ids);
		}

		/// <summary>
		/// Copies the specified profile parameters, as far as they are cached by another lookup, into this one. Parameters
		/// this lookup already resolved are left untouched, so lookups shared by several owners are filled only once.
		/// </summary>
		/// <param name="other">The lookup to copy from.</param>
		/// <param name="ids">The identifiers of the profile parameters to copy.</param>
		internal void SeedFrom(ProfileParameterLookup other, IEnumerable<Guid> ids)
		{
			if (other == null || ReferenceEquals(other, this) || ids == null)
			{
				return;
			}

			var requested = ids.Where(x => x != Guid.Empty).Distinct().ToList();
			if (requested.Count == 0)
			{
				return;
			}

			lock (sync)
			{
				requested.RemoveAll(x => parametersById.ContainsKey(x) || missingIds.Contains(x));
			}

			if (requested.Count == 0)
			{
				return;
			}

			var parameters = new List<Net.Profiles.Parameter>();
			var missing = new List<Guid>();
			lock (other.sync)
			{
				foreach (var id in requested)
				{
					if (other.parametersById.TryGetValue(id, out var parameter))
					{
						parameters.Add(parameter);
					}
					else if (other.missingIds.Contains(id))
					{
						missing.Add(id);
					}
				}
			}

			lock (sync)
			{
				foreach (var parameter in parameters)
				{
					parametersById[parameter.ID] = parameter;
					missingIds.Remove(parameter.ID);
				}

				foreach (var id in missing.Where(x => !parametersById.ContainsKey(x)))
				{
					missingIds.Add(id);
				}
			}
		}
	}
}
