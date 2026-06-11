namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class JobPropertySettingsTests
	{
		[TestMethod]
		public void Job_NewInstance_CustomPropertySettingsIsEmpty()
		{
			var job = new Job { Name = "Test" };

			Assert.IsNotNull(job.CustomPropertySettings);
			Assert.AreEqual(0, job.CustomPropertySettings.Count);
		}

		[TestMethod]
		public void Job_NewInstance_PropertySettingsIsEmpty()
		{
			var job = new Job { Name = "Test" };

			Assert.IsNotNull(job.PropertySettings);
			Assert.AreEqual(0, job.PropertySettings.Count);
		}

		[TestMethod]
		public void Job_NewInstanceWithId_CustomPropertySettingsIsEmpty()
		{
			var job = new Job(Guid.NewGuid()) { Name = "Test" };

			Assert.IsNotNull(job.CustomPropertySettings);
			Assert.AreEqual(0, job.CustomPropertySettings.Count);
		}

		[TestMethod]
		public void Job_NewInstanceWithId_PropertySettingsIsEmpty()
		{
			var job = new Job(Guid.NewGuid()) { Name = "Test" };

			Assert.IsNotNull(job.PropertySettings);
			Assert.AreEqual(0, job.PropertySettings.Count);
		}

		[TestMethod]
		public void JobResourceNode_NewInstance_CustomPropertySettingsIsEmpty()
		{
			var node = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());

			Assert.IsNotNull(node.CustomPropertySettings);
			Assert.AreEqual(0, node.CustomPropertySettings.Count);
		}

		[TestMethod]
		public void JobResourceNode_NewInstance_PropertySettingsIsEmpty()
		{
			var node = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());

			Assert.IsNotNull(node.PropertySettings);
			Assert.AreEqual(0, node.PropertySettings.Count);
		}

		[TestMethod]
		public void JobResourcePoolNode_NewInstance_CustomPropertySettingsIsEmpty()
		{
			var node = new JobResourcePoolNode(Guid.NewGuid());

			Assert.IsNotNull(node.CustomPropertySettings);
			Assert.AreEqual(0, node.CustomPropertySettings.Count);
		}

		[TestMethod]
		public void JobResourcePoolNode_NewInstance_PropertySettingsIsEmpty()
		{
			var node = new JobResourcePoolNode(Guid.NewGuid());

			Assert.IsNotNull(node.PropertySettings);
			Assert.AreEqual(0, node.PropertySettings.Count);
		}

		[TestMethod]
		public void NewJob_NodeAddedThenProperties_PersistenceActionTargetsJobAndNode()
		{
			// Arrange: brand-new job with a new node added before any properties are configured.
			var job = new Job(Guid.NewGuid()) { Name = "Test" };
			var node = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			job.NodeGraph.Add(node);

			node.AddCustomProperty(new CustomPropertySetting("Tag") { Value = "live" });

			// Act: simulate the save path - DomJobHandler calls EnsureContext() on each job.
			job.EnsureContext();

			var ownerScopes = new[]
			{
				new KeyValuePair<Guid, Skyline.DataMiner.Solutions.MediaOps.Plan.API.PropertySettingsScope>(job.Id, job.PropertySettingsScope),
				new KeyValuePair<Guid, Skyline.DataMiner.Solutions.MediaOps.Plan.API.PropertySettingsScope>(job.Id, node.PropertySettingsScope),
			};

			var (toCreateOrUpdate, toDelete, ownerByCollectionId) = ownerScopes.BuildPersistenceActions();

			// Assert
			Assert.AreEqual(0, toDelete.Count, "Nothing to delete - nothing was previously persisted.");
			Assert.AreEqual(1, toCreateOrUpdate.Count, "Only the node has dirty properties.");

			var collection = toCreateOrUpdate[0];
			Assert.AreEqual(job.Id.ToString(), collection.LinkedObjectId, "LinkedObjectId should be the owning job id.");
			Assert.AreEqual(PropertySettingsContext.MediaOpsScope, collection.Scope, "Scope should be the MediaOps scope.");
			Assert.AreEqual(node.Id, collection.SubId, "SubId should be the node id.");
			Assert.AreEqual(1, collection.CustomSettings.Count, "The custom property added on the node should be present.");
			Assert.AreEqual("Tag", collection.CustomSettings.First().Name);

			Assert.IsTrue(ownerByCollectionId.TryGetValue(collection.Id, out var ownerId));
			Assert.AreEqual(job.Id, ownerId, "Failure reporting must map back to the job id.");
		}

		[TestMethod]
		public void NewJob_NodeAddedAfterContextEnsured_PersistenceActionTargetsJobAndNode()
		{
			// Arrange: job whose context has already been created (e.g. user touched properties
			// before adding the node). This simulates the loaded-job scenario where the context
			// existed before a freshly created node was attached to the graph.
			var job = new Job(Guid.NewGuid()) { Name = "Test" };

			// Trigger context creation up-front so the new node is added AFTER the context exists.
			job.EnsureContext();

			var node = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			node.AddCustomProperty(new CustomPropertySetting("Channel") { Value = "1" });
			job.NodeGraph.Add(node);

			// Act: save path re-runs EnsureContext, which must re-wire newly added nodes.
			job.EnsureContext();

			var ownerScopes = new[]
			{
				new KeyValuePair<Guid, Skyline.DataMiner.Solutions.MediaOps.Plan.API.PropertySettingsScope>(job.Id, job.PropertySettingsScope),
				new KeyValuePair<Guid, Skyline.DataMiner.Solutions.MediaOps.Plan.API.PropertySettingsScope>(job.Id, node.PropertySettingsScope),
			};

			var (toCreateOrUpdate, toDelete, ownerByCollectionId) = ownerScopes.BuildPersistenceActions();

			// Assert
			Assert.AreEqual(0, toDelete.Count);
			Assert.AreEqual(1, toCreateOrUpdate.Count);

			var collection = toCreateOrUpdate[0];
			Assert.AreEqual(job.Id.ToString(), collection.LinkedObjectId, "Node added after the context was created must still resolve to the job id.");
			Assert.AreEqual(PropertySettingsContext.MediaOpsScope, collection.Scope);
			Assert.AreEqual(node.Id, collection.SubId);

			Assert.IsTrue(ownerByCollectionId.TryGetValue(collection.Id, out var ownerId));
			Assert.AreEqual(job.Id, ownerId);
		}

		[TestMethod]
		public void Job_AddCustomProperty_StoresIndependentCopy()
		{
			var job = new Job(Guid.NewGuid()) { Name = "Test" };
			var setting = new CustomPropertySetting("Tag") { Value = "live" };

			job.AddCustomProperty(setting);

			var stored = job.CustomPropertySettings.Single();
			Assert.IsFalse(ReferenceEquals(setting, stored), "The scope must store a copy, not the caller's instance.");

			// Mutating the original after adding it must not affect the owner's stored value.
			setting.Value = "mutated";
			Assert.AreEqual("live", job.CustomPropertySettings.Single().Value);
		}

		[TestMethod]
		public void Job_AddProperty_StoresIndependentCopy()
		{
			var property = new StringProperty { Name = "Channel", Scope = "global", SectionName = "General" };
			var job = new Job(Guid.NewGuid()) { Name = "Test" };
			var setting = new StringPropertySetting(property) { Value = "one" };

			job.AddProperty(setting);

			var stored = (StringPropertySetting)job.PropertySettings.Single();
			Assert.IsFalse(ReferenceEquals(setting, stored), "The scope must store a copy, not the caller's instance.");

			setting.Value = "two";
			Assert.AreEqual("one", ((StringPropertySetting)job.PropertySettings.Single()).Value);
		}

		[TestMethod]
		public void Job_SetCustomProperties_StoresIndependentCopies()
		{
			var job = new Job(Guid.NewGuid()) { Name = "Test" };
			var setting = new CustomPropertySetting("Tag") { Value = "live" };

			job.SetCustomProperties(new[] { setting });

			var stored = job.CustomPropertySettings.Single();
			Assert.IsFalse(ReferenceEquals(setting, stored));

			setting.Value = "mutated";
			Assert.AreEqual("live", job.CustomPropertySettings.Single().Value);
		}

		[TestMethod]
		public void CopyingCustomPropertyBetweenJobs_DoesNotShareReference()
		{
			var source = new Job(Guid.NewGuid()) { Name = "Source" };
			source.AddCustomProperty(new CustomPropertySetting("Tag") { Value = "live" });

			var target = new Job(Guid.NewGuid()) { Name = "Target" };

			// Simulate a user copying the source's properties onto another job.
			foreach (var setting in source.CustomPropertySettings)
			{
				target.AddCustomProperty(setting);
			}

			var targetSetting = target.CustomPropertySettings.Single();
			var sourceSetting = source.CustomPropertySettings.Single();
			Assert.IsFalse(ReferenceEquals(sourceSetting, targetSetting), "Copies between jobs must not share references.");

			// Mutating the copy on the target must not bleed back into the source job.
			targetSetting.Value = "vod";
			Assert.AreEqual("live", source.CustomPropertySettings.Single().Value);
			Assert.AreEqual("vod", target.CustomPropertySettings.Single().Value);
		}

		[TestMethod]
		public void JobResourceNode_AddCustomProperty_StoresIndependentCopy()
		{
			var node = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			var setting = new CustomPropertySetting("Tag") { Value = "live" };

			node.AddCustomProperty(setting);

			var stored = node.CustomPropertySettings.Single();
			Assert.IsFalse(ReferenceEquals(setting, stored), "The scope must store a copy, not the caller's instance.");

			setting.Value = "mutated";
			Assert.AreEqual("live", node.CustomPropertySettings.Single().Value);
		}

		[TestMethod]
		public void NewJob_OwnerProperties_PersistenceActionCarriesOwnerMetadata()
		{
			// Arrange: brand-new job with owner-level properties only.
			var job = new Job(Guid.NewGuid()) { Name = "Test" };
			job.AddCustomProperty(new CustomPropertySetting("Tag") { Value = "live" });

			// Act
			job.EnsureContext();
			var action = job.PropertySettingsScope.BuildPersistenceAction();

			// Assert: the owner collection must be fully addressable.
			Assert.IsNotNull(action);
			Assert.IsFalse(action.IsDelete);
			Assert.AreEqual(job.Id.ToString(), action.Collection.LinkedObjectId, "LinkedObjectId should be the owning job id.");
			Assert.AreEqual(PropertySettingsContext.MediaOpsScope, action.Collection.Scope, "Scope should be the MediaOps scope.");
			Assert.AreEqual(string.Empty, action.Collection.SubId, "Owner-level collections use an empty SubId.");
		}

		[TestMethod]
		public void DirtyScopeWithoutWiredContext_BuildPersistenceAction_Throws()
		{
			// Arrange: a node scope whose context was never wired (e.g. node never attached to a graph).
			var node = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			node.AddCustomProperty(new CustomPropertySetting("Tag") { Value = "live" });

			// Act + Assert: persisting dirty content without an owner context would create an orphaned
			// collection with a null LinkedObjectId, so it must fail fast instead.
			Assert.ThrowsException<InvalidOperationException>(() => node.PropertySettingsScope.BuildPersistenceAction());
		}
	}
}
