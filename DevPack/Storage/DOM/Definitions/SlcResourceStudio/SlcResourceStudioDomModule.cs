namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcResourceStudio
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Settings;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

	internal sealed class SlcResourceStudioDomModule : IDomModuleInfo
	{
		public string ModuleId => ModuleSettings.ModuleId;

		public ModuleSettings ModuleSettings { get; } = new ModuleSettings(SlcResource_StudioIds.ModuleId)
		{
			DomManagerSettings = new DomManagerSettings
			{
				ScriptSettings = new ExecuteScriptOnDomInstanceActionSettings { ScriptType = OnDomInstanceActionScriptType.FullCrudMeta },
				SecuritySettings = new()
				{
					ConfigurePermission = (PermissionFlags)170,
					CreateAndUpdateDomInstancePermission = (PermissionFlags)170,
					DeleteDomInstancePermission = (PermissionFlags)170,
					ViewPermission = (PermissionFlags)170,
				},
			},
		};

		public IEnumerable<IDomDefinitionInfo> Definitions { get; } = new IDomDefinitionInfo[]
		{
			new ResourcePropertyDefinition(),
			new ResourcePoolDefinition(),
			new ResourceDefinition(),
			new ConfigurationDefinition(),
			new FlowRulesDefinition(),
		};

		public IEnumerable<CustomSectionDefinition> AdditionalSectionDefinitions => Array.Empty<CustomSectionDefinition>();

		public IEnumerable<DomBehaviorDefinition> BehaviorDefinitions { get; } = new[] { ResourceBehaviorDefinition.Create(), ResourcePoolBehaviorDefinition.Create() };
	}
}
