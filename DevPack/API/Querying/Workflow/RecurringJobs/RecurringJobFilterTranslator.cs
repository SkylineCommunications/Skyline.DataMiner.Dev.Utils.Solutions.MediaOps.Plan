namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	internal class RecurringJobFilterTranslator : DomInstanceFilterTranslator<RecurringJob>
	{
		private readonly FilterElement<DomInstance> recurringJobsDomDefinitionFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.RecurringJobs.Id);
		private readonly Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>>
		{
			[RecurringJobExposers.Id.fieldName] = HandleGuid,
			[RecurringJobExposers.Name.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobName), comparer, (string)value),
		};

		protected override FilterElement<DomInstance> DomDefinitionFilter => recurringJobsDomDefinitionFilter;

		protected override Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> Handlers => handlers;
	}
}