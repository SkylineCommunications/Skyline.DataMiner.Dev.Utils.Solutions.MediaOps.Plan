namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcRelationships;

	using SLDataGateway.API.Types.Querying;

	internal class RelationshipFilterTranslator : DomInstanceFilterTranslator<Relationship>
	{
		private readonly FilterElement<DomInstance> linkDomDefinitionFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcRelationshipsIds.Definitions.Links.Id);
		private readonly Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>>
		{
			[RelationshipExposers.Id.fieldName] = HandleGuid,
			[RelationshipExposers.Parent.ObjectTypeId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcRelationshipsIds.Sections.LinkInfo.ParentObjectType), comparer, (Guid)value),
			[RelationshipExposers.Parent.ObjectId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcRelationshipsIds.Sections.LinkInfo.ParentObjectID), comparer, (string)value),
			[RelationshipExposers.Parent.ObjectName.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcRelationshipsIds.Sections.LinkInfo.ParentObjectName), comparer, (string)value),
			[RelationshipExposers.Parent.Url.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcRelationshipsIds.Sections.LinkInfo.ParentURL), comparer, (string)value),
			[RelationshipExposers.Parent.Order.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcRelationshipsIds.Sections.LinkInfo.ParentOrder), comparer, (long)value),
			[RelationshipExposers.Child.ObjectTypeId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcRelationshipsIds.Sections.LinkInfo.ChildObjectType), comparer, (Guid)value),
			[RelationshipExposers.Child.ObjectId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcRelationshipsIds.Sections.LinkInfo.ChildObjectID), comparer, (string)value),
			[RelationshipExposers.Child.ObjectName.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcRelationshipsIds.Sections.LinkInfo.ChildObjectName), comparer, (string)value),
			[RelationshipExposers.Child.Url.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcRelationshipsIds.Sections.LinkInfo.ChildURL), comparer, (string)value),
			[RelationshipExposers.Child.Order.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcRelationshipsIds.Sections.LinkInfo.ChildOrder), comparer, (long)value),
		};

		private readonly Dictionary<string, Func<SortOrder, bool, IOrderByElement>> orderByHandlers = new Dictionary<string, Func<SortOrder, bool, IOrderByElement>>
		{
			[RelationshipExposers.Id.fieldName] = HandleGuid,
			[RelationshipExposers.Parent.ObjectName.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcRelationshipsIds.Sections.LinkInfo.ParentObjectName), sortOrder, naturalSort),
			[RelationshipExposers.Parent.Order.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcRelationshipsIds.Sections.LinkInfo.ParentOrder), sortOrder, naturalSort),
			[RelationshipExposers.Child.ObjectName.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcRelationshipsIds.Sections.LinkInfo.ChildObjectName), sortOrder, naturalSort),
			[RelationshipExposers.Child.Order.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcRelationshipsIds.Sections.LinkInfo.ChildOrder), sortOrder, naturalSort),
		};

		protected override Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> FilterHandlers => handlers;

		protected override FilterElement<DomInstance> DomDefinitionFilter => linkDomDefinitionFilter;

		protected override Dictionary<string, Func<SortOrder, bool, IOrderByElement>> OrderByHandlers => orderByHandlers;
	}
}
