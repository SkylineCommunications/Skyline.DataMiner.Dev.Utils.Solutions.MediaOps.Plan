namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcResourceStudio
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.GenericEnums;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Tools;

	internal sealed class ResourcePoolDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("ResourcePool")
		{
			ID = SlcResource_StudioIds.Definitions.Resourcepool,
			DomBehaviorDefinitionId = SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Id,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition { ConcatenationItems = { new FieldValueConcatenationItem(SlcResource_StudioIds.Sections.ResourcePoolInfo.Name) } },
			},
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ResourcePoolInfo.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ResourcePoolCost.Id) { IsOptional = true, AllowMultipleSections = false, IsSoftDeleted = true },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ResourcePoolCapabilities.Id) { IsOptional = true, AllowMultipleSections = true },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ResourcePoolLinks.Id) { IsOptional = true, AllowMultipleSections = true },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ExternalMetadata.Id) { IsOptional = true, AllowMultipleSections = false },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ResourcePoolOther.Id) { IsOptional = true, AllowMultipleSections = false },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ConfigurationInfo.Id) { IsOptional = true, AllowMultipleSections = false },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.Errors.Id) { IsOptional = true, AllowMultipleSections = true, IsSoftDeleted = true },
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[]
		{
			GetResourcePoolInfo(), GetResourcePoolCost(), GetResourcePoolInternalProperties(), GetResourcePoolCapabilities(), GetResourcePoolLinks(),
			ResourceStudioSectionDefinitions.GetExternalMetadata(), GetResourcePoolOther(), ResourceStudioSectionDefinitions.GetConfigurationInfo(), ResourceStudioSectionDefinitions.GetErrors(),
		};

		private static CustomSectionDefinition GetResourcePoolInfo()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.ResourcePoolInfo.Id, Name = "Resource pool info" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourcePoolInfo.Name, Name = "Name", FieldType = typeof(string), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor("(slc)media_ops")
			{
				ID = SlcResource_StudioIds.Sections.ResourcePoolInfo.Domain, Name = "Domain", FieldType = typeof(Guid), IsOptional = true, IsSoftDeleted = true,
				DomDefinitionIds = { new DomDefinitionId(new Guid("7b135f7c-3010-499e-bfb6-29ea3dabda4a")) { ModuleId = "(slc)media_ops" } },
				Tooltip = "Pools can be grouped into domains for filtering purposes in certain apps.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourcePoolInfo.Category, Name = "Category", FieldType = typeof(string), IsOptional = true,
				Tooltip = "Identifier of the associated category",
			});
			return section;
		}

		private static CustomSectionDefinition GetResourcePoolCost()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.ResourcePoolCost.Id, Name = "Resource pool cost" };
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor("(slc)cost_billing")
			{
				ID = SlcResource_StudioIds.Sections.ResourcePoolCost.CostRatecard, Name = "Cost ratecard", FieldType = typeof(Guid), IsOptional = true, IsSoftDeleted = true,
				DomDefinitionIds = { new DomDefinitionId(new Guid("dd02ad2b-460c-42b1-b9f2-9f6c391ca72f")) { ModuleId = "(slc)cost_billing" } },
				Tooltip = "Default ratecard for all resources in the pool to calculate cost of usage.",
			});
			return section;
		}

		private static CustomSectionDefinition GetResourcePoolInternalProperties()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Id, Name = "Resource pool internal properties" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Resource_Pool_Id, Name = "resource_pool_id", FieldType = typeof(string), IsOptional = true,
				Tooltip = "GUID of the resource pool in DataMiner.",
			});
			return section;
		}

		private static CustomSectionDefinition GetResourcePoolCapabilities()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.ResourcePoolCapabilities.Id, Name = "Resource pool capabilities" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourcePoolCapabilities.ProfileParameterID, Name = "Profile parameter ID", FieldType = typeof(string), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourcePoolCapabilities.StringValue, Name = "String value", FieldType = typeof(string), IsOptional = true, Tooltip = String.Empty,
			});
			return section;
		}

		private static CustomSectionDefinition GetResourcePoolLinks()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.ResourcePoolLinks.Id, Name = "Resource pool links" };
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(SlcResource_StudioIds.ModuleId)
			{
				ID = SlcResource_StudioIds.Sections.ResourcePoolLinks.LinkedResourcePool, Name = "Linked resource pool", FieldType = typeof(Guid), IsOptional = true,
				DomDefinitionIds = { SlcResource_StudioIds.Definitions.Resourcepool },
				Tooltip = "When booking the current resource pool, a resource from the linked resource pool must also be booked (automatically or manually).",
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourcePoolLinks.ResourceSelectionType, Name = "Resource selection type", FieldType = typeof(GenericEnum<int>), IsOptional = true,
				GenericEnumInstance = GenericEnumFactory.Create("Resource selection type", ("Automatic", (int)SlcResource_StudioIds.Enums.Resourceselectiontype.Automatic), ("Manual", (int)SlcResource_StudioIds.Enums.Resourceselectiontype.Manual)),
				Tooltip = "Specifies whether a resource from the linked resource pool should be selected automatically or manually by the user.",
			});
			return section;
		}

		private static CustomSectionDefinition GetResourcePoolOther()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.ResourcePoolOther.Id, Name = "Resource pool other" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourcePoolOther.IconImage, Name = "Icon image", FieldType = typeof(string), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourcePoolOther.URL, Name = "URL", FieldType = typeof(string), IsOptional = true,
				Tooltip = "Link to Visio or Monitoring page for resource pool.",
			});
			return section;
		}
	}
}
