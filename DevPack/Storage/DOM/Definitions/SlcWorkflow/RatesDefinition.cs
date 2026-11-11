namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcWorkflow
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.GenericEnums;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Tools;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds;

	internal sealed class RatesDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Rates")
		{
			ID = Ids.Definitions.Rates,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			ModuleSettingsOverrides = new ModuleSettingsOverrides(),
			SectionDefinitionLinks = { new SectionDefinitionLink(Ids.Sections.Rate.Id) { IsOptional = false, AllowMultipleSections = false, IsSoftDeleted = true } },
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[] { GetRate() };

		private static CustomSectionDefinition GetRate()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.Rate.Id, Name = "Rate" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Rate.Qty, Name = "Qty", FieldType = typeof(long), IsOptional = false, IsSoftDeleted = true,
				Tooltip = "The quantity of units (expressed in Qty Type), e.g. 10 EUR per hour.",
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.Rate.QtyType, Name = "Qty type", FieldType = typeof(GenericEnum<string>), IsOptional = false, IsSoftDeleted = true,
				GenericEnumInstance = GenericEnumFactory.Create(String.Empty, ("Min", Ids.Enums.Qtytype.Min), ("Hour", Ids.Enums.Qtytype.Hour), ("Day", Ids.Enums.Qtytype.Day), ("Per Use", Ids.Enums.Qtytype.PerUse)),
				Tooltip = "Determines whether this rate is charged by the minute, hour, day, or per use.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Rate.AmountPerUnit, Name = "Amount per unit", FieldType = typeof(double), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "The price per unit based on the quantity type, which can be minute, hour, day, or per use.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Rate.TotalAmount, Name = "Total amount", FieldType = typeof(double), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "The quantity multiplied by the amount per unit, before applying any discounts.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Rate.TotalNetAmount, Name = "Total net amount", FieldType = typeof(double), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "The total amount after applying the discount.",
			});
			return section;
		}
	}
}
