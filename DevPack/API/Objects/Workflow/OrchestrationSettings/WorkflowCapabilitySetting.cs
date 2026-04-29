namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	internal class WorkflowCapabilitySetting : CapabilitySetting
	{
		private StorageWorkflow.ProfileParameterValuesSection originalSection;
		private StorageWorkflow.ProfileParameterValuesSection updatedSection;

		internal WorkflowCapabilitySetting(CapabilitySetting capabilitySetting) : base(capabilitySetting)
		{
		}

		internal WorkflowCapabilitySetting(StorageWorkflow.ProfileParameterValuesSection section)
		{
			ParseSection(section);
			InitTracking();
		}

		internal override Storage.DOM.DomSectionBase OriginalSection => originalSection;

		internal StorageWorkflow.ProfileParameterValuesSection GetSectionWithChanges()
		{
			if (updatedSection == null)
			{
				updatedSection = IsNew ? new StorageWorkflow.ProfileParameterValuesSection() : originalSection.Clone();
			}

			updatedSection.ProfileParameterId = Id;
			updatedSection.StringValue = Value;
			updatedSection.Reference = Reference?.ToStorage();

			return updatedSection;
		}

		private void ParseSection(StorageWorkflow.ProfileParameterValuesSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Id = section.ProfileParameterId;
			Value = section.StringValue;
			if (!String.IsNullOrEmpty(Value) && Value.Contains(';'))
			{
				Value = Value.Split([";"], StringSplitOptions.RemoveEmptyEntries).First();
			}

			Reference = section.Reference.ToDataReference();
		}
	}
}
