namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcResourceStudio
{
	using System;

	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

	internal static class ResourceStudioSectionDefinitions
	{
		internal static CustomSectionDefinition GetExternalMetadata()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.ExternalMetadata.Id, Name = "External metadata" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ExternalMetadata.ExternallyManaged,
				Name = "Externally managed",
				FieldType = typeof(bool),
				IsOptional = true,
				DefaultValue = new ValueWrapper<bool>(false),
				Tooltip = "Indicates whether this resource was created from another app or managed solely by Resource Studio.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ExternalMetadata.BookingExtensionScript,
				Name = "Booking Extension Script",
				FieldType = typeof(string),
				IsOptional = true,
				IsSoftDeleted = true,
				Tooltip = "Script that will be triggered when the current resource is being booked. This allows related resources to be included in the same booking based on predefined relations or custom logic.",
			});
			return section;
		}

		internal static CustomSectionDefinition GetErrors()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.Errors.Id, Name = "Errors" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.Errors.ErrorCode,
				Name = "Error Code",
				FieldType = typeof(string),
				IsOptional = true,
				IsSoftDeleted = true,
				Tooltip = "The code of the error, which has to be unique for the linked object",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.Errors.ErrorMessage,
				Name = "Error Message",
				FieldType = typeof(string),
				IsOptional = true,
				IsSoftDeleted = true,
				Tooltip = "The message explaining why the error was raised",
			});
			return section;
		}

		internal static CustomSectionDefinition GetConfigurationInfo()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.ConfigurationInfo.Id, Name = "Configuration info" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ConfigurationInfo.ProfileDefinitionID,
				Name = "Profile Definition ID",
				FieldType = typeof(string),
				IsOptional = true,
				IsReadonly = true,
				IsSoftDeleted = true,
				Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(SlcResource_StudioIds.ModuleId)
			{
				ID = SlcResource_StudioIds.Sections.ConfigurationInfo.PoolConfiguration,
				Name = "Pool Configuration",
				FieldType = typeof(Guid),
				IsOptional = true,
				DomDefinitionIds = { SlcResource_StudioIds.Definitions.Configuration },
				Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(SlcResource_StudioIds.ModuleId)
			{
				ID = SlcResource_StudioIds.Sections.ConfigurationInfo.FlowRules,
				Name = "Flow Rules",
				FieldType = typeof(Guid),
				IsOptional = true,
				DomDefinitionIds = { SlcResource_StudioIds.Definitions.Flowrules },
				Tooltip = String.Empty,
			});
			return section;
		}
	}
}
