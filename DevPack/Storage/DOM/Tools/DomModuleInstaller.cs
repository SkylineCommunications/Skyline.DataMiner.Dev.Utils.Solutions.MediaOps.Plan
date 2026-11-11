namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Tools
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;

	internal static class DomModuleInstaller
	{
		internal static void Install(Func<DMSMessage[], DMSMessage[]> messageHandler, IDomModuleInfo module, Action<string> logAction = null)
		{
			if (messageHandler == null)
			{
				throw new ArgumentNullException(nameof(messageHandler));
			}

			if (module == null)
			{
				throw new ArgumentNullException(nameof(module));
			}

			if (module.ModuleSettings == null || String.IsNullOrWhiteSpace(module.ModuleId) || module.ModuleId != module.ModuleSettings.ModuleId)
			{
				throw new ArgumentException("Module settings must specify the declared module ID.", nameof(module));
			}

			var definitions = module.Definitions?.ToArray() ?? throw new ArgumentException("Definitions cannot be null.", nameof(module));
			var behaviors = module.BehaviorDefinitions?.ToArray() ?? throw new ArgumentException("Behavior definitions cannot be null.", nameof(module));
			var sections = GetSectionDefinitions(module, definitions);
			if (definitions.Any(x => x?.Definition == null) || behaviors.Any(x => x == null))
			{
				throw new ArgumentException("Definitions and behavior definitions cannot contain null entries.", nameof(module));
			}

			if (definitions.Select(x => x.Definition.ID).Distinct().Count() != definitions.Length || behaviors.Select(x => x.ID).Distinct().Count() != behaviors.Length)
			{
				throw new ArgumentException("Definitions and behavior definitions must have unique IDs within a module.", nameof(module));
			}

			var settingsHelper = new ModuleSettingsHelper(messageHandler);
			var helper = new DomHelper(messageHandler, module.ModuleId);
			CreateOrUpdateModuleSettings(settingsHelper, module.ModuleSettings, logAction);

			// Plan also owns behaviors and unlinked sections. Install all sections first,
			// then behaviors, then the definitions that reference them.
			foreach (var section in sections)
			{
				CreateOrUpdateSectionDefinition(helper, section, logAction);
			}

			foreach (var behavior in behaviors)
			{
				CreateOrUpdateBehaviorDefinition(helper, behavior, logAction);
			}

			foreach (var definition in definitions)
			{
				CreateOrUpdateDomDefinition(helper, definition.Definition, logAction);
			}

			ReinitializeDomManager(messageHandler, module.ModuleId, logAction);
		}

		internal static bool CompareSectionDefinitions(SectionDefinition definition, SectionDefinition other)
		{
			if (definition == null)
			{
				throw new ArgumentNullException(nameof(definition));
			}

			// The typed custom-section overload compares more than the base overload.
			if (definition is CustomSectionDefinition customDefinition && other is CustomSectionDefinition customOther)
			{
				return customDefinition.Equals(customOther);
			}

			return definition.Equals(other);
		}

		private static CustomSectionDefinition[] GetSectionDefinitions(IDomModuleInfo module, IDomDefinitionInfo[] definitions)
		{
			var additional = module.AdditionalSectionDefinitions ?? throw new ArgumentException("Additional section definitions cannot be null.", nameof(module));
			var sections = new List<CustomSectionDefinition>();
			var sectionsById = new Dictionary<SectionDefinitionID, CustomSectionDefinition>();

			foreach (var definition in definitions)
			{
				if (definition?.Definition == null || definition.SectionDefinitions == null)
				{
					throw new ArgumentException("Definitions and their section collections cannot be null.", nameof(module));
				}
			}

			foreach (var section in definitions.SelectMany(x => x.SectionDefinitions).Concat(additional))
			{
				if (section == null)
				{
					throw new ArgumentException("Section definitions cannot contain null entries.", nameof(module));
				}

				if (sectionsById.TryGetValue(section.GetID(), out var existing))
				{
					if (!CompareSectionDefinitions(section, existing))
					{
						throw new ArgumentException($"Conflicting declarations for section '{section.GetID()}'.", nameof(module));
					}

					continue;
				}

				sectionsById.Add(section.GetID(), section);
				sections.Add(section);
			}

			return sections.ToArray();
		}

		private static void CreateOrUpdateModuleSettings(ModuleSettingsHelper helper, ModuleSettings settings, Action<string> logAction)
		{
			var existing = helper.ModuleSettings.Read(ModuleSettingsExposers.ModuleId.Equal(settings.ModuleId)).SingleOrDefault();
			if (existing == null)
			{
				Log(logAction, "Creating", settings);
				helper.ModuleSettings.Create(settings);
			}
			else if (settings.Equals(existing))
			{
				Log(logAction, "Skipping", settings);
			}
			else
			{
				Log(logAction, "Updating", settings);
				helper.ModuleSettings.Update(settings);
			}
		}

		private static void CreateOrUpdateSectionDefinition(DomHelper helper, CustomSectionDefinition definition, Action<string> logAction)
		{
			var existing = helper.SectionDefinitions.Read(SectionDefinitionExposers.ID.Equal(definition.GetID())).SingleOrDefault();
			if (existing == null)
			{
				Log(logAction, "Creating", definition);
				helper.SectionDefinitions.Create(definition);
			}
			else if (CompareSectionDefinitions(definition, existing))
			{
				Log(logAction, "Skipping", definition);
			}
			else
			{
				Log(logAction, "Updating", definition);
				var incomingIds = new HashSet<FieldDescriptorID>(definition.GetAllFieldDescriptors().Select(x => x.ID));
				foreach (var field in existing.GetAllFieldDescriptors().Where(x => !incomingIds.Contains(x.ID)))
				{
					field.IsSoftDeleted = true;
					definition.AddOrReplaceFieldDescriptor(field);
				}

				helper.SectionDefinitions.Update(definition);
			}
		}

		private static void CreateOrUpdateBehaviorDefinition(DomHelper helper, DomBehaviorDefinition definition, Action<string> logAction)
		{
			var existing = helper.DomBehaviorDefinitions.Read(DomBehaviorDefinitionExposers.Id.Equal(definition.ID)).SingleOrDefault();
			if (existing == null)
			{
				Log(logAction, "Creating", definition);
				helper.DomBehaviorDefinitions.Create(definition);
			}
			else if (definition.Equals(existing))
			{
				Log(logAction, "Skipping", definition);
			}
			else
			{
				Log(logAction, "Updating", definition);
				helper.DomBehaviorDefinitions.Update(definition);
			}
		}

		private static void CreateOrUpdateDomDefinition(DomHelper helper, DomDefinition definition, Action<string> logAction)
		{
			var existing = helper.DomDefinitions.Read(DomDefinitionExposers.Id.Equal(definition.ID)).SingleOrDefault();
			if (existing == null)
			{
				Log(logAction, "Creating", definition);
				helper.DomDefinitions.Create(definition);
			}
			else if (definition.Equals(existing))
			{
				Log(logAction, "Skipping", definition);
			}
			else
			{
				Log(logAction, "Updating", definition);
				var incomingIds = new HashSet<SectionDefinitionID>(definition.SectionDefinitionLinks.Select(x => x.SectionDefinitionID));
				foreach (var link in existing.SectionDefinitionLinks.Where(x => !incomingIds.Contains(x.SectionDefinitionID)))
				{
					link.IsSoftDeleted = true;
					definition.SectionDefinitionLinks.Add(link);
				}

				helper.DomDefinitions.Update(definition);
			}
		}

		private static void ReinitializeDomManager(Func<DMSMessage[], DMSMessage[]> messageHandler, string moduleId, Action<string> logAction)
		{
			logAction?.Invoke($"Reinitializing DOM Manager for module '{moduleId}'.");
			var responses = messageHandler(new DMSMessage[] { new ManagerStoreReinitializeCustomManagerRequest(moduleId, "DomManager") });
			var response = responses?.OfType<ManagerStoreReinitializeCustomManagerResponse>().SingleOrDefault();
			if (response?.TraceData == null || !response.TraceData.HasSucceeded())
			{
				throw new InvalidOperationException($"Failed to reinitialize DOM Manager for module '{moduleId}'. Trace: {response?.TraceData?.ToString() ?? "<NULL>"}");
			}
		}

		private static void Log(Action<string> logAction, string action, DataType dataType)
		{
			string name;
			switch (dataType)
			{
				case ModuleSettings settings:
					name = settings.ModuleId;
					break;
				case CustomSectionDefinition section:
					name = section.Name;
					break;
				case DomBehaviorDefinition behavior:
					name = behavior.Name;
					break;
				case DomDefinition definition:
					name = definition.Name;
					break;
				default:
					name = String.Empty;
					break;
			}

			logAction?.Invoke($"{action} {dataType.GetType().Name}: {name} [{dataType.DataTypeID}].");
		}
	}
}
