namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Holds the parsed orchestration settings of a job and its nodes during a create-or-update operation, keyed by the
	/// orchestration settings identifier, so the typed capabilities and capacities are available later in the pipeline
	/// (for example when building the core resource usages) without parsing the configuration again.
	/// </summary>
	internal sealed class OrchestrationSettingsCache
	{
		private IReadOnlyDictionary<Guid, OrchestrationSettings> orchestrationSettings = new Dictionary<Guid, OrchestrationSettings>();

		/// <summary>
		/// Gets the orchestration settings keyed by their identifier.
		/// </summary>
		public IReadOnlyDictionary<Guid, OrchestrationSettings> Settings => orchestrationSettings;

		/// <summary>
		/// Replaces the cached orchestration settings with the specified collection.
		/// </summary>
		/// <param name="settings">The orchestration settings keyed by their identifier.</param>
		public void SetCache(IReadOnlyDictionary<Guid, OrchestrationSettings> settings)
		{
			orchestrationSettings = settings ?? throw new ArgumentNullException(nameof(settings));
		}

		/// <summary>
		/// Tries to get the orchestration settings for the specified identifier.
		/// </summary>
		/// <param name="id">The orchestration settings identifier to look up.</param>
		/// <param name="value">When this method returns, contains the orchestration settings if found; otherwise <see langword="null"/>.</param>
		/// <returns><see langword="true"/> if orchestration settings were found; otherwise <see langword="false"/>.</returns>
		public bool TryGetValue(Guid id, out OrchestrationSettings value)
		{
			return orchestrationSettings.TryGetValue(id, out value);
		}
	}
}
