namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	internal class PropertySettingValidator : ApiObjectValidator
	{
		private readonly Guid apiObjectId;

		private readonly Property property;

		private readonly PropertySetting propertySetting;

		private readonly bool valueExpected;

		private readonly long? maxDocumentSizeInMegaBytes;

		private PropertySettingValidator(Guid apiObjectId, Property property, PropertySetting propertySetting, bool valueExpected, long? maxDocumentSizeInMegaBytes)
		{
			if (apiObjectId == Guid.Empty)
			{
				throw new ArgumentException("API object ID cannot be an empty GUID.", nameof(apiObjectId));
			}

			this.apiObjectId = apiObjectId;
			this.property = property;
			this.propertySetting = propertySetting;
			this.valueExpected = valueExpected;
			this.maxDocumentSizeInMegaBytes = maxDocumentSizeInMegaBytes;

			Validate();
		}

		public static PropertySettingValidator Validate(Guid apiObjectId, Property property, PropertySetting propertySetting, bool valueExpected, long? maxDocumentSizeInMegaBytes = null)
		{
			return new PropertySettingValidator(apiObjectId, property, propertySetting, valueExpected, maxDocumentSizeInMegaBytes);
		}

		private void Validate()
		{
			if (propertySetting is StringPropertySetting stringPropertySetting)
			{
				ValidateStringPropertySetting(stringPropertySetting);
			}
			else if (propertySetting is BooleanPropertySetting)
			{
				ValidateBooleanPropertySetting();
			}
			else if (propertySetting is DiscretePropertySetting discretePropertySetting)
			{
				ValidateDiscretePropertySetting(discretePropertySetting);
			}
			else if (propertySetting is FilePropertySetting filePropertySetting)
			{
				ValidateFilePropertySetting(filePropertySetting);
			}
			else
			{
				throw new InvalidOperationException($"Unsupported property setting type: {propertySetting.GetType().FullName}");
			}
		}

		private void ValidateStringPropertySetting(StringPropertySetting setting)
		{
			if (property is not StringProperty stringProperty)
			{
				ReportError(apiObjectId, ComposePropertySettingError(propertySetting.Id, $"A string property setting cannot be used with a property of type '{property.GetType().Name}'."));
				return;
			}

			ValidateValue(setting.Value, stringProperty);
		}

		private void ValidateBooleanPropertySetting()
		{
			if (property is not BooleanProperty)
			{
				ReportError(apiObjectId, ComposePropertySettingError(propertySetting.Id, $"A boolean property setting cannot be used with a property of type '{property.GetType().Name}'."));
			}
		}

		private void ValidateDiscretePropertySetting(DiscretePropertySetting setting)
		{
			if (property is not DiscreteProperty discreteProperty)
			{
				ReportError(apiObjectId, ComposePropertySettingError(propertySetting.Id, $"A discrete property setting cannot be used with a property of type '{property.GetType().Name}'."));
				return;
			}

			ValidateValue(setting.Value, discreteProperty);
		}

		private void ValidateFilePropertySetting(FilePropertySetting setting)
		{
			if (property is not FileProperty fileProperty)
			{
				ReportError(apiObjectId, ComposePropertySettingError(propertySetting.Id, $"A file property setting cannot be used with a property of type '{property.GetType().Name}'."));
				return;
			}

			if (setting.Files.Count == 0)
			{
				if (valueExpected)
				{
					ReportError(apiObjectId, ComposePropertySettingError(propertySetting.Id, "Value cannot be null."));
				}

				return;
			}

			if (!fileProperty.AllowMultiple && setting.Files.Count > 1)
			{
				ReportError(apiObjectId, ComposePropertySettingError(propertySetting.Id, "This property does not allow multiple files."));
			}

			var filesExceedingPropertyLimit = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (fileProperty.HasSizeLimit)
			{
				var sizeLimitInBytes = fileProperty.SizeLimit * 1024L * 1024L;
				foreach (var fileToUpload in setting.FilesToUpload.Where(x => x.Value.LongLength > sizeLimitInBytes))
				{
					filesExceedingPropertyLimit.Add(fileToUpload.Key);
					ReportError(apiObjectId, ComposePropertySettingError(propertySetting.Id, $"File '{fileToUpload.Key}' exceeds the maximum file size of {fileProperty.SizeLimit} MB."));
				}
			}

			// A property limit that was valid when it was configured can still exceed the current server limit.
			if (!maxDocumentSizeInMegaBytes.HasValue || maxDocumentSizeInMegaBytes.Value <= 0)
			{
				return;
			}

			var maxSizeInBytes = maxDocumentSizeInMegaBytes.Value * 1024L * 1024L;
			foreach (var fileToUpload in setting.FilesToUpload.Where(x => x.Value.LongLength > maxSizeInBytes && !filesExceedingPropertyLimit.Contains(x.Key)))
			{
				ReportError(apiObjectId, new PropertySettingCollectionFileSizeExceededError
				{
					ErrorMessage = $"File '{fileToUpload.Key}' exceeds the maximum file size of {maxDocumentSizeInMegaBytes.Value} MB configured in DataMiner. Contact your DataMiner administrator to increase the maximum document size.",
					PropertyId = propertySetting.Id,
					Id = apiObjectId,
					FileName = fileToUpload.Key,
					FileSize = fileToUpload.Value.LongLength,
					MaxFileSize = maxSizeInBytes,
				});
			}
		}

		private MediaOpsErrorData ComposePropertySettingError(Guid propertyId, string errorMessage)
		{
			return new PropertySettingCollectionInvalidPropertySettingsError
			{
				ErrorMessage = errorMessage,
				PropertyId = propertyId,
				Id = apiObjectId,
			};
		}

		private void ValidateValue(string value, StringProperty stringProperty)
		{
			if (value == null)
			{
				if (valueExpected)
				{
					ReportError(apiObjectId, ComposePropertySettingError(propertySetting.Id, "Value cannot be null."));
				}

				return;
			}

			if (!InputValidator.HasValidTextLength(value, stringProperty.SizeLimit))
			{
				ReportError(apiObjectId, ComposePropertySettingError(propertySetting.Id, $"Value exceeds the maximum length of {stringProperty.SizeLimit} characters."));
			}
		}

		private void ValidateValue(string value, DiscreteProperty discreteProperty)
		{
			if (value == null)
			{
				if (valueExpected)
				{
					ReportError(apiObjectId, ComposePropertySettingError(propertySetting.Id, "Value cannot be null."));
				}

				return;
			}

			if (!discreteProperty.Discretes.Contains(value))
			{
				ReportError(apiObjectId, ComposePropertySettingError(propertySetting.Id, $"Value '{value}' is not a valid discrete value for this property."));
			}
		}
	}
}
