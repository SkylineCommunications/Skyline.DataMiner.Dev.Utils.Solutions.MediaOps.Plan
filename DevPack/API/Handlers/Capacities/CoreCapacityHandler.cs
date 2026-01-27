namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

    using CoreParameter = Net.Profiles.Parameter;

    internal class CoreCapacityHandler : ApiObjectValidator
    {
        private readonly MediaOpsPlanApi planApi;

        private CoreCapacityHandler(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<Capacity> apiCapacities, out BulkOperationResult<Guid> result)
        {
            var handler = new CoreCapacityHandler(planApi);
            handler.CreateOrUpdate(apiCapacities);

            result = new BulkOperationResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures;
        }

        internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<Capacity> apiCapacities, out BulkOperationResult<Guid> result)
        {
            var handler = new CoreCapacityHandler(planApi);
            handler.Delete(apiCapacities);

            result = new BulkOperationResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures;
        }

        private void CreateOrUpdate(ICollection<Capacity> apiCapacities)
        {
            if (apiCapacities == null)
            {
                throw new ArgumentNullException(nameof(apiCapacities));
            }

            if (apiCapacities.Count == 0)
            {
                return;
            }

            var toCreate = new List<Capacity>();
            var toUpdate = new List<Capacity>();
            foreach (var capacity in apiCapacities)
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
            ValidateNames(apiCapacities);
            ValidateRangeSettings(apiCapacities);
            ValidateDecimals(apiCapacities);

            var validCapacities = apiCapacities.Where(IsValid).ToList();
            var lockResult = planApi.LockManager.LockAndExecute(validCapacities, CreateOrUpdateCoreCapacities);
            ReportError(lockResult);
        }

        private void CreateOrUpdateCoreCapacities(ICollection<Capacity> capacities)
        {
            if (capacities == null)
            {
                throw new ArgumentNullException(nameof(capacities));
            }

            if (capacities.Count == 0)
            {
                return;
            }

            var coreCapacities = capacities.Select(x => x.GetParameterWithChanges()).ToList();
            planApi.CoreHelpers.ProfileProvider.TryCreateOrUpdateParametersInBatches(coreCapacities, out var result);

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

        private void Delete(ICollection<Capacity> apiCapacities)
        {
            if (apiCapacities == null)
            {
                throw new ArgumentNullException(nameof(apiCapacities));
            }

            if (apiCapacities.Count == 0)
            {
                return;
            }

            ValidateExistence(apiCapacities);
            ValidateResourceStudioUsage(apiCapacities);
            ValidateWorkflowUsage(apiCapacities);

            var capacitiesToDelete = apiCapacities.Where(IsValid).ToList();
            var lockResult = planApi.LockManager.LockAndExecute(capacitiesToDelete, DeleteCoreCapacities);
            ReportError(lockResult);
        }

        private void DeleteCoreCapacities(ICollection<Capacity> capacitiesToDelete)
        {
            var coreCapacitiesToDelete = capacitiesToDelete.Select(x => x.CoreParameter);
            planApi.CoreHelpers.ProfileProvider.TryDeleteParametersInBatches(coreCapacitiesToDelete, out var result);

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

        private void ValidateIdsNotInUse(ICollection<Capacity> apiCapacities)
        {
            if (apiCapacities == null)
            {
                throw new ArgumentNullException(nameof(apiCapacities));
            }

            if (apiCapacities.Count == 0)
            {
                return;
            }

            var capacitiesRequiringValidation = apiCapacities.Where(x => x.IsNew && x.HasUserDefinedId).ToList();
            if (capacitiesRequiringValidation.Count == 0)
            {
                return;
            }

            var capacitiesWithDuplicateIds = capacitiesRequiringValidation
                .GroupBy(resource => resource.Id)
                .Where(g => g.Count() > 1)
                .SelectMany(x => x)
                .ToList();

            foreach (var capacity in capacitiesWithDuplicateIds)
            {
                var error = new CapacityDuplicateIdError
                {
                    ErrorMessage = $"Capacity '{capacity.Name}' has a duplicate ID.",
                    Id = capacity.Id,
                };

                ReportError(capacity.Id, error);

                capacitiesRequiringValidation.Remove(capacity);
            }

            foreach (var foundProfileParameter in planApi.CoreHelpers.ProfileProvider.GetParametersById(capacitiesRequiringValidation.Select(x => x.Id)))
            {
                planApi.Logger.LogInformation(this, $"ID is already in use by a Profile Parameter.", foundProfileParameter.ID);

                var error = new CapacityIdInUseError
                {
                    ErrorMessage = "ID is already in use.",
                    Id = foundProfileParameter.ID,
                };

                ReportError(foundProfileParameter.ID, error);
            }
        }

        private void ValidateNames(ICollection<Capacity> apiCapacities)
        {
            if (apiCapacities == null)
            {
                throw new ArgumentNullException(nameof(apiCapacities));
            }

            if (apiCapacities.Count == 0)
            {
                return;
            }

            var capacitiesRequiringValidation = apiCapacities.ToList();

            foreach (var capacity in capacitiesRequiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Name)).ToArray())
            {
                if (string.IsNullOrWhiteSpace(capacity.Name))
                {
                    var error = new CapacityInvalidNameError
                    {
                        ErrorMessage = "Name cannot be empty.",
                        Id = capacity.Id,
                    };

                    ReportError(capacity.Id, error);
                    capacitiesRequiringValidation.Remove(capacity);
                }
            }

            foreach (var capacity in capacitiesRequiringValidation.Where(x => !InputValidator.HasValidTextLength(x.Name)).ToArray())
            {
                if (string.IsNullOrWhiteSpace(capacity.Name))
                {
                    var error = new CapacityInvalidNameError
                    {
                        ErrorMessage = $"Name exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
                        Id = capacity.Id,
                        Name = capacity.Name,
                    };

                    ReportError(capacity.Id, error);
                    capacitiesRequiringValidation.Remove(capacity);
                }
            }

            var capacitiesWithDuplicateNames = capacitiesRequiringValidation
                .GroupBy(capability => capability.Name)
                .Where(g => g.Count() > 1)
                .SelectMany(x => x)
                .ToList();

            foreach (var capacity in capacitiesWithDuplicateNames)
            {
                var error = new CapacityDuplicateNameError
                {
                    ErrorMessage = $"Capacity '{capacity.Name}' has a duplicate name.",
                    Id = capacity.Id,
                    Name = capacity.Name,
                };

                ReportError(capacity.Id, error);
                capacitiesRequiringValidation.Remove(capacity);
            }

            var coreParametersByName = planApi.CoreHelpers.ProfileProvider.GetParametersByName(capacitiesRequiringValidation.Select(x => x.Name))
                .GroupBy(x => x.Name)
                .ToDictionary(x => x.Key, x => (IReadOnlyCollection<CoreParameter>)x.ToList());

            foreach (var capacity in capacitiesRequiringValidation)
            {
                if (!coreParametersByName.TryGetValue(capacity.Name, out var coreParameters))
                {
                    continue;
                }

                var existingParameters = coreParameters.Where(x => x.ID != capacity.Id).ToList();
                if (existingParameters.Count == 0)
                {
                    continue;
                }

                planApi.Logger.LogInformation(this, $"Name '{capacity.Name}' is already in use by Profile Parameter(s) with ID(s)", existingParameters.Select(x => x.ID).ToArray());

                var error = new CapacityNameExistsError
                {
                    ErrorMessage = "Name is already in use.",
                    Id = capacity.Id,
                    Name = capacity.Name,
                };

                ReportError(capacity.Id, error);
            }
        }

        private void ValidateRangeSettings(ICollection<Capacity> apiCapacities)
        {
            if (apiCapacities == null)
            {
                throw new ArgumentNullException(nameof(apiCapacities));
            }

            if (apiCapacities.Count == 0)
            {
                return;
            }

            foreach (var capacity in apiCapacities)
            {
                if (capacity.RangeMin.HasValue && capacity.RangeMax.HasValue
                    && capacity.RangeMax <= capacity.RangeMin)
                {
                    var error = new CapacityInvalidRangeError
                    {
                        ErrorMessage = "RangeMax must be greater than RangeMin.",
                        Id = capacity.Id,
                        RangeMin = capacity.RangeMin.Value,
                        RangeMax = capacity.RangeMax.Value,
                    };

                    ReportError(capacity.Id, error);
                }

                if (capacity.StepSize.HasValue && capacity.StepSize <= 0)
                {
                    var error = new CapacityInvalidStepSizeError
                    {
                        ErrorMessage = "StepSize must be greater than 0.",
                        Id = capacity.Id,
                        StepSize = capacity.StepSize.Value,
                    };

                    ReportError(capacity.Id, error);
                }
            }
        }

        private void ValidateDecimals(ICollection<Capacity> apiCapacities)
        {
            if (apiCapacities == null)
            {
                throw new ArgumentNullException(nameof(apiCapacities));
            }

            if (apiCapacities.Count == 0)
            {
                return;
            }

            foreach (var capacity in apiCapacities.Where(x => x.Decimals.HasValue))
            {
                if (capacity.Decimals < 0 || capacity.Decimals > 15)
                {
                    var error = new CapacityInvalidDecimalsError
                    {
                        ErrorMessage = "Decimals must be between 0 and 15.",
                        Id = capacity.Id,
                        Decimals = capacity.Decimals.Value,
                    };

                    ReportError(capacity.Id, error);
                    continue;
                }

                if (capacity.RangeMin.HasValue && (Math.Round(capacity.RangeMin.Value, capacity.Decimals.Value) - capacity.RangeMin.Value) != 0)
                {
                    var error = new CapacityInvalidRangeMinError
                    {
                        ErrorMessage = $"RangeMin has more decimal places than allowed by Decimals ({capacity.Decimals}).",
                        Id = capacity.Id,
                        RangeMin = capacity.RangeMin.Value,
                    };

                    ReportError(capacity.Id, error);
                }

                if (capacity.RangeMax.HasValue && (Math.Round(capacity.RangeMax.Value, capacity.Decimals.Value) - capacity.RangeMax.Value) != 0)
                {
                    var error = new CapacityInvalidRangeMaxError
                    {
                        ErrorMessage = $"RangeMax has more decimal places than allowed by Decimals ({capacity.Decimals}).",
                        Id = capacity.Id,
                        RangeMax = capacity.RangeMax.Value,
                    };

                    ReportError(capacity.Id, error);
                }

                if (capacity.StepSize.HasValue && (Math.Round(capacity.StepSize.Value, capacity.Decimals.Value) - capacity.StepSize.Value) != 0)
                {
                    var error = new CapacityInvalidStepSizeError
                    {
                        ErrorMessage = $"StepSize has more decimal places than allowed by Decimals ({capacity.Decimals}).",
                        Id = capacity.Id,
                        StepSize = capacity.StepSize.Value,
                    };

                    ReportError(capacity.Id, error);
                }
            }
        }

        private void ValidateExistence(ICollection<Capacity> capacities)
        {
            var newCapabilities = capacities.Where(x => x.IsNew).ToList();
            newCapabilities.ForEach(x =>
            {
                var error = new CapacityInvalidStateError
                {
                    ErrorMessage = $"A capacity that was not saved cannot be removed.",
                    Id = x.Id,
                };

                ReportError(x.Id, error);
            });
        }

        private void ValidateWorkflowUsage(ICollection<Capacity> capacities)
        {
            PassTraceData(SlcWorkflowParameterUsageValidator.Validate(planApi, capacities));
        }

        private void ValidateResourceStudioUsage(ICollection<Capacity> capacities)
        {
            PassTraceData(SlcResourceStudioParameterUsageValidator.Validate(planApi, capacities));
        }
    }
}
