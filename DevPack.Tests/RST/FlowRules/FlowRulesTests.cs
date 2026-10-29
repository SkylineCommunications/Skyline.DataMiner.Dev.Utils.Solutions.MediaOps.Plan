namespace RT_MediaOps.Plan.RST.FlowRules
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	using SLDataGateway.API.Querying;

	using ApiFlowRules = Skyline.DataMiner.Solutions.MediaOps.Plan.API.FlowRules;
	using StorageResourceStudio = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class FlowRulesTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public FlowRulesTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void NewResourcePool_HasEmptyFlowRules()
		{
			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{Guid.NewGuid()}_Pool" });

			Assert.IsNotNull(pool.FlowRules);
			Assert.AreEqual(FlowRuleObjectType.ResourcePool, pool.FlowRules.OwnerType);
			Assert.AreEqual(pool.Id, pool.FlowRules.OwnerId);
			Assert.AreEqual(0, pool.FlowRules.Rules.Count);

			var stored = ReadByOwner(pool.Id);
			Assert.IsNotNull(stored, "Expected every new resource pool to get a stored rule set.");
			Assert.AreEqual(pool.FlowRules.Id, stored.Id);
		}

		[TestMethod]
		public void ReadAll_ReturnsTheStoredOwner()
		{
			var pool = new ResourcePool { Name = $"{Guid.NewGuid()}_Pool" };
			pool.FlowRules.SetDefault(FlowRuleDirection.After, FlowRuleOutcome.Warning);
			pool = objectCreator.CreateResourcePool(pool);

			var stored = TestContext.Api.FlowRules.Read().Single(x => x.Id == pool.FlowRules.Id);

			Assert.AreEqual(FlowRuleObjectType.ResourcePool, stored.OwnerType);
			Assert.AreEqual(pool.Id, stored.OwnerId);
			Assert.AreEqual(FlowRuleOutcome.Warning, stored.GetDefault(FlowRuleDirection.After).Outcome);
		}

		[TestMethod]
		public void Querying_ByIdOwnerAndTarget()
		{
			var prefix = Guid.NewGuid();
			var target = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Target" });

			var referencing = new ResourcePool { Name = $"{prefix}_Referencing" };
			referencing.FlowRules.SetOverride(FlowRuleDirection.After, target, FlowRuleOutcome.Forbidden);
			referencing = objectCreator.CreateResourcePool(referencing);

			var other = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Other" });

			var repository = TestContext.Api.FlowRules;
			var ownFilter = new ORFilterElement<ApiFlowRules>(new[] { target, referencing, other }.Select(x => FlowRulesExposers.OwnerId.Equal(x.Id)).ToArray());

			// By its own ID
			Assert.AreEqual(referencing.Id, repository.Read(referencing.FlowRules.Id).OwnerId);
			Assert.AreEqual(2, repository.Read([referencing.FlowRules.Id, other.FlowRules.Id]).Count());
			Assert.IsNull(repository.Read(Guid.NewGuid()));

			// By owner
			var byOwner = repository.Read(FlowRulesExposers.OwnerType.Equal(FlowRuleObjectType.ResourcePool).AND(FlowRulesExposers.OwnerId.Equal(other.Id))).Single();
			Assert.AreEqual(other.FlowRules.Id, byOwner.Id);

			// By target of an override
			var referencingTarget = repository.Read(ownFilter.AND(FlowRulesExposers.Rules.TargetId.Contains(target.Id))).Single();
			Assert.AreEqual(referencing.Id, referencingTarget.OwnerId);

			// Count, paging and query with ordering
			Assert.AreEqual(3, repository.Count(ownFilter));
			Assert.AreEqual(1, repository.Count(ownFilter.AND(FlowRulesExposers.Rules.TargetId.Contains(target.Id)).ToQuery()));

			var pages = repository.ReadPaged(ownFilter, 2).ToList();
			Assert.AreEqual(2, pages.Count);
			Assert.AreEqual(3, pages.Sum(x => x.Count()));

			var ordered = repository.Read(ownFilter.ToQuery().OrderBy(FlowRulesExposers.OwnerId)).Select(x => x.OwnerId).ToList();
			CollectionAssert.AreEqual(ordered.OrderBy(x => x.ToString()).ToList(), ordered);

			Assert.AreEqual(0, repository.Count(new ORFilterElement<ApiFlowRules>()));
		}

		[TestMethod]
		public void CreateAndReadResourcePoolWithFlowRules()
		{
			var prefix = Guid.NewGuid();
			var target = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Target" });

			var pool = new ResourcePool { Name = $"{prefix}_Pool" };
			pool.FlowRules
				.SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Warning, "Unusual predecessor.")
				.SetOverride(FlowRuleDirection.After, target, FlowRuleOutcome.Forbidden, "Never after this pool.");
			objectCreator.CreateResourcePool(pool);

			var read = TestContext.Api.ResourcePools.Read(pool.Id);

			Assert.AreEqual(2, read.FlowRules.Rules.Count);
			Assert.AreEqual(new FlowRule(FlowRuleDirection.Before, FlowRuleOutcome.Warning, "Unusual predecessor."), read.FlowRules.GetDefault(FlowRuleDirection.Before));
			Assert.AreEqual(new FlowRule(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, target.Id, FlowRuleOutcome.Forbidden, "Never after this pool."), read.FlowRules.GetOverride(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, target.Id));
			Assert.IsNull(read.FlowRules.GetDefault(FlowRuleDirection.After));

			var fromRepository = TestContext.Api.FlowRules
				.Read(new ORFilterElement<ApiFlowRules>(FlowRulesExposers.OwnerId.Equal(pool.Id), FlowRulesExposers.OwnerId.Equal(target.Id)))
				.ToDictionary(x => x.OwnerId);
			Assert.AreEqual(2, fromRepository.Count);
			Assert.AreEqual(2, fromRepository[pool.Id].Rules.Count);
			Assert.AreEqual(0, fromRepository[target.Id].Rules.Count);
		}

		[TestMethod]
		public void UpdateFlowRules()
		{
			var prefix = Guid.NewGuid();
			var target = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Target" });

			var pool = new ResourcePool { Name = $"{prefix}_Pool" };
			pool.FlowRules
				.SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Warning)
				.SetOverride(FlowRuleDirection.Before, target, FlowRuleOutcome.Forbidden);
			pool = objectCreator.CreateResourcePool(pool);

			pool.FlowRules
				.RemoveOverride(FlowRuleDirection.Before, FlowRuleObjectType.ResourcePool, target.Id)
				.SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Forbidden, "Updated")
				.SetDefault(FlowRuleDirection.After, FlowRuleOutcome.Warning);
			TestContext.Api.ResourcePools.Update(pool);

			var read = TestContext.Api.ResourcePools.Read(pool.Id);
			Assert.AreEqual(2, read.FlowRules.Rules.Count);
			Assert.AreEqual(FlowRuleOutcome.Forbidden, read.FlowRules.GetDefault(FlowRuleDirection.Before).Outcome);
			Assert.AreEqual("Updated", read.FlowRules.GetDefault(FlowRuleDirection.Before).Message);
			Assert.AreEqual(FlowRuleOutcome.Warning, read.FlowRules.GetDefault(FlowRuleDirection.After).Outcome);
			Assert.IsNull(read.FlowRules.GetOverride(FlowRuleDirection.Before, FlowRuleObjectType.ResourcePool, target.Id));

			read.FlowRules.Clear();
			TestContext.Api.ResourcePools.Update(read);
			Assert.AreEqual(0, TestContext.Api.ResourcePools.Read(pool.Id).FlowRules.Rules.Count);
		}

		[TestMethod]
		public void ReadPool_IsUnchanged()
		{
			var pool = new ResourcePool { Name = $"{Guid.NewGuid()}_Pool" };
			pool.FlowRules.SetDefault(FlowRuleDirection.After, FlowRuleOutcome.Warning);
			pool = objectCreator.CreateResourcePool(pool);

			var read = TestContext.Api.ResourcePools.Read(pool.Id);

			Assert.IsFalse(read.HasChanges);
			Assert.IsFalse(read.FlowRules.HasChanges);

			read.FlowRules.SetDefault(FlowRuleDirection.After, FlowRuleOutcome.Forbidden);
			Assert.IsTrue(read.HasChanges, "Expected a flow rule change to mark the resource pool as changed.");
		}

		[TestMethod]
		public void EditingARuleDirectly_IsPersisted()
		{
			var pool = new ResourcePool { Name = $"{Guid.NewGuid()}_Pool" };
			pool.FlowRules.SetDefault(FlowRuleDirection.After, FlowRuleOutcome.Warning, "before");
			pool = objectCreator.CreateResourcePool(pool);

			var read = TestContext.Api.ResourcePools.Read(pool.Id);
			var rule = read.FlowRules.GetDefault(FlowRuleDirection.After);
			rule.Outcome = FlowRuleOutcome.Forbidden;
			rule.Message = "after";

			Assert.IsTrue(read.HasChanges, "Expected a change on a rule to mark the resource pool as changed.");
			TestContext.Api.ResourcePools.Update(read);

			var stored = TestContext.Api.ResourcePools.Read(pool.Id).FlowRules.GetDefault(FlowRuleDirection.After);
			Assert.AreEqual(FlowRuleOutcome.Forbidden, stored.Outcome);
			Assert.AreEqual("after", stored.Message);
		}

		[TestMethod]
		public void UpdatingARule_KeepsItsStoredSection()
		{
			var prefix = Guid.NewGuid();
			var target = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Target" });

			var pool = new ResourcePool { Name = $"{prefix}_Pool" };
			pool.FlowRules
				.SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Warning)
				.SetOverride(FlowRuleDirection.Before, target, FlowRuleOutcome.Warning);
			pool = objectCreator.CreateResourcePool(pool);

			var sectionIdsBefore = ReadFlowRuleSectionIds(pool.FlowRules.Id);

			var read = TestContext.Api.ResourcePools.Read(pool.Id);
			read.FlowRules
				.SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Forbidden, "changed")
				.SetOverride(FlowRuleDirection.Before, target, FlowRuleOutcome.Allowed);
			TestContext.Api.ResourcePools.Update(read);

			CollectionAssert.AreEquivalent(sectionIdsBefore, ReadFlowRuleSectionIds(pool.FlowRules.Id), "Expected updated rules to keep their DOM section.");
		}

		[TestMethod]
		public void SelfTargetingOverride_IsAllowed()
		{
			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{Guid.NewGuid()}_Switch" });

			pool.FlowRules.SetOverride(FlowRuleDirection.After, pool, FlowRuleOutcome.Warning);
			TestContext.Api.ResourcePools.Update(pool);

			Assert.AreEqual(FlowRuleOutcome.Warning, TestContext.Api.ResourcePools.Read(pool.Id).FlowRules.GetOverride(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, pool.Id).Outcome);
		}

		[TestMethod]
		public void OverrideToResourcePoolCreatedInSameCall_IsAllowed()
		{
			var prefix = Guid.NewGuid();
			var target = new ResourcePool { Name = $"{prefix}_Target" };
			var pool = new ResourcePool { Name = $"{prefix}_Pool" };
			pool.FlowRules.SetOverride(FlowRuleDirection.After, target, FlowRuleOutcome.Forbidden);

			objectCreator.CreateResourcePools([target, pool]);

			Assert.IsNotNull(TestContext.Api.ResourcePools.Read(pool.Id).FlowRules.GetOverride(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, target.Id));
		}

		[TestMethod]
		public void DuplicateRules_AreRejected()
		{
			var pool = new ResourcePool { Name = $"{Guid.NewGuid()}_Pool" };
			pool.FlowRules
				.Add(new FlowRule(FlowRuleDirection.Before, FlowRuleOutcome.Warning))
				.Add(new FlowRule(FlowRuleDirection.Before, FlowRuleOutcome.Forbidden));

			var exception = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreateResourcePool(pool));

			var error = exception.TraceData.ErrorData.OfType<ResourcePoolDuplicateFlowRuleError>().Single();
			Assert.AreEqual(pool.Id, error.Id);
			Assert.AreEqual(FlowRuleDirection.Before, error.Direction);
			Assert.IsNull(error.TargetType);
			Assert.IsNull(error.TargetId);
			Assert.IsNull(TestContext.Api.ResourcePools.Read(pool.Id));
		}

		[TestMethod]
		public void OverrideToUnknownResourcePool_IsRejected()
		{
			var unknownId = Guid.NewGuid();
			var pool = new ResourcePool { Name = $"{Guid.NewGuid()}_Pool" };
			pool.FlowRules.SetOverride(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, unknownId, FlowRuleOutcome.Forbidden);

			var exception = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreateResourcePool(pool));

			var error = exception.TraceData.ErrorData.OfType<ResourcePoolFlowRuleTargetNotFoundError>().Single();
			Assert.AreEqual(FlowRuleObjectType.ResourcePool, error.TargetType);
			Assert.AreEqual(unknownId, error.TargetId);
			Assert.AreEqual(FlowRuleDirection.After, error.Direction);
		}

		[TestMethod]
		public void OverrideToDeprecatedResourcePool_IsRejected()
		{
			var prefix = Guid.NewGuid();
			var target = CreateDeprecatedResourcePool($"{prefix}_Target");

			var pool = new ResourcePool { Name = $"{prefix}_Pool" };
			pool.FlowRules.SetOverride(FlowRuleDirection.Before, target, FlowRuleOutcome.Forbidden);

			var exception = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreateResourcePool(pool));

			Assert.AreEqual(target.Id, exception.TraceData.ErrorData.OfType<ResourcePoolFlowRuleTargetInvalidStateError>().Single().TargetId);
		}

		[TestMethod]
		public void ExistingOverrideToResourcePoolThatGotDeprecated_DoesNotBlockUpdates()
		{
			var prefix = Guid.NewGuid();
			var target = TestContext.Api.ResourcePools.Complete(objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Target" }));

			var pool = new ResourcePool { Name = $"{prefix}_Pool" };
			pool.FlowRules.SetOverride(FlowRuleDirection.Before, target, FlowRuleOutcome.Forbidden);
			pool = objectCreator.CreateResourcePool(pool);

			TestContext.Api.ResourcePools.Deprecate(target);

			pool.Name = $"{prefix}_Renamed";
			var updated = TestContext.Api.ResourcePools.Update(pool);

			Assert.AreEqual($"{prefix}_Renamed", updated.Name);
			Assert.IsNotNull(updated.FlowRules.GetOverride(FlowRuleDirection.Before, FlowRuleObjectType.ResourcePool, target.Id), "Expected the override to be kept when the target is deprecated.");
		}

		[TestMethod]
		public void TooLongMessage_IsRejected()
		{
			var pool = new ResourcePool { Name = $"{Guid.NewGuid()}_Pool" };
			pool.FlowRules.SetDefault(FlowRuleDirection.After, FlowRuleOutcome.Warning, new string('x', 500));

			var exception = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreateResourcePool(pool));

			Assert.AreEqual(1, exception.TraceData.ErrorData.OfType<ResourcePoolFlowRuleInvalidMessageError>().Count());
		}

		[TestMethod]
		public void DeleteResourcePoolReferencedByOverride_IsRejected()
		{
			var prefix = Guid.NewGuid();
			var target = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Target" });

			var pool = new ResourcePool { Name = $"{prefix}_Pool" };
			pool.FlowRules.SetOverride(FlowRuleDirection.After, target, FlowRuleOutcome.Forbidden);
			pool = objectCreator.CreateResourcePool(pool);

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.ResourcePools.Delete(target));

			var error = exception.TraceData.ErrorData.OfType<ResourcePoolInUseByFlowRulesError>().Single();
			Assert.AreEqual(target.Id, error.Id);
			CollectionAssert.AreEqual(new[] { pool.Id }, error.ReferencingOwnerIds.ToArray());
			Assert.IsNotNull(TestContext.Api.ResourcePools.Read(target.Id));

			// Once the override is removed the pool can be deleted.
			pool.FlowRules.RemoveOverride(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, target.Id);
			TestContext.Api.ResourcePools.Update(pool);
			TestContext.Api.ResourcePools.Delete(target);
			Assert.IsNull(TestContext.Api.ResourcePools.Read(target.Id));
		}

		[TestMethod]
		public void DeprecateResourcePoolReferencedByOverride_IsAllowed()
		{
			var prefix = Guid.NewGuid();
			var target = TestContext.Api.ResourcePools.Complete(objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Target" }));

			var pool = new ResourcePool { Name = $"{prefix}_Pool" };
			pool.FlowRules.SetOverride(FlowRuleDirection.After, target, FlowRuleOutcome.Forbidden);
			objectCreator.CreateResourcePool(pool);

			var deprecated = TestContext.Api.ResourcePools.Deprecate(target);

			Assert.AreEqual(ResourcePoolState.Deprecated, deprecated.State);
		}

		[TestMethod]
		public void DeleteReferencingAndReferencedResourcePoolsTogether_IsAllowed()
		{
			var prefix = Guid.NewGuid();
			var target = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Target" });

			var pool = new ResourcePool { Name = $"{prefix}_Pool" };
			pool.FlowRules.SetOverride(FlowRuleDirection.After, target, FlowRuleOutcome.Forbidden);
			pool = objectCreator.CreateResourcePool(pool);

			TestContext.Api.ResourcePools.Delete([target, pool]);

			Assert.IsNull(TestContext.Api.ResourcePools.Read(target.Id));
			Assert.IsNull(TestContext.Api.ResourcePools.Read(pool.Id));
		}

		[TestMethod]
		public void DeleteResourcePool_DeletesItsFlowRules()
		{
			var pool = new ResourcePool { Name = $"{Guid.NewGuid()}_Pool" };
			pool.FlowRules.SetDefault(FlowRuleDirection.After, FlowRuleOutcome.Warning);
			pool = objectCreator.CreateResourcePool(pool);
			var flowRulesId = pool.FlowRules.Id;

			TestContext.Api.ResourcePools.Delete(pool);

			Assert.IsFalse(ReadFlowRulesInstances().Any(x => x.ID.Id == flowRulesId), "Expected the rule set of the deleted pool to be removed.");
		}

		[TestMethod]
		public void PoolWithoutStoredFlowRules_GetsThemOnItsNextChange()
		{
			var prefix = Guid.NewGuid();
			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			RemoveFlowRulesFromStorage(pool.Id);

			var legacyPool = TestContext.Api.ResourcePools.Read(pool.Id);
			Assert.AreEqual(0, legacyPool.FlowRules.Rules.Count);
			Assert.IsNull(ReadByOwner(pool.Id));

			// An unchanged pool must not leave an orphan rule set behind.
			var instancesBefore = ReadFlowRulesInstances().Count();
			TestContext.Api.ResourcePools.CreateOrUpdate([legacyPool]);
			Assert.AreEqual(instancesBefore, ReadFlowRulesInstances().Count());
			Assert.IsNull(ReadByOwner(pool.Id));

			legacyPool = TestContext.Api.ResourcePools.Read(pool.Id);
			legacyPool.Name = $"{prefix}_Renamed";
			TestContext.Api.ResourcePools.Update(legacyPool);

			Assert.IsNotNull(ReadByOwner(pool.Id), "Expected the rule set to be created when the pool is saved.");
		}

		[TestMethod]
		public void Repository_EvaluateConnectionsAndCandidates()
		{
			var prefix = Guid.NewGuid();
			var ird = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_IRD" });
			var encoder = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Encoder" });

			var downstream = new ResourcePool { Name = $"{prefix}_Downstream" };
			downstream.FlowRules
				.SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Allowed)
				.SetOverride(FlowRuleDirection.Before, ird, FlowRuleOutcome.Forbidden, "No IRD before downstream.")
				.SetOverride(FlowRuleDirection.Before, encoder, FlowRuleOutcome.Warning);
			downstream = objectCreator.CreateResourcePool(downstream);

			var single = TestContext.Api.FlowRules.Evaluate(FlowRuleObjectType.ResourcePool, ird.Id, FlowRuleObjectType.ResourcePool, downstream.Id);
			Assert.AreEqual(FlowRuleOutcome.Forbidden, single.Outcome);
			Assert.AreEqual(ird.Id, single.FromObjectId);
			Assert.AreEqual(downstream.Id, single.ToObjectId);
			CollectionAssert.AreEqual(new[] { "No IRD before downstream." }, single.Messages.ToArray());

			var bulk = TestContext.Api.FlowRules.Evaluate(
				[
					(FlowRuleObjectType.ResourcePool, ird.Id, FlowRuleObjectType.ResourcePool, downstream.Id),
					(FlowRuleObjectType.ResourcePool, encoder.Id, FlowRuleObjectType.ResourcePool, downstream.Id),
					(FlowRuleObjectType.ResourcePool, downstream.Id, FlowRuleObjectType.ResourcePool, ird.Id),
				]).ToList();
			CollectionAssert.AreEqual(
				new[] { FlowRuleOutcome.Forbidden, FlowRuleOutcome.Warning, FlowRuleOutcome.Allowed },
				bulk.Select(x => x.Outcome).ToArray());

			var candidates = TestContext.Api.FlowRules.EvaluateCandidates(FlowRuleObjectType.ResourcePool, downstream.Id, FlowRuleDirection.Before, FlowRuleObjectType.ResourcePool, [ird.Id, encoder.Id]);
			Assert.AreEqual(FlowRuleOutcome.Forbidden, candidates[ird.Id].Outcome);
			Assert.AreEqual(FlowRuleOutcome.Warning, candidates[encoder.Id].Outcome);

			var graph = new Workflow { Name = "Graph" }.NodeGraph;
			var irdNode = new WorkflowResourcePoolNode(ird);
			var downstreamNode = new WorkflowResourcePoolNode(downstream);
			graph.Add(irdNode).Add(downstreamNode).Connect(irdNode, downstreamNode);

			var graphEvaluation = TestContext.Api.FlowRules.Evaluate(graph).Single();
			Assert.AreEqual(FlowRuleOutcome.Forbidden, graphEvaluation.Outcome);
			Assert.AreEqual(irdNode.Id, graphEvaluation.FromNodeId);
			Assert.AreEqual(downstreamNode.Id, graphEvaluation.ToNodeId);
		}

		private ResourcePool CreateDeprecatedResourcePool(string name)
		{
			var pool = TestContext.Api.ResourcePools.Complete(objectCreator.CreateResourcePool(new ResourcePool { Name = name }));
			return TestContext.Api.ResourcePools.Deprecate(pool);
		}

		private static ApiFlowRules ReadByOwner(Guid resourcePoolId)
		{
			return TestContext.Api.FlowRules
				.Read(FlowRulesExposers.OwnerType.Equal(FlowRuleObjectType.ResourcePool).AND(FlowRulesExposers.OwnerId.Equal(resourcePoolId)))
				.SingleOrDefault();
		}

		private static System.Collections.Generic.IEnumerable<DomInstance> ReadFlowRulesInstances()
		{
			return TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.DomDefinitionId.Equal(StorageResourceStudio.SlcResource_StudioIds.Definitions.Flowrules.Id));
		}

		private static Guid[] ReadFlowRuleSectionIds(Guid flowRulesId)
		{
			return TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(flowRulesId))
				.Single()
				.Sections
				.Where(x => x.SectionDefinitionID.Equals(StorageResourceStudio.SlcResource_StudioIds.Sections.Flowrule.Id))
				.Select(x => x.ID.Id)
				.ToArray();
		}

		// Simulates a resource pool that was created before flow rules existed.
		private static void RemoveFlowRulesFromStorage(Guid resourcePoolId)
		{
			var domHelper = TestContext.ResourceStudioDomHelper;
			var poolInstance = new StorageResourceStudio.ResourcepoolInstance(domHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(resourcePoolId)).Single());

			var flowRulesId = poolInstance.ConfigurationInfo.FlowRules;
			poolInstance.ConfigurationInfo.FlowRules = null;
			domHelper.DomInstances.Update(poolInstance.ToInstance());

			if (flowRulesId.HasValue)
			{
				domHelper.DomInstances.Delete(domHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(flowRulesId.Value)).Single());
			}
		}
	}
}
