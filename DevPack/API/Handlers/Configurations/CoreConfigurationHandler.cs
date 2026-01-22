namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Helper;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

    internal class CoreConfigurationHandler : ApiObjectValidator
    {
        private readonly MediaOpsPlanApi planApi;

        private CoreConfigurationHandler(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<Configuration> apiConfigurations, out BulkOperationResult<Guid> result)
        {
            var handler = new CoreConfigurationHandler(planApi);
            handler.CreateOrUpdate(apiConfigurations);

            result = new BulkOperationResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures;
        }

        internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<Configuration> apiConfigurations, out BulkOperationResult<Guid> result)
        {
            var handler = new CoreConfigurationHandler(planApi);
            handler.Delete(apiConfigurations);

            result = new BulkOperationResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures;
        }

        private void CreateOrUpdate(ICollection<Configuration> apiConfigurations)
        {
            if (apiConfigurations == null)
            {
                throw new ArgumentNullException(nameof(apiConfigurations));
            }

            if (apiConfigurations.Count == 0)
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

            ValidateTextConfigurations(apiConfigurations.OfType<TextConfiguration>().ToList());
            ValidateNumberConfigurations(apiConfigurations.OfType<NumberConfiguration>().ToList());
            ValidateDiscreteTextConfigurations(apiConfigurations.OfType<DiscreteTextConfiguration>().ToList());
            ValidateDiscreteNumberConfigurations(apiConfigurations.OfType<DiscreteNumberConfiguration>().ToList());

            var validConfigurations = apiConfigurations.Where(IsValid).ToList();

            var result = planApi.LockManager.LockAndExecute(validConfigurations, CreateOrUpdateCoreConfigurations);
            ReportError(result);
        }

        private void CreateOrUpdateCoreConfigurations(ICollection<Configuration> configurations)
        {
            List<Net.Profiles.Parameter> coreParameters = new List<Net.Profiles.Parameter>();
            foreach (var configurationToAddOrUpdate in configurations.Where(x => !TraceDataPerItem.Keys.Contains(x.Id)))
            {
                if (!TryGetParameterWithChanges(configurationToAddOrUpdate, out var coreParameter))
                {
                    continue;
                }

                coreParameters.Add(coreParameter);
            }

            planApi.CoreHelpers.ProfileProvider.TryCreateOrUpdateParametersInBatches(coreParameters, out var result);

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

        private void Delete(ICollection<Configuration> apiConfigurations)
        {
            if (apiConfigurations == null)
            {
                throw new ArgumentNullException(nameof(apiConfigurations));
            }

            if (apiConfigurations.Count == 0)
            {
                return;
            }

            ValidateExistence(apiConfigurations);
            ValidateResourceStudioUsage(apiConfigurations);
            ValidateWorkflowUsage(apiConfigurations);

            var validConfigurationsToDelete = apiConfigurations.Where(IsValid).ToArray();
            var lockResult = planApi.LockManager.LockAndExecute(validConfigurationsToDelete, DeleteCoreConfigurations);
            ReportError(lockResult);
        }

        private void DeleteCoreConfigurations(ICollection<Configuration> configurations)
        {
            var coreConfigurationsToRemove = configurations.Select(x => x.CoreParameter);
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

        private void ValidateIdsNotInUse(ICollection<Configuration> apiConfigurations)
        {
            if (apiConfigurations == null)
            {
                throw new ArgumentNullException(nameof(apiConfigurations));
            }

            if (apiConfigurations.Count == 0)
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
                var error = new ConfigurationDuplicateIdError
                {
                    ErrorMessage = $"Configuration '{configuration.Name}' has a duplicate ID.",
                    Id = configuration.Id,
                };

                ReportError(configuration.Id, error);

                configurationsRequiringValidation.Remove(configuration);
            }

            foreach (var foundProfileParameter in planApi.CoreHelpers.ProfileProvider.GetParametersById(configurationsRequiringValidation.Select(x => x.Id)))
            {
                planApi.Logger.LogInformation(this, $"ID is already in use by a Profile Parameter.", foundProfileParameter.ID);

                var error = new ConfigurationIdInUseError
                {
                    ErrorMessage = "ID is already in use.",
                    Id = foundProfileParameter.ID,
                };

                ReportError(foundProfileParameter.ID, error);
            }
        }

        private void ValidateNames(ICollection<Configuration> apiConfigurations)
        {
            if (apiConfigurations == null)
            {
                throw new ArgumentNullException(nameof(apiConfigurations));
            }

            if (apiConfigurations.Count == 0)
            {
                return;
            }

            var configurationsRequiringValidation = apiConfigurations.ToList();

            foreach (var configuration in configurationsRequiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Name)))
            {
                var error = new ConfigurationInvalidNameError
                {
                    ErrorMessage = "Name cannot be empty.",
                    Id = configuration.Id,
                };

                ReportError(configuration.Id, error);
                configurationsRequiringValidation.Remove(configuration);
            }

            foreach (var configuration in configurationsRequiringValidation.Where(x => !InputValidator.HasValidTextLength(x.Name)))
            {
                var error = new ConfigurationInvalidNameError
                {
                    ErrorMessage = $"Name exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
                    Id = configuration.Id,
                    Name = configuration.Name,
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
                var error = new ConfigurationDuplicateNameError
                {
                    ErrorMessage = $"Configuration '{configuration.Name}' has a duplicate name.",
                    Id = configuration.Id,
                    Name = configuration.Name,
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

                planApi.Logger.LogInformation(this, $"Name '{configuration.Name}' is already in use by Profile Parameter(s) with ID(s)", coreParametersWithSameNameAndDifferentIds.Select(x => x.ID).ToArray());

                var error = new ConfigurationNameExistsError
                {
                    ErrorMessage = "Name is already in use.",
                    Id = configuration.Id,
                    Name = configuration.Name,
                };

                ReportError(configuration.Id, error);
            }
        }

        private void ValidateNumberConfigurations(ICollection<NumberConfiguration> apiConfigurations)
        {
            foreach (var apiConfiguration in apiConfigurations)
            {
                PassTraceData(NumberConfigurationValidator.Validate(apiConfiguration));
            }
        }

        private void ValidateTextConfigurations(ICollection<TextConfiguration> apiConfigurations)
        {
            foreach (var apiConfiguration in apiConfigurations)
            {
                PassTraceData(TextConfigurationValidator.Validate(apiConfiguration));
            }
        }

        private void ValidateDiscreteNumberConfigurations(ICollection<DiscreteNumberConfiguration> apiConfigurations)
        {
            foreach (var discreteNumberConfiguration in apiConfigurations)
            {
                PassTraceData(DiscreteNumberConfigurationValidator.Validate(discreteNumberConfiguration));
            }
        }

        private void ValidateDiscreteTextConfigurations(ICollection<DiscreteTextConfiguration> apiConfigurations)
        {
            foreach (var discreteTextConfiguration in apiConfigurations)
            {
                PassTraceData(DiscreteTextDiscreteConfigurationValidator.Validate(discreteTextConfiguration));
            }
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

        private void ValidateExistence(ICollection<Configuration> apiConfigurations)
        {
            foreach (var configuration in apiConfigurations.Where(x => x.IsNew))
            {
                var error = new ConfigurationInvalidStateError
                {
                    ErrorMessage = "Cannot delete a configuration that does not exist.",
                    Id = configuration.Id,
                };

                ReportError(configuration.Id, error);
            }
        }

        private void ValidateWorkflowUsage(ICollection<Configuration> configurations)
        {
            PassTraceData(SlcWorkflowParameterUsageValidator.Validate(planApi, configurations));
        }

        private void ValidateResourceStudioUsage(ICollection<Configuration> configurations)
        {
            PassTraceData(SlcResourceStudioParameterUsageValidator.Validate(planApi, configurations));
        }

        private Net.Profiles.Parameter GetNumberConfigurationWithChanges(NumberConfiguration apiConfiguration)
        {
            Net.Profiles.Parameter updatedParameter = apiConfiguration.IsNew ? new Net.Profiles.Parameter(apiConfiguration.Id) : new Net.Profiles.Parameter(apiConfiguration.CoreParameter);

            updatedParameter.Categories = Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Configuration;
            updatedParameter.Type = Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Number;
            updatedParameter.InterpreteType = new Skyline.DataMiner.Net.Profiles.InterpreteType
            {
                Type = Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Undefined,
                RawType = Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Undefined,
            };

            updatedParameter.Name = apiConfiguration.Name;
            updatedParameter.IsOptional = !apiConfiguration.IsMandatory;
            if (apiConfiguration.DefaultValue != null)
            {
                updatedParameter.DefaultValue = new Skyline.DataMiner.Net.Profiles.ParameterValue
                {
                    Type = Skyline.DataMiner.Net.Profiles.ParameterValue.ValueType.Double,
                    DoubleValue = (double)apiConfiguration.DefaultValue
                };
            }
            else
            {
                updatedParameter.DefaultValue = null;
            }

            updatedParameter.RangeMin = apiConfiguration.RangeMin.HasValue ? (double)apiConfiguration.RangeMin.Value : double.NaN;
            updatedParameter.RangeMax = apiConfiguration.RangeMax.HasValue ? (double)apiConfiguration.RangeMax.Value : double.NaN;
            updatedParameter.Stepsize = apiConfiguration.StepSize.HasValue ? (double)apiConfiguration.StepSize.Value : double.NaN;
            updatedParameter.Units = apiConfiguration.Units;
            updatedParameter.Decimals = apiConfiguration.Decimals ?? int.MaxValue;

            updatedParameter.Discretes = new List<string>(); // Clear discretes if any.
            updatedParameter.DiscreetDisplayValues = new List<string>();

            return updatedParameter;
        }

        private Net.Profiles.Parameter GetTextConfigurationWithChanges(TextConfiguration apiConfiguration)
        {
            Net.Profiles.Parameter updatedParameter = apiConfiguration.IsNew ? new Net.Profiles.Parameter(apiConfiguration.Id) : new Net.Profiles.Parameter(apiConfiguration.CoreParameter);

            updatedParameter.Categories = Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Configuration;
            updatedParameter.Type = Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Text;
            updatedParameter.InterpreteType = new Skyline.DataMiner.Net.Profiles.InterpreteType
            {
                Type = Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Undefined,
                RawType = Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Undefined,
            };

            updatedParameter.Name = apiConfiguration.Name;
            updatedParameter.IsOptional = !apiConfiguration.IsMandatory;
            if (apiConfiguration.DefaultValue != null)
            {
                updatedParameter.DefaultValue = new Skyline.DataMiner.Net.Profiles.ParameterValue
                {
                    Type = Skyline.DataMiner.Net.Profiles.ParameterValue.ValueType.String,
                    StringValue = apiConfiguration.DefaultValue
                };
            }
            else
            {
                updatedParameter.DefaultValue = null;
            }

            updatedParameter.RangeMin = double.NaN;
            updatedParameter.RangeMax = double.NaN;
            updatedParameter.Stepsize = double.NaN;
            updatedParameter.Units = null;
            updatedParameter.Decimals = int.MaxValue;

            updatedParameter.Discretes = new List<string>(); // Clear discretes if any.
            updatedParameter.DiscreetDisplayValues = new List<string>();

            return updatedParameter;
        }

        private Net.Profiles.Parameter GetParameterWithChanges(DiscreteNumberConfiguration apiConfiguration)
        {
            Net.Profiles.Parameter updatedParameter = apiConfiguration.IsNew ? new Net.Profiles.Parameter(apiConfiguration.Id) : new Net.Profiles.Parameter(apiConfiguration.CoreParameter);

            updatedParameter.Categories = Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Configuration;
            updatedParameter.Type = Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Discrete;
            updatedParameter.InterpreteType = new Skyline.DataMiner.Net.Profiles.InterpreteType
            {
                Type = Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.Double,
                RawType = Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.NumericText,
            };

            updatedParameter.Name = apiConfiguration.Name;
            updatedParameter.IsOptional = !apiConfiguration.IsMandatory;
            if (apiConfiguration.DefaultValue != null)
            {
                updatedParameter.DefaultValue = new Skyline.DataMiner.Net.Profiles.ParameterValue
                {
                    Type = Skyline.DataMiner.Net.Profiles.ParameterValue.ValueType.String,
                    StringValue = Convert.ToString(apiConfiguration.DefaultValue.Value, CultureInfo.InvariantCulture)
                };
            }
            else
            {
                updatedParameter.DefaultValue = null;
            }

            updatedParameter.RangeMin = double.NaN;
            updatedParameter.RangeMax = double.NaN;
            updatedParameter.Stepsize = double.NaN;
            updatedParameter.Units = null;
            updatedParameter.Decimals = int.MaxValue;

            updatedParameter.Discretes = apiConfiguration.Discretes.Select(x => Convert.ToString((double)x.Value)).ToList();
            updatedParameter.DiscreetDisplayValues = apiConfiguration.Discretes.Select(x => x.DisplayName).ToList();

            return updatedParameter;
        }

        private Net.Profiles.Parameter GetParameterWithChanges(DiscreteTextConfiguration apiConfiguration)
        {
            Net.Profiles.Parameter updatedParameter = apiConfiguration.IsNew ? new Net.Profiles.Parameter(apiConfiguration.Id) : new Net.Profiles.Parameter(apiConfiguration.CoreParameter);

            updatedParameter.Categories = Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Configuration;
            updatedParameter.Type = Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Discrete;
            updatedParameter.InterpreteType = new Skyline.DataMiner.Net.Profiles.InterpreteType
            {
                Type = Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.String,
                RawType = Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Other,
            };

            updatedParameter.Name = apiConfiguration.Name;
            updatedParameter.IsOptional = !apiConfiguration.IsMandatory;
            if (apiConfiguration.DefaultValue != null)
            {
                updatedParameter.DefaultValue = new Skyline.DataMiner.Net.Profiles.ParameterValue
                {
                    Type = Skyline.DataMiner.Net.Profiles.ParameterValue.ValueType.String,
                    StringValue = apiConfiguration.DefaultValue.Value
                };
            }
            else
            {
                updatedParameter.DefaultValue = null;
            }

            updatedParameter.RangeMin = double.NaN;
            updatedParameter.RangeMax = double.NaN;
            updatedParameter.Stepsize = double.NaN;
            updatedParameter.Units = null;
            updatedParameter.Decimals = int.MaxValue;

            updatedParameter.Discretes = apiConfiguration.Discretes.Select(x => x.Value).ToList();
            updatedParameter.DiscreetDisplayValues = apiConfiguration.Discretes.Select(x => x.DisplayName).ToList();

            return updatedParameter;
        }
    }
}
