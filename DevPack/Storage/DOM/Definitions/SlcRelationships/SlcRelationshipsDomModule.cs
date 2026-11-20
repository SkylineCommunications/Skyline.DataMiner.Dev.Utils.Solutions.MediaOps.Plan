namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcRelationships
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcRelationships;

	internal sealed class SlcRelationshipsDomModule : IDomModuleInfo
	{
		public string ModuleId => ModuleSettings.ModuleId;

		public ModuleSettings ModuleSettings { get; } = new ModuleSettings(SlcRelationshipsIds.ModuleId);

		public IEnumerable<IDomDefinitionInfo> Definitions { get; } = new IDomDefinitionInfo[]
		{
			new ObjectTypesDefinition(),
			new LinksDefinition(),
		};

		public IEnumerable<CustomSectionDefinition> AdditionalSectionDefinitions => Array.Empty<CustomSectionDefinition>();

		public IEnumerable<DomBehaviorDefinition> BehaviorDefinitions => Array.Empty<DomBehaviorDefinition>();
	}
}
