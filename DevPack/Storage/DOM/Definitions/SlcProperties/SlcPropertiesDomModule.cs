namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcProperties
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties;

	internal sealed class SlcPropertiesDomModule : IDomModuleInfo
	{
		public string ModuleId => ModuleSettings.ModuleId;

		public ModuleSettings ModuleSettings { get; } = new ModuleSettings(SlcPropertiesIds.ModuleId);

		public IEnumerable<IDomDefinitionInfo> Definitions { get; } = new IDomDefinitionInfo[]
		{
			new PropertyDefinition(),
			new PropertyValuesDefinition(),
		};

		public IEnumerable<CustomSectionDefinition> AdditionalSectionDefinitions => Array.Empty<CustomSectionDefinition>();

		public IEnumerable<DomBehaviorDefinition> BehaviorDefinitions => Array.Empty<DomBehaviorDefinition>();
	}
}
