namespace Skyline.DataMiner.MediaOps.Plan.Storage.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.MediaOps.Plan.Tools;
    using Skyline.DataMiner.Net.ManagerStore;
    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.Profiles;

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

        /// <summary>
        /// A lazily initialized <see cref="DualDictionary{Guid, Parameter}"/> for storing and retrieving parameters by ID or name.
        /// </summary>
        private Lazy<DualDictionary<Guid, Skyline.DataMiner.Net.Profiles.Parameter>> lazyParameterDualDictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileProvider"/> class using the specified engine.
        /// </summary>
        /// <param name="messageHandler">The message handler.</param>
        public ProfileProvider(Func<DMSMessage[], DMSMessage[]> messageHandler) : this(new ProfileHelper(messageHandler))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileProvider"/> class using the specified profile helper.
        /// </summary>
        /// <param name="profileHelper">The helper used to perform profile operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="profileHelper"/> is null.</exception>
        public ProfileProvider(ProfileHelper profileHelper)
        {
            this.profileHelper = profileHelper ?? throw new ArgumentNullException(nameof(profileHelper));
            Init();
        }

        #region Properties
        /// <summary>
        /// Gets the dual dictionary containing parameters, keyed by their ID and name.
        /// </summary>
        private DualDictionary<Guid, Skyline.DataMiner.Net.Profiles.Parameter> ParameterDualDictionary => lazyParameterDualDictionary.Value;
        #endregion

        #region Methods
        /// <summary>
        /// Retrieves a parameter by its ID.
        /// </summary>
        /// <param name="id">The ID of the parameter.</param>
        /// <param name="forceGet">Indicates whether to force retrieving the parameter from the source, bypassing the cache.</param>
        /// <returns>The parameter associated with the specified ID, or <see langword="null"/> if not found.</returns>
        public Skyline.DataMiner.Net.Profiles.Parameter GetParameterById(Guid id, bool forceGet = false)
        {
            if (!forceGet)
            {
                if (ParameterDualDictionary.TryGetByKey(id, out var parameter))
                {
                    return parameter;
                }
            }
            else
            {
                var parameter = profileHelper.ProfileParameters.Read(ParameterExposers.ID.Equal(id)).SingleOrDefault();
                if (parameter != null)
                {
                    ParameterDualDictionary.AddOrUpdate(parameter.ID, parameter);
                    return parameter;
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves multiple parameters by their IDs.
        /// </summary>
        /// <param name="ids">The collection of parameter IDs.</param>
        /// <param name="forceGet">Indicates whether to force retrieving the parameters from the source, bypassing the cache.</param>
        /// <returns>A dictionary mapping each ID to its associated parameter.</returns>
        public IDictionary<Guid, Skyline.DataMiner.Net.Profiles.Parameter> GetParametersById(IEnumerable<Guid> ids, bool forceGet = false)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            var result = new Dictionary<Guid, Skyline.DataMiner.Net.Profiles.Parameter>();
            var idsToRetrieve = new List<Guid>();

            var idsToProcess = ids.Where(x => x != Guid.Empty).Distinct().ToList();
            if (!forceGet)
            {
                foreach (var id in idsToProcess)
                {
                    if (ParameterDualDictionary.TryGetByKey(id, out var parameter))
                    {
                        result[id] = parameter;
                    }
                    else
                    {
                        idsToRetrieve.Add(id);
                    }
                }
            }
            else
            {
                idsToRetrieve = idsToProcess;
            }

            if (idsToRetrieve.Count > 0)
            {
                var filter = new ORFilterElement<Skyline.DataMiner.Net.Profiles.Parameter>(idsToRetrieve.Select(id => ParameterExposers.ID.Equal(id)).ToArray());
                foreach (var parameter in profileHelper.ProfileParameters.Read(filter))
                {
                    ParameterDualDictionary.AddOrUpdate(parameter.ID, parameter);
                    result[parameter.ID] = parameter;
                }
            }

            return result;
        }

        /// <summary>
        /// Retrieves a parameter by its name.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="forceGet">Indicates whether to force retrieving the parameter from the source, bypassing the cache.</param>
        /// <returns>The parameter associated with the specified name, or <see langword="null"/> if not found.</returns>
        public Skyline.DataMiner.Net.Profiles.Parameter GetParameterByName(string name, bool forceGet = false)
        {
            if (!forceGet)
            {
                if (ParameterDualDictionary.TryGetByName(name, out var parameter))
                {
                    return parameter;
                }
            }
            else
            {
                var parameter = profileHelper.ProfileParameters.Read(ParameterExposers.Name.Equal(name)).SingleOrDefault();
                if (parameter != null)
                {
                    ParameterDualDictionary.AddOrUpdate(parameter.ID, parameter);
                    return parameter;
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves multiple parameters by their names.
        /// </summary>
        /// <param name="names">The collection of parameter names.</param>
        /// <param name="forceGet">Indicates whether to force retrieving the parameters from the source, bypassing the cache.</param>
        /// <returns>A dictionary mapping each name to its associated parameter.</returns>
        public IDictionary<string, Skyline.DataMiner.Net.Profiles.Parameter> GetParametersByName(IEnumerable<string> names, bool forceGet = false)
        {
            if (names == null)
            {
                throw new ArgumentNullException(nameof(names));
            }

            var result = new Dictionary<string, Skyline.DataMiner.Net.Profiles.Parameter>();
            var namesToRetrieve = new List<string>();

            var namesToProcess = names.Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            if (!forceGet)
            {
                foreach (var name in namesToProcess)
                {
                    if (ParameterDualDictionary.TryGetByName(name, out var parameter))
                    {
                        result[name] = parameter;
                    }
                    else
                    {
                        namesToRetrieve.Add(name);
                    }
                }
            }
            else
            {
                namesToRetrieve = namesToProcess;
            }

            if (namesToRetrieve.Count > 0)
            {
                var filter = new ORFilterElement<Skyline.DataMiner.Net.Profiles.Parameter>(namesToRetrieve.Select(name => ParameterExposers.Name.Equal(name)).ToArray());
                foreach (var parameter in profileHelper.ProfileParameters.Read(filter))
                {
                    ParameterDualDictionary.AddOrUpdate(parameter.ID, parameter);
                    result[parameter.Name] = parameter;
                }
            }

            return result;
        }

        /// <summary>
        /// Retrieves all capacity parameters.
        /// </summary>
        /// <returns>A collection of capacity parameters.</returns>
        public IReadOnlyCollection<Skyline.DataMiner.Net.Profiles.Parameter> GetAllCapacities()
        {
            var capacities = ParameterDualDictionary.Values.Where(x => x.Categories == ProfileParameterCategory.Capacity).ToList();

            return capacities;
        }

        /// <summary>
        /// Retrieves a capacity parameter by its ID.
        /// </summary>
        /// <param name="id">The ID of the capacity parameter.</param>
        /// <param name="forceGet">Indicates whether to force retrieving the parameter from the source, bypassing the cache.</param>
        /// <returns>The capacity parameter associated with the specified ID, or <see langword="null"/> if not found.</returns>
        public Skyline.DataMiner.Net.Profiles.Parameter GetCapacityById(Guid id, bool forceGet = false)
        {
            var parameter = GetParameterById(id, forceGet);
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
        /// <param name="forceGet">Indicates whether to force retrieving the parameters from the source, bypassing the cache.</param>
        /// <returns>A dictionary mapping each ID to its associated capacity parameter.</returns>
        public IDictionary<Guid, Skyline.DataMiner.Net.Profiles.Parameter> GetCapacitiesById(IEnumerable<Guid> ids, bool forceGet = false)
        {
            return GetParametersById(ids, forceGet).Values.Where(x => x.Categories.HasFlag(ProfileParameterCategory.Capacity)).ToDictionary(x => x.ID);
        }

        /// <summary>
        /// Retrieves a capacity parameter by its name.
        /// </summary>
        /// <param name="name">The name of the capacity parameter.</param>
        /// <param name="forceGet">Indicates whether to force retrieving the parameter from the source, bypassing the cache.</param>
        /// <returns>The capacity parameter associated with the specified name, or <see langword="null"/> if not found.</returns>
        public Skyline.DataMiner.Net.Profiles.Parameter GetCapacityByName(string name, bool forceGet = false)
        {
            var parameter = GetParameterByName(name, forceGet);
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
        /// <param name="forceGet">Indicates whether to force retrieving the parameters from the source, bypassing the cache.</param>
        /// <returns>A dictionary mapping each name to its associated capacity parameter.</returns>
        public IDictionary<string, Skyline.DataMiner.Net.Profiles.Parameter> GetCapacitiesByName(IEnumerable<string> names, bool forceGet = false)
        {
            return GetParametersByName(names, forceGet).Values.Where(x => x.Categories.HasFlag(ProfileParameterCategory.Capacity)).ToDictionary(x => x.Name);
        }

        /// <summary>
        /// Retrieves all configuration parameters.
        /// </summary>
        /// <returns>A collection of configuration parameters.</returns>
        public IReadOnlyCollection<Skyline.DataMiner.Net.Profiles.Parameter> GetAllConfigurations()
        {
            var configurations = ParameterDualDictionary.Values.Where(x => x.Categories == ProfileParameterCategory.Configuration).ToList();

            return configurations;
        }

        /// <summary>
        /// Retrieves a configuration parameter by its ID.
        /// </summary>
        /// <param name="id">The ID of the configuration parameter.</param>
        /// <param name="forceGet">Indicates whether to force retrieving the parameter from the source, bypassing the cache.</param>
        /// <returns>The configuration parameter associated with the specified ID, or <see langword="null"/> if not found.</returns>
        public Skyline.DataMiner.Net.Profiles.Parameter GetConfigurationById(Guid id, bool forceGet = false)
        {
            var parameter = GetParameterById(id, forceGet);
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
        /// <param name="forceGet">Indicates whether to force retrieving the parameters from the source, bypassing the cache.</param>
        /// <returns>A dictionary mapping each ID to its associated configuration parameter.</returns>
        public IDictionary<Guid, Skyline.DataMiner.Net.Profiles.Parameter> GetConfigurationsById(IEnumerable<Guid> ids, bool forceGet = false)
        {
            return GetParametersById(ids, forceGet).Values.Where(x => x.Categories.HasFlag(ProfileParameterCategory.Configuration)).ToDictionary(x => x.ID);
        }

        /// <summary>
        /// Retrieves a configuration parameter by its name.
        /// </summary>
        /// <param name="name">The name of the configuration parameter.</param>
        /// <param name="forceGet">Indicates whether to force retrieving the parameter from the source, bypassing the cache.</param>
        /// <returns>The configuration parameter associated with the specified name, or <see langword="null"/> if not found.</returns>
        public Skyline.DataMiner.Net.Profiles.Parameter GetConfigurationByName(string name, bool forceGet = false)
        {
            var parameter = GetParameterByName(name, forceGet);
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
        /// <param name="forceGet">Indicates whether to force retrieving the parameters from the source, bypassing the cache.</param>
        /// <returns>A dictionary mapping each name to its associated configuration parameter.</returns>
        public IDictionary<string, Skyline.DataMiner.Net.Profiles.Parameter> GetConfigurationsByName(IEnumerable<string> names, bool forceGet = false)
        {
            return GetParametersByName(names, forceGet).Values.Where(x => x.Categories.HasFlag(ProfileParameterCategory.Configuration)).ToDictionary(x => x.Name);
        }

        /// <summary>
        /// Retrieves all capability parameters.
        /// </summary>
        /// <returns>A collection of capability parameters.</returns>
        public IReadOnlyCollection<Skyline.DataMiner.Net.Profiles.Parameter> GetAllCapabilities()
        {
            var capabilities = ParameterDualDictionary.Values.Where(x => x.Categories == ProfileParameterCategory.Capability && x.Type == Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Discrete && x.InterpreteType.RawType == InterpreteType.RawTypeEnum.Other).ToList();

            return capabilities;
        }

        /// <summary>
        /// Retrieves a capability parameter by its ID.
        /// </summary>
        /// <param name="id">The ID of the capability parameter.</param>
        /// <param name="forceGet">Indicates whether to force retrieving the parameter from the source, bypassing the cache.</param>
        /// <returns>The capability parameter associated with the specified ID, or <see langword="null"/> if not found.</returns>
        public Skyline.DataMiner.Net.Profiles.Parameter GetCapabilityById(Guid id, bool forceGet = false)
        {
            var parameter = GetParameterById(id, forceGet);
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
        /// <param name="forceGet">Indicates whether to force retrieving the parameters from the source, bypassing the cache.</param>
        /// <returns>A dictionary mapping each ID to its associated capability parameter.</returns>
        public IDictionary<Guid, Skyline.DataMiner.Net.Profiles.Parameter> GetCapabilitiesById(IEnumerable<Guid> ids, bool forceGet = false)
        {
            return GetParametersById(ids, forceGet).Values.Where(x => x.Categories.HasFlag(ProfileParameterCategory.Capability)).ToDictionary(x => x.ID);
        }

        /// <summary>
        /// Retrieves a capability parameter by its name.
        /// </summary>
        /// <param name="name">The name of the capability parameter.</param>
        /// <param name="forceGet">Indicates whether to force retrieving the parameter from the source, bypassing the cache.</param>
        /// <returns>The capability parameter associated with the specified name, or <see langword="null"/> if not found.</returns>
        public Skyline.DataMiner.Net.Profiles.Parameter GetCapabilityByName(string name, bool forceGet = false)
        {
            var parameter = GetParameterByName(name, forceGet);
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
        /// <param name="forceGet">Indicates whether to force retrieving the parameters from the source, bypassing the cache.</param>
        /// <returns>A dictionary mapping each name to its associated capability parameter.</returns>
        public IDictionary<string, Skyline.DataMiner.Net.Profiles.Parameter> GetCapabilitiesByName(IEnumerable<string> names, bool forceGet = false)
        {
            return GetParametersByName(names, forceGet).Values.Where(x => x.Categories.HasFlag(ProfileParameterCategory.Capability)).ToDictionary(x => x.Name);
        }

        /// <summary>
        /// Initializes the lazy-loaded parameter dictionary.
        /// </summary>
        private void Init()
        {
            lazyParameterDualDictionary = new Lazy<DualDictionary<Guid, Skyline.DataMiner.Net.Profiles.Parameter>>(() => InitParameterDualDictionary());
        }

        /// <summary>
        /// Initializes the parameter dual dictionary by reading all parameters from the profile helper.
        /// </summary>
        /// <returns>The initialized <see cref="DualDictionary{Guid, Parameter}"/>.</returns>
        private DualDictionary<Guid, Skyline.DataMiner.Net.Profiles.Parameter> InitParameterDualDictionary()
        {
            var dualDictionary = new DualDictionary<Guid, Skyline.DataMiner.Net.Profiles.Parameter>(param => param.Name);

            var profileParameters = profileHelper.ProfileParameters.ReadAll().ToList();
            var duplicates = profileParameters.Select(x => x.Name).Duplicates().ToList();

            if (duplicates.Any())
            {
                throw new InvalidOperationException($"Duplicate profile parameter(s) detected: {String.Join(", ", duplicates)}");
            }

            foreach (var parameter in profileParameters)
            {
                dualDictionary.Add(parameter.ID, parameter);
            }

            return dualDictionary;
        }
        #endregion
    }
}
