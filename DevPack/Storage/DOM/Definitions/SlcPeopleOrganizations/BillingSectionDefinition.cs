namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations
{
	using Skyline.DataMiner.Net.GenericEnums;
	using Skyline.DataMiner.Net.Sections;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations.SlcPeopleOrganizationsIds;

	internal static class BillingSectionDefinition
	{
		internal static CustomSectionDefinition Create()
		{
			// Imported by the old workflow, but deliberately not linked to any
			// DOM definition in the authoritative P&O export.
			var section = new CustomSectionDefinition { ID = Ids.Sections.Billing.Id, Name = "Billing" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Billing.Email, Name = "Email", FieldType = typeof(string), IsOptional = false, Tooltip = "The billing email address for this organization",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Billing.Phone, Name = "Phone", FieldType = typeof(string), IsOptional = true, Tooltip = "This organization's billing phone number contact",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Billing.StreetAddress, Name = "Street address", FieldType = typeof(string), IsOptional = true, Tooltip = "This organization's billing street address",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Billing.City, Name = "City", FieldType = typeof(string), IsOptional = true, Tooltip = "This organization's billing address city",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Billing.Zip, Name = "Zip", FieldType = typeof(long), IsOptional = true, Tooltip = "This organization's billing address zip code",
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.Billing.Country, Name = "Country", FieldType = typeof(GenericEnum<int>), IsOptional = true,
				GenericEnumInstance = CountryEnum.Create(), Tooltip = "This orgnaization's billing address country",
			});
			return section;
		}
	}
}
