namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using SLDataGateway.API.Types.Querying;

    internal partial class ConfigurationsRepository : ProfileParameterRepository<Configuration>, IConfigurationsRepository
    {
        public ConfigurationsRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        public long CountAll()
        {
            return PlanApi.CoreHelpers.ProfileProvider.CountAllConfigurations();
        }

        public IQueryable<Configuration> Query()
        {
            return new ApiRepositoryQuery<Configuration, Net.Profiles.Parameter>(QueryProvider);
        }

        public Configuration Read(Guid id)
        {
            PlanApi.Logger.LogInformation($"Reading Configuration with ID: {id}...");

            if (id == Guid.Empty)
            {
                throw new ArgumentException(nameof(id));
            }

            return ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(Read), act =>
            {
                act?.AddTag("CapacityId", id);
                var coreConfiguration = PlanApi.CoreHelpers.ProfileProvider.GetConfigurationById(id);

                if (coreConfiguration == null)
                {
                    act?.AddTag("Hit", false);
                    return null;
                }

                act?.AddTag("Hit", true);

                return InstantiateConfigurations([coreConfiguration]).FirstOrDefault();
            });
        }

        public IDictionary<Guid, Configuration> Read(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            return ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(Read), act =>
            {
                act?.AddTag("ConfigurationIds", String.Join(", ", ids));
                act?.AddTag("ConfigurationIds Count", ids.Count());

                var configurations = PlanApi.CoreHelpers.ProfileProvider.GetConfigurationsById(ids);

                return InstantiateConfigurations(configurations).ToDictionary(x => x.Id);
            });
        }

        public IEnumerable<Configuration> ReadAll()
        {
            return ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(ReadAll), act =>
            {
                return InstantiateConfigurations(PlanApi.CoreHelpers.ProfileProvider.GetAllConfigurations());
            });
        }

        public IEnumerable<IEnumerable<Configuration>> ReadAllPaged()
        {
            return ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(ReadAllPaged), act =>
            {
                return PlanApi.CoreHelpers.ProfileProvider.GetAllConfigurationsPaged().Select(page => InstantiateConfigurationsIterator(page));
            });
        }

        internal override long Count(FilterElement<Net.Profiles.Parameter> filter)
        {
            return PlanApi.CoreHelpers.ProfileProvider.CountConfigurations(filter);
        }

        internal override IEnumerable<Configuration> Read(IQuery<Net.Profiles.Parameter> query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return InstantiateConfigurations(PlanApi.CoreHelpers.ProfileProvider.GetConfigurations(query));
        }

        protected internal override FilterElement<Net.Profiles.Parameter> CreateFilter(string fieldName, Comparer comparer, object value)
        {
            switch (fieldName)
            {
                // DefaultValue and DisplayValues are not exposed through ParameterExposers.
                case nameof(NumberConfiguration.Units):
                    return FilterElementFactory<Net.Profiles.Parameter>.Create(Net.Profiles.ParameterExposers.Units, comparer, value);
                case nameof(NumberConfiguration.RangeMin):
                    return FilterElementFactory<Net.Profiles.Parameter>.Create(Net.Profiles.ParameterExposers.RangeMin, comparer, Convert.ToDouble(value));
                case nameof(NumberConfiguration.RangeMax):
                    return FilterElementFactory<Net.Profiles.Parameter>.Create(Net.Profiles.ParameterExposers.RangeMax, comparer, Convert.ToDouble(value));
                case nameof(NumberConfiguration.StepSize):
                    return FilterElementFactory<Net.Profiles.Parameter>.Create(Net.Profiles.ParameterExposers.Stepsize, comparer, Convert.ToDouble(value));
                case nameof(NumberConfiguration.Decimals):
                    return FilterElementFactory<Net.Profiles.Parameter>.Create(Net.Profiles.ParameterExposers.Decimals, comparer, value);
                case nameof(DiscreteNumberConfiguration.Discretes.Values):
                    return FilterElementFactory<Net.Profiles.Parameter>.Create(Net.Profiles.ParameterExposers.Discretes, comparer, value);
            }

            return base.CreateFilter(fieldName, comparer, value);
        }

        protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
        {
            switch (fieldName)
            {
                // DefaultValue and DisplayValues are not exposed through ParameterExposers.
                case nameof(NumberConfiguration.Units):
                    return OrderByElementFactory.Create(Net.Profiles.ParameterExposers.Units, sortOrder, naturalSort);
                case nameof(NumberConfiguration.RangeMin):
                    return OrderByElementFactory.Create(Net.Profiles.ParameterExposers.RangeMin, sortOrder, naturalSort);
                case nameof(NumberConfiguration.RangeMax):
                    return OrderByElementFactory.Create(Net.Profiles.ParameterExposers.RangeMax, sortOrder, naturalSort);
                case nameof(NumberConfiguration.StepSize):
                    return OrderByElementFactory.Create(Net.Profiles.ParameterExposers.Stepsize, sortOrder, naturalSort);
                case nameof(NumberConfiguration.Decimals):
                    return OrderByElementFactory.Create(Net.Profiles.ParameterExposers.Decimals, sortOrder, naturalSort);
                case nameof(DiscreteNumberConfiguration.Discretes.Values):
                    return OrderByElementFactory.Create(Net.Profiles.ParameterExposers.Discretes, sortOrder, naturalSort);
            }

            return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
        }

        internal static IEnumerable<Configuration> InstantiateConfigurations(IEnumerable<Net.Profiles.Parameter> instances)
        {
            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            if (!instances.Any())
            {
                return [];
            }

            return InstantiateConfigurationsIterator(instances);
        }

        private static IEnumerable<Configuration> InstantiateConfigurationsIterator(IEnumerable<Net.Profiles.Parameter> instances)
        {
            foreach (var instance in instances)
            {
                if (!instance.IsConfiguration())
                {
                    continue;
                }

                if (instance.IsText())
                {
                    yield return new TextConfiguration(instance);
                }
                else if (instance.IsNumber())
                {
                    yield return new NumberConfiguration(instance);
                }
                else if (instance.IsTextDiscreet())
                {
                    yield return new DiscreteTextConfiguration(instance);
                }
                else if (instance.IsNumberDiscreet())
                {
                    yield return new DiscreteNumberConfiguration(instance);
                }
                else
                {
                    // continue
                }
            }
        }
    }
}
