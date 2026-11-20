namespace RT_MediaOps.Plan.Storage.DOM
{
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcResourceStudio;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Tools;
	using ResourcePool = Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool;

	// The DOM mock does not enforce section-link cardinality. Persistence checks
	// cover serialization/read-back; required and repeatable links are asserted separately.
	[TestClass]
	[TestCategory("UnitTest")]
	public sealed class FlowRulesPersistenceTests
	{
		[TestMethod]
		public void EmptyCollection_CanBeCreatedWithOnlyRequiredOwnerInfo()
		{
			var helper = CreateHelper();
			var pool = new ResourcePool();

			helper.DomInstances.Create(pool.FlowRules.GetInstanceWithChanges().ToInstance());

			AssertEmptyCollection(helper, pool.FlowRules.Id, pool.Id);
		}

		[TestMethod]
		[DataRow("Remove")]
		[DataRow("RemoveDefault")]
		[DataRow("RemoveOverride")]
		[DataRow("Clear")]
		[DataRow("ClearDirection")]
		public void RemovingLastStoredRule_CanPersistAnEmptyCollection(string removal)
		{
			var helper = CreateHelper();
			var pool = new ResourcePool();
			var targetId = Guid.NewGuid();
			if (removal == "RemoveOverride")
			{
				pool.FlowRules.SetOverride(FlowRuleDirection.Before, FlowRuleObjectType.ResourcePool, targetId, FlowRuleOutcome.Forbidden);
			}
			else
			{
				pool.FlowRules.SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Forbidden);
			}

			helper.DomInstances.Create(pool.FlowRules.GetInstanceWithChanges().ToInstance());
			var stored = ReadInstance(helper, pool.FlowRules.Id);
			var ownerSectionId = stored.Sections.Single(x => x.SectionDefinitionID.Equals(SlcResource_StudioIds.Sections.Flowrulesinfo.Id)).ID;
			var rules = new FlowRuleCollection(new FlowrulesInstance(stored));
			Assert.AreEqual(1, rules.Count);

			switch (removal)
			{
				case "Remove": Assert.IsTrue(rules.Remove(rules.Single())); break;
				case "RemoveDefault": rules.RemoveDefault(FlowRuleDirection.Before); break;
				case "RemoveOverride": rules.RemoveOverride(FlowRuleDirection.Before, FlowRuleObjectType.ResourcePool, targetId); break;
				case "Clear": rules.Clear(); break;
				case "ClearDirection": rules.Clear(FlowRuleDirection.Before); break;
				default: Assert.Fail("Unknown removal operation."); break;
			}

			helper.DomInstances.Update(rules.GetInstanceWithChanges().ToInstance());

			var updated = AssertEmptyCollection(helper, pool.FlowRules.Id, pool.Id);
			Assert.AreEqual(ownerSectionId, updated.Sections.Single().ID);
		}

		[TestMethod]
		public void InstalledDefinition_RequiresSingleOwnerInfoAndAllowsOptionalRepeatedRules()
		{
			var helper = CreateHelper();
			var definition = helper.DomDefinitions.Read(DomDefinitionExposers.Id.Equal(SlcResource_StudioIds.Definitions.Flowrules)).Single();
			Assert.AreEqual(SlcResource_StudioIds.Definitions.Flowrules, definition.ID);
			Assert.AreEqual(2, definition.SectionDefinitionLinks.Count);

			var ownerLink = definition.SectionDefinitionLinks.Single(x => x.SectionDefinitionID.Equals(SlcResource_StudioIds.Sections.Flowrulesinfo.Id));
			Assert.IsFalse(ownerLink.IsOptional);
			Assert.IsFalse(ownerLink.AllowMultipleSections);
			Assert.IsFalse(ownerLink.IsSoftDeleted);

			var ruleLink = definition.SectionDefinitionLinks.Single(x => x.SectionDefinitionID.Equals(SlcResource_StudioIds.Sections.Flowrule.Id));
			Assert.IsTrue(ruleLink.IsOptional);
			Assert.IsTrue(ruleLink.AllowMultipleSections);
			Assert.IsFalse(ruleLink.IsSoftDeleted);
		}

		[TestMethod]
		public void ExistingMandatoryRuleLink_IsRelaxedWithoutChangingOtherSchema()
		{
			var store = new InMemoryDomSchemaStore();
			var previous = new SlcResourceStudioDomModule();
			previous.Definitions.Single(x => x.Definition.ID.Equals(SlcResource_StudioIds.Definitions.Flowrules))
				.Definition.SectionDefinitionLinks.Single(x => x.SectionDefinitionID.Equals(SlcResource_StudioIds.Sections.Flowrule.Id)).IsOptional = false;
			DomModuleInstaller.Install(store.HandleMessages, previous);
			store.Messages.Clear();

			DomModuleInstaller.Install(store.HandleMessages, new SlcResourceStudioDomModule());

			var update = store.Messages.OfType<ManagerStoreUpdateRequest<DomDefinition>>().Single();
			Assert.AreEqual(SlcResource_StudioIds.Definitions.Flowrules, update.Object.ID);
			Assert.IsTrue(new FlowRulesDefinition().Definition.Equals(update.Object));
			var ownerLink = update.Object.SectionDefinitionLinks.Single(x => x.SectionDefinitionID.Equals(SlcResource_StudioIds.Sections.Flowrulesinfo.Id));
			Assert.IsFalse(ownerLink.IsOptional);
			Assert.IsFalse(ownerLink.AllowMultipleSections);
			var ruleLink = update.Object.SectionDefinitionLinks.Single(x => x.SectionDefinitionID.Equals(SlcResource_StudioIds.Sections.Flowrule.Id));
			Assert.IsTrue(ruleLink.IsOptional);
			Assert.IsTrue(ruleLink.AllowMultipleSections);
			Assert.AreEqual(2, update.Object.SectionDefinitionLinks.Count);
			Assert.AreEqual(0, store.Messages.OfType<ManagerStoreUpdateRequest<DomBehaviorDefinition>>().Count());
			var helper = new DomHelper(store.HandleMessages, SlcResource_StudioIds.ModuleId);
			var pool = new ResourcePool();
			helper.DomInstances.Create(pool.FlowRules.GetInstanceWithChanges().ToInstance());
			AssertEmptyCollection(helper, pool.FlowRules.Id, pool.Id);
			store.Messages.Clear();

			DomModuleInstaller.Install(store.HandleMessages, new SlcResourceStudioDomModule());

			Assert.AreEqual(0, store.Messages.OfType<ManagerStoreUpdateRequest<DomDefinition>>().Count());
			AssertEmptyCollection(helper, pool.FlowRules.Id, pool.Id);
		}

		private static DomHelper CreateHelper()
		{
			var store = new InMemoryDomSchemaStore();
			// Use the production module, not the legacy simulation's partial schema.
			DomModuleInstaller.Install(store.HandleMessages, new SlcResourceStudioDomModule());
			return new DomHelper(store.HandleMessages, SlcResource_StudioIds.ModuleId);
		}

		private static DomInstance ReadInstance(DomHelper helper, Guid id) => helper.DomInstances.Read(DomInstanceExposers.Id.Equal(new DomInstanceId(id) { ModuleId = SlcResource_StudioIds.ModuleId })).Single();

		private static DomInstance AssertEmptyCollection(DomHelper helper, Guid id, Guid ownerId)
		{
			var instance = ReadInstance(helper, id);
			Assert.AreEqual(id, instance.ID.Id);
			Assert.AreEqual(SlcResource_StudioIds.Definitions.Flowrules, instance.DomDefinitionId);
			Assert.AreEqual(1, instance.Sections.Count);
			Assert.AreEqual(SlcResource_StudioIds.Sections.Flowrulesinfo.Id, instance.Sections.Single().SectionDefinitionID);
			var rules = new FlowRuleCollection(new FlowrulesInstance(instance));
			Assert.AreEqual(0, rules.Count);
			Assert.AreEqual(ownerId, rules.OwnerId);
			Assert.AreEqual(FlowRuleObjectType.ResourcePool, rules.OwnerType);
			var evaluation = FlowRuleEvaluator.Evaluate(FlowRuleObject.ResourcePool(Guid.NewGuid()), FlowRuleObject.ResourcePool(ownerId), owner => owner.Id == ownerId ? rules : null);
			Assert.AreEqual(FlowRuleOutcome.Allowed, evaluation.Outcome);
			return instance;
		}
	}
}
