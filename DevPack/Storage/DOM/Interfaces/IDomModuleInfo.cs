namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.Sections;

	internal interface IDomModuleInfo
	{
		string ModuleId { get; }

		ModuleSettings ModuleSettings { get; }

		IEnumerable<IDomDefinitionInfo> Definitions { get; }

		// Schema objects that must exist even though no definition links to them.
		IEnumerable<CustomSectionDefinition> AdditionalSectionDefinitions { get; }

		IEnumerable<DomBehaviorDefinition> BehaviorDefinitions { get; }
	}
}
