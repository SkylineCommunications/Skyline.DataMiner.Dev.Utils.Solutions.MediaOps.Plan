namespace Skyline.DataMiner.MediaOps.Plan.Storage.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.MediaOps.Plan.API;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.Profiles;
    using static Skyline.DataMiner.Net.Profiles.Parameter;

    /// <summary>
    /// Provides methods to manage profiles, including retrieving parameters by ID or name, filtering based on categories, 
    /// and managing capabilities and capacities.
    /// </summary>
    public class ProfileProvider
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
        public Skyline.DataMiner.Net.Profiles.Parameter GetParameterById(Guid id)
        {
            return profileHelper.ProfileParameters.Read(ParameterExposers.ID.Equal(id)).SingleOrDefault();
        }

        /// <summary>
        /// Retrieves multiple parameters by their IDs.
        /// </summary>
        /// <param name="ids">The collection of parameter IDs.</param>
        /// <returns>A dictionary mapping each ID to its associated parameter.</returns>
        public IEnumerable<Skyline.DataMiner.Net.Profiles.Parameter> GetParametersById(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            var filter = new ORFilterElement<Skyline.DataMiner.Net.Profiles.Parameter>(ids.Select(id => ParameterExposers.ID.Equal(id)).ToArray());
            return profileHelper.ProfileParameters.Read(filter);
        }

        /// <summary>
        /// Retrieves a parameter by its name.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <returns>The parameter associated with the specified name, or <see langword="null"/> if not found.</returns>
        public Skyline.DataMiner.Net.Profiles.Parameter GetParameterByName(string name)
        {
            return profileHelper.ProfileParameters.Read(ParameterExposers.Name.Equal(name)).SingleOrDefault();
        }

        /// <summary>
        /// Retrieves multiple parameters by their names.
        /// </summary>
        /// <param name="names">The collection of parameter names.</param>
        /// <returns>A dictionary mapping each name to its associated parameter.</returns>
        public IEnumerable<Skyline.DataMiner.Net.Profiles.Parameter> GetParametersByName(IEnumerable<string> names)
        {
            var filter = new ORFilterElement<Skyline.DataMiner.Net.Profiles.Parameter>(names.Select(name => ParameterExposers.Name.Equal(name)).ToArray());
            return profileHelper.ProfileParameters.Read(filter);
        }

        public IEnumerable<Net.Profiles.Parameter> CreateOrUpdateParameters(IEnumerable<Skyline.DataMiner.Net.Profiles.Parameter> parameters)
        {
            return profileHelper.ProfileParameters.AddOrUpdateBulk(parameters.ToArray());
        }

        /// <summary>
        /// Retrieves all capacity parameters.
        /// </summary>
        /// <returns>A collection of capacity parameters.</returns>
        public IReadOnlyCollection<Skyline.DataMiner.Net.Profiles.Parameter> GetAllCapacities()
        {
            return profileHelper.ProfileParameters.Read(AllCapacitiesFilter);
        }

        /// <summary>
        /// Total amount of capacity parameters.
        /// </summary>
        /// <returns>Total amount of capacity parameters.</returns>
        public long CountAllCapacities()
        {
            return profileHelper.ProfileParameters.Count(AllCapacitiesFilter);
        }

        /// <summary>
        /// Retrieves a capacity parameter by its ID.
        /// </summary>
        /// <param name="id">The ID of the capacity parameter.</param>
        /// <returns>The capacity parameter associated with the specified ID, or <see langword="null"/> if not found.</returns>
        public Skyline.DataMiner.Net.Profiles.Parameter GetCapacityById(Guid id)
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
        public IEnumerable<Skyline.DataMiner.Net.Profiles.Parameter> GetCapacitiesById(IEnumerable<Guid> ids)
        {
            return GetParametersById(ids).Where(x => x.Categories.HasFlag(ProfileParameterCategory.Capacity));
        }

        /// <summary>
        /// Retrieves a capacity parameter by its name.
        /// </summary>
        /// <param name="name">The name of the capacity parameter.</param>
        /// <returns>The capacity parameter associated with the specified name, or <see langword="null"/> if not found.</returns>
        public Skyline.DataMiner.Net.Profiles.Parameter GetCapacityByName(string name)
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
        public IEnumerable<Skyline.DataMiner.Net.Profiles.Parameter> GetCapacitiesByName(IEnumerable<string> names)
        {
            return GetParametersByName(names).Where(x => x.Categories.HasFlag(ProfileParameterCategory.Capacity));
        }

        /// <summary>
        /// Retrieves all configuration parameters.
        /// </summary>
        /// <returns>A collection of configuration parameters.</returns>
        public IReadOnlyCollection<Skyline.DataMiner.Net.Profiles.Parameter> GetAllConfigurations()
        {
            return profileHelper.ProfileParameters.Read(AllConfigurationsFilter);
        }

        public long CountAllConfigurations()
        {
            return profileHelper.ProfileParameters.Count(AllConfigurationsFilter);
        }

        /// <summary>
        /// Retrieves a configuration parameter by its ID.
        /// </summary>
        /// <param name="id">The ID of the configuration parameter.</param>
        /// <returns>The configuration parameter associated with the specified ID, or <see langword="null"/> if not found.</returns>
        public Skyline.DataMiner.Net.Profiles.Parameter GetConfigurationById(Guid id)
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
        public IEnumerable<Skyline.DataMiner.Net.Profiles.Parameter> GetConfigurationsById(IEnumerable<Guid> ids)
        {
            return GetParametersById(ids).Where(x => x.Categories.HasFlag(ProfileParameterCategory.Configuration));
        }

        /// <summary>
        /// Retrieves a configuration parameter by its name.
        /// </summary>
        /// <param name="name">The name of the configuration parameter.</param>
        /// <returns>The configuration parameter associated with the specified name, or <see langword="null"/> if not found.</returns>
        public Skyline.DataMiner.Net.Profiles.Parameter GetConfigurationByName(string name)
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
        public IEnumerable<Skyline.DataMiner.Net.Profiles.Parameter> GetConfigurationsByName(IEnumerable<string> names)
        {
            return GetParametersByName(names).Where(x => x.Categories.HasFlag(ProfileParameterCategory.Configuration));
        }

        /// <summary>
        /// Retrieves all capability parameters.
        /// </summary>
        /// <returns>A collection of capability parameters.</returns>
        public IReadOnlyCollection<Skyline.DataMiner.Net.Profiles.Parameter> GetAllCapabilities()
        {
            return profileHelper.ProfileParameters.Read(AllCapabilitiesFilter);
        }

        public long CountCapabilities()
        {
            return profileHelper.ProfileParameters.Count(AllCapabilitiesFilter);
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
        public Skyline.DataMiner.Net.Profiles.Parameter GetCapabilityById(Guid id)
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
        public IEnumerable<Skyline.DataMiner.Net.Profiles.Parameter> GetCapabilitiesById(IEnumerable<Guid> ids)
        {
            return GetParametersById(ids).Where(x => x.Categories.HasFlag(ProfileParameterCategory.Capability));
        }

        /// <summary>
        /// Retrieves a capability parameter by its name.
        /// </summary>
        /// <param name="name">The name of the capability parameter.</param>
        /// <returns>The capability parameter associated with the specified name, or <see langword="null"/> if not found.</returns>
        public Skyline.DataMiner.Net.Profiles.Parameter GetCapabilityByName(string name)
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
        public IEnumerable<Skyline.DataMiner.Net.Profiles.Parameter> GetCapabilitiesByName(IEnumerable<string> names)
        {
            return GetParametersByName(names).Where(x => x.Categories.HasFlag(ProfileParameterCategory.Capability));
        }

        public IEnumerable<Skyline.DataMiner.Net.Profiles.Parameter> Delete(IEnumerable<Skyline.DataMiner.Net.Profiles.Parameter> parametersToDelete)
        {
            return profileHelper.ProfileParameters.RemoveBulk(parametersToDelete.ToArray());
        }
    }
}