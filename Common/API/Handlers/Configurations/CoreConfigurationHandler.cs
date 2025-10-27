namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Xml.Linq;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Profiles;

    internal class CoreConfigurationHandler : ApiObjectValidator<Guid>
    {
        private readonly MediaOpsPlanApi planApi;

        private CoreConfigurationHandler(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, IEnumerable<Configuration> apiConfigurations, out BulkCreateOrUpdateResult<Guid> result)
        {
            var handler = new CoreConfigurationHandler(planApi);
            handler.CreateOrUpdate(apiConfigurations);

            result = new BulkCreateOrUpdateResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures();
        }

        internal static bool TryDelete(MediaOpsPlanApi planApi, IEnumerable<Configuration> apiConfigurations, out BulkDeleteResult<Guid> result)
        {
            var handler = new CoreConfigurationHandler(planApi);
            handler.Delete(apiConfigurations);

            result = new BulkDeleteResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures();
        }

        private void CreateOrUpdate(IEnumerable<Configuration> apiConfigurations)
        {
            if (apiConfigurations == null)
            {
                throw new ArgumentNullException(nameof(apiConfigurations));
            }

            if (!apiConfigurations.Any())
            {
                return;
            }

            var toCreate = new List<Configuration>();
            var toUpdate = new List<Configuration>();
            foreach (var capacity in apiConfigurations)
            {
                if (capacity.IsNew)
                {
                    toCreate.Add(capacity);
                }
                else
                {
                    toUpdate.Add(capacity);
                }
            }

            ValidateIdsNotInUse(toCreate);
            ValidateNames(apiConfigurations);

            ValidateTextConfigurations(apiConfigurations.OfType<TextConfiguration>());
            ValidateNumberConfigurations(apiConfigurations.OfType<NumberConfiguration>());
            ValidateDiscreteTextConfigurations(apiConfigurations.OfType<DiscreteTextConfiguration>());
            ValidateDiscreteNumberConfigurations(apiConfigurations.OfType<DiscreteNumberConfiguration>());

            List<Net.Profiles.Parameter> coreParameters = new List<Net.Profiles.Parameter>();
            foreach (var configurationToAddOrUpdate in apiConfigurations.Where(x => !TraceDataPerItem.Keys.Contains(x.Id)))
            {
                if (!TryGetParameterWithChanges(configurationToAddOrUpdate, out var coreParameter))
                {
                    continue;
                }

                coreParameters.Add(coreParameter);
            }

            CreateOrUpdate(coreParameters);
        }

        private void CreateOrUpdate(IEnumerable<Net.Profiles.Parameter> coreConfigurations)
        {
            if (coreConfigurations == null)
            {
                throw new ArgumentNullException(nameof(coreConfigurations));
            }

            if (!coreConfigurations.Any())
            {
                return;
            }

            planApi.CoreHelpers.ProfileProvider.TryCreateOrUpdateParametersInBatches(coreConfigurations, out var result);

            foreach (var id in result.UnsuccessfulIds)
            {
                ReportError(id);

                if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
                {
                    PassTraceData(id, traceData);
                }
            }

            ReportSuccess(result.SuccessfulIds);
        }

        private void Delete(IEnumerable<Configuration> apiConfigurations)
        {
            if (apiConfigurations == null)
            {
                throw new ArgumentNullException(nameof(apiConfigurations));
            }

            if (!apiConfigurations.Any())
            {
                return;
            }

            foreach (var capacity in apiConfigurations.Where(x => x.IsNew))
            {
                var error = new ConfigurationConfigurationError
                {
                    ErrorReason = ConfigurationConfigurationError.Reason.InvalidState,
                    ErrorMessage = "Cannot delete a configuration that does not exist.",
                };

                ReportError(capacity.Id, error);
            }

            var coreConfigurationsToRemove = apiConfigurations
                .Where(x => !TraceDataPerItem.Keys.Contains(x.Id))
                .Select(x => x.CoreParameter);
            planApi.CoreHelpers.ProfileProvider.TryDeleteParametersInBatches(coreConfigurationsToRemove, out var result);

            foreach (var id in result.UnsuccessfulIds)
            {
                ReportError(id);

                if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
                {
                    PassTraceData(id, traceData);
                }
            }

            ReportSuccess(result.SuccessfulIds);
        }

        private void ValidateIdsNotInUse(IEnumerable<Configuration> apiConfigurations)
        {
            if (apiConfigurations == null)
            {
                throw new ArgumentNullException(nameof(apiConfigurations));
            }

            if (!apiConfigurations.Any())
            {
                return;
            }

            var configurationsRequiringValidation = apiConfigurations.Where(x => x.IsNew && x.HasUserDefinedId).ToList();
            if (configurationsRequiringValidation.Count == 0)
            {
                return;
            }

            var capabilitiesWithDuplicateIds = configurationsRequiringValidation
                .GroupBy(configuration => configuration.Id)
                .Where(g => g.Count() > 1)
                .SelectMany(x => x)
                .ToList();

            foreach (var configuration in capabilitiesWithDuplicateIds)
            {
                var error = new ConfigurationConfigurationError
                {
                    ErrorReason = ConfigurationConfigurationError.Reason.DuplicateId,
                    ErrorMessage = $"Configuration '{configuration.Name}' has a duplicate ID.",
                };

                ReportError(configuration.Id, error);

                configurationsRequiringValidation.Remove(configuration);
            }

            foreach (var foundProfileParameter in planApi.CoreHelpers.ProfileProvider.GetParametersById(configurationsRequiringValidation.Select(x => x.Id)))
            {
                planApi.Logger.LogInformation($"ID is already in use by a Profile Parameter.", foundProfileParameter.ID);

                var error = new ConfigurationConfigurationError
                {
                    ErrorReason = ConfigurationConfigurationError.Reason.IdInUse,
                    ErrorMessage = "ID is already in use.",
                };

                ReportError(foundProfileParameter.ID, error);
            }
        }

        private void ValidateNames(IEnumerable<Configuration> apiConfigurations)
        {
            if (apiConfigurations == null)
            {
                throw new ArgumentNullException(nameof(apiConfigurations));
            }

            if (!apiConfigurations.Any())
            {
                return;
            }

            var configurationsRequiringValidation = apiConfigurations.ToList();

            foreach (var configuration in configurationsRequiringValidation.Where(x => !InputValidator.ValidateEmptyText(x.Name)))
            {
                var error = new ConfigurationConfigurationError
                {
                    ErrorReason = ConfigurationConfigurationError.Reason.InvalidName,
                    ErrorMessage = "Name cannot be empty.",
                };

                ReportError(configuration.Id, error);
                configurationsRequiringValidation.Remove(configuration);
            }

            foreach (var configuration in configurationsRequiringValidation.Where(x => !InputValidator.ValidateTextLength(x.Name)))
            {
                var error = new ConfigurationConfigurationError
                {
                    ErrorReason = ConfigurationConfigurationError.Reason.InvalidName,
                    ErrorMessage = $"Name exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
                };

                ReportError(configuration.Id, error);
                configurationsRequiringValidation.Remove(configuration);
            }

            var configurationsWithDuplicateNames = configurationsRequiringValidation
                .GroupBy(configuration => configuration.Name)
                .Where(g => g.Count() > 1)
                .SelectMany(x => x)
                .ToList();

            foreach (var configuration in configurationsWithDuplicateNames)
            {
                var error = new ConfigurationConfigurationError
                {
                    ErrorReason = ConfigurationConfigurationError.Reason.DuplicateName,
                    ErrorMessage = $"Configuration '{configuration.Name}' has a duplicate name.",
                };

                ReportError(configuration.Id, error);
                configurationsRequiringValidation.Remove(configuration);
            }

            var coreParameters = planApi.CoreHelpers.ProfileProvider.GetParametersByName(configurationsRequiringValidation.Select(x => x.Name));
            foreach (var configuration in configurationsRequiringValidation)
            {
                var coreParametersWithSameName = coreParameters.Where(x => x.Name.Equals(configuration.Name));
                if (!coreParametersWithSameName.Any())
                {
                    continue;
                }

                var coreParametersWithSameNameAndDifferentIds = coreParametersWithSameName.Where(x => x.ID != configuration.Id).ToList();
                if (coreParametersWithSameNameAndDifferentIds.Count == 0)
                {
                    continue;
                }

                planApi.Logger.LogInformation($"Name '{configuration.Name}' is already in use by Profile Parameter(s) with ID(s)", coreParametersWithSameNameAndDifferentIds.Select(x => x.ID).ToArray());

                var error = new ConfigurationConfigurationError
                {
                    ErrorReason = ConfigurationConfigurationError.Reason.NameExists,
                    ErrorMessage = "Name is already in use.",
                };

                ReportError(configuration.Id, error);
            }
        }

        private void ValidateNumberConfigurations(IEnumerable<NumberConfiguration> apiConfigurations)
        {

        }

        private void ValidateTextConfigurations(IEnumerable<TextConfiguration> apiConfigurations)
        {

        }

        private void ValidateDiscreteNumberConfigurations(IEnumerable<DiscreteNumberConfiguration> apiConfigurations)
        {

        }

        private void ValidateDiscreteTextConfigurations(IEnumerable<DiscreteTextConfiguration> apiConfigurations)
        {

        }

        private bool TryGetParameterWithChanges(Configuration apiConfiguration, out Net.Profiles.Parameter parameter)
        {
            if (apiConfiguration is TextConfiguration textConfiguration)
            {
                parameter = GetTextConfigurationWithChanges(textConfiguration);
                return true;
            }

            if (apiConfiguration is NumberConfiguration numberConfiguration)
            {
                parameter = GetNumberConfigurationWithChanges(numberConfiguration);
                return true;
            }

            if (apiConfiguration is DiscreteTextConfiguration discreteTextConfiguration)
            {
                parameter = GetParameterWithChanges(discreteTextConfiguration);
                return true;
            }

            if (apiConfiguration is DiscreteNumberConfiguration discreteNumberConfiguration)
            {
                parameter = GetParameterWithChanges(discreteNumberConfiguration);
                return true;
            }

            parameter = null;
            return false;
        }

        private Net.Profiles.Parameter GetNumberConfigurationWithChanges(NumberConfiguration apiConfiguration)
        {

        }

        private Net.Profiles.Parameter GetTextConfigurationWithChanges(TextConfiguration apiConfiguration)
        {
            Net.Profiles.Parameter updatedParameter;
            if (apiConfiguration.IsNew)
            {
                updatedParameter = new Net.Profiles.Parameter(apiConfiguration.Id)
                {
                    Categories = Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Configuration,
                    Type = Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Text,
                    InterpreteType = new Skyline.DataMiner.Net.Profiles.InterpreteType
                    {
                        Type = Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Undefined,
                        RawType = Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Undefined
                    }
                };
            }
            else
            {
                updatedParameter = new Net.Profiles.Parameter(apiConfiguration.CoreParameter)
                {
                    Categories = Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Configuration,
                    Type = Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Text,
                    InterpreteType = new Skyline.DataMiner.Net.Profiles.InterpreteType
                    {
                        Type = Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Undefined,
                        RawType = Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Undefined
                    }
                };
            }

            updatedParameter.Name = apiConfiguration.Name;
            updatedParameter.IsOptional = !apiConfiguration.IsMandatory;
            if (apiConfiguration.DefaultValue != null)
            {
                updatedParameter.DefaultValue = new Skyline.DataMiner.Net.Profiles.ParameterValue
                {
                    Type = Skyline.DataMiner.Net.Profiles.ParameterValue.ValueType.String,
                    StringValue = (string)apiConfiguration.DefaultValue
                };
            }
            else
            {
                updatedParameter.DefaultValue = null;
            }

            return updatedParameter;
        }

        private Net.Profiles.Parameter GetParameterWithChanges(DiscreteNumberConfiguration apiConfiguration)
        {
        }

        private Net.Profiles.Parameter GetParameterWithChanges(DiscreteTextConfiguration apiConfiguration)
        {
        }
    }
}
