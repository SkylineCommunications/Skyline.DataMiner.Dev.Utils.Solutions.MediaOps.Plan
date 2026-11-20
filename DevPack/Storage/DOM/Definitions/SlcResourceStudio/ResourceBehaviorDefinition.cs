namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcResourceStudio
{
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds;
	using Statuses = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses;

	internal static class ResourceBehaviorDefinition
	{
		internal static DomBehaviorDefinition Create()
		{
			var definition = new DomBehaviorDefinition
			{
				ID = Ids.Behaviors.Resource_Behavior.Id,
				Name = "Resource_Behavior",
				InitialStatusId = Statuses.Draft,
				Statuses = { new(Statuses.Draft, "Draft"), new(Statuses.Complete, "Complete"), new(Statuses.Deprecated, "Deprecated") },
				StatusTransitions =
				{
					new("draft_to_complete", Statuses.Draft, Statuses.Complete),
					new("complete_to_deprecated", Statuses.Complete, Statuses.Deprecated),
					new("deprecated_to_complete", Statuses.Deprecated, Statuses.Complete),
				},
			};
			foreach (var status in definition.Statuses)
			{
				AddInfoAndInternalProperties(definition, status.Id);
			}

			// Preserve the legacy status/section ordering as well as its rules.
			foreach (var status in definition.Statuses)
			{
				AddConnectionManagement(definition, status.Id);
			}

			foreach (var status in definition.Statuses)
			{
				AddCapabilitiesAndCapacities(definition, status.Id);
			}

			foreach (var status in definition.Statuses)
			{
				AddPropertiesControlCostAndOther(definition, status.Id);
			}

			foreach (var status in definition.Statuses)
			{
				AddExternalMetadata(definition, status.Id);
			}

			foreach (var status in definition.Statuses)
			{
				AddErrors(definition, status.Id);
			}

			return definition;
		}

		private static void AddInfoAndInternalProperties(DomBehaviorDefinition definition, string statusId)
		{
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ResourceInfo.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ResourceInfo.Name) { Visible = true, RequiredForStatus = true, ReadOnly = false, ClientReadOnly = statusId == Statuses.Deprecated },
					new(Ids.Sections.ResourceInfo.Type) { Visible = true, RequiredForStatus = statusId == Statuses.Draft, ReadOnly = false, ClientReadOnly = statusId != Statuses.Draft },
					new(Ids.Sections.ResourceInfo.Favorite) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = statusId == Statuses.Deprecated },
					new(Ids.Sections.ResourceInfo.Element) { Visible = statusId != Statuses.Draft, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = statusId != Statuses.Draft },
					new(Ids.Sections.ResourceInfo.Service) { Visible = statusId != Statuses.Draft, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = statusId != Statuses.Draft },
					new(Ids.Sections.ResourceInfo.Concurrency) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = statusId == Statuses.Deprecated },
					new(Ids.Sections.ResourceInfo.LinkDetails) { Visible = statusId != Statuses.Draft, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = true },
				},
			});
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ResourceInternalProperties.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ResourceInternalProperties.Pool_Ids) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = statusId == Statuses.Deprecated },
					new(Ids.Sections.ResourceInternalProperties.Resource_Id) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = statusId == Statuses.Deprecated },
					new(Ids.Sections.ResourceInternalProperties.FunctionName) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.ResourceInternalProperties.FunctionTableIndex) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.ResourceInternalProperties.ProtocolName) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.ResourceInternalProperties.ResourceMetadata) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			});
		}

		private static void AddConnectionManagement(DomBehaviorDefinition definition, string statusId)
		{
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ResourceConnectionManagement.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ResourceConnectionManagement.InputReference) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = statusId == Statuses.Deprecated },
					new(Ids.Sections.ResourceConnectionManagement.OutputReference) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = statusId == Statuses.Deprecated },
					new(Ids.Sections.ResourceConnectionManagement.InputVsgs) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.ResourceConnectionManagement.OutputVsgs) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			});
		}

		private static void AddCapabilitiesAndCapacities(DomBehaviorDefinition definition, string statusId)
		{
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ResourceCapabilities.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ResourceCapabilities.ProfileParameterID) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.ResourceCapabilities.StringValue) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			});
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ResourceCapacities.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ResourceCapacities.ProfileParameterID) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.ResourceCapacities.DoubleMaxValue) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.ResourceCapacities.DoubleMinValue) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			});
		}

		private static void AddPropertiesControlCostAndOther(DomBehaviorDefinition definition, string statusId)
		{
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ResourceProperties.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ResourceProperties.Property) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = statusId == Statuses.Deprecated },
					new(Ids.Sections.ResourceProperties.PropertyValue) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = statusId == Statuses.Deprecated },
				},
			});
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ResourceControl.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = true,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ResourceControl.AutomationScriptName) { Visible = statusId == Statuses.Complete, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = statusId == Statuses.Deprecated },
				},
			});
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ResourceCost.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = true,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ResourceCost.CostRatecard) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = statusId == Statuses.Deprecated },
				},
			});
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ResourceOther.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ResourceOther.IconImage) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = true },
					new(Ids.Sections.ResourceOther.URL) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = statusId == Statuses.Deprecated },
				},
			});
		}

		private static void AddExternalMetadata(DomBehaviorDefinition definition, string statusId)
		{
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ExternalMetadata.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ExternalMetadata.ExternallyManaged) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.ExternalMetadata.BookingExtensionScript) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			});
		}

		private static void AddErrors(DomBehaviorDefinition definition, string statusId)
		{
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.Errors.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = true,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.Errors.ErrorCode) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = true },
					new(Ids.Sections.Errors.ErrorMessage) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = true },
				},
			});
		}
	}
}
