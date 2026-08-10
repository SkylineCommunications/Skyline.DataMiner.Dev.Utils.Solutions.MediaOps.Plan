namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using SLDataGateway.API.Types.Querying;

	using static Skyline.DataMiner.Net.Profiles.Parameter;

	/// <summary>
	/// Provides methods to manage profiles, including retrieving parameters by ID or name, filtering based on categories,
	/// and managing capabilities and capacities.
	/// </summary>
	internal class ProfileProvider
	{
		internal static readonly FilterElement<Net.Profiles.Parameter> AllCapabilitiesFilter =
					ParameterExposers.Categories.Contains((int)ProfileParameterCategory.Capability)
					.AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Capacity))
					.AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Configuration))
					.AND(ParameterExposers.Type.Equal((int)ParameterType.Discrete))
					.AND(ParameterExposers.Name.NotMatches(".*- Time dependent$"));

		internal static readonly FilterElement<Net.Profiles.Parameter> AllCapacitiesFilter =
					ParameterExposers.Categories.Contains((int)ProfileParameterCategory.Capacity)
					.AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Capability))
					.AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Configuration));

		// Don't include linked Time dependent capabilities.
		internal static readonly FilterElement<Net.Profiles.Parameter> AllConfigurationsFilter =
			ParameterExposers.Categories.Contains((int)ProfileParameterCategory.Configuration)
			.AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Capability))
			.AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Capacity));

		private static readonly FilterElement<Net.Profiles.Parameter> AllTimeDependentCapabilitiesFilter =
					ParameterExposers.Categories.Contains((int)ProfileParameterCategory.Capability)
					.AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Capacity))
					.AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Configuration))
					.AND(ParameterExposers.Name.Matches(".*- Time dependent$"));

		/// <summary>
		/// A helper to facilitate profile-related operations.
		/// </summary>
		private readonly ProfileHelper profileHelper;
		/// <summary>
		/// Initializes a new instance of the <see cref="ProfileProvider"/> class using the specified connection.
		/// </summary>
		/// <param name="connection">The connection used to handle messages for profile operations. Cannot be null.</param>
		public ProfileProvider(IConnection connection)
		{
			if (connection == null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			this.profileHelper = new ProfileHelper(connection.HandleMessages);
		}

		public long CountCapabilities(FilterElement<Net.Profiles.Parameter> filter)
		{
			return profileHelper.ProfileParameters.Count(AllCapabilitiesFilter.AND(filter));
		}

		public long CountCapabilities(IQuery<Net.Profiles.Parameter> query)
		{
			return profileHelper.ProfileParameters.Count(ApplyParameterTypeFilter(query, AllCapabilitiesFilter));
		}

		public long CountCapacities(FilterElement<Net.Profiles.Parameter> filter)
		{
			return profileHelper.ProfileParameters.Count(AllCapacitiesFilter.AND(filter));
		}

		public long CountCapacities(IQuery<Net.Profiles.Parameter> query)
		{
			return profileHelper.ProfileParameters.Count(ApplyParameterTypeFilter(query, AllCapacitiesFilter));
		}

		public long CountConfigurations(FilterElement<Net.Profiles.Parameter> filter)
		{
			return profileHelper.ProfileParameters.Count(AllConfigurationsFilter.AND(filter));
		}

		public long CountConfigurations(IQuery<Net.Profiles.Parameter> query)
		{
			return profileHelper.ProfileParameters.Count(ApplyParameterTypeFilter(query, AllConfigurationsFilter));
		}

		public long CountNonTimeDependentCapabilities()
		{
			return profileHelper.ProfileParameters.Count(AllCapabilitiesFilter);
		}

		public IEnumerable<Net.Profiles.Parameter> GetCapabilities(FilterElement<Net.Profiles.Parameter> filter)
		{
			if (filter == null)
				throw new ArgumentNullException(nameof(filter));

			return profileHelper.ProfileParameters.Read(AllCapabilitiesFilter.AND(filter));
		}

		public IEnumerable<Net.Profiles.Parameter> GetCapabilities(IQuery<Net.Profiles.Parameter> query)
		{
			return profileHelper.ProfileParameters.Read(ApplyParameterTypeFilter(query, AllCapabilitiesFilter));
		}

		public IEnumerable<IEnumerable<Net.Profiles.Parameter>> GetCapabilitiesPaged(FilterElement<Net.Profiles.Parameter> filter, long pageSize = 500)
		{
			return profileHelper.ProfileParameters.ReadPaged(AllCapabilitiesFilter.AND(filter), pageSize);
		}

		public IEnumerable<IEnumerable<Net.Profiles.Parameter>> GetCapabilitiesPaged(IQuery<Net.Profiles.Parameter> query, long pageSize = 500)
		{
			return profileHelper.ProfileParameters.ReadPaged(ApplyParameterTypeFilter(query, AllCapabilitiesFilter), pageSize);
		}

		public IEnumerable<Net.Profiles.Parameter> GetCapacities(FilterElement<Net.Profiles.Parameter> filter)
		{
			if (filter == null)
				throw new ArgumentNullException(nameof(filter));

			return profileHelper.ProfileParameters.Read(AllCapacitiesFilter.AND(filter));
		}

		public IEnumerable<Net.Profiles.Parameter> GetCapacities(IQuery<Net.Profiles.Parameter> query)
		{
			return profileHelper.ProfileParameters.Read(ApplyParameterTypeFilter(query, AllCapacitiesFilter));
		}

		/// <summary>
		/// Retrieves capacity parameters in pages.
		/// </summary>
		/// <param name="filter">The filter to apply when retrieving capacity parameters.</param>
		/// <param name="pageSize">The number of items per page. Default is 500.</param>
		/// <returns>A collection of pages, where each page contains a collection of capacity parameters.</returns>
		public IEnumerable<IEnumerable<Net.Profiles.Parameter>> GetCapacitiesPaged(FilterElement<Net.Profiles.Parameter> filter, long pageSize = 500)
		{
			return profileHelper.ProfileParameters.ReadPaged(AllCapacitiesFilter.AND(filter), pageSize);
		}

		public IEnumerable<IEnumerable<Net.Profiles.Parameter>> GetCapacitiesPaged(IQuery<Net.Profiles.Parameter> query, long pageSize = 500)
		{
			return profileHelper.ProfileParameters.ReadPaged(ApplyParameterTypeFilter(query, AllCapacitiesFilter), pageSize);
		}

		public IEnumerable<Net.Profiles.Parameter> GetConfigurations(FilterElement<Net.Profiles.Parameter> filter)
		{
			if (filter == null)
				throw new ArgumentNullException(nameof(filter));

			return profileHelper.ProfileParameters.Read(AllConfigurationsFilter.AND(filter));
		}

		public IEnumerable<Net.Profiles.Parameter> GetConfigurations(IQuery<Net.Profiles.Parameter> query)
		{
			return profileHelper.ProfileParameters.Read(ApplyParameterTypeFilter(query, AllConfigurationsFilter));
		}

		/// <summary>
		/// Retrieves all configuration parameters.
		/// </summary>
		/// <param name="filter">The filter to apply when retrieving configuration parameters.</param>
		/// <param name="pageSize">The number of items per page. Default is 500.</param>
		/// <returns>A collection of pages, where each page contains a collection of configuration parameters.</returns>
		public IEnumerable<IEnumerable<Net.Profiles.Parameter>> GetConfigurationsPaged(FilterElement<Net.Profiles.Parameter> filter, long pageSize = 500)
		{
			return profileHelper.ProfileParameters.ReadPaged(AllConfigurationsFilter.AND(filter), pageSize);
		}

		public IEnumerable<IEnumerable<Net.Profiles.Parameter>> GetConfigurationsPaged(IQuery<Net.Profiles.Parameter> query, long pageSize = 500)
		{
			return profileHelper.ProfileParameters.ReadPaged(ApplyParameterTypeFilter(query, AllConfigurationsFilter), pageSize);
		}

		public IEnumerable<Net.Profiles.Parameter> GetParameters(FilterElement<Net.Profiles.Parameter> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return profileHelper.ProfileParameters.Read(filter);
		}

		/// <summary>
		/// Retrieves multiple parameters by their IDs.
		/// </summary>
		/// <param name="ids">The collection of parameter IDs.</param>
		/// <returns>A dictionary mapping each ID to its associated parameter.</returns>
		public IEnumerable<Net.Profiles.Parameter> GetParametersById(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Array.Empty<Net.Profiles.Parameter>();
			}

			var filter = new ORFilterElement<Net.Profiles.Parameter>(ids.Select(id => ParameterExposers.ID.Equal(id)).ToArray());
			return GetParameters(filter);
		}

		/// <summary>
		/// Retrieves multiple parameters by their names.
		/// </summary>
		/// <param name="names">The collection of parameter names.</param>
		/// <returns>A dictionary mapping each name to its associated parameter.</returns>
		public IEnumerable<Net.Profiles.Parameter> GetParametersByName(IEnumerable<string> names)
		{
			if (names == null)
			{
				throw new ArgumentNullException(nameof(names));
			}

			if (!names.Any())
			{
				return Array.Empty<Net.Profiles.Parameter>();
			}

			var filter = new ORFilterElement<Net.Profiles.Parameter>(names.Select(name => ParameterExposers.Name.Equal(name)).ToArray());
			return GetParameters(filter);
		}
		public IReadOnlyCollection<Net.Profiles.Parameter> GetTimeDependentCapabilities(FilterElement<Net.Profiles.Parameter> filter)
		{
			return profileHelper.ProfileParameters.Read(AllTimeDependentCapabilitiesFilter.AND(filter));
		}

		/// <summary>
		/// Attempts to create or update the specified parameters in batches.
		/// </summary>
		/// <remarks>The method processes the parameters in batches of 100 to optimize performance. If any
		/// parameters fail to be created or updated, their IDs and associated error details are included in the
		/// <paramref name="result"/>.</remarks>
		/// <param name="parameters">A collection of <see cref="Net.Profiles.Parameter"/> objects to be created or updated.
		/// Cannot be <see langword="null"/>.</param>
		/// <param name="result">When the method returns, contains a <see cref="BulkOperationResult{T}"/> object that provides details
		/// about the operation, including the IDs of successfully processed parameters, IDs of failed parameters, and
		/// any associated error trace data.</param>
		/// <returns><see langword="true"/> if all parameters were successfully created or updated; otherwise, <see
		/// langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="parameters"/> is <see langword="null"/>.</exception>
		public bool TryCreateOrUpdateParametersInBatches(IEnumerable<Net.Profiles.Parameter> parameters, out ParameterBulkOperationResult result)
		{
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			var successfulItems = new List<Net.Profiles.Parameter>();
			var unsuccessfulIds = new List<Guid>();
			var traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();

			foreach (var batch in parameters.Batch(100))
			{
				var succeededParameters = profileHelper.ProfileParameters.AddOrUpdateBulk(batch.ToArray());

				successfulItems.AddRange(succeededParameters);

				var traceData = profileHelper.ProfileParameters.GetTraceDataLastCall();
				foreach (var error in traceData.ErrorData.OfType<ProfileManagerErrorData>())
				{
					if (Guid.Equals(error.ProfileParameterID, Guid.Empty))
					{
						continue;
					}

					if (!traceDataPerItem.TryGetValue(error.ProfileParameterID, out var mediaOpsTraceData))
					{
						mediaOpsTraceData = new MediaOpsTraceData();
						traceDataPerItem.Add(error.ProfileParameterID, mediaOpsTraceData);

						unsuccessfulIds.Add(error.ProfileParameterID);
					}

					mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = error.ToString() });
				}
			}

			result = new ParameterBulkOperationResult(successfulItems, unsuccessfulIds, traceDataPerItem);
			return !result.HasFailures;
		}
		public bool TryDeleteParametersInBatches(IEnumerable<Net.Profiles.Parameter> parameters, out ParameterBulkOperationResult result)
		{
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			var successfulItems = new List<Net.Profiles.Parameter>();
			var unsuccessfulIds = new List<Guid>();
			var traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();

			foreach (var batch in parameters.Batch(100))
			{
				var succeededParameters = profileHelper.ProfileParameters.RemoveBulk(batch.ToArray());

				successfulItems.AddRange(succeededParameters);

				var traceData = profileHelper.ProfileParameters.GetTraceDataLastCall();
				foreach (var error in traceData.ErrorData.OfType<ProfileManagerErrorData>())
				{
					if (Guid.Equals(error.ProfileParameterID, Guid.Empty))
					{
						continue;
					}

					if (!traceDataPerItem.TryGetValue(error.ProfileParameterID, out var mediaOpsTraceData))
					{
						mediaOpsTraceData = new MediaOpsTraceData();
						traceDataPerItem.Add(error.ProfileParameterID, mediaOpsTraceData);

						unsuccessfulIds.Add(error.ProfileParameterID);
					}

					mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = error.ToString() });
				}
			}

			result = new ParameterBulkOperationResult(successfulItems, unsuccessfulIds, traceDataPerItem);
			return !result.HasFailures;
		}

		private static IQuery<Net.Profiles.Parameter> ApplyParameterTypeFilter(IQuery<Net.Profiles.Parameter> query, FilterElement<Net.Profiles.Parameter> parameterTypeFilter)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			return query.WithFilter(parameterTypeFilter.AND(query.Filter));
		}
	}
}