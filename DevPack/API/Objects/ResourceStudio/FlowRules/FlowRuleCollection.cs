namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	/// <summary>
	/// Represents the collection of flow rules of an object: per direction an optional default rule and any number of
	/// overrides for specific objects. Directions without a default rule behave as <see cref="FlowRuleOutcome.Allowed"/>.
	/// </summary>
	/// <remarks>Flow rules are persisted together with the object that owns them.</remarks>
	public sealed class FlowRuleCollection : ApiObject, ICollection<FlowRule>
	{
		private readonly List<FlowRule> rules = [];

		private StorageResourceStudio.FlowrulesInstance originalInstance;
		private StorageResourceStudio.FlowrulesInstance updatedInstance;

		internal FlowRuleCollection(FlowRuleObject owner) : base()
		{
			IsNew = true;
			Owner = owner;
		}

		internal FlowRuleCollection(StorageResourceStudio.FlowrulesInstance instance) : base(instance?.ID.Id ?? throw new ArgumentNullException(nameof(instance)))
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

		/// <summary>
		/// Gets the number of flow rules, both defaults and overrides.
		/// </summary>
		public int Count => rules.Count;

		/// <summary>
		/// Gets a value indicating whether the collection is read-only. Always <see langword="false"/>.
		/// </summary>
		public bool IsReadOnly => false;

		internal StorageResourceStudio.FlowrulesInstance OriginalInstance => originalInstance;

		/// <summary>
		/// Adds a flow rule.
		/// </summary>
		/// <param name="item">The flow rule to add.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
		/// <remarks>A copy of <paramref name="item"/> is added. Adding a second default for the same direction, or a second override for the same direction and target, is reported as an error when the owning object is saved. Use <see cref="SetDefault"/> or <see cref="SetOverride(FlowRuleDirection, FlowRuleObjectType, Guid, FlowRuleOutcome, string)"/> to update an existing rule, or change the <see cref="FlowRule.Outcome"/> and <see cref="FlowRule.Message"/> of a rule in the collection directly.</remarks>
		public void Add(FlowRule item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item));
			}

			rules.Add(new FlowRule(item));
		}

		/// <summary>
		/// Removes a flow rule.
		/// </summary>
		/// <param name="item">The flow rule to remove. A rule with the same direction, target, outcome and message is removed.</param>
		/// <returns><see langword="true"/> when a rule was removed; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
		public bool Remove(FlowRule item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item));
			}

			var index = rules.FindIndex(x => x.Equals(item));
			if (index < 0)
			{
				return false;
			}

			rules.RemoveAt(index);
			return true;
		}

		/// <summary>
		/// Determines whether the collection contains a flow rule.
		/// </summary>
		/// <param name="item">The flow rule to locate. A rule with the same direction, target, outcome and message matches.</param>
		/// <returns><see langword="true"/> when the collection contains the rule; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
		public bool Contains(FlowRule item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item));
			}

			return rules.Any(x => x.Equals(item));
		}

		/// <summary>
		/// Copies the flow rules to an array, starting at a particular index.
		/// </summary>
		/// <param name="array">The destination array.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="arrayIndex"/> is less than zero.</exception>
		/// <exception cref="ArgumentException">Thrown when the rules do not fit in <paramref name="array"/> from <paramref name="arrayIndex"/> on.</exception>
		public void CopyTo(FlowRule[] array, int arrayIndex)
		{
			rules.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the flow rules.
		/// </summary>
		/// <returns>An enumerator for the flow rules.</returns>
		public IEnumerator<FlowRule> GetEnumerator()
		{
			return rules.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// Sets the default rule of a direction. An existing default for that direction is updated.
		/// </summary>
		/// <param name="direction">The direction to set the default for.</param>
		/// <param name="outcome">The outcome for any object that is not covered by an override.</param>
		/// <param name="message">An optional message that explains the outcome.</param>
		public void SetDefault(FlowRuleDirection direction, FlowRuleOutcome outcome, string message = null)
		{
			Set(new FlowRule(direction, outcome, message));
		}

		/// <summary>
		/// Removes the default rule of a direction, so that direction behaves as <see cref="FlowRuleOutcome.Allowed"/> for objects without an override.
		/// </summary>
		/// <param name="direction">The direction to remove the default for.</param>
		public void RemoveDefault(FlowRuleDirection direction)
		{
			rules.RemoveAll(x => x.Matches(direction, null));
		}

		/// <summary>
		/// Sets an override for a specific object. An existing override for that direction and object is updated.
		/// </summary>
		/// <param name="direction">The direction the override applies to.</param>
		/// <param name="targetType">The type of the object the override applies to.</param>
		/// <param name="targetId">The unique identifier of the object the override applies to.</param>
		/// <param name="outcome">The outcome for the target object.</param>
		/// <param name="message">An optional message that explains the outcome.</param>
		public void SetOverride(FlowRuleDirection direction, FlowRuleObjectType targetType, Guid targetId, FlowRuleOutcome outcome, string message = null)
		{
			Set(new FlowRule(direction, targetType, targetId, outcome, message));
		}

		/// <summary>
		/// Sets an override for a specific object. An existing override for that direction and object is updated.
		/// </summary>
		/// <param name="direction">The direction the override applies to.</param>
		/// <param name="target">The object the override applies to.</param>
		/// <param name="outcome">The outcome for the target object.</param>
		/// <param name="message">An optional message that explains the outcome.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is <see langword="null"/>.</exception>
		/// <remarks>Shorthand for <see cref="SetOverride(FlowRuleDirection, FlowRuleObjectType, Guid, FlowRuleOutcome, string)"/> with the type and identifier of <paramref name="target"/>.</remarks>
		public void SetOverride(FlowRuleDirection direction, IFlowRuleObject target, FlowRuleOutcome outcome, string message = null)
		{
			if (target == null)
			{
				throw new ArgumentNullException(nameof(target));
			}

			SetOverride(direction, target.FlowRuleObjectType, target.Id, outcome, message);
		}

		/// <summary>
		/// Removes the override of a direction for a specific object.
		/// </summary>
		/// <param name="direction">The direction of the override.</param>
		/// <param name="targetType">The type of the object the override applies to.</param>
		/// <param name="targetId">The unique identifier of the object the override applies to.</param>
		public void RemoveOverride(FlowRuleDirection direction, FlowRuleObjectType targetType, Guid targetId)
		{
			var target = new FlowRuleObject(targetType, targetId);
			rules.RemoveAll(x => x.Matches(direction, target));
		}

		/// <summary>
		/// Removes all flow rules.
		/// </summary>
		public void Clear()
		{
			rules.Clear();
		}

		/// <summary>
		/// Removes all flow rules of a direction.
		/// </summary>
		/// <param name="direction">The direction to remove the rules for.</param>
		public void Clear(FlowRuleDirection direction)
		{
			rules.RemoveAll(x => x.Direction == direction);
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

				foreach (var rule in GetOrderedRules())
				{
					hash = (hash * 23) + rule.GetHashCode();
				}

				return hash;
			}
		}

		/// <inheritdoc/>
		/// <remarks>Two instances are equal when they belong to the same object and contain the same rules, as often, regardless of their order.</remarks>
		public override bool Equals(object obj)
		{
			if (obj is not FlowRuleCollection other)
			{
				return false;
			}

			// Compared in a fixed order, so duplicates are counted like in GetHashCode.
			return Owner == other.Owner
				&& rules.Count == other.rules.Count
				&& GetOrderedRules().SequenceEqual(other.GetOrderedRules());
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

		private IEnumerable<FlowRule> GetOrderedRules()
		{
			return rules
				.OrderBy(x => x.Direction)
				.ThenBy(x => x.TargetId.HasValue)
				.ThenBy(x => x.TargetType)
				.ThenBy(x => x.TargetId)
				.ThenBy(x => x.Outcome)
				.ThenBy(x => x.Message, StringComparer.Ordinal);
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
