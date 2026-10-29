namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

	using SLDataGateway.API.Types.Querying;

	internal class FlowRulesFilterTranslator : DomInstanceFilterTranslator<FlowRules>
	{
		private readonly FilterElement<DomInstance> flowRulesDomDefinitionFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Flowrules.Id);
		private readonly Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>>
		{
			[FlowRulesExposers.Id.fieldName] = HandleGuid,
			[FlowRulesExposers.OwnerType.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.Flowrulesinfo.Ownertype), comparer, (int)FlowRuleObject.ToStorage((FlowRuleObjectType)value)),
			[FlowRulesExposers.OwnerId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.Flowrulesinfo.Owner), comparer, Convert.ToString(value)),
			[FlowRulesExposers.Rules.TargetId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.Flowrule.Target), comparer, Convert.ToString(value)),
		};

		private readonly Dictionary<string, Func<SortOrder, bool, IOrderByElement>> orderByHandlers = new Dictionary<string, Func<SortOrder, bool, IOrderByElement>>
		{
			[FlowRulesExposers.Id.fieldName] = HandleGuid,
			[FlowRulesExposers.OwnerType.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.Flowrulesinfo.Ownertype), sortOrder, naturalSort),
			[FlowRulesExposers.OwnerId.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.Flowrulesinfo.Owner), sortOrder, naturalSort),
		};

		protected override Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> FilterHandlers => handlers;

		protected override FilterElement<DomInstance> DomDefinitionFilter => flowRulesDomDefinitionFilter;

		protected override Dictionary<string, Func<SortOrder, bool, IOrderByElement>> OrderByHandlers => orderByHandlers;
	}
}
