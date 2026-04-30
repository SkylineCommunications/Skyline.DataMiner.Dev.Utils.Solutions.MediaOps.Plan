namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	internal class ResourceStudioDiscreteTextConfigurationSetting : DiscreteTextConfigurationSetting
	{
		private StorageResourceStudio.ProfileParameterValuesSection originalSection;
		private StorageResourceStudio.ProfileParameterValuesSection updatedSection;

		internal ResourceStudioDiscreteTextConfigurationSetting(DiscreteTextConfigurationSetting discreteTextConfigurationSetting) : base(discreteTextConfigurationSetting)
		{
		}

		internal ResourceStudioDiscreteTextConfigurationSetting(DiscreteTextConfiguration configuration, StorageResourceStudio.ProfileParameterValuesSection section)
		{
			ParseSection(configuration, section);
			InitTracking();
		}

		internal override Storage.DOM.DomSectionBase OriginalSection => originalSection;

		internal StorageResourceStudio.ProfileParameterValuesSection GetSectionWithChanges()
		{
			if (updatedSection == null)
			{
				updatedSection = IsNew ? new StorageResourceStudio.ProfileParameterValuesSection() : originalSection.Clone();
			}

			updatedSection.ProfileParameterId = Id;
			updatedSection.StringValue = Value?.Value;
			updatedSection.Reference = Reference?.ToStorage();

			return updatedSection;
		}

		private void ParseSection(DiscreteTextConfiguration configuration, StorageResourceStudio.ProfileParameterValuesSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Id = section.ProfileParameterId;

			if (!string.IsNullOrEmpty(section.StringValue))
			{
				var discreteValue = configuration.Discretes.FirstOrDefault(dv => dv.Value == section.StringValue);
				if (discreteValue != null)
				{
					Value = discreteValue;
				}
			}

			Reference = section.Reference?.ToDataReference();
		}
	}
}
