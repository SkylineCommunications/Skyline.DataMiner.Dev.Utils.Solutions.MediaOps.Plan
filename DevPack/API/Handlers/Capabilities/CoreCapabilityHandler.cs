namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

    using CoreParameter = Net.Profiles.Parameter;

    internal class CoreCapabilityHandler : ParameterApiObjectValidator
    {
        private readonly MediaOpsPlanApi planApi;

        private CoreCapabilityHandler(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<Capability> apiCapabilities, out ParameterBulkOperationResult result)
        {
            var handler = new CoreCapabilityHandler(planApi);
            handler.CreateOrUpdate(apiCapabilities);

            result = new ParameterBulkOperationResult(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures;
        }

        internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<Capability> apiCapabilities, out ParameterBulkOperationResult result)
        {
            var handler = new CoreCapabilityHandler(planApi);
            handler.Delete(apiCapabilities);

            result = new ParameterBulkOperationResult(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures;
        }

        private void CreateOrUpdate(ICollection<Capability> apiCapabilities)
        {
            if (apiCapabilities == null)
            {
                throw new ArgumentNullException(nameof(apiCapabilities));
            }

            if (apiCapabilities.Count == 0)
            {
                return;
            }

            var toCreate = new List<Capability>();
            var toUpdate = new List<Capability>();
            foreach (var capability in apiCapabilities)
            {
                if (capability.IsNew)
                {
                    toCreate.Add(capability);
                }
                else
                {
                    toUpdate.Add(capability);
                }
            }

            ValidateIdsNotInUse(toCreate);
            ValidateNames(apiCapabilities);
            ValidateDiscretes(apiCapabilities);
            ValidateDiscreteValueChanges(toUpdate.Where(IsValid).ToArray());
            ValidateTimeDependency(toUpdate);

            var validCapabilities = apiCapabilities.Where(IsValid).ToList();
            var lockResult = planApi.LockManager.LockAndExecute(validCapabilities, CreateOrUpdateCoreCapabilities);
            ReportError(lockResult);
        }

        private void CreateOrUpdateCoreCapabilities(ICollection<Capability> capabilities)
        {
            var toCreate = capabilities.Where(x => x.IsNew).ToList();
            var toUpdate = capabilities.Except(toCreate).ToList();

            List<CoreParameter> coreParametersToCreate = new List<CoreParameter>();
            foreach (var capabilityToCreate in toCreate.Where(x => !TraceDataPerItem.Keys.Contains(x.Id)))
            {
                if (capabilityToCreate.IsTimeDependent)
                {
                    var timeDependentCapability = CreateLinkedTimeDependentCapability(capabilityToCreate);
                    coreParametersToCreate.Add(CreateOrUpdateCoreParameter(capabilityToCreate, timeDependentCapability.ID));
                    coreParametersToCreate.Add(timeDependentCapability);
                }
                else
                {
                    coreParametersToCreate.Add(CreateOrUpdateCoreParameter(capabilityToCreate, null));
                }
            }

            var coreParametersToUpdate = toUpdate
                .Select(x => CreateOrUpdateCoreParameter(x, null))
                .ToList();

            CreateOrUpdateCoreParameters(coreParametersToCreate.Concat(coreParametersToUpdate).ToList());
        }

        private void CreateOrUpdateCoreParameters(ICollection<CoreParameter> coreParameters)
        {
            if (coreParameters == null)
            {
                throw new ArgumentNullException(nameof(coreParameters));
            }

            if (coreParameters.Count == 0)
            {
                return;
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

            ReportSuccess(result.SuccessfulItems);
        }

        private void Delete(ICollection<Capability> apiCapabilities)
        {
            if (apiCapabilities == null)
            {
                throw new ArgumentNullException(nameof(apiCapabilities));
            }

            if (apiCapabilities.Count == 0)
            {
                return;
            }

            ValidateExistence(apiCapabilities);
            ValidateResourceStudioUsage(apiCapabilities);
            ValidateWorkflowUsage(apiCapabilities);

            var validCapabilitiesToDelete = apiCapabilities.Where(IsValid).ToList();
            var lockResult = planApi.LockManager.LockAndExecute(validCapabilitiesToDelete, DeleteCoreCapabilities);
            ReportError(lockResult);
        }

        private void DeleteCoreCapabilities(ICollection<Capability> capabilitiesToDelete)
        {
            var coreCapabilitiesToDelete = capabilitiesToDelete.Select(x => x.CoreParameter).ToList();

            var linkedTimeDependentCapabilityIds = capabilitiesToDelete.Where(x => x.IsTimeDependent).Select(x => x.LinkedTimeDependentCapabilityId);
            if (linkedTimeDependentCapabilityIds.Any())
            {
                coreCapabilitiesToDelete.AddRange(planApi.CoreHelpers.ProfileProvider.GetParametersById(linkedTimeDependentCapabilityIds));
            }

            planApi.CoreHelpers.ProfileProvider.TryDeleteParametersInBatches(coreCapabilitiesToDelete, out var result);

            foreach (var id in result.UnsuccessfulIds)
            {
                ReportError(id);
                if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
                {
                    PassTraceData(id, traceData);
                }
            }

            ReportSuccess(result.SuccessfulItems);
        }

        private void ValidateIdsNotInUse(ICollection<Capability> apiCapabilities)
        {
            if (apiCapabilities == null)
            {
                throw new ArgumentNullException(nameof(apiCapabilities));
            }

            if (apiCapabilities.Count == 0)
            {
                return;
            }

            var capabilitiesRequiringValidation = apiCapabilities.Where(x => x.IsNew && x.HasUserDefinedId).ToList();
            if (capabilitiesRequiringValidation.Count == 0)
            {
                return;
            }

            var capabilitiesWithDuplicateIds = capabilitiesRequiringValidation
                .GroupBy(capability => capability.Id)
                .Where(g => g.Count() > 1)
                .SelectMany(x => x)
                .ToList();

            foreach (var capability in capabilitiesWithDuplicateIds)
            {
                var error = new CapabilityDuplicateIdError
                {
                    ErrorMessage = $"Capability '{capability.Name}' has a duplicate ID.",
                    Id = capability.Id,
                };

                ReportError(capability.Id, error);

                capabilitiesRequiringValidation.Remove(capability);
            }

            foreach (var foundProfileParameter in planApi.CoreHelpers.ProfileProvider.GetParametersById(capabilitiesRequiringValidation.Select(x => x.Id)))
            {
                planApi.Logger.Information(this, $"ID is already in use by a Profile Parameter.", [foundProfileParameter.ID]);

                var error = new CapabilityIdInUseError
                {
                    ErrorMessage = "ID is already in use.",
                    Id = foundProfileParameter.ID,
                };

                ReportError(foundProfileParameter.ID, error);
            }
        }

        private void ValidateNames(ICollection<Capability> apiCapabilities)
        {
            if (apiCapabilities == null)
            {
                throw new ArgumentNullException(nameof(apiCapabilities));
            }

            if (apiCapabilities.Count == 0)
            {
                return;
            }

            var capabilitiesRequiringValidation = apiCapabilities.ToList();

            foreach (var capability in capabilitiesRequiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Name)).ToArray())
            {
                var error = new CapabilityInvalidNameError
                {
                    ErrorMessage = "Name cannot be empty.",
                    Id = capability.Id,
                };

                ReportError(capability.Id, error);
                capabilitiesRequiringValidation.Remove(capability);
            }

            foreach (var capability in capabilitiesRequiringValidation.Where(x => !InputValidator.HasValidTextLength(x.Name)).ToArray())
            {
                var error = new CapabilityInvalidNameError
                {
                    ErrorMessage = $"Name exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
                    Id = capability.Id,
                    Name = capability.Name,
                };

                ReportError(capability.Id, error);
                capabilitiesRequiringValidation.Remove(capability);
            }

            var capabilitiesWithDuplicateNames = capabilitiesRequiringValidation
                .GroupBy(capability => capability.Name)
                .Where(g => g.Count() > 1)
                .SelectMany(x => x)
                .ToList();

            foreach (var capability in capabilitiesWithDuplicateNames)
            {
                var error = new CapabilityDuplicateNameError
                {
                    ErrorMessage = $"Capability '{capability.Name}' has a duplicate name.",
                    Id = capability.Id,
                    Name = capability.Name,
                };

                ReportError(capability.Id, error);
                capabilitiesRequiringValidation.Remove(capability);
            }

            var coreParameters = planApi.CoreHelpers.ProfileProvider.GetParametersByName(capabilitiesRequiringValidation.Select(x => x.Name));
            foreach (var capability in capabilitiesRequiringValidation)
            {
                var coreParametersWithSameName = coreParameters.Where(x => x.Name.Equals(capability.Name));
                if (!coreParametersWithSameName.Any())
                {
                    continue;
                }

                var coreParametersWithSameNameAndDifferentIds = coreParametersWithSameName.Where(x => x.ID != capability.Id).ToList();
                if (coreParametersWithSameNameAndDifferentIds.Count == 0)
                {
                    continue;
                }

                planApi.Logger.Information(this, $"Name '{capability.Name}' is already in use by Profile Parameter(s) with ID(s)", [coreParametersWithSameNameAndDifferentIds.Select(x => x.ID).ToArray()]);

                var error = new CapabilityNameExistsError
                {
                    ErrorMessage = "Name is already in use.",
                    Id = capability.Id,
                    Name = capability.Name,
                };

                ReportError(capability.Id, error);
            }
        }

        private void ValidateDiscretes(ICollection<Capability> apiCapabilities)
        {
            foreach (var capability in apiCapabilities)
            {
                if (capability.Discretes.Count == 0)
                {
                    ReportError(capability.Id, new CapabilityNoDiscretesError
                    {
                        ErrorMessage = "Empty discretes list is not allowed.",
                        Id = capability.Id,
                    });
                }
                else
                {
                    var duplicateDiscretes = capability.Discretes
                        .GroupBy(x => x.Trim())
                        .Where(g => g.Count() > 1)
                        .SelectMany(g => g)
                        .ToList();

                    if (duplicateDiscretes.Count != 0)
                    {
                        ReportError(capability.Id, new CapabilityDuplicateDiscretesError
                        {
                            ErrorMessage = $"The capability defines the following duplicate discretes: {String.Join(", ", duplicateDiscretes)}.",
                            Id = capability.Id,
                            Discretes = duplicateDiscretes,
                        });
                    }
                }
            }
        }

        private void ValidateDiscreteValueChanges(ICollection<Capability> apiCapabilities)
        {
            if (apiCapabilities == null)
            {
                throw new ArgumentNullException(nameof(apiCapabilities));
            }

            if (!apiCapabilities.Any())
            {
                return;
            }

            var capabilityDiscreteValuesToVerify = apiCapabilities
                .Select(x => new { ParameterId = x.Id, RemovedDiscretes = x.CoreParameter.Discretes.Except(x.Discretes).ToList() })
                .Where(x => x.RemovedDiscretes.Any())
                .SelectMany(x => x.RemovedDiscretes.Select(y => new ParameterDiscreteValue<string>
                {
                    ParameterId = x.ParameterId,
                    DiscreteValue = y,
                }))
                .ToList();

            PassTraceData(SlcResourceStudioParameterDiscreteValueUsageValidator.Validate(planApi, capabilityDiscreteValuesToVerify));
            PassTraceData(SlcWorkflowParameterDiscreteValueUsageValidator.Validate(planApi, capabilityDiscreteValuesToVerify));
        }

        private void ValidateTimeDependency(ICollection<Capability> apiCapabilities)
        {
            foreach (var capability in apiCapabilities)
            {
                if (capability.IsNew)
                    continue;

                if (capability.IsTimeDependent == capability.CoreParameter.IsTimeDependent())
                    continue;

                ReportError(capability.Id, new CapabilityInvalidTimeDependencyError
                {
                    ErrorMessage = "Changing the time dependency of a capability is not allowed.",
                    Id = capability.Id,
                });
            }
        }

        private void ValidateExistence(ICollection<Capability> capabilities)
        {
            var newCapabilities = capabilities.Where(x => x.IsNew).ToList();
            newCapabilities.ForEach(x =>
            {
                var error = new CapabilityInvalidStateError
                {
                    ErrorMessage = $"A capability that was not saved cannot be removed.",
                    Id = x.Id,
                };

                ReportError(x.Id, error);
            });
        }

        private void ValidateWorkflowUsage(ICollection<Capability> capabilities)
        {
            PassTraceData(SlcWorkflowParameterUsageValidator.Validate(planApi, capabilities));
        }

        private void ValidateResourceStudioUsage(ICollection<Capability> capabilities)
        {
            PassTraceData(SlcResourceStudioParameterUsageValidator.Validate(planApi, capabilities));
        }

        private CoreParameter CreateOrUpdateCoreParameter(Capability capability, Guid? linkedTimedependantCapabilityId)
        {
            if (capability.IsNew)
            {
                if (capability.IsTimeDependent && linkedTimedependantCapabilityId == null)
                {
                    throw new InvalidOperationException("A linked time-dependent capability ID must be provided for time-dependent capabilities.");
                }

                var sortedDiscretes = GetSortedDiscretes(capability);

                return new CoreParameter
                {
                    ID = capability.Id,
                    Name = capability.Name,
                    Remarks = capability.IsTimeDependent ? new TimeDependentCapabilityLink { IsTimeDependent = true, LinkedParameterId = linkedTimedependantCapabilityId.Value }.Serialize() : String.Empty,
                    Categories = Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Capability,
                    IsOptional = !capability.IsMandatory,
                    Discretes = sortedDiscretes,
                    DiscreetDisplayValues = sortedDiscretes,
                    Type = Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Discrete,
                    InterpreteType = new Net.Profiles.InterpreteType
                    {
                        Type = Skyline.DataMiner.Net.Profiles.InterpreteType.TypeEnum.String,
                        RawType = Skyline.DataMiner.Net.Profiles.InterpreteType.RawTypeEnum.Other,
                    },
                };
            }
            else
            {
                var sortedDiscretes = GetSortedDiscretes(capability);

                capability.CoreParameter.IsOptional = !capability.IsMandatory;
                capability.CoreParameter.Discretes = sortedDiscretes;
                capability.CoreParameter.DiscreetDisplayValues = sortedDiscretes;

                return capability.CoreParameter;
            }
        }

        private CoreParameter CreateLinkedTimeDependentCapability(Capability capability)
        {
            return new CoreParameter
            {
                ID = Guid.NewGuid(),
                Name = $"{capability.Name} - Time dependent",
                Remarks = String.Empty,
                Categories = Skyline.DataMiner.Net.Profiles.ProfileParameterCategory.Capability,
                IsOptional = !capability.IsMandatory,
                Type = Skyline.DataMiner.Net.Profiles.Parameter.ParameterType.Text,
            };
        }

        private List<string> GetSortedDiscretes(Capability capability)
        {
            var uniqueDiscretes = GetCleanInputDiscretes(capability.Discretes);
            NaturalSortComparer comparer = new NaturalSortComparer();
            uniqueDiscretes.Sort((x, y) => comparer.Compare(x, y));
            return uniqueDiscretes;
        }

        private List<string> GetCleanInputDiscretes(IReadOnlyCollection<string> discretes)
        {
            return discretes.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList();
        }
    }
}
