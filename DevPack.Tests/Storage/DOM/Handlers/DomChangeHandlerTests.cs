namespace RT_MediaOps.Plan.Storage.DOM.Handlers
{
	using System;
	using System.Linq;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.General;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	[TestClass]
	public sealed class DomChangeHandlerTests
	{
		private static readonly Guid SectionDefinition = Guid.NewGuid();
		private static readonly Guid SectionId = Guid.NewGuid();
		private static readonly Guid NameFieldId = Guid.NewGuid();
		private static readonly Guid PoolIdsFieldId = Guid.NewGuid();

		/// <summary>
		/// Reproduces the scenario where <c>AssignResourcesToPool</c> has already persisted the pool
		/// assignment, but the local object's original snapshot still has the field empty. Updating the
		/// object afterwards must not fail because the stored value is already equal to the value that the
		/// update wants to apply.
		/// </summary>
		[TestMethod]
		public void HandleChanges_FieldUpdatedToValueAlreadyStored_IsIgnored()
		{
			var original = CreateInstance(poolIds: null);
			var updated = CreateInstance(poolIds: "pool-1");
			var stored = CreateInstance(poolIds: "pool-1");

			var results = DomChangeHandler.HandleChanges(original, updated, stored);

			Assert.IsFalse(results.HasErrors, "A field that is updated to the value already stored should not be reported as a conflict.");
			Assert.AreEqual(0, results.ChangedFields.Count);
		}

		[TestMethod]
		public void HandleChanges_FieldRemovedButAlreadyAbsentInStored_IsIgnored()
		{
			var original = CreateInstance(poolIds: "pool-1");
			var updated = CreateInstance(poolIds: null);
			var stored = CreateInstance(poolIds: null);

			var results = DomChangeHandler.HandleChanges(original, updated, stored);

			Assert.IsFalse(results.HasErrors, "Removing a field that is already absent in the stored instance should not be reported as a conflict.");
		}

		[TestMethod]
		public void HandleChanges_FieldChangedToDifferentStoredValue_IsReportedAsConflict()
		{
			var original = CreateInstance(poolIds: null);
			var updated = CreateInstance(poolIds: "pool-1");
			var stored = CreateInstance(poolIds: "pool-other");

			var results = DomChangeHandler.HandleChanges(original, updated, stored);

			Assert.IsTrue(results.HasErrors, "A field that was concurrently changed to a different value should still be reported as a conflict.");
		}

		[TestMethod]
		public void HandleChanges_FieldUpdatedAgainstUnchangedStoredValue_IsApplied()
		{
			var original = CreateInstance(poolIds: "pool-1");
			var updated = CreateInstance(poolIds: "pool-2");
			var stored = CreateInstance(poolIds: "pool-1");

			var results = DomChangeHandler.HandleChanges(original, updated, stored);

			Assert.IsFalse(results.HasErrors);
			Assert.AreEqual(1, results.ChangedFields.Count);

			var storedSection = results.Instance.Sections.Single(s => s.ID.Id == SectionId);
			var poolField = storedSection.FieldValues.Single(f => f.FieldDescriptorID.Id == PoolIdsFieldId);
			Assert.AreEqual("pool-2", poolField.Value.Value);
		}

		private static TestDomInstance CreateInstance(string? poolIds)
		{
			var section = new Section(new SectionDefinitionID(SectionDefinition))
			{
				ID = new SectionID(SectionId),
			};
			section.AddOrReplaceFieldValue(new FieldValue(new FieldDescriptorID(NameFieldId), ValueWrapperFactory.Create("name")));
			if (poolIds != null)
			{
				section.AddOrReplaceFieldValue(new FieldValue(new FieldDescriptorID(PoolIdsFieldId), ValueWrapperFactory.Create(poolIds)));
			}

			var domInstance = new DomInstance
			{
				ID = new DomInstanceId(Guid.NewGuid()),
			};
			domInstance.Sections.Add(section);

			return new TestDomInstance(domInstance);
		}

		private sealed class TestDomInstance : DomInstanceBase
		{
			public TestDomInstance(DomInstance domInstance) : base(domInstance)
			{
			}

			public override void Save(DomHelper helper) => throw new NotSupportedException();

			protected override DomInstance InternalToInstance() => domInstance;

			protected override void InitializeProperties()
			{
			}
		}
	}
}
