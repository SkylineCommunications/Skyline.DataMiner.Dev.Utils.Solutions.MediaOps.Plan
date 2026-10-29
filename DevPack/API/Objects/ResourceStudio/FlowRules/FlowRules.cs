namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	/// <summary>
	/// Represents the flow rules of an object: per direction an optional default rule and any number of overrides for
	/// specific objects. Directions without a default rule behave as <see cref="FlowRuleOutcome.Allowed"/>.
	/// </summary>
	/// <remarks>Flow rules are persisted together with the object that owns them.</remarks>
	public sealed class FlowRules : ApiObject
	{
		private readonly List<FlowRule> rules = [];

		private StorageResourceStudio.FlowrulesInstance originalInstance;
		private StorageResourceStudio.FlowrulesInstance updatedInstance;

		// A failed save leaves an owner without a stored pointer to its rule set, so every attempt starts a new rule set;
		// a fixed ID per owner makes the next attempt overwrite the rule set of the previous one instead of adding another.
		internal FlowRules(FlowRuleObject owner) : base(owner.GetFlowRulesId())
		{
			IsNew = true;
			Owner = owner;
		}

		internal FlowRules(StorageResourceStudio.FlowrulesInstance instance) : base(instance?.ID.Id ?? throw new ArgumentNullException(nameof(instance)))
		{
			ParseInstance(instance);
			InitTracking();
		}

		/// <summary>
		/// Gets the type of the object that owns these flow rules.
		/// </summary>
		public FlowRuleObjectType OwnerType => Owner.Type;

		/// <summary>
		/// Gets the unique identifier of the object that owns these flow rules.
		/// </summary>
		public Guid OwnerId => Owner.Id;

		internal FlowRuleObject Owner { get; private set; }

		/// <summary>
		/// Gets all flow rules, both defaults and overrides.
		/// </summary>
		public IReadOnlyCollection<FlowRule> Rules => rules;

		internal StorageResourceStudio.FlowrulesInstance OriginalInstance => originalInstance;

		/// <summary>
		/// Adds a flow rule.
		/// </summary>
		/// <param name="rule">The flow rule to add.</param>
		/// <returns>The current <see cref="FlowRules"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="rule"/> is <see langword="null"/>.</exception>
		/// <remarks>A copy of <paramref name="rule"/> is added. Adding a second default for the same direction, or a second override for the same direction and target, is reported as an error when the owning object is saved. Use <see cref="SetDefault"/> or <see cref="SetOverride(FlowRuleDirection, FlowRuleObjectType, Guid, FlowRuleOutcome, string)"/> to update an existing rule, or change the <see cref="FlowRule.Outcome"/> and <see cref="FlowRule.Message"/> of a rule in <see cref="Rules"/> directly.</remarks>
		public FlowRules Add(FlowRule rule)
		{
			if (rule == null)
			{
				throw new ArgumentNullException(nameof(rule));
			}

			rules.Add(new FlowRule(rule));
			return this;
		}

		/// <summary>
		/// Removes a flow rule.
		/// </summary>
		/// <param name="rule">The flow rule to remove.</param>
		/// <returns>The current <see cref="FlowRules"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="rule"/> is <see langword="null"/>.</exception>
		public FlowRules Remove(FlowRule rule)
		{
			if (rule == null)
			{
				throw new ArgumentNullException(nameof(rule));
			}

			rules.RemoveAll(x => x.Equals(rule));
			return this;
		}

		/// <summary>
		/// Sets the default rule of a direction. An existing default for that direction is updated.
		/// </summary>
		/// <param name="direction">The direction to set the default for.</param>
		/// <param name="outcome">The outcome for any object that is not covered by an override.</param>
		/// <param name="message">An optional message that explains the outcome.</param>
		/// <returns>The current <see cref="FlowRules"/> instance.</returns>
		public FlowRules SetDefault(FlowRuleDirection direction, FlowRuleOutcome outcome, string message = null)
		{
			Set(new FlowRule(direction, outcome, message));
			return this;
		}

		/// <summary>
		/// Removes the default rule of a direction, so that direction behaves as <see cref="FlowRuleOutcome.Allowed"/> for objects without an override.
		/// </summary>
		/// <param name="direction">The direction to remove the default for.</param>
		/// <returns>The current <see cref="FlowRules"/> instance.</returns>
		public FlowRules RemoveDefault(FlowRuleDirection direction)
		{
			rules.RemoveAll(x => x.Matches(direction, null));
			return this;
		}

		/// <summary>
		/// Sets an override for a specific object. An existing override for that direction and object is updated.
		/// </summary>
		/// <param name="direction">The direction the override applies to.</param>
		/// <param name="targetType">The type of the object the override applies to.</param>
		/// <param name="targetId">The unique identifier of the object the override applies to.</param>
		/// <param name="outcome">The outcome for the target object.</param>
		/// <param name="message">An optional message that explains the outcome.</param>
		/// <returns>The current <see cref="FlowRules"/> instance.</returns>
		public FlowRules SetOverride(FlowRuleDirection direction, FlowRuleObjectType targetType, Guid targetId, FlowRuleOutcome outcome, string message = null)
		{
			Set(new FlowRule(direction, targetType, targetId, outcome, message));
			return this;
		}

		/// <summary>
		/// Sets an override for a specific object. An existing override for that direction and object is updated.
		/// </summary>
		/// <param name="direction">The direction the override applies to.</param>
		/// <param name="target">The object the override applies to.</param>
		/// <param name="outcome">The outcome for the target object.</param>
		/// <param name="message">An optional message that explains the outcome.</param>
		/// <returns>The current <see cref="FlowRules"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is <see langword="null"/>.</exception>
		/// <remarks>Shorthand for <see cref="SetOverride(FlowRuleDirection, FlowRuleObjectType, Guid, FlowRuleOutcome, string)"/> with the type and identifier of <paramref name="target"/>.</remarks>
		public FlowRules SetOverride(FlowRuleDirection direction, IFlowRuleObject target, FlowRuleOutcome outcome, string message = null)
		{
			if (target == null)
			{
				throw new ArgumentNullException(nameof(target));
			}

			return SetOverride(direction, target.FlowRuleObjectType, target.Id, outcome, message);
		}

		/// <summary>
		/// Removes the override of a direction for a specific object.
		/// </summary>
		/// <param name="direction">The direction of the override.</param>
		/// <param name="targetType">The type of the object the override applies to.</param>
		/// <param name="targetId">The unique identifier of the object the override applies to.</param>
		/// <returns>The current <see cref="FlowRules"/> instance.</returns>
		public FlowRules RemoveOverride(FlowRuleDirection direction, FlowRuleObjectType targetType, Guid targetId)
		{
			var target = new FlowRuleObject(targetType, targetId);
			rules.RemoveAll(x => x.Matches(direction, target));
			return this;
		}

		/// <summary>
		/// Removes all flow rules.
		/// </summary>
		/// <returns>The current <see cref="FlowRules"/> instance.</returns>
		public FlowRules Clear()
		{
			rules.Clear();
			return this;
		}

		/// <summary>
		/// Removes all flow rules of a direction.
		/// </summary>
		/// <param name="direction">The direction to remove the rules for.</param>
		/// <returns>The current <see cref="FlowRules"/> instance.</returns>
		public FlowRules Clear(FlowRuleDirection direction)
		{
			rules.RemoveAll(x => x.Direction == direction);
			return this;
		}

		/// <summary>
		/// Gets the default rule of a direction.
		/// </summary>
		/// <param name="direction">The direction.</param>
		/// <returns>The default rule, or <see langword="null"/> when no default is configured.</returns>
		public FlowRule GetDefault(FlowRuleDirection direction)
		{
			return rules.FirstOrDefault(x => x.Matches(direction, null));
		}

		/// <summary>
		/// Gets the override of a direction for a specific object.
		/// </summary>
		/// <param name="direction">The direction.</param>
		/// <param name="targetType">The type of the object.</param>
		/// <param name="targetId">The unique identifier of the object.</param>
		/// <returns>The override, or <see langword="null"/> when no override is configured.</returns>
		public FlowRule GetOverride(FlowRuleDirection direction, FlowRuleObjectType targetType, Guid targetId)
		{
			return GetOverride(direction, new FlowRuleObject(targetType, targetId));
		}

		/// <summary>
		/// Gets the rule that applies to a specific object in a direction: its override when present, otherwise the default of the direction.
		/// </summary>
		/// <param name="direction">The direction.</param>
		/// <param name="type">The type of the object.</param>
		/// <param name="id">The unique identifier of the object.</param>
		/// <returns>The applicable rule, or <see langword="null"/> when neither an override nor a default is configured.</returns>
		public FlowRule GetApplicableRule(FlowRuleDirection direction, FlowRuleObjectType type, Guid id)
		{
			return GetApplicableRule(direction, new FlowRuleObject(type, id));
		}

		/// <inheritdoc/>
		/// <remarks>The identifier is not part of the hash code: an object that has no stored flow rules yet gets a new identifier on every read.</remarks>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + Owner.GetHashCode();

				foreach (var rule in rules
					.OrderBy(x => x.Direction)
					.ThenBy(x => x.TargetId.HasValue)
					.ThenBy(x => x.TargetType)
					.ThenBy(x => x.TargetId)
					.ThenBy(x => x.Outcome)
					.ThenBy(x => x.Message, StringComparer.Ordinal))
				{
					hash = (hash * 23) + rule.GetHashCode();
				}

				return hash;
			}
		}

		/// <inheritdoc/>
		/// <remarks>Two instances are equal when they belong to the same object and contain the same rules, regardless of their order.</remarks>
		public override bool Equals(object obj)
		{
			if (obj is not FlowRules other)
			{
				return false;
			}

			return Owner == other.Owner
				&& rules.Count == other.rules.Count
				&& rules.All(other.rules.Contains)
				&& other.rules.All(rules.Contains);
		}

		internal StorageResourceStudio.FlowrulesInstance GetInstanceWithChanges()
		{
			if (updatedInstance == null)
			{
				updatedInstance = IsNew ? new StorageResourceStudio.FlowrulesInstance(Id) : originalInstance.Clone();
			}

			updatedInstance.Flowrulesinfo.OwnerId = Owner.Id;
			updatedInstance.Flowrulesinfo.Ownertype = FlowRuleObject.ToStorage(Owner.Type);

			updatedInstance.Flowrule.Clear();
			foreach (var rule in rules)
			{
				updatedInstance.Flowrule.Add(rule.GetSectionWithChanges());
			}

			return updatedInstance;
		}

		internal FlowRule GetOverride(FlowRuleDirection direction, FlowRuleObject target)
		{
			return rules.FirstOrDefault(x => x.Matches(direction, target));
		}

		internal FlowRule GetApplicableRule(FlowRuleDirection direction, FlowRuleObject target)
		{
			return GetOverride(direction, target) ?? GetDefault(direction);
		}

		private void ParseInstance(StorageResourceStudio.FlowrulesInstance instance)
		{
			originalInstance = instance;

			Owner = new FlowRuleObject(FlowRuleObject.FromStorage(instance.Flowrulesinfo.Ownertype), instance.Flowrulesinfo.OwnerId);

			foreach (var section in instance.Flowrule.Where(x => x.Direction.HasValue))
			{
				rules.Add(new FlowRule(section));
			}
		}

		// Updates the existing rule in place so its stored section, and therefore its section ID, is kept.
		private void Set(FlowRule rule)
		{
			var existing = rules.FirstOrDefault(x => x.Matches(rule.Direction, rule.Target));
			if (existing == null)
			{
				rules.Add(rule);
				return;
			}

			existing.Outcome = rule.Outcome;
			existing.Message = rule.Message;

			// Duplicates added through Add are collapsed onto the updated rule.
			rules.RemoveAll(x => !ReferenceEquals(x, existing) && x.Matches(rule.Direction, rule.Target));
		}
	}
}
