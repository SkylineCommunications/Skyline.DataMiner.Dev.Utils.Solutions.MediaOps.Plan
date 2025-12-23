namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    internal class ResourceCapacitySettingsValidator : ApiObjectValidator
    {
        private readonly Guid resourceId;

        private readonly Capacity capacity;

        private readonly CapacitySetting resourceCapacitySettings;

        private ResourceCapacitySettingsValidator(Guid resourceId, Capacity capacity, CapacitySetting resourceCapacitySettings)
        {
            if (resourceId == Guid.Empty)
            {
                throw new ArgumentException("Resource ID cannot be an empty GUID.", nameof(resourceId));
            }

            this.resourceId = resourceId;
            this.capacity = capacity ?? throw new ArgumentNullException(nameof(capacity));
            this.resourceCapacitySettings = resourceCapacitySettings ?? throw new ArgumentNullException(nameof(resourceCapacitySettings));
        }

        public static ResourceCapacitySettingsValidator Validate(Guid resourceId, Capacity apiCapacity, CapacitySetting apiCapacitySettings)
        {
            var validator = new ResourceCapacitySettingsValidator(resourceId, apiCapacity, apiCapacitySettings);
            validator.Validate();

            return validator;
        }

        private void Validate()
        {
            if (resourceCapacitySettings is ResourceNumberCapacitySetting numberCapacitySettings)
            {
                ValidateValue(numberCapacitySettings.Value);
            }
            else if (resourceCapacitySettings is ResourceRangeCapacitySetting rangeCapacitySettings)
            {
                var validMinValue = ValidateValue(rangeCapacitySettings.MinValue);
                var validMaxValue = ValidateValue(rangeCapacitySettings.MaxValue);

                if (validMinValue && validMaxValue)
                {
                    ValidateRange(rangeCapacitySettings.MinValue, rangeCapacitySettings.MaxValue);
                }
            }
        }

        private bool ValidateValue(decimal capacityValue)
        {
            if (capacity.rangeMin.HasValue && capacityValue < capacity.rangeMin.Value)
            {
                ReportError(resourceId, new ResourceInvalidCapacitySettingsError
                {
                    ErrorMessage = $"Value '{capacityValue}' must be greater than or equal to '{capacity.RangeMin}'.",
                    CapacityId = resourceCapacitySettings.Id,
                });

                return false;
            }

            if (capacity.rangeMax.HasValue && capacityValue > capacity.rangeMax.Value)
            {
                ReportError(resourceId, new ResourceInvalidCapacitySettingsError
                {
                    ErrorMessage = $"Value '{capacityValue}' must be lower than or equal to '{capacity.RangeMax}'.",
                    CapacityId = resourceCapacitySettings.Id,
                });

                return false;
            }

            if (capacity.stepSize.HasValue && !ValidateValueStepSize(capacityValue))
            {
                return false;
            }

            if (capacity.decimals.HasValue && (Math.Round(capacityValue, capacity.Decimals.Value) - capacityValue) != 0)
            {
                ReportError(resourceId, new ResourceInvalidCapacitySettingsError
                {
                    ErrorMessage = $"Value '{capacityValue}' must contain less than '{capacity.Decimals}' decimals.",
                    CapacityId = resourceCapacitySettings.Id,
                });

                return false;
            }

            return true;
        }

        private bool ValidateValueStepSize(decimal capacityValue)
        {
            if (capacity.stepSize - 0.0m == 0)
            {
                return true;
            }

            var valueToCheck = capacityValue;
            if (capacity.rangeMin.HasValue)
            {
                valueToCheck = capacityValue - capacity.rangeMin.Value;
            }
            else if (capacity.rangeMax.HasValue)
            {
                valueToCheck = capacity.rangeMax.Value - capacityValue;
            }
            else
            {
                // no range defined
            }

            if ((valueToCheck % capacity.stepSize.Value) != 0)
            {
                ReportError(resourceId, new ResourceInvalidCapacitySettingsError
                {
                    ErrorMessage = $"Value '{capacityValue}' must align with the step size of '{capacity.StepSize}'.",
                    CapacityId = resourceCapacitySettings.Id,
                });

                return false;
            }

            return true;
        }

        private void ValidateRange(decimal minValue, decimal maxValue)
        {
            if (maxValue <= minValue)
            {
                ReportError(resourceId, new ResourceInvalidCapacitySettingsError
                {
                    ErrorMessage = $"Max value '{maxValue}' must be greater than min value '{minValue}'.",
                    CapacityId = resourceCapacitySettings.Id,
                });
            }
        }
    }
}
