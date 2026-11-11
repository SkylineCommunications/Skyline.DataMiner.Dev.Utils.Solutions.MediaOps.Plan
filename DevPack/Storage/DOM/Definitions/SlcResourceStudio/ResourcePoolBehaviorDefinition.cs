namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcResourceStudio
{
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds;
	using Statuses = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Statuses;

	internal static class ResourcePoolBehaviorDefinition
	{
		internal static DomBehaviorDefinition Create()
		{
			var definition = new DomBehaviorDefinition
			{
				ID = Ids.Behaviors.Resourcepool_Behavior.Id,
				Name = "ResourcePool_Behavior",
				InitialStatusId = Statuses.Draft,
				Statuses = { new(Statuses.Draft, "Draft"), new(Statuses.Complete, "Complete"), new(Statuses.Deprecated, "Deprecated") },
				StatusTransitions = { new("draft_to_complete", Statuses.Draft, Statuses.Complete), new("complete_to_deprecated", Statuses.Complete, Statuses.Deprecated) },
			};
			foreach (var status in definition.Statuses)
			{
				AddInfoCostAndInternalProperties(definition, status.Id);
			}

			foreach (var status in definition.Statuses)
			{
				AddCapabilities(definition, status.Id);
			}

			foreach (var status in definition.Statuses)
			{
				AddLinks(definition, status.Id);
			}

			// The exported behavior puts Complete before Draft for this section.
			AddExternalMetadata(definition, Statuses.Complete);
			AddExternalMetadata(definition, Statuses.Draft);
			AddExternalMetadata(definition, Statuses.Deprecated);
			foreach (var status in definition.Statuses)
			{
				AddOther(definition, status.Id);
			}

			foreach (var status in definition.Statuses)
			{
				AddConfigurationAndErrors(definition, status.Id);
			}

			return definition;
		}

		private static void AddInfoCostAndInternalProperties(DomBehaviorDefinition definition, string statusId)
		{
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ResourcePoolInfo.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ResourcePoolInfo.Name) { Visible = true, RequiredForStatus = true, ReadOnly = statusId == Statuses.Deprecated, ClientReadOnly = false },
					new(Ids.Sections.ResourcePoolInfo.Domain) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.ResourcePoolInfo.Category) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			});
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ResourcePoolCost.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = true,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ResourcePoolCost.CostRatecard) { Visible = true, RequiredForStatus = false, ReadOnly = statusId == Statuses.Deprecated, ClientReadOnly = false },
				},
			});
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ResourcePoolInternalProperties.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ResourcePoolInternalProperties.Resource_Pool_Id) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			});
		}

		private static void AddCapabilities(DomBehaviorDefinition definition, string statusId)
		{
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ResourcePoolCapabilities.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ResourcePoolCapabilities.ProfileParameterID) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.ResourcePoolCapabilities.StringValue) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			});
		}

		private static void AddLinks(DomBehaviorDefinition definition, string statusId)
		{
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ResourcePoolLinks.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ResourcePoolLinks.LinkedResourcePool) { Visible = true, RequiredForStatus = false, ReadOnly = statusId == Statuses.Deprecated, ClientReadOnly = false },
					new(Ids.Sections.ResourcePoolLinks.ResourceSelectionType) { Visible = true, RequiredForStatus = false, ReadOnly = statusId == Statuses.Deprecated, ClientReadOnly = false },
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

		private static void AddOther(DomBehaviorDefinition definition, string statusId)
		{
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ResourcePoolOther.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ResourcePoolOther.IconImage) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = true },
					new(Ids.Sections.ResourcePoolOther.URL) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			});
		}

		private static void AddConfigurationAndErrors(DomBehaviorDefinition definition, string statusId)
		{
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ConfigurationInfo.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ConfigurationInfo.ProfileDefinitionID) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.ConfigurationInfo.PoolConfiguration) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.ConfigurationInfo.FlowRules) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			});
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
