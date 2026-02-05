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

    using static Skyline.DataMiner.Net.Profiles.Parameter;

    /// <summary>
    /// Provides methods to manage profiles, including retrieving parameters by ID or name, filtering based on categories, 
    /// and managing capabilities and capacities.
    /// </summary>
    internal class ProfileProvider
    {
        /// <summary>
        /// A helper to facilitate profile-related operations.
        /// </summary>
        private readonly ProfileHelper profileHelper;

        private static readonly FilterElement<Net.Profiles.Parameter> AllTimeDependentCapabilitiesFilter =
            ParameterExposers.Categories.Contains((int)ProfileParameterCategory.Capability)
            .AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Capacity))
            .AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Configuration))
            .AND(ParameterExposers.Name.Matches(".*- Time dependent$"));

        internal static readonly FilterElement<Net.Profiles.Parameter> AllCapabilitiesFilter =
            ParameterExposers.Categories.Contains((int)ProfileParameterCategory.Capability)
            .AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Capacity))
            .AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Configuration))
            .AND(ParameterExposers.Type.Equal((int)ParameterType.Discrete))
            .AND(ParameterExposers.Name.NotMatches(".*- Time dependent$")); // Don't include linked Time dependent capabilities.

        internal static readonly FilterElement<Net.Profiles.Parameter> AllCapacitiesFilter =
            ParameterExposers.Categories.Contains((int)ProfileParameterCategory.Capacity)
            .AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Capability))
            .AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Configuration));

        internal static readonly FilterElement<Net.Profiles.Parameter> AllConfigurationsFilter =
            ParameterExposers.Categories.Contains((int)ProfileParameterCategory.Configuration)
            .AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Capability))
            .AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Capacity));

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

        public IEnumerable<Net.Profiles.Parameter> GetParameters(FilterElement<Net.Profiles.Parameter> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return profileHelper.ProfileParameters.Read(filter);
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

        /// <summary>
        /// Retrieves all capacity parameters.
        /// </summary>
        /// <returns>A collection of capacity parameters.</returns>
        public IEnumerable<IEnumerable<Net.Profiles.Parameter>> GetCapacitiesPaged(FilterElement<Net.Profiles.Parameter> filter, long pageSize = 500)
        {
            return profileHelper.ProfileParameters.ReadPaged(AllCapacitiesFilter.AND(filter), pageSize);
        }

        public long CountCapacities(FilterElement<Net.Profiles.Parameter> filter)
        {
            return profileHelper.ProfileParameters.Count(AllCapacitiesFilter.AND(filter));
        }

        /// <summary>
        /// Retrieves all configuration parameters.
        /// </summary>
        /// <returns>A collection of configuration parameters.</returns>
        public IEnumerable<IEnumerable<Net.Profiles.Parameter>> GetConfigurationsPaged(FilterElement<Net.Profiles.Parameter> filter, long pageSize = 500)
        {
            return profileHelper.ProfileParameters.ReadPaged(AllConfigurationsFilter.AND(filter), pageSize);
        }

        public long CountConfigurations(FilterElement<Net.Profiles.Parameter> filter)
        {
            return profileHelper.ProfileParameters.Count(AllConfigurationsFilter.AND(filter));
        }

        public IReadOnlyCollection<Net.Profiles.Parameter> GetTimeDependentCapabilities(FilterElement<Net.Profiles.Parameter> filter)
        {
            return profileHelper.ProfileParameters.Read(AllTimeDependentCapabilitiesFilter.AND(filter));
        }

        public long CountNonTimeDependentCapabilities()
        {
            return profileHelper.ProfileParameters.Count(AllCapabilitiesFilter);
        }

        public IEnumerable<IEnumerable<Net.Profiles.Parameter>> GetCapabilitiesPaged(FilterElement<Net.Profiles.Parameter> filter, long pageSize = 500)
        {
            return profileHelper.ProfileParameters.ReadPaged(AllCapabilitiesFilter.AND(filter), pageSize);
        }

        public IEnumerable<Net.Profiles.Parameter> GetCapabilities(FilterElement<Net.Profiles.Parameter> filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            return profileHelper.ProfileParameters.Read(AllCapabilitiesFilter.AND(filter));
        }

        public IEnumerable<Net.Profiles.Parameter> GetCapacities(FilterElement<Net.Profiles.Parameter> filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            return profileHelper.ProfileParameters.Read(AllCapacitiesFilter.AND(filter));
        }

        public IEnumerable<Net.Profiles.Parameter> GetConfigurations(FilterElement<Net.Profiles.Parameter> filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            return profileHelper.ProfileParameters.Read(AllConfigurationsFilter.AND(filter));
        }

        public long CountCapabilities(FilterElement<Net.Profiles.Parameter> filter)
        {
            return profileHelper.ProfileParameters.Count(AllCapabilitiesFilter.AND(filter));
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
    }
}