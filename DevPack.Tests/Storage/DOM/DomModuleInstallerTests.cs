namespace RT_MediaOps.Plan.Storage.DOM
{
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResponseErrorData;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcProperties;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcResourceStudio;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Tools;

	[TestClass]
	[TestCategory("UnitTest")]
	public sealed class DomModuleInstallerTests
	{
		private const string ModuleId = "plan_installer_unit_test";
		private static readonly Guid DefinitionGuid = new Guid("10000000-0000-0000-0000-000000000001");
		private static readonly Guid MainSectionGuid = new Guid("20000000-0000-0000-0000-000000000001");
		private static readonly Guid RemovedSectionGuid = new Guid("20000000-0000-0000-0000-000000000002");
		private static readonly FieldDescriptorID MainFieldId = new FieldDescriptorID(new Guid("30000000-0000-0000-0000-000000000001"));
		private static readonly FieldDescriptorID RemovedFieldId = new FieldDescriptorID(new Guid("30000000-0000-0000-0000-000000000002"));

		[TestMethod]
		public void EmptyStore_CreatesSettingsSectionsAndDefinitions_ThenReinitializes()
		{
			var store = new InMemoryDomSchemaStore();
			var log = new List<string>();

			DomModuleInstaller.Install(store.HandleMessages, new SlcPropertiesDomModule(), log.Add);

			Assert.AreEqual(1, store.Messages.OfType<ManagerStoreCreateRequest<ModuleSettings>>().Count());
			Assert.AreEqual(5, store.Messages.OfType<ManagerStoreCreateRequest<SectionDefinition>>().Count());
			Assert.AreEqual(2, store.Messages.OfType<ManagerStoreCreateRequest<DomDefinition>>().Count());
			Assert.IsInstanceOfType<ManagerStoreReinitializeCustomManagerRequest>(store.Messages.Last());
			Assert.IsTrue(log.First().StartsWith("Creating ModuleSettings:", StringComparison.Ordinal));
		}

		[TestMethod]
		public void NullMessageHandler_ThrowsBeforeAnyWork()
		{
			Assert.ThrowsException<ArgumentNullException>(() => DomModuleInstaller.Install(null!, new SlcPropertiesDomModule()));
		}

		[TestMethod]
		public void NullModule_ThrowsBeforeAnyMessages()
		{
			var store = new InMemoryDomSchemaStore();
			Assert.ThrowsException<ArgumentNullException>(() => DomModuleInstaller.Install(store.HandleMessages, null!));
			Assert.AreEqual(0, store.Messages.Count);
		}

		[TestMethod]
		public void SharedSections_AreWrittenOnceBeforeBehaviorsAndDefinitions()
		{
			var store = new InMemoryDomSchemaStore();

			DomModuleInstaller.Install(store.HandleMessages, new SlcResourceStudioDomModule());

			Assert.AreEqual(23, store.Messages.OfType<ManagerStoreCreateRequest<SectionDefinition>>().Count());
			var lastSection = store.Messages.FindLastIndex(x => x is ManagerStoreCreateRequest<SectionDefinition>);
			var firstBehavior = store.Messages.FindIndex(x => x is ManagerStoreCreateRequest<DomBehaviorDefinition>);
			var lastBehavior = store.Messages.FindLastIndex(x => x is ManagerStoreCreateRequest<DomBehaviorDefinition>);
			var firstDefinition = store.Messages.FindIndex(x => x is ManagerStoreCreateRequest<DomDefinition>);
			Assert.IsTrue(lastSection < firstBehavior);
			Assert.IsTrue(lastBehavior < firstDefinition);
			Assert.IsInstanceOfType<ManagerStoreReinitializeCustomManagerRequest>(store.Messages.Last());
		}

		[TestMethod]
		public void EqualObjects_AreSkippedWithoutSchemaWrites()
		{
			var store = new InMemoryDomSchemaStore();
			DomModuleInstaller.Install(store.HandleMessages, CreateModule());
			store.Messages.Clear();
			var log = new List<string>();

			DomModuleInstaller.Install(store.HandleMessages, CreateModule(), log.Add);

			Assert.AreEqual(3, log.Count(x => x.StartsWith("Skipping ", StringComparison.Ordinal)));
			Assert.AreEqual(4, log.Count);
			Assert.IsFalse(log.Any(x => x.StartsWith("Creating ", StringComparison.Ordinal) || x.StartsWith("Updating ", StringComparison.Ordinal)));
		}

		[TestMethod]
		public void PartialInstallation_CreatesMissingDefinitionWithoutRewritingEqualObjects()
		{
			var store = new InMemoryDomSchemaStore();
			var module = CreateModule();
			store.ModuleSettings.Add(module.ModuleSettings);
			store.Handler.SetSectionDefinitions(ModuleId, new[] { MainSection(module) });
			var log = new List<string>();

			DomModuleInstaller.Install(store.HandleMessages, CreateModule(), log.Add);

			Assert.AreEqual(2, log.Count(x => x.StartsWith("Skipping ", StringComparison.Ordinal)));
			Assert.AreEqual(1, store.Messages.OfType<ManagerStoreCreateRequest<DomDefinition>>().Count());
			Assert.AreEqual(0, store.Messages.OfType<ManagerStoreCreateRequest<SectionDefinition>>().Count());
		}

		[TestMethod]
		public void ChangedModuleSettings_AreAuthoritative()
		{
			var store = new InMemoryDomSchemaStore();
			DomModuleInstaller.Install(store.HandleMessages, CreateModule());
			store.Messages.Clear();
			var incoming = CreateModule();
			incoming.ModuleSettings.DomManagerSettings.InformationEventSettings.Enable = true;

			DomModuleInstaller.Install(store.HandleMessages, incoming);

			Assert.AreEqual(1, store.Messages.OfType<ManagerStoreUpdateRequest<ModuleSettings>>().Count());
			Assert.IsTrue(store.ModuleSettings.Single().DomManagerSettings.InformationEventSettings.Enable);
		}

		[TestMethod]
		public void SameIdRenames_UpdateExistingSectionsFieldsAndDefinitions()
		{
			var store = new InMemoryDomSchemaStore();
			DomModuleInstaller.Install(store.HandleMessages, CreateModule());
			store.Messages.Clear();
			var incoming = CreateModule();
			MainSection(incoming).Name = "Renamed section";
			MainSection(incoming).GetAllFieldDescriptors().Single().Name = "Renamed field";
			incoming.Definitions.Single().Definition.Name = "Renamed definition";

			DomModuleInstaller.Install(store.HandleMessages, incoming);

			Assert.AreEqual(1, store.Messages.OfType<ManagerStoreUpdateRequest<SectionDefinition>>().Count());
			Assert.AreEqual(1, store.Messages.OfType<ManagerStoreUpdateRequest<DomDefinition>>().Count());
			Assert.AreEqual("Renamed section", ReadMainSection(store).Name);
			Assert.AreEqual("Renamed field", ReadMainSection(store).GetAllFieldDescriptors().Single().Name);
			Assert.AreEqual("Renamed definition", ReadDefinition(store).Name);
		}

		[TestMethod]
		public void SameNameWithDifferentId_CreatesAnotherDefinition()
		{
			var store = new InMemoryDomSchemaStore();
			DomModuleInstaller.Install(store.HandleMessages, CreateModule());
			store.Messages.Clear();
			var incoming = CreateModule();
			incoming.Definitions.Single().Definition.ID = new DomDefinitionId(new Guid("10000000-0000-0000-0000-000000000002")) { ModuleId = ModuleId };

			DomModuleInstaller.Install(store.HandleMessages, incoming);

			Assert.AreEqual(1, store.Messages.OfType<ManagerStoreCreateRequest<DomDefinition>>().Count());
			Assert.AreEqual(0, store.Messages.OfType<ManagerStoreUpdateRequest<DomDefinition>>().Count());
			Assert.AreEqual(2, new DomHelper(store.HandleMessages, ModuleId).DomDefinitions.ReadAll().Count);
		}

		[TestMethod]
		public void ChangedBehavior_UpdatesById()
		{
			var store = new InMemoryDomSchemaStore();
			var previous = CreateModule();
			previous.BehaviorDefinitions = new[] { CreateBehavior() };
			DomModuleInstaller.Install(store.HandleMessages, previous);
			store.Messages.Clear();
			var incoming = CreateModule();
			var behavior = CreateBehavior();
			behavior.Name = "Renamed behavior";
			incoming.BehaviorDefinitions = new[] { behavior };

			DomModuleInstaller.Install(store.HandleMessages, incoming);

			var update = store.Messages.OfType<ManagerStoreUpdateRequest<DomBehaviorDefinition>>().Single();
			Assert.AreEqual(behavior.ID, update.Object.ID);
			Assert.AreEqual("Renamed behavior", update.Object.Name);
		}

		[TestMethod]
		public void RemovedFieldsAndLinks_RetainTheirOriginalMetadataAsSoftDeleted()
		{
			var store = new InMemoryDomSchemaStore();
			DomModuleInstaller.Install(store.HandleMessages, CreateModule(includeRemovedContent: true));
			var existing = ReadMainSection(store);
			var oldField = existing.GetAllFieldDescriptors().Single(x => x.ID.Equals(RemovedFieldId));
			var oldLink = ReadDefinition(store).SectionDefinitionLinks.Last();
			var incoming = CreateModule();
			// Return these exact objects so the test also verifies Live's in-memory mutation semantics.
			store.OverrideResponse = message => message is ManagerStoreReadRequest<SectionDefinition> request
				? new ManagerStoreCrudResponse<SectionDefinition>(request.Query.ExecuteInMemory(new[] { existing }).ToList())
				: message is ManagerStoreReadRequest<DomDefinition>
					? new ManagerStoreCrudResponse<DomDefinition>(new[] { CreateExistingDefinitionWithLink(oldLink) }.ToList())
					: null;

			DomModuleInstaller.Install(store.HandleMessages, incoming);

			var preserved = MainSection(incoming).GetAllFieldDescriptors().Single(x => x.ID.Equals(RemovedFieldId));
			Assert.AreSame(oldField, preserved);
			Assert.IsTrue(preserved.IsSoftDeleted);
			Assert.AreEqual("Retired field", preserved.Name);
			Assert.AreEqual("Keep this metadata.", preserved.Tooltip);
			Assert.AreEqual(typeof(long), preserved.FieldType);
			Assert.IsTrue(preserved.IsReadonly);
			Assert.AreEqual(42L, ((ValueWrapper<long>)preserved.DefaultValue).Value);
			var link = incoming.Definitions.Single().Definition.SectionDefinitionLinks.Last();
			Assert.AreSame(oldLink, link);
			Assert.IsTrue(link.IsSoftDeleted);
			Assert.IsTrue(link.IsOptional);
			Assert.IsTrue(link.AllowMultipleSections);
		}

		[TestMethod]
		public void PreviouslySoftDeletedFieldsAndLinks_AreStillPreserved()
		{
			var store = new InMemoryDomSchemaStore();
			var previous = CreateModule(includeRemovedContent: true);
			MainSection(previous).GetAllFieldDescriptors().Last().IsSoftDeleted = true;
			previous.Definitions.Single().Definition.SectionDefinitionLinks.Last().IsSoftDeleted = true;
			DomModuleInstaller.Install(store.HandleMessages, previous);

			DomModuleInstaller.Install(store.HandleMessages, CreateModule());

			Assert.IsTrue(ReadMainSection(store).GetAllFieldDescriptors().Single(x => x.ID.Equals(RemovedFieldId)).IsSoftDeleted);
			Assert.IsTrue(ReadDefinition(store).SectionDefinitionLinks.Last().IsSoftDeleted);
		}

		[TestMethod]
		public void ReintroducedIds_UseIncomingMetadataAndDeletionFlags()
		{
			var store = new InMemoryDomSchemaStore();
			DomModuleInstaller.Install(store.HandleMessages, CreateModule(includeRemovedContent: true));
			DomModuleInstaller.Install(store.HandleMessages, CreateModule());
			var incoming = CreateModule(includeRemovedContent: true);
			var field = MainSection(incoming).GetAllFieldDescriptors().Last();
			field.Name = "Reintroduced field";
			field.IsReadonly = false;
			incoming.Definitions.Single().Definition.SectionDefinitionLinks.Last().IsOptional = false;

			DomModuleInstaller.Install(store.HandleMessages, incoming);

			var storedField = ReadMainSection(store).GetAllFieldDescriptors().Single(x => x.ID.Equals(RemovedFieldId));
			Assert.IsFalse(storedField.IsSoftDeleted);
			Assert.IsFalse(storedField.IsReadonly);
			Assert.AreEqual("Reintroduced field", storedField.Name);
			var storedLink = ReadDefinition(store).SectionDefinitionLinks.Last();
			Assert.IsFalse(storedLink.IsSoftDeleted);
			Assert.IsFalse(storedLink.IsOptional);
		}

		[TestMethod]
		public void ExplicitIncomingTombstones_AreNotDuplicated()
		{
			var store = new InMemoryDomSchemaStore();
			DomModuleInstaller.Install(store.HandleMessages, CreateModule(includeRemovedContent: true));
			var incoming = CreateModule(includeRemovedContent: true);
			MainSection(incoming).GetAllFieldDescriptors().Last().IsSoftDeleted = true;
			incoming.Definitions.Single().Definition.SectionDefinitionLinks.Last().IsSoftDeleted = true;

			DomModuleInstaller.Install(store.HandleMessages, incoming);

			Assert.AreEqual(2, ReadMainSection(store).GetAllFieldDescriptors().Count());
			Assert.AreEqual(2, ReadDefinition(store).SectionDefinitionLinks.Count);
			Assert.IsTrue(ReadMainSection(store).GetAllFieldDescriptors().Last().IsSoftDeleted);
			Assert.IsTrue(ReadDefinition(store).SectionDefinitionLinks.Last().IsSoftDeleted);
		}

		[TestMethod]
		public void OmittedWholeDefinitionsAndSections_AreLeftUntouched()
		{
			var store = new InMemoryDomSchemaStore();
			DomModuleInstaller.Install(store.HandleMessages, CreateModule(includeRemovedContent: true));
			var previousDefinition = ReadDefinition(store).ToJson();
			var previousSection = ReadMainSection(store).ToJson();
			var incoming = CreateModule();
			incoming.Definitions = Array.Empty<IDomDefinitionInfo>();
			store.Messages.Clear();

			DomModuleInstaller.Install(store.HandleMessages, incoming);

			Assert.AreEqual(previousDefinition, ReadDefinition(store).ToJson());
			Assert.AreEqual(previousSection, ReadMainSection(store).ToJson());
			Assert.AreEqual(0, store.Messages.OfType<ManagerStoreUpdateRequest<DomDefinition>>().Count());
			Assert.AreEqual(0, store.Messages.OfType<ManagerStoreUpdateRequest<SectionDefinition>>().Count());
		}

		[TestMethod]
		public void RepeatedInstallAfterRemoval_PreservesExactlyOneCopyOfEachTombstone()
		{
			var store = new InMemoryDomSchemaStore();
			DomModuleInstaller.Install(store.HandleMessages, CreateModule(includeRemovedContent: true));
			DomModuleInstaller.Install(store.HandleMessages, CreateModule());

			DomModuleInstaller.Install(store.HandleMessages, CreateModule());

			Assert.AreEqual(2, ReadMainSection(store).GetAllFieldDescriptors().Count());
			Assert.AreEqual(1, ReadMainSection(store).GetAllFieldDescriptors().Count(x => x.IsSoftDeleted));
			Assert.AreEqual(2, ReadDefinition(store).SectionDefinitionLinks.Count);
			Assert.AreEqual(1, ReadDefinition(store).SectionDefinitionLinks.Count(x => x.IsSoftDeleted));
			// Equality precedes tombstone preservation, as in Live. This intentionally does not promise zero updates.
		}

		[TestMethod]
		public void SectionComparison_UsesTypedEqualityAndSupportsTheFallbackPath()
		{
			var first = MainSection(CreateModule());
			var equal = MainSection(CreateModule());
			Assert.IsTrue(DomModuleInstaller.CompareSectionDefinitions(first, equal));
			equal.GetAllFieldDescriptors().Single().Tooltip = "Changed metadata";
			Assert.IsFalse(DomModuleInstaller.CompareSectionDefinitions(first, equal));
			Assert.IsFalse(DomModuleInstaller.CompareSectionDefinitions(first, null!));
			Assert.ThrowsException<ArgumentNullException>(() => DomModuleInstaller.CompareSectionDefinitions(null!, first));
		}

		[TestMethod]
		public void WriteFailure_StopsWithoutReinitialization_AndCanBeRerun()
		{
			var store = new InMemoryDomSchemaStore();
			store.OverrideResponse = message => message is ManagerStoreCreateRequest<DomDefinition>
				? throw new InvalidOperationException("Injected write failure.")
				: null;

			Assert.ThrowsException<InvalidOperationException>(() => DomModuleInstaller.Install(store.HandleMessages, CreateModule()));

			Assert.AreEqual(1, store.ModuleSettings.Count);
			Assert.AreEqual(1, new DomHelper(store.HandleMessages, ModuleId).SectionDefinitions.ReadAll().Count);
			Assert.AreEqual(0, store.Messages.OfType<ManagerStoreReinitializeCustomManagerRequest>().Count());
			store.OverrideResponse = null;
			store.Messages.Clear();
			var log = new List<string>();

			DomModuleInstaller.Install(store.HandleMessages, CreateModule(), log.Add);

			Assert.AreEqual(2, log.Count(x => x.StartsWith("Skipping ", StringComparison.Ordinal)));
			Assert.AreEqual(1, store.Messages.OfType<ManagerStoreCreateRequest<DomDefinition>>().Count());
			Assert.IsInstanceOfType<ManagerStoreReinitializeCustomManagerRequest>(store.Messages.Last());
		}

		[TestMethod]
		[DataRow(1)]
		[DataRow(2)]
		public void LoggerFailure_PropagatesBeforeTheCurrentWrite(int throwOnLog)
		{
			var store = new InMemoryDomSchemaStore();
			var count = 0;

			Assert.ThrowsException<InvalidOperationException>(() => DomModuleInstaller.Install(store.HandleMessages, CreateModule(), message =>
			{
				if (++count == throwOnLog)
				{
					throw new InvalidOperationException("Injected logger failure.");
				}
			}));

			Assert.AreEqual(throwOnLog - 1, store.ModuleSettings.Count);
			Assert.AreEqual(0, store.Messages.OfType<ManagerStoreCreateRequest<SectionDefinition>>().Count());
			Assert.AreEqual(0, store.Messages.OfType<ManagerStoreReinitializeCustomManagerRequest>().Count());
		}

		[TestMethod]
		[DataRow("missing")]
		[DataRow("null trace")]
		public void MissingReinitializationConfirmation_ThrowsAfterEarlierWrites(string failure)
		{
			var store = new InMemoryDomSchemaStore();
			DMSMessage[] Handle(DMSMessage[] messages)
			{
				var responses = store.HandleMessages(messages);
				if (messages.Any(x => x is ManagerStoreReinitializeCustomManagerRequest))
				{
					return failure == "missing"
						? Array.Empty<DMSMessage>()
						: new DMSMessage[] { new ManagerStoreReinitializeCustomManagerResponse { TraceData = null! } };
				}

				return responses;
			}

			Assert.ThrowsException<InvalidOperationException>(() => DomModuleInstaller.Install(Handle, CreateModule()));
			Assert.AreEqual(1, new DomHelper(store.HandleMessages, ModuleId).DomDefinitions.ReadAll().Count);
		}

		[TestMethod]
		public void UnsuccessfulReinitializationTrace_ThrowsAndRetainsEarlierWrites()
		{
			var store = new InMemoryDomSchemaStore();
			var trace = new TraceData();
			// The trace contract treats any error as failure, regardless of its originating subsystem.
			trace.Add(new ResourceManagerErrorData(ResourceManagerErrorData.Reason.ReservationUpdateCausedReservationsToGoToQuarantine, Guid.Empty)
			{
				Message = "Injected error in the reinitialization response.",
			});
			Assert.IsFalse(trace.HasSucceeded());
			store.OverrideResponse = message => message is ManagerStoreReinitializeCustomManagerRequest
				? new ManagerStoreReinitializeCustomManagerResponse { TraceData = trace }
				: null;

			var exception = Assert.ThrowsException<InvalidOperationException>(() => DomModuleInstaller.Install(store.HandleMessages, CreateModule()));

			StringAssert.Contains(exception.Message, ModuleId);
			Assert.AreEqual(1, new DomHelper(store.HandleMessages, ModuleId).DomDefinitions.ReadAll().Count);
		}

		[TestMethod]
		public void SchemaUpdates_DoNotReadOrWriteExistingInstanceData()
		{
			var store = new InMemoryDomSchemaStore();
			var previous = CreateModule(includeRemovedContent: true);
			MainSection(previous).GetAllFieldDescriptors().Last().IsReadonly = false;
			DomModuleInstaller.Install(store.HandleMessages, previous);
			var helper = new DomHelper(store.HandleMessages, ModuleId);
			var section = new Section(new SectionDefinitionID(MainSectionGuid) { ModuleId = ModuleId });
			section.AddOrReplaceFieldValue(new FieldValue(MainFieldId, new ValueWrapper<string>("Existing value")));
			section.AddOrReplaceFieldValue(new FieldValue(RemovedFieldId, new ValueWrapper<long>(123)));
			var instance = helper.DomInstances.Create(new DomInstance
			{
				ID = new DomInstanceId(Guid.NewGuid()) { ModuleId = ModuleId },
				DomDefinitionId = previous.Definitions.Single().Definition.ID,
				Sections = { section },
			});
			var before = instance.ToJson();
			store.Messages.Clear();
			var incoming = CreateModule();
			incoming.Definitions.Single().Definition.Name = "Renamed definition";

			DomModuleInstaller.Install(store.HandleMessages, incoming);

			Assert.AreEqual(0, store.Messages.OfType<ManagerStoreReadRequest<DomInstance>>().Count());
			Assert.AreEqual(0, store.Messages.OfType<ManagerStoreCreateRequest<DomInstance>>().Count());
			Assert.AreEqual(0, store.Messages.OfType<ManagerStoreUpdateRequest<DomInstance>>().Count());
			Assert.AreEqual(0, store.Messages.OfType<ManagerStoreDeleteRequest<DomInstance>>().Count());
			var after = helper.DomInstances.Read(DomInstanceExposers.Id.Equal(instance.ID)).Single();
			Assert.AreEqual(before, after.ToJson());
		}

		[TestMethod]
		[DataRow("null settings")]
		[DataRow("empty module ID")]
		[DataRow("inconsistent module ID")]
		[DataRow("null definitions")]
		[DataRow("null behaviors")]
		[DataRow("null additional sections")]
		[DataRow("null definition entry")]
		[DataRow("null definition")]
		[DataRow("null section collection")]
		[DataRow("null section entry")]
		[DataRow("null behavior entry")]
		[DataRow("duplicate definitions")]
		[DataRow("duplicate behaviors")]
		[DataRow("conflicting shared sections")]
		public void InvalidDeclarations_FailBeforeAnyMessages(string failure)
		{
			var store = new InMemoryDomSchemaStore();
			var module = CreateModule();
			var definition = (TestDefinition)module.Definitions.Single();
			switch (failure)
			{
				case "null settings": module.ModuleSettings = null!; break;
				case "empty module ID": module.ModuleId = " "; break;
				case "inconsistent module ID": module.ModuleId = "another_module"; break;
				case "null definitions": module.Definitions = null!; break;
				case "null behaviors": module.BehaviorDefinitions = null!; break;
				case "null additional sections": module.AdditionalSectionDefinitions = null!; break;
				case "null definition entry": module.Definitions = new IDomDefinitionInfo[] { null! }; break;
				case "null definition": definition.Definition = null!; break;
				case "null section collection": definition.SectionDefinitions = null!; break;
				case "null section entry": definition.SectionDefinitions = new CustomSectionDefinition[] { null! }; break;
				case "null behavior entry": module.BehaviorDefinitions = new DomBehaviorDefinition[] { null! }; break;
				case "duplicate definitions": module.Definitions = new[] { definition, definition }; break;
				case "duplicate behaviors": module.BehaviorDefinitions = new[] { CreateBehavior(), CreateBehavior() }; break;
				case "conflicting shared sections":
					var conflicting = MainSection(CreateModule());
					conflicting.Name = "Conflicting section";
					module.AdditionalSectionDefinitions = new[] { conflicting };
					break;
				default: Assert.Fail("Unknown invalid-declaration case."); break;
			}

			Assert.ThrowsException<ArgumentException>(() => DomModuleInstaller.Install(store.HandleMessages, module));
			Assert.AreEqual(0, store.Messages.Count);
		}

		[TestMethod]
		public void DuplicateLookupResults_AreNotSilentlyAdopted()
		{
			var store = new InMemoryDomSchemaStore();
			store.ModuleSettings.Add(new ModuleSettings(ModuleId));
			store.ModuleSettings.Add(new ModuleSettings(ModuleId));

			Assert.ThrowsException<InvalidOperationException>(() => DomModuleInstaller.Install(store.HandleMessages, CreateModule()));

			Assert.AreEqual(1, store.Messages.Count);
			Assert.IsInstanceOfType<ManagerStoreReadRequest<ModuleSettings>>(store.Messages.Single());
		}

		private static TestModule CreateModule(bool includeRemovedContent = false)
		{
			var sectionId = new SectionDefinitionID(MainSectionGuid) { ModuleId = ModuleId };
			var section = new CustomSectionDefinition { ID = sectionId, Name = "Info" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = MainFieldId,
				Name = "Name",
				FieldType = typeof(string),
				IsOptional = true,
				Tooltip = String.Empty,
			});
			var definition = new DomDefinition("Definition")
			{
				ID = new DomDefinitionId(DefinitionGuid) { ModuleId = ModuleId },
				SectionDefinitionLinks = { new SectionDefinitionLink(sectionId) },
			};
			var sections = new List<CustomSectionDefinition> { section };
			if (includeRemovedContent)
			{
				section.AddOrReplaceFieldDescriptor(new FieldDescriptor
				{
					ID = RemovedFieldId,
					Name = "Retired field",
					FieldType = typeof(long),
					DefaultValue = new ValueWrapper<long>(42),
					IsOptional = true,
					IsReadonly = true,
					Tooltip = "Keep this metadata.",
				});
				var removedSectionId = new SectionDefinitionID(RemovedSectionGuid) { ModuleId = ModuleId };
				sections.Add(new CustomSectionDefinition { ID = removedSectionId, Name = "Optional repeated section" });
				definition.SectionDefinitionLinks.Add(new SectionDefinitionLink(removedSectionId) { IsOptional = true, AllowMultipleSections = true });
			}

			return new TestModule
			{
				Definitions = new[] { new TestDefinition { Definition = definition, SectionDefinitions = sections } },
			};
		}

		private static DomBehaviorDefinition CreateBehavior() => new DomBehaviorDefinition
		{
			ID = new DomBehaviorDefinitionId(new Guid("40000000-0000-0000-0000-000000000001")) { ModuleId = ModuleId },
			Name = "Behavior",
			InitialStatusId = "draft",
			Statuses = { new("draft", "Draft") },
		};

		private static CustomSectionDefinition MainSection(TestModule module) => module.Definitions.Single().SectionDefinitions.First();

		private static CustomSectionDefinition ReadMainSection(InMemoryDomSchemaStore store) => (CustomSectionDefinition)new DomHelper(store.HandleMessages, ModuleId)
			.SectionDefinitions.Read(SectionDefinitionExposers.ID.Equal(new SectionDefinitionID(MainSectionGuid) { ModuleId = ModuleId })).Single();

		private static DomDefinition ReadDefinition(InMemoryDomSchemaStore store) => new DomHelper(store.HandleMessages, ModuleId)
			.DomDefinitions.Read(DomDefinitionExposers.Id.Equal(new DomDefinitionId(DefinitionGuid) { ModuleId = ModuleId })).Single();

		private static DomDefinition CreateExistingDefinitionWithLink(SectionDefinitionLink oldLink)
		{
			var definition = CreateModule().Definitions.Single().Definition;
			definition.SectionDefinitionLinks.Add(oldLink);
			return definition;
		}

		private sealed class TestModule : IDomModuleInfo
		{
			public string ModuleId { get; set; } = DomModuleInstallerTests.ModuleId;
			public ModuleSettings ModuleSettings { get; set; } = new ModuleSettings(DomModuleInstallerTests.ModuleId);
			public IEnumerable<IDomDefinitionInfo> Definitions { get; set; } = Array.Empty<IDomDefinitionInfo>();
			public IEnumerable<CustomSectionDefinition> AdditionalSectionDefinitions { get; set; } = Array.Empty<CustomSectionDefinition>();
			public IEnumerable<DomBehaviorDefinition> BehaviorDefinitions { get; set; } = Array.Empty<DomBehaviorDefinition>();
		}

		private sealed class TestDefinition : IDomDefinitionInfo
		{
			public DomDefinition Definition { get; set; } = null!;
			public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; set; } = Array.Empty<CustomSectionDefinition>();
		}
	}
}
