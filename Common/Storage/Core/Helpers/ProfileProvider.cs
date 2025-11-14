namespace Skyline.DataMiner.MediaOps.Plan.Storage.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Helper;
    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.Profiles;
    using Skyline.DataMiner.Utils.DOM.Extensions;

    using SLDataGateway.API.Types.Querying;

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

        private readonly FilterElement<Net.Profiles.Parameter> AllCapabilitiesFilter =
            ParameterExposers.Categories.Contains((int)ProfileParameterCategory.Capability)
            .AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Capacity))
            .AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Configuration))
            .AND(ParameterExposers.Type.Equal((int)ParameterType.Discrete))
            .AND(ParameterExposers.Name.NotMatches(".*- Time dependent$")); // Don't include linked Time dependent capabilities.

        private readonly FilterElement<Net.Profiles.Parameter> AllCapacitiesFilter =
            ParameterExposers.Categories.Contains((int)ProfileParameterCategory.Capacity)
            .AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Capability))
            .AND(ParameterExposers.Categories.NotContains((int)ProfileParameterCategory.Configuration));

        private readonly FilterElement<Net.Profiles.Parameter> AllConfigurationsFilter =
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
        /// Retrieves a parameter by its ID.
        /// </summary>
        /// <param name="id">The ID of the parameter.</param>
        /// <returns>The parameter associated with the specified ID, or <see langword="null"/> if not found.</returns>
        public Net.Profiles.Parameter GetParameterById(Guid id)
        {
            return profileHelper.ProfileParameters.Read(ParameterExposers.ID.Equal(id)).SingleOrDefault();
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

            var filter = new ORFilterElement<Net.Profiles.Parameter>(ids.Select(id => ParameterExposers.ID.Equal(id)).ToArray());
            return profileHelper.ProfileParameters.Read(filter);
        }

        /// <summary>
        /// Retrieves a parameter by its name.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <returns>The parameter associated with the specified name, or <see langword="null"/> if not found.</returns>
        public Net.Profiles.Parameter GetParameterByName(string name)
        {
            return profileHelper.ProfileParameters.Read(ParameterExposers.Name.Equal(name)).SingleOrDefault();
        }

        /// <summary>
        /// Retrieves multiple parameters by their names.
        /// </summary>
        /// <param name="names">The collection of parameter names.</param>
        /// <returns>A dictionary mapping each name to its associated parameter.</returns>
        public IEnumerable<Net.Profiles.Parameter> GetParametersByName(IEnumerable<string> names)
        {
            var filter = new ORFilterElement<Net.Profiles.Parameter>(names.Select(name => ParameterExposers.Name.Equal(name)).ToArray());
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
        /// <param name="result">When the method returns, contains a <see cref="BulkCreateOrUpdateResult{T}"/> object that provides details
        /// about the operation, including the IDs of successfully processed parameters, IDs of failed parameters, and
        /// any associated error trace data.</param>
        /// <returns><see langword="true"/> if all parameters were successfully created or updated; otherwise, <see
        /// langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="parameters"/> is <see langword="null"/>.</exception>
        public bool TryCreateOrUpdateParametersInBatches(IEnumerable<Net.Profiles.Parameter> parameters, out BulkCreateOrUpdateResult<Guid> result)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var successfulIds = new List<Guid>();
            var unsuccessfulIds = new List<Guid>();
            var traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();

            foreach (var batch in parameters.Batch(100))
            {
                var succeededParameters = profileHelper.ProfileParameters.AddOrUpdateBulk(batch.ToArray());

                successfulIds.AddRange(succeededParameters.Select(x => x.ID));

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

            result = new BulkCreateOrUpdateResult<Guid>(successfulIds, unsuccessfulIds, traceDataPerItem);
            return !result.HasFailures();
        }

        /// <summary>
        /// Retrieves all capacity parameters.
        /// </summary>
        /// <returns>A collection of capacity parameters.</returns>
        public IReadOnlyCollection<Net.Profiles.Parameter> GetAllCapacities()
        {
            return profileHelper.ProfileParameters.Read(AllCapacitiesFilter);
        }

        /// <summary>
        /// Retrieves all capacity parameters.
        /// </summary>
        /// <returns>A collection of capacity parameters.</returns>
        public IEnumerable<IEnumerable<Net.Profiles.Parameter>> GetAllCapacitiesPaged(long? pageSize = null)
        {
            var pages = pageSize.HasValue
                ? profileHelper.ProfileParameters.ReadPaged(AllCapacitiesFilter, pageSize.Value)
                : profileHelper.ProfileParameters.ReadPaged(AllCapacitiesFilter);

            return pages;
        }

        public IEnumerable<Net.Profiles.Parameter> GetCapacities(IQuery<Net.Profiles.Parameter> query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            query = query.WithFilter(AllCapacitiesFilter.AND(query.Filter));
            return profileHelper.ProfileParameters.Read(query);
        }

        /// <summary>
        /// Total amount of capacity parameters.
        /// </summary>
        /// <returns>Total amount of capacity parameters.</returns>
        public long CountAllCapacities()
        {
            return profileHelper.ProfileParameters.Count(AllCapacitiesFilter);
        }

        public long CountCapacities(FilterElement<Net.Profiles.Parameter> filter)
        {
            return profileHelper.ProfileParameters.Count(AllCapacitiesFilter.AND(filter));
        }

        /// <summary>
        /// Retrieves a capacity parameter by its ID.
        /// </summary>
        /// <param name="id">The ID of the capacity parameter.</param>
        /// <returns>The capacity parameter associated with the specified ID, or <see langword="null"/> if not found.</returns>
        public Net.Profiles.Parameter GetCapacityById(Guid id)
        {
            var parameter = GetParameterById(id);
            if (parameter == null || !parameter.Categories.HasFlag(ProfileParameterCategory.Capacity))
            {
                return null;
            }

            return parameter;
        }

        /// <summary>
        /// Retrieves multiple capacity parameters by their IDs.
        /// </summary>
        /// <param name="ids">The collection of capacity parameter IDs.</param>
        /// <returns>A dictionary mapping each ID to its associated capacity parameter.</returns>
        public IEnumerable<Net.Profiles.Parameter> GetCapacitiesById(IEnumerable<Guid> ids)
        {
            return GetParametersById(ids).Where(x => x.Categories.HasFlag(ProfileParameterCategory.Capacity));
        }

        /// <summary>
        /// Retrieves a capacity parameter by its name.
        /// </summary>
        /// <param name="name">The name of the capacity parameter.</param>
        /// <returns>The capacity parameter associated with the specified name, or <see langword="null"/> if not found.</returns>
        public Net.Profiles.Parameter GetCapacityByName(string name)
        {
            var parameter = GetParameterByName(name);
            if (parameter == null || !parameter.Categories.HasFlag(ProfileParameterCategory.Capacity))
            {
                return null;
            }

            return parameter;
        }

        /// <summary>
        /// Retrieves multiple capacity parameters by their names.
        /// </summary>
        /// <param name="names">The collection of capacity parameter names.</param>
        /// <returns>A dictionary mapping each name to its associated capacity parameter.</returns>
        public IEnumerable<Net.Profiles.Parameter> GetCapacitiesByName(IEnumerable<string> names)
        {
            return GetParametersByName(names).Where(x => x.Categories.HasFlag(ProfileParameterCategory.Capacity));
        }

        /// <summary>
        /// Retrieves all configuration parameters.
        /// </summary>
        /// <returns>A collection of configuration parameters.</returns>
        public IReadOnlyCollection<Net.Profiles.Parameter> GetAllConfigurations()
        {
            return profileHelper.ProfileParameters.Read(AllConfigurationsFilter);
        }

        /// <summary>
        /// Retrieves all configuration parameters.
        /// </summary>
        /// <returns>A collection of configuration parameters.</returns>
        public IEnumerable<IEnumerable<Net.Profiles.Parameter>> GetAllConfigurationsPaged(long? pageSize = null)
        {
            var pages = pageSize.HasValue
                ? profileHelper.ProfileParameters.ReadPaged(AllConfigurationsFilter, pageSize.Value)
                : profileHelper.ProfileParameters.ReadPaged(AllConfigurationsFilter);

            return pages;
        }

        public IEnumerable<Net.Profiles.Parameter> GetConfigurations(IQuery<Net.Profiles.Parameter> query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            query = query.WithFilter(AllConfigurationsFilter.AND(query.Filter));
            return profileHelper.ProfileParameters.Read(query);
        }

        public long CountAllConfigurations()
        {
            return profileHelper.ProfileParameters.Count(AllConfigurationsFilter);
        }

        public long CountConfigurations(FilterElement<Net.Profiles.Parameter> filter)
        {
            return profileHelper.ProfileParameters.Count(AllConfigurationsFilter.AND(filter));
        }

        /// <summary>
        /// Retrieves a configuration parameter by its ID.
        /// </summary>
        /// <param name="id">The ID of the configuration parameter.</param>
        /// <returns>The configuration parameter associated with the specified ID, or <see langword="null"/> if not found.</returns>
        public Net.Profiles.Parameter GetConfigurationById(Guid id)
        {
            var parameter = GetParameterById(id);
            if (parameter == null || !parameter.Categories.HasFlag(ProfileParameterCategory.Configuration))
            {
                return null;
            }

            return parameter;
        }

        /// <summary>
        /// Retrieves multiple configuration parameters by their IDs.
        /// </summary>
        /// <param name="ids">The collection of configuration parameter IDs.</param>
        /// <returns>A dictionary mapping each ID to its associated configuration parameter.</returns>
        public IEnumerable<Net.Profiles.Parameter> GetConfigurationsById(IEnumerable<Guid> ids)
        {
            return GetParametersById(ids).Where(x => x.Categories.HasFlag(ProfileParameterCategory.Configuration));
        }

        /// <summary>
        /// Retrieves a configuration parameter by its name.
        /// </summary>
        /// <param name="name">The name of the configuration parameter.</param>
        /// <returns>The configuration parameter associated with the specified name, or <see langword="null"/> if not found.</returns>
        public Net.Profiles.Parameter GetConfigurationByName(string name)
        {
            var parameter = GetParameterByName(name);
            if (parameter == null || !parameter.Categories.HasFlag(ProfileParameterCategory.Configuration))
            {
                return null;
            }

            return parameter;
        }

        /// <summary>
        /// Retrieves multiple configuration parameters by their names.
        /// </summary>
        /// <param name="names">The collection of configuration parameter names.</param>
        /// <returns>A dictionary mapping each name to its associated configuration parameter.</returns>
        public IEnumerable<Net.Profiles.Parameter> GetConfigurationsByName(IEnumerable<string> names)
        {
            return GetParametersByName(names).Where(x => x.Categories.HasFlag(ProfileParameterCategory.Configuration));
        }

        /// <summary>
        /// Retrieves all capability parameters.
        /// </summary>
        /// <returns>A collection of capability parameters.</returns>
        public IReadOnlyCollection<Net.Profiles.Parameter> GetAllCapabilities()
        {
            return profileHelper.ProfileParameters.Read(AllCapabilitiesFilter);
        }

        /// <summary>
        /// Retrieves all capability parameters.
        /// </summary>
        /// <returns>A collection of capability parameters.</returns>
        public IEnumerable<IEnumerable<Net.Profiles.Parameter>> GetAllCapabilitiesPaged(long? pageSize = null)
        {
            var pages = pageSize.HasValue
                ? profileHelper.ProfileParameters.ReadPaged(AllCapabilitiesFilter, pageSize.Value)
                : profileHelper.ProfileParameters.ReadPaged(AllCapabilitiesFilter);

            return pages;
        }

        public IEnumerable<Net.Profiles.Parameter> GetCapabilities(IQuery<Net.Profiles.Parameter> query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            query = query.WithFilter(AllCapabilitiesFilter.AND(query.Filter));
            return profileHelper.ProfileParameters.Read(query);
        }

        public long CountAllCapabilities()
        {
            return profileHelper.ProfileParameters.Count(AllCapabilitiesFilter);
        }

        public long CountCapabilities(FilterElement<Net.Profiles.Parameter> filter)
        {
            return profileHelper.ProfileParameters.Count(AllCapabilitiesFilter.AND(filter));
        }

        public long CountNonTimeDependentCapabilities()
        {
            return profileHelper.ProfileParameters.Count(AllCapabilitiesFilter);
        }

        /// <summary>
        /// Retrieves a capability parameter by its ID.
        /// </summary>
        /// <param name="id">The ID of the capability parameter.</param>
        /// <returns>The capability parameter associated with the specified ID, or <see langword="null"/> if not found.</returns>
        public Net.Profiles.Parameter GetCapabilityById(Guid id)
        {
            var parameter = GetParameterById(id);
            if (parameter == null || !parameter.Categories.HasFlag(ProfileParameterCategory.Capability))
            {
                return null;
            }

            return parameter;
        }

        /// <summary>
        /// Retrieves multiple capability parameters by their IDs.
        /// </summary>
        /// <param name="ids">The collection of capability parameter IDs.</param>
        /// <returns>A dictionary mapping each ID to its associated capability parameter.</returns>
        public IEnumerable<Net.Profiles.Parameter> GetCapabilitiesById(IEnumerable<Guid> ids)
        {
            return GetParametersById(ids).Where(x => x.Categories.HasFlag(ProfileParameterCategory.Capability));
        }

        /// <summary>
        /// Retrieves a capability parameter by its name.
        /// </summary>
        /// <param name="name">The name of the capability parameter.</param>
        /// <returns>The capability parameter associated with the specified name, or <see langword="null"/> if not found.</returns>
        public Net.Profiles.Parameter GetCapabilityByName(string name)
        {
            var parameter = GetParameterByName(name);
            if (parameter == null || !parameter.Categories.HasFlag(ProfileParameterCategory.Capability))
            {
                return null;
            }

            return parameter;
        }

        /// <summary>
        /// Retrieves multiple capability parameters by their names.
        /// </summary>
        /// <param name="names">The collection of capability parameter names.</param>
        /// <returns>A dictionary mapping each name to its associated capability parameter.</returns>
        public IEnumerable<Net.Profiles.Parameter> GetCapabilitiesByName(IEnumerable<string> names)
        {
            return GetParametersByName(names).Where(x => x.Categories.HasFlag(ProfileParameterCategory.Capability));
        }

        public bool TryDeleteParametersInBatches(IEnumerable<Net.Profiles.Parameter> parameters, out Exceptions.BulkDeleteResult<Guid> result)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var successfulIds = new List<Guid>();
            var unsuccessfulIds = new List<Guid>();
            var traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();

            foreach (var batch in parameters.Batch(100))
            {
                var succeededParameters = profileHelper.ProfileParameters.RemoveBulk(batch.ToArray());

                successfulIds.AddRange(succeededParameters.Select(x => x.ID));

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

            result = new Exceptions.BulkDeleteResult<Guid>(successfulIds, unsuccessfulIds, traceDataPerItem);
            return !result.HasFailures();
        }
    }
}