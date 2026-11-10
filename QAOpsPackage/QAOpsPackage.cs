using System;
using System.Linq;

using Skyline.AppInstaller;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net;
using Skyline.DataMiner.Net.AppPackages;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
using Skyline.DataMiner.Net.Messages.SLDataGateway;
using Skyline.DataMiner.Net.Sections;
using Skyline.DataMiner.Solutions.Categories.API;
using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

/// <summary>
/// DataMiner Script Class.
/// </summary>
internal class Script
{
	/// <summary>
	/// The script entry point.
	/// </summary>
	/// <param name="engine">Provides access to the Automation engine.</param>
	/// <param name="context">Provides access to the installation context.</param>
	[AutomationEntryPoint(AutomationEntryPointType.Types.InstallAppPackage)]
	public void Install(IEngine engine, AppInstallContext context)
	{
		try
		{
			engine.Timeout = new TimeSpan(0, 10, 0);
			engine.GenerateInformation("Starting installation");
			var installer = new AppInstaller(Engine.SLNetRaw, context);
			installer.InstallDefaultContent();

			var connection = Engine.SLNetRaw;
			var planApi = connection.GetMediaOpsPlanApi();
			installer.Log("Ensuring MediaOps Plan DOM schemas exist before seeding test prerequisites.");
			planApi.InstallDomModules();

			var categoriesApi = connection.GetCategoriesApi();
			EnsureCategoryScope(categoriesApi, CategoryScopes.JobTypes, installer);
			EnsureCategoryScope(categoriesApi, CategoryScopes.ResourcePools, installer);
			EnsureJobRelationshipObjectType(planApi, connection, installer);
		}
		catch (Exception e) when (!(e is ScriptAbortException || e is ScriptForceAbortException || e is ScriptTimeoutException || e is InteractiveUserDetachedException))
		{
			engine.Log("Install|Unexpected exception: " + e);
			engine.ExitFail("QAOps prerequisite installation failed. See the Automation log for details.");
		}
	}

	private static void EnsureCategoryScope(ICategoriesApi categoriesApi, string name, AppInstaller installer)
	{
		if (categoriesApi.Scopes.Read(ScopeExposers.Name.Equal(name)).Any())
		{
			installer.Log($"Category scope '{name}' already exists; preserving it.");
			return;
		}

		categoriesApi.Scopes.Create(new Scope { Name = name });
		installer.Log($"Created category scope '{name}'.");
	}

	private static void EnsureJobRelationshipObjectType(IMediaOpsPlanApi planApi, IConnection connection, AppInstaller installer)
	{
		const string objectName = "Job";
		if (planApi.RelationshipObjectTypes.Read(RelationshipObjectTypeExposers.Name.Equal(objectName)).Any())
		{
			installer.Log("Relationship object type 'Job' already exists; preserving it.");
			return;
		}

		// Match the solution's DOM seeding path: the public repository rejects reserved names.
		// These stable schema IDs are owned by DevPack/Storage/DOM/Model/SlcRelationshipsGeneratedDomCode.cs.
		const string moduleId = "(slc)relationships";
		var definitionId = new DomDefinitionId(new Guid("7ab57219-6b21-4abb-83a4-f5c22de38f20")) { ModuleId = moduleId };
		var sectionId = new SectionDefinitionID(new Guid("ac97c003-884e-48d7-9ff3-e84b13a349be")) { ModuleId = moduleId };
		var fieldId = new FieldDescriptorID(new Guid("fd6b00c0-0f16-4025-8f13-f9b18f9e2fe0"));
		var section = new Section(sectionId);
		section.AddOrReplaceFieldValue(new FieldValue(fieldId, new ValueWrapper<string>(objectName)));

		var helper = new DomHelper(connection.HandleMessages, moduleId);
		helper.DomInstances.Create(new DomInstance
		{
			ID = new DomInstanceId(Guid.NewGuid()) { ModuleId = moduleId },
			DomDefinitionId = definitionId,
			Sections = { section },
		});
		installer.Log("Created reserved relationship object type 'Job'.");
	}
}
