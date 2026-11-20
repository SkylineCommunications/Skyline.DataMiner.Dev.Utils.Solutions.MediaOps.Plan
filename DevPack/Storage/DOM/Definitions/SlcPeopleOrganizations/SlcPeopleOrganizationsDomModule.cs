namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;

	internal sealed class SlcPeopleOrganizationsDomModule : IDomModuleInfo
	{
		public string ModuleId => ModuleSettings.ModuleId;

		public ModuleSettings ModuleSettings { get; } = new ModuleSettings(SlcPeopleOrganizationsIds.ModuleId);

		public IEnumerable<IDomDefinitionInfo> Definitions { get; } = new IDomDefinitionInfo[]
		{
			new PeopleDefinition(),
			new TeamsDefinition(),
			new ExperienceDefinition(),
			new RoleDefinition(),
			new CategoryDefinition(),
			new OrganizationsDefinition(),
		};

		public IEnumerable<CustomSectionDefinition> AdditionalSectionDefinitions { get; } = new[] { BillingSectionDefinition.Create() };

		public IEnumerable<DomBehaviorDefinition> BehaviorDefinitions { get; } = new[] { PeopleBehaviorDefinition.Create(), OrganizationsBehaviorDefinition.Create(), TeamsBehaviorDefinition.Create() };
	}
}
