namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

	internal class ResourcePropertyFilterTranslator : DomInstanceFilterTranslator<ResourceProperty>
	{
		private readonly FilterElement<DomInstance> resourcePropertyDomDefinitionFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resourceproperty.Id);
		private readonly Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>>
		{
			[ResourcePropertyExposers.Id.fieldName] = HandleGuid,
			[ResourcePropertyExposers.Name.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.PropertyInfo.PropertyName), comparer, (string)value),
		};

		protected override Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> Handlers => handlers;

		protected override FilterElement<DomInstance> DomDefinitionFilter => resourcePropertyDomDefinitionFilter;
	}
}
