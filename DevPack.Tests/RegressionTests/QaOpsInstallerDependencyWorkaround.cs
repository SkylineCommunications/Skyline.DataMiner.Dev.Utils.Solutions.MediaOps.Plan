namespace RT_MediaOps.Plan.RegressionTests
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.Categories.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Temporary workaround for Skyline.DataMiner.Sdk 2.6.2 excluding solution-library DLLs from
	/// Scripts/InstallDependencies, preventing the QAOps installer from compiling with the Plan API.
	/// Once fixed, move this provisioning back to QAOpsPackage/QAOpsPackage.cs, restore its DevPack
	/// project reference, and delete this file plus the single call in IntegrationTestContext.Initialize.
	/// </summary>
	internal static class QaOpsInstallerDependencyWorkaround
	{
		internal static void Apply(IConnection connection, ConsolePlanLogger logger)
		{
			if (!Config.IsQaOps)
			{
				return;
			}

			logger.Warning("Applying temporary QAOps installer dependency workaround.");
			var planApi = connection.GetMediaOpsPlanApi();
			logger.Information("Ensuring MediaOps Plan DOM schemas exist before seeding test prerequisites.");
			planApi.InstallDomModules();

			var categoriesApi = connection.GetCategoriesApi();
			EnsureCategoryScope(categoriesApi, CategoryScopes.JobTypes, logger);
			EnsureCategoryScope(categoriesApi, CategoryScopes.ResourcePools, logger);
			EnsureJobRelationshipObjectType(planApi, connection, logger);
		}

		private static void EnsureCategoryScope(ICategoriesApi categoriesApi, string name, ConsolePlanLogger logger)
		{
			if (categoriesApi.Scopes.Read(ScopeExposers.Name.Equal(name)).Any())
			{
				logger.Information($"Category scope '{name}' already exists; preserving it.");
				return;
			}

			categoriesApi.Scopes.Create(new Scope { Name = name });
			logger.Information($"Created category scope '{name}'.");
		}

		private static void EnsureJobRelationshipObjectType(IMediaOpsPlanApi planApi, IConnection connection, ConsolePlanLogger logger)
		{
			const string objectName = "Job";
			if (planApi.RelationshipObjectTypes.Read(RelationshipObjectTypeExposers.Name.Equal(objectName)).Any())
			{
				logger.Information("Relationship object type 'Job' already exists; preserving it.");
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
			logger.Information("Created reserved relationship object type 'Job'.");
		}
	}
}
