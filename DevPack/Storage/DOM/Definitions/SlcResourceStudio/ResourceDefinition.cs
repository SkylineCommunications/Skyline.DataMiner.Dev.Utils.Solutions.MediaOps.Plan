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

	internal sealed class ResourceDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Resource")
		{
			ID = SlcResource_StudioIds.Definitions.Resource,
			DomBehaviorDefinitionId = SlcResource_StudioIds.Behaviors.Resource_Behavior.Id,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition { ConcatenationItems = { new FieldValueConcatenationItem(SlcResource_StudioIds.Sections.ResourceInfo.Name) } },
			},
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ResourceInfo.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ResourceInternalProperties.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ResourceConnectionManagement.Id) { IsOptional = true, AllowMultipleSections = false },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ResourceCapabilities.Id) { IsOptional = true, AllowMultipleSections = true },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ResourceCapacities.Id) { IsOptional = true, AllowMultipleSections = true },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ResourceProperties.Id) { IsOptional = true, AllowMultipleSections = true },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ResourceControl.Id) { IsOptional = true, AllowMultipleSections = true, IsSoftDeleted = true },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ResourceCost.Id) { IsOptional = true, AllowMultipleSections = false, IsSoftDeleted = true },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ResourceOther.Id) { IsOptional = true, AllowMultipleSections = false },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ExternalMetadata.Id) { IsOptional = true, AllowMultipleSections = false },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.Errors.Id) { IsOptional = true, AllowMultipleSections = true, IsSoftDeleted = true },
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[]
		{
			GetResourceInfo(), GetResourceInternalProperties(), GetResourceConnectionManagement(), GetResourceCapabilities(), GetResourceCapacities(), GetResourceProperties(),
			GetResourceControl(), GetResourceCost(), GetResourceOther(), ResourceStudioSectionDefinitions.GetExternalMetadata(), ResourceStudioSectionDefinitions.GetErrors(),
		};

		private static CustomSectionDefinition GetResourceInfo()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.ResourceInfo.Id, Name = "Resource info" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceInfo.Name, Name = "Name", FieldType = typeof(string), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceInfo.Type, Name = "Type", FieldType = typeof(GenericEnum<int>), IsOptional = true, Tooltip = String.Empty,
				GenericEnumInstance = GenericEnumFactory.Create(String.Empty, ("Element", (int)SlcResource_StudioIds.Enums.Type.Element), ("Virtual Function", (int)SlcResource_StudioIds.Enums.Type.VirtualFunction), ("Service", (int)SlcResource_StudioIds.Enums.Type.Service), ("Unmanaged", (int)SlcResource_StudioIds.Enums.Type.Unmanaged)),
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceInfo.Favorite, Name = "Favorite", FieldType = typeof(bool), IsOptional = true, DefaultValue = new ValueWrapper<bool>(false),
				Tooltip = "Indicates that this resource can be used as a default for resource duplication/bulk creation.",
			});
			section.AddOrReplaceFieldDescriptor(new ElementFieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceInfo.Element, Name = "Element", FieldType = typeof(string), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "If the resource is of type 'Element', select a specific element to link to the resource.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceInfo.Service, Name = "Service", FieldType = typeof(string), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "If the resource is of type 'Service', define a specific service to link to the resource.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceInfo.Concurrency, Name = "Concurrency", FieldType = typeof(long), IsOptional = true, DefaultValue = new ValueWrapper<long>(1),
				Tooltip = "Indicates the number of times this resource can be booked within the same time interval.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceInfo.LinkDetails, Name = "Link details", FieldType = typeof(string), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "Summarizes how the resource is linked (e.g. to an element, function).",
			});
			return section;
		}

		private static CustomSectionDefinition GetResourceInternalProperties()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.ResourceInternalProperties.Id, Name = "Resource internal properties" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceInternalProperties.Pool_Ids, Name = "pool_ids", FieldType = typeof(string), IsOptional = true,
				Tooltip = "Comma-separated list of GUIDs representing the resource pool DOMs to which the resource belongs.",
			});
			section.AddOrReplaceFieldDescriptor(new ResourceFieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceInternalProperties.Resource_Id, Name = "resource_id", FieldType = typeof(Guid), IsOptional = true, Tooltip = "Resource in DataMiner.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceInternalProperties.FunctionName, Name = "Function Name", FieldType = typeof(string), IsOptional = true, IsSoftDeleted = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceInternalProperties.FunctionTableIndex, Name = "Function Table Index", FieldType = typeof(string), IsOptional = true, IsSoftDeleted = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceInternalProperties.ProtocolName, Name = "Protocol Name", FieldType = typeof(string), IsOptional = true, IsSoftDeleted = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceInternalProperties.ResourceMetadata, Name = "Resource Metadata", FieldType = typeof(string), IsOptional = true, Tooltip = String.Empty,
			});
			return section;
		}

		private static CustomSectionDefinition GetResourceConnectionManagement()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.ResourceConnectionManagement.Id, Name = "Resource connection management" };
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor("(slc)errors")
			{
				ID = SlcResource_StudioIds.Sections.ResourceConnectionManagement.InputVsgs, Name = "input vSGs", FieldType = typeof(Guid), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "Links the resource to input virtual signal groups that can be used to set up connections to the resource.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceConnectionManagement.InputReference, Name = "input reference", FieldType = typeof(string), IsOptional = true,
				Tooltip = "Contains serialized information about the input linked with this resource",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceConnectionManagement.OutputReference, Name = "output reference", FieldType = typeof(string), IsOptional = true,
				Tooltip = "Contains serialized information about the output linked with this resource",
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor("(slc)errors")
			{
				ID = SlcResource_StudioIds.Sections.ResourceConnectionManagement.OutputVsgs, Name = "output vSGs", FieldType = typeof(Guid), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "Links the resource to output virtual signal groups that can be used to output connections from the resource.",
			});
			return section;
		}

		private static CustomSectionDefinition GetResourceCapabilities()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.ResourceCapabilities.Id, Name = "Resource capabilities" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceCapabilities.ProfileParameterID, Name = "Profile parameter ID", FieldType = typeof(string), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceCapabilities.StringValue, Name = "String value", FieldType = typeof(string), IsOptional = true, Tooltip = String.Empty,
			});
			return section;
		}

		private static CustomSectionDefinition GetResourceCapacities()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.ResourceCapacities.Id, Name = "Resource capacities" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceCapacities.ProfileParameterID, Name = "Profile parameter ID", FieldType = typeof(string), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceCapacities.DoubleMaxValue, Name = "Double max value", FieldType = typeof(double), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceCapacities.DoubleMinValue, Name = "Double min value", FieldType = typeof(double), IsOptional = true, Tooltip = String.Empty,
			});
			return section;
		}

		private static CustomSectionDefinition GetResourceProperties()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.ResourceProperties.Id, Name = "Resource properties" };
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(SlcResource_StudioIds.ModuleId)
			{
				ID = SlcResource_StudioIds.Sections.ResourceProperties.Property, Name = "Property", FieldType = typeof(Guid), IsOptional = true, Tooltip = String.Empty,
				DomDefinitionIds = { SlcResource_StudioIds.Definitions.Resourceproperty },
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceProperties.PropertyValue, Name = "Property value", FieldType = typeof(string), IsOptional = true, Tooltip = String.Empty,
			});
			return section;
		}

		private static CustomSectionDefinition GetResourceControl()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.ResourceControl.Id, Name = "Resource control" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceControl.AutomationScriptName, Name = "Automation script name", FieldType = typeof(string), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "Specifies the script used to configure the resource ad hoc.",
			});
			return section;
		}

		private static CustomSectionDefinition GetResourceCost()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.ResourceCost.Id, Name = "Resource cost" };
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor("(slc)cost_billing")
			{
				ID = SlcResource_StudioIds.Sections.ResourceCost.CostRatecard, Name = "Cost ratecard", FieldType = typeof(Guid), IsOptional = true, IsSoftDeleted = true,
				DomDefinitionIds = { new DomDefinitionId(new Guid("dd02ad2b-460c-42b1-b9f2-9f6c391ca72f")) { ModuleId = "(slc)cost_billing" } },
				Tooltip = "Used to calculate cost of resource usage. If not defined, the ratecard assigned on pool level will be used.",
			});
			return section;
		}

		private static CustomSectionDefinition GetResourceOther()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.ResourceOther.Id, Name = "Resource other" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceOther.IconImage, Name = "Icon image", FieldType = typeof(string), IsOptional = true, IsReadonly = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ResourceOther.URL, Name = "URL", FieldType = typeof(string), IsOptional = true,
				Tooltip = "Link to Visio or Monitoring page for resource.",
			});
			return section;
		}
	}
}
