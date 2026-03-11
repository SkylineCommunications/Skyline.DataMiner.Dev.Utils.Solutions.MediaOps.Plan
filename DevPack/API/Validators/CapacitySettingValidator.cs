namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	internal abstract class CapacitySettingValidator : ApiObjectValidator
	{
		private readonly Guid apiObjectId;

		private readonly Capacity capacity;

		private readonly CapacitySetting capacitySetting;

		private protected CapacitySettingValidator(Guid apiObjectId, Capacity capacity, CapacitySetting capacitySetting)
		{
			if (apiObjectId == Guid.Empty)
			{
				throw new ArgumentException("API object ID cannot be an empty GUID.", nameof(apiObjectId));
			}

			this.apiObjectId = apiObjectId;
			this.capacity = capacity ?? throw new ArgumentNullException(nameof(capacity));
			this.capacitySetting = capacitySetting ?? throw new ArgumentNullException(nameof(capacitySetting));

			Validate();
		}

		internal abstract MediaOpsErrorData ComposeCapacitySettingError(Guid capacityId, string errorMessage);

		private void Validate()
		{
			if (capacitySetting is NumberCapacitySetting numberCapacitySettings)
			{
				ValidateValue(numberCapacitySettings.Value);
			}
			else if (capacitySetting is RangeCapacitySetting rangeCapacitySettings)
			{
				var validMinValue = ValidateValue(rangeCapacitySettings.MinValue);
				var validMaxValue = ValidateValue(rangeCapacitySettings.MaxValue);

				if (validMinValue && validMaxValue)
				{
					ValidateRange(rangeCapacitySettings.MinValue.Value, rangeCapacitySettings.MaxValue.Value);
				}
			}
		}

		private bool ValidateValue(decimal? capacityValue)
		{
			if (!capacityValue.HasValue)
			{
				ReportError(apiObjectId, ComposeCapacitySettingError(capacitySetting.Id, "Value cannot be null."));
				return false;
			}

			if (capacity.RangeMin.HasValue && capacityValue < capacity.RangeMin.Value)
			{
				ReportError(apiObjectId, ComposeCapacitySettingError(capacitySetting.Id, $"Value '{capacityValue}' must be greater than or equal to '{capacity.RangeMin}'."));

				return false;
			}

			if (capacity.RangeMax.HasValue && capacityValue > capacity.RangeMax.Value)
			{
				ReportError(apiObjectId, ComposeCapacitySettingError(capacitySetting.Id, $"Value '{capacityValue}' must be lower than or equal to '{capacity.RangeMax}'."));

				return false;
			}

			if (capacity.StepSize.HasValue && !ValidateValueStepSize(capacityValue.Value))
			{
				return false;
			}

			if (capacity.Decimals.HasValue && (Math.Round(capacityValue.Value, capacity.Decimals.Value) - capacityValue) != 0)
			{
				ReportError(apiObjectId, ComposeCapacitySettingError(capacitySetting.Id, $"Value '{capacityValue}' must contain less than '{capacity.Decimals}' decimals."));

				return false;
			}

			return true;
		}

		private bool ValidateValueStepSize(decimal capacityValue)
		{
			if (capacity.StepSize - 0.0m == 0)
			{
				return true;
			}

			var valueToCheck = capacityValue;
			if (capacity.RangeMin.HasValue)
			{
				valueToCheck = capacityValue - capacity.RangeMin.Value;
			}
			else if (capacity.RangeMax.HasValue)
			{
				valueToCheck = capacity.RangeMax.Value - capacityValue;
			}
			else
			{
				// no range defined
			}

			if ((valueToCheck % capacity.StepSize.Value) != 0)
			{
				ReportError(apiObjectId, ComposeCapacitySettingError(capacitySetting.Id, $"Value '{capacityValue}' must align with the step size of '{capacity.StepSize}'."));

				return false;
			}

			return true;
		}

		private void ValidateRange(decimal minValue, decimal maxValue)
		{
			if (maxValue <= minValue)
			{
				ReportError(apiObjectId, ComposeCapacitySettingError(capacitySetting.Id, $"Max value '{maxValue}' must be greater than min value '{minValue}'."));
			}
		}
	}

	internal class ResourceCapacitySettingValidator : CapacitySettingValidator
	{
		private ResourceCapacitySettingValidator(Guid resourceId, Capacity capacity, CapacitySetting capacitySetting)
			: base(resourceId, capacity, capacitySetting)
		{
		}

		public static CapacitySettingValidator Validate(Guid resourceId, Capacity apiCapacity, CapacitySetting apiCapacitySettings)
		{
			return new ResourceCapacitySettingValidator(resourceId, apiCapacity, apiCapacitySettings);
		}

		internal override MediaOpsErrorData ComposeCapacitySettingError(Guid capacityId, string errorMessage)
		{
			return new ResourceInvalidCapacitySettingsError
			{
				ErrorMessage = errorMessage,
				CapacityId = capacityId,
			};
		}
	}

	internal class OrchestrationSettingsCapacitySettingValidator : CapacitySettingValidator
	{
		private OrchestrationSettingsCapacitySettingValidator(Guid orchestrationSettingsId, Capacity capacity, CapacitySetting capacitySetting)
			: base(orchestrationSettingsId, capacity, capacitySetting)
		{
		}

		public static CapacitySettingValidator Validate(Guid orchestrationSettingsId, Capacity apiCapacity, CapacitySetting apiCapacitySettings)
		{
			return new OrchestrationSettingsCapacitySettingValidator(orchestrationSettingsId, apiCapacity, apiCapacitySettings);
		}

		internal override MediaOpsErrorData ComposeCapacitySettingError(Guid capacityId, string errorMessage)
		{
			return new OrchestrationSettingsInvalidCapacitySettingsError
			{
				ErrorMessage = errorMessage,
				CapacityId = capacityId,
			};
		}
	}
}
