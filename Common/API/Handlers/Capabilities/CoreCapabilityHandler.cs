namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.MediaOps.Plan.Storage.Core;
    using Skyline.DataMiner.Net;

    using CoreParameter = Net.Profiles.Parameter;

    internal class CoreCapabilityHandler : ApiObjectValidator<Guid>
    {
        private readonly MediaOpsPlanApi planApi;

        private CoreCapabilityHandler(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, IEnumerable<Capability> apiCapabilities, out BulkCreateOrUpdateResult<Guid> result)
        {
            var handler = new CoreCapabilityHandler(planApi);
            handler.CreateOrUpdate(apiCapabilities);

            result = new BulkCreateOrUpdateResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures();
        }

        internal static bool TryDelete(MediaOpsPlanApi planApi, IEnumerable<Capability> apiCapabilities, out BulkDeleteResult<Guid> result)
        {
            var handler = new CoreCapabilityHandler(planApi);
            handler.Delete(apiCapabilities);

            result = new BulkDeleteResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures();
        }

        private void CreateOrUpdate(IEnumerable<Capability> apiCapabilities)
        {
            if (apiCapabilities == null)
            {
                throw new ArgumentNullException(nameof(apiCapabilities));
            }

            if (!apiCapabilities.Any())
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
            ValidateTimeDependency(toUpdate);

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
                .Where(x => !TraceDataPerItem.Keys.Contains(x.Id))
                .Select(x => CreateOrUpdateCoreParameter(x, null))
                .ToList();

            CreateOrUpdate(coreParametersToCreate.Concat(coreParametersToUpdate));
        }

        private void CreateOrUpdate(IEnumerable<CoreParameter> coreParameters)
        {
            if (coreParameters == null)
            {
                throw new ArgumentNullException(nameof(coreParameters));
            }

            if (!coreParameters.Any())
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

            ReportSuccess(result.SuccessfulIds);
        }

        private void ValidateTimeDependency(List<Capability> apiCapabilities)
        {
            foreach (var capability in apiCapabilities)
            {
                if (capability.IsNew)
                    continue;

                if (capability.IsTimeDependent == capability.CoreParameter.IsTimeDependent())
                    continue;

                ReportError(capability.Id, new CapabilityConfigurationError
                {
                    ErrorReason = CapabilityConfigurationError.Reason.InvalidTimeDependency,
                    ErrorMessage = "Changing the time dependency of a capability is not allowed.",
                });
            }
        }

        private void Delete(IEnumerable<Capability> apiCapabilities)
        {
            if (apiCapabilities == null)
            {
                throw new ArgumentNullException(nameof(apiCapabilities));
            }

            if (!apiCapabilities.Any())
            {
                return;
            }

            var newCapabilities = apiCapabilities.Where(x => x.IsNew).ToList();
            newCapabilities.ForEach(x =>
            {
                var error = new CapabilityConfigurationError
                {
                    ErrorReason = CapabilityConfigurationError.Reason.InvalidState,
                    ErrorMessage = $"A capability that was not saved cannot be removed.",
                };

                ReportError(x.Id, error);
            });

            var capabilitiesToDelete = apiCapabilities.Except(newCapabilities).ToList();

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

            ReportSuccess(result.SuccessfulIds);
        }

        private void ValidateIdsNotInUse(IEnumerable<Capability> apiCapabilities)
        {
            if (apiCapabilities == null)
            {
                throw new ArgumentNullException(nameof(apiCapabilities));
            }

            if (!apiCapabilities.Any())
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
                var error = new CapabilityConfigurationError
                {
                    ErrorReason = CapabilityConfigurationError.Reason.DuplicateId,
                    ErrorMessage = $"Capability '{capability.Name}' has a duplicate ID.",
                };

                ReportError(capability.Id, error);

                capabilitiesRequiringValidation.Remove(capability);
            }

            foreach (var foundProfileParameter in planApi.CoreHelpers.ProfileProvider.GetParametersById(capabilitiesRequiringValidation.Select(x => x.Id)))
            {
                planApi.Logger.LogInformation($"ID is already in use by a Profile Parameter.", foundProfileParameter.ID);

                var error = new CapabilityConfigurationError
                {
                    ErrorReason = CapabilityConfigurationError.Reason.IdInUse,
                    ErrorMessage = "ID is already in use.",
                };

                ReportError(foundProfileParameter.ID, error);
            }
        }

        private void ValidateNames(IEnumerable<Capability> apiCapabilities)
        {
            if (apiCapabilities == null)
            {
                throw new ArgumentNullException(nameof(apiCapabilities));
            }

            if (!apiCapabilities.Any())
            {
                return;
            }

            var capabilitiesRequiringValidation = apiCapabilities.ToList();

            foreach (var capability in capabilitiesRequiringValidation.Where(x => !InputValidator.ValidateEmptyText(x.Name)))
            {
                var error = new CapabilityConfigurationError
                {
                    ErrorReason = CapabilityConfigurationError.Reason.InvalidName,
                    ErrorMessage = "Name cannot be empty.",
                };

                ReportError(capability.Id, error);
                capabilitiesRequiringValidation.Remove(capability);
            }

            foreach (var capability in capabilitiesRequiringValidation.Where(x => !InputValidator.ValidateTextLength(x.Name)))
            {
                var error = new CapabilityConfigurationError
                {
                    ErrorReason = CapabilityConfigurationError.Reason.InvalidName,
                    ErrorMessage = $"Name exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
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
                var error = new CapabilityConfigurationError
                {
                    ErrorReason = CapabilityConfigurationError.Reason.DuplicateName,
                    ErrorMessage = $"Capability '{capability.Name}' has a duplicate name.",
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

                planApi.Logger.LogInformation($"Name '{capability.Name}' is already in use by Profile Parameter(s) with ID(s)", coreParametersWithSameNameAndDifferentIds.Select(x => x.ID).ToArray());

                var error = new CapabilityConfigurationError
                {
                    ErrorReason = CapabilityConfigurationError.Reason.NameExists,
                    ErrorMessage = "Name is already in use.",
                };

                ReportError(capability.Id, error);
            }
        }

        private void ValidateDiscretes(IEnumerable<Capability> apiCapabilities)
        {
            foreach (var capability in apiCapabilities)
            {
                if (capability.Discretes.Count == 0)
                {
                    ReportError(capability.Id, new CapabilityConfigurationError
                    {
                        ErrorReason = CapabilityConfigurationError.Reason.InvalidDiscretes,
                        ErrorMessage = "Empty discretes list is not allowed.",
                    });
                }
                else
                {
                    var duplicateDiscretes = capability.Discretes
                        .GroupBy(x => x.Trim())
                        .Where(g => g.Count() > 1)
                        .SelectMany(g => g)
                        .ToList();

                    if (duplicateDiscretes.Any())
                    {
                        ReportError(capability.Id, new CapabilityConfigurationError
                        {
                            ErrorReason = CapabilityConfigurationError.Reason.InvalidDiscretes,
                            ErrorMessage = $"The capability defines the following duplicate discretes: {String.Join(", ", duplicateDiscretes)}.",
                        });
                    }
                }
            }
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

        private List<string> GetCleanInputDiscretes(IEnumerable<string> discretes)
        {
            return discretes.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList();
        }
    }
}
