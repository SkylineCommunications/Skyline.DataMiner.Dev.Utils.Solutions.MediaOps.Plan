namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcWorkflow
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	internal sealed class SlcWorkflowDomModule : IDomModuleInfo
	{
		public string ModuleId => ModuleSettings.ModuleId;

		public ModuleSettings ModuleSettings { get; } = new ModuleSettings(SlcWorkflowIds.ModuleId);

		public IEnumerable<IDomDefinitionInfo> Definitions { get; } = new IDomDefinitionInfo[]
		{
			new RecurringJobsDefinition(),
			new JobsDefinition(),
			new JobsLiteDefinition(),
			new WorkflowsDefinition(),
			new ConfigurationDefinition(),
			new AppSettingsDefinition(),
			new JobNodeRelationshipActionsDefinition(),
			new RatesDefinition(),
		};

		public IEnumerable<CustomSectionDefinition> AdditionalSectionDefinitions => Array.Empty<CustomSectionDefinition>();

		public IEnumerable<DomBehaviorDefinition> BehaviorDefinitions { get; } = new[] { JobBehaviorDefinition.Create(), WorkflowBehaviorDefinition.Create(), RecurringJobBehaviorDefinition.Create() };
	}
}
