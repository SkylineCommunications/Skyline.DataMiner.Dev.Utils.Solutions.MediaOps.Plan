namespace RT_MediaOps.Plan.RST.FlowRules
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;


	[TestClass]
	public sealed class FlowRuleEvaluatorTests
	{
		private readonly Guid poolA = Guid.NewGuid();
		private readonly Guid poolB = Guid.NewGuid();
		private readonly Guid poolC = Guid.NewGuid();

		private readonly Dictionary<Guid, FlowRuleCollection> flowRulesByPoolId = new Dictionary<Guid, FlowRuleCollection>();

		[TestMethod]
		public void Evaluate_WithoutRuleSets_IsAllowed()
		{
			var evaluation = Evaluate(poolA, poolB);

			Assert.AreEqual(FlowRuleOutcome.Allowed, evaluation.Outcome);
			Assert.IsNull(evaluation.FromRule);
			Assert.IsNull(evaluation.ToRule);
			Assert.AreEqual(0, evaluation.Messages.Count);
		}

		[TestMethod]
		public void Evaluate_WithEmptyRuleSets_IsAllowed()
		{
			RulesOf(poolA);
			RulesOf(poolB);

			Assert.AreEqual(FlowRuleOutcome.Allowed, Evaluate(poolA, poolB).Outcome);
		}

		[TestMethod]
		public void Evaluate_UsesAfterRulesOfSourceAndBeforeRulesOfDestination()
		{
			// Rules in the opposite directions must not influence the connection A → B.
			RulesOf(poolA).SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Forbidden);
			RulesOf(poolB).SetDefault(FlowRuleDirection.After, FlowRuleOutcome.Forbidden);

			Assert.AreEqual(FlowRuleOutcome.Allowed, Evaluate(poolA, poolB).Outcome);
			Assert.AreEqual(FlowRuleOutcome.Forbidden, Evaluate(poolB, poolA).Outcome);
		}

		[TestMethod]
		public void Evaluate_OverrideTakesPrecedenceOverDefault()
		{
			RulesOf(poolB).SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Allowed);
			RulesOf(poolB).SetOverride(FlowRuleDirection.Before, FlowRuleObjectType.ResourcePool, poolA, FlowRuleOutcome.Forbidden, "No IRD before this pool.");

			var forbidden = Evaluate(poolA, poolB);
			Assert.AreEqual(FlowRuleOutcome.Forbidden, forbidden.Outcome);
			Assert.AreEqual(FlowRuleOutcome.Forbidden, forbidden.ToOutcome);
			Assert.AreEqual(poolA, forbidden.ToRule.TargetId);
			CollectionAssert.AreEqual(new[] { "No IRD before this pool." }, forbidden.Messages.ToArray());

			Assert.AreEqual(FlowRuleOutcome.Allowed, Evaluate(poolC, poolB).Outcome);
		}

		[TestMethod]
		public void Evaluate_AllowingOverrideLiftsForbiddingDefault()
		{
			RulesOf(poolB).SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Forbidden);
			RulesOf(poolB).SetOverride(FlowRuleDirection.Before, FlowRuleObjectType.ResourcePool, poolA, FlowRuleOutcome.Allowed);

			Assert.AreEqual(FlowRuleOutcome.Allowed, Evaluate(poolA, poolB).Outcome);
			Assert.AreEqual(FlowRuleOutcome.Forbidden, Evaluate(poolC, poolB).Outcome);
		}

		[TestMethod]
		[DataRow(FlowRuleOutcome.Allowed, FlowRuleOutcome.Warning, FlowRuleOutcome.Warning)]
		[DataRow(FlowRuleOutcome.Warning, FlowRuleOutcome.Allowed, FlowRuleOutcome.Warning)]
		[DataRow(FlowRuleOutcome.Warning, FlowRuleOutcome.Forbidden, FlowRuleOutcome.Forbidden)]
		[DataRow(FlowRuleOutcome.Forbidden, FlowRuleOutcome.Warning, FlowRuleOutcome.Forbidden)]
		[DataRow(FlowRuleOutcome.Allowed, FlowRuleOutcome.Allowed, FlowRuleOutcome.Allowed)]
		public void Evaluate_MostRestrictiveOutcomeWins(FlowRuleOutcome sourceOutcome, FlowRuleOutcome destinationOutcome, FlowRuleOutcome expected)
		{
			RulesOf(poolA).SetDefault(FlowRuleDirection.After, sourceOutcome, "source");
			RulesOf(poolB).SetDefault(FlowRuleDirection.Before, destinationOutcome, "destination");

			var evaluation = Evaluate(poolA, poolB);

			Assert.AreEqual(expected, evaluation.Outcome);
			Assert.AreEqual(sourceOutcome, evaluation.FromOutcome);
			Assert.AreEqual(destinationOutcome, evaluation.ToOutcome);
		}

		[TestMethod]
		public void Evaluate_MessagesOnlyComeFromTheDecisiveSide()
		{
			RulesOf(poolA).SetDefault(FlowRuleDirection.After, FlowRuleOutcome.Warning, "source warning");
			RulesOf(poolB).SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Forbidden, "destination forbids");

			CollectionAssert.AreEqual(new[] { "destination forbids" }, Evaluate(poolA, poolB).Messages.ToArray());
		}

		[TestMethod]
		public void Evaluate_MessagesOfBothSidesWhenEquallyRestrictive()
		{
			RulesOf(poolA).SetDefault(FlowRuleDirection.After, FlowRuleOutcome.Warning, "source warning");
			RulesOf(poolB).SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Warning, "destination warning");

			CollectionAssert.AreEquivalent(new[] { "source warning", "destination warning" }, Evaluate(poolA, poolB).Messages.ToArray());
		}

		[TestMethod]
		public void Evaluate_SelfTargetingOverride_AppliesToConnectionBetweenNodesOfTheSamePool()
		{
			RulesOf(poolA).SetOverride(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, poolA, FlowRuleOutcome.Forbidden);

			Assert.AreEqual(FlowRuleOutcome.Forbidden, Evaluate(poolA, poolA).Outcome);
			Assert.AreEqual(FlowRuleOutcome.Allowed, Evaluate(poolA, poolB).Outcome);
		}

		[TestMethod]
		public void Evaluate_NodeGraph_EvaluatesEachConnectionWithItsNodes()
		{
			RulesOf(poolB).SetOverride(FlowRuleDirection.Before, FlowRuleObjectType.ResourcePool, poolA, FlowRuleOutcome.Forbidden);

			var nodeA = new WorkflowResourcePoolNode(poolA);
			var nodeB = new WorkflowResourcePoolNode(poolB);
			var nodeC = new WorkflowResourcePoolNode(poolC);

			var graph = new Workflow { Name = "Graph" }.NodeGraph
				.Add(nodeA)
				.Add(nodeB)
				.Add(nodeC)
				.Connect(nodeA, nodeB)
				.Connect(nodeC, nodeB);

			var evaluations = FlowRuleEvaluator.Evaluate(graph, GetFlowRules);

			Assert.AreEqual(2, evaluations.Count);

			var forbidden = evaluations.Single(x => x.FromNodeId == nodeA.Id);
			Assert.AreEqual(FlowRuleOutcome.Forbidden, forbidden.Outcome);
			Assert.AreEqual(nodeB.Id, forbidden.ToNodeId);
			Assert.AreEqual(FlowRuleObjectType.ResourcePool, forbidden.FromObjectType);
			Assert.AreEqual(poolA, forbidden.FromObjectId);
			Assert.AreEqual(FlowRuleObjectType.ResourcePool, forbidden.ToObjectType);
			Assert.AreEqual(poolB, forbidden.ToObjectId);
			Assert.IsFalse(String.IsNullOrEmpty(forbidden.ConnectionId));

			Assert.AreEqual(FlowRuleOutcome.Allowed, evaluations.Single(x => x.FromNodeId == nodeC.Id).Outcome);
		}

		[TestMethod]
		public void FlowRules_SetDefaultAndSetOverride_ReplaceExistingRules()
		{
			var rules = RulesOf(poolA);
			rules.SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Warning);
			rules.SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Forbidden);
			rules.SetOverride(FlowRuleDirection.Before, FlowRuleObjectType.ResourcePool, poolB, FlowRuleOutcome.Warning);
			rules.SetOverride(FlowRuleDirection.Before, FlowRuleObjectType.ResourcePool, poolB, FlowRuleOutcome.Allowed);

			Assert.AreEqual(2, rules.Rules.Count);
			Assert.AreEqual(FlowRuleOutcome.Forbidden, rules.GetDefault(FlowRuleDirection.Before).Outcome);
			Assert.AreEqual(FlowRuleOutcome.Allowed, rules.GetOverride(FlowRuleDirection.Before, FlowRuleObjectType.ResourcePool, poolB).Outcome);
			Assert.IsNull(rules.GetDefault(FlowRuleDirection.After));
		}

		[TestMethod]
		public void FlowRules_SetDefaultAndSetOverride_UpdateTheExistingRuleInPlace()
		{
			var rules = RulesOf(poolA);
			rules.SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Warning);
			rules.SetOverride(FlowRuleDirection.Before, FlowRuleObjectType.ResourcePool, poolB, FlowRuleOutcome.Warning);

			var defaultRule = rules.GetDefault(FlowRuleDirection.Before);
			var overrideRule = rules.GetOverride(FlowRuleDirection.Before, FlowRuleObjectType.ResourcePool, poolB);

			rules.SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Forbidden, "updated");
			rules.SetOverride(FlowRuleDirection.Before, FlowRuleObjectType.ResourcePool, poolB, FlowRuleOutcome.Allowed);

			Assert.AreSame(defaultRule, rules.GetDefault(FlowRuleDirection.Before));
			Assert.AreSame(overrideRule, rules.GetOverride(FlowRuleDirection.Before, FlowRuleObjectType.ResourcePool, poolB));
			Assert.AreEqual(FlowRuleOutcome.Forbidden, defaultRule.Outcome);
			Assert.AreEqual("updated", defaultRule.Message);
			Assert.AreEqual(FlowRuleOutcome.Allowed, overrideRule.Outcome);
		}

		[TestMethod]
		public void FlowRules_Add_AddsACopy()
		{
			var rule = new FlowRule(FlowRuleDirection.After, FlowRuleOutcome.Warning);
			var rules = RulesOf(poolA);
			rules.Add(rule);

			rule.Outcome = FlowRuleOutcome.Forbidden;

			Assert.AreEqual(FlowRuleOutcome.Warning, rules.GetDefault(FlowRuleDirection.After).Outcome);
		}

		[TestMethod]
		public void FlowRuleCollection_CollectionMembers()
		{
			var rules = RulesOf(poolA);
			var defaultRule = new FlowRule(FlowRuleDirection.Before, FlowRuleOutcome.Warning);
			var overrideRule = new FlowRule(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, poolB, FlowRuleOutcome.Forbidden, "message");

			rules.Add(defaultRule);
			rules.Add(overrideRule);

			Assert.AreEqual(2, rules.Count);
			Assert.IsFalse(rules.IsReadOnly);
			CollectionAssert.AreEqual(rules.Rules.ToArray(), rules.ToArray());

			// Matching is by value: an equal rule is found and removed, a rule with another message is not.
			Assert.IsTrue(rules.Contains(new FlowRule(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, poolB, FlowRuleOutcome.Forbidden, "message")));
			Assert.IsFalse(rules.Contains(new FlowRule(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, poolB, FlowRuleOutcome.Forbidden, "other")));

			var array = new FlowRule[3];
			rules.CopyTo(array, 1);
			Assert.IsNull(array[0]);
			Assert.AreEqual(defaultRule, array[1]);
			Assert.AreEqual(overrideRule, array[2]);

			Assert.IsTrue(rules.Remove(new FlowRule(FlowRuleDirection.Before, FlowRuleOutcome.Warning)));
			Assert.IsFalse(rules.Remove(new FlowRule(FlowRuleDirection.Before, FlowRuleOutcome.Warning)));
			Assert.AreEqual(1, rules.Count);

			rules.Clear();
			Assert.AreEqual(0, rules.Count);
			Assert.ThrowsException<ArgumentNullException>(() => rules.Add(null));
			Assert.ThrowsException<ArgumentNullException>(() => rules.Remove(null));
			Assert.ThrowsException<ArgumentNullException>(() => rules.Contains(null));
		}

		[TestMethod]
		public void FlowRule_Outcome_RejectsUndefinedValues()
		{
			var rule = new FlowRule(FlowRuleDirection.After, FlowRuleOutcome.Warning);

			Assert.ThrowsException<ArgumentException>(() => rule.Outcome = (FlowRuleOutcome)5);
		}

		[TestMethod]
		public void FlowRules_SetOverrideWithObject_UsesItsTypeAndId()
		{
			IFlowRuleObject target = new ResourcePool(poolB);

			var rules = RulesOf(poolA);
			rules.SetOverride(FlowRuleDirection.After, target, FlowRuleOutcome.Forbidden);
			var rule = rules.GetOverride(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, poolB);

			Assert.IsNotNull(rule);
			Assert.AreEqual(FlowRuleObjectType.ResourcePool, rule.TargetType);
			Assert.AreEqual(poolB, rule.TargetId);
			Assert.ThrowsException<ArgumentNullException>(() => RulesOf(poolA).SetOverride(FlowRuleDirection.After, (IFlowRuleObject)null, FlowRuleOutcome.Forbidden));
		}

		[TestMethod]
		public void FlowRules_RemoveAndClear()
		{
			var rules = RulesOf(poolA);
			rules.SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Warning);
			rules.SetDefault(FlowRuleDirection.After, FlowRuleOutcome.Warning);
			rules.SetOverride(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, poolB, FlowRuleOutcome.Forbidden);

			rules.RemoveOverride(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, poolB);
			Assert.AreEqual(2, rules.Rules.Count);

			rules.RemoveDefault(FlowRuleDirection.Before);
			Assert.AreEqual(1, rules.Rules.Count);

			rules.Clear(FlowRuleDirection.After);
			Assert.AreEqual(0, rules.Rules.Count);
		}

		[TestMethod]
		public void FlowRule_InvalidArguments_Throw()
		{
			Assert.ThrowsException<ArgumentException>(() => new FlowRule(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, Guid.Empty, FlowRuleOutcome.Forbidden));
			Assert.ThrowsException<ArgumentException>(() => new FlowRule(FlowRuleDirection.After, (FlowRuleObjectType)5, poolA, FlowRuleOutcome.Forbidden));
			Assert.ThrowsException<ArgumentException>(() => new FlowRule((FlowRuleDirection)5, FlowRuleOutcome.Forbidden));
			Assert.ThrowsException<ArgumentException>(() => new FlowRule(FlowRuleDirection.After, (FlowRuleOutcome)5));
		}

		[TestMethod]
		public void FlowRule_ValueEquality()
		{
			Assert.AreEqual(
				new FlowRule(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, poolA, FlowRuleOutcome.Warning, "message"),
				new FlowRule(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, poolA, FlowRuleOutcome.Warning, "message"));

			Assert.AreNotEqual(
				new FlowRule(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, poolA, FlowRuleOutcome.Warning, "message"),
				new FlowRule(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, poolA, FlowRuleOutcome.Warning, "other"));
		}

		[TestMethod]
		public void FlowRules_HashCode_ChangesWithRulesButNotWithTheirOrder()
		{
			var rules = new ResourcePool().FlowRules;
			var emptyHash = rules.GetHashCode();

			rules.SetDefault(FlowRuleDirection.After, FlowRuleOutcome.Warning);
			rules.SetOverride(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, poolB, FlowRuleOutcome.Forbidden);
			var hashInOrder = rules.GetHashCode();

			rules.Clear();
			rules.SetOverride(FlowRuleDirection.After, FlowRuleObjectType.ResourcePool, poolB, FlowRuleOutcome.Forbidden);
			rules.SetDefault(FlowRuleDirection.After, FlowRuleOutcome.Warning);
			var hashReversed = rules.GetHashCode();

			Assert.AreNotEqual(emptyHash, hashInOrder);
			Assert.AreEqual(hashInOrder, hashReversed);
		}

		[TestMethod]
		public void ResourcePool_HashCode_ChangesWithFlowRules()
		{
			var pool = new ResourcePool { Name = "Pool" };
			var hashBefore = pool.GetHashCode();

			pool.FlowRules.SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Forbidden);

			Assert.AreNotEqual(hashBefore, pool.GetHashCode());
		}

		private FlowRuleCollection RulesOf(Guid poolId)
		{
			if (!flowRulesByPoolId.TryGetValue(poolId, out var rules))
			{
				rules = new ResourcePool(poolId).FlowRules;
				flowRulesByPoolId[poolId] = rules;
			}

			return rules;
		}

		private FlowRuleCollection GetFlowRules(FlowRuleObject owner)
		{
			return owner.Type == FlowRuleObjectType.ResourcePool && flowRulesByPoolId.TryGetValue(owner.Id, out var rules) ? rules : null;
		}

		private FlowRuleEvaluation Evaluate(Guid from, Guid to)
		{
			return FlowRuleEvaluator.Evaluate(FlowRuleObject.ResourcePool(from), FlowRuleObject.ResourcePool(to), GetFlowRules);
		}
	}
}
