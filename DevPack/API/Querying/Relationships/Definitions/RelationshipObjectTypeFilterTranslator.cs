namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcRelationships;

	using SLDataGateway.API.Types.Querying;

	internal class RelationshipObjectTypeFilterTranslator : DomInstanceFilterTranslator<RelationshipObjectType>
	{
		private readonly FilterElement<DomInstance> objectTypeDomDefinitionFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcRelationshipsIds.Definitions.ObjectTypes.Id);
		private readonly Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>>
		{
			[RelationshipObjectTypeExposers.Id.fieldName] = HandleGuid,
			[RelationshipObjectTypeExposers.Name.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcRelationshipsIds.Sections.ObjectTypeInfo.ObjectName), comparer, (string)value),
		};

		private readonly Dictionary<string, Func<SortOrder, bool, IOrderByElement>> orderByHandlers = new Dictionary<string, Func<SortOrder, bool, IOrderByElement>>
		{
			[RelationshipObjectTypeExposers.Id.fieldName] = HandleGuid,
			[RelationshipObjectTypeExposers.Name.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcRelationshipsIds.Sections.ObjectTypeInfo.ObjectName), sortOrder, naturalSort),
		};

		protected override Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> FilterHandlers => handlers;

		protected override FilterElement<DomInstance> DomDefinitionFilter => objectTypeDomDefinitionFilter;

		protected override Dictionary<string, Func<SortOrder, bool, IOrderByElement>> OrderByHandlers => orderByHandlers;
	}
}
