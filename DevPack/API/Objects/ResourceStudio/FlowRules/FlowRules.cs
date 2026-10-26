namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	/// <summary>
	/// Represents the flow rules of a resource pool: per direction an optional default rule and any number of overrides for
	/// specific resource pools. Directions without a default rule behave as <see cref="FlowRuleOutcome.Allowed"/>.
	/// </summary>
	/// <remarks>Flow rules are persisted together with the owning <see cref="ResourcePool"/>.</remarks>
	public sealed class FlowRules : ApiObject
	{
		private readonly List<FlowRule> rules = [];

		private StorageResourceStudio.FlowrulesInstance originalInstance;
		private StorageResourceStudio.FlowrulesInstance updatedInstance;

		internal FlowRules(Guid resourcePoolId) : base()
		{
			IsNew = true;
			ResourcePoolId = resourcePoolId;
		}

		internal FlowRules(StorageResourceStudio.FlowrulesInstance instance, Guid resourcePoolId) : base(instance?.ID.Id ?? throw new ArgumentNullException(nameof(instance)))
		{
			originalInstance = instance;

			// The owning pool is authoritative; the stored owner only matters for queries on the rule set itself.
			ResourcePoolId = resourcePoolId;

			foreach (var section in instance.Flowrule.Where(x => x.Direction.HasValue))
			{
				rules.Add(new FlowRule(section));
			}

			InitTracking();
		}

		/// <summary>
		/// Gets the unique identifier of the resource pool that owns these flow rules.
		/// </summary>
		public Guid ResourcePoolId { get; private set; }

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
		/// <remarks>Adding a second default for the same direction, or a second override for the same direction and target, is reported as an error when the resource pool is saved. Use <see cref="SetDefault"/> or <see cref="SetOverride(FlowRuleDirection, Guid, FlowRuleOutcome, string)"/> to replace an existing rule.</remarks>
		public FlowRules Add(FlowRule rule)
		{
			if (rule == null)
			{
				throw new ArgumentNullException(nameof(rule));
			}

			rules.Add(rule);
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
		/// Sets the default rule of a direction, replacing any existing default for that direction.
		/// </summary>
		/// <param name="direction">The direction to set the default for.</param>
		/// <param name="outcome">The outcome for any resource pool that is not covered by an override.</param>
		/// <param name="message">An optional message that explains the outcome.</param>
		/// <returns>The current <see cref="FlowRules"/> instance.</returns>
		public FlowRules SetDefault(FlowRuleDirection direction, FlowRuleOutcome outcome, string message = null)
		{
			Replace(new FlowRule(direction, outcome, message));
			return this;
		}

		/// <summary>
		/// Removes the default rule of a direction, so that direction behaves as <see cref="FlowRuleOutcome.Allowed"/> for resource pools without an override.
		/// </summary>
		/// <param name="direction">The direction to remove the default for.</param>
		/// <returns>The current <see cref="FlowRules"/> instance.</returns>
		public FlowRules RemoveDefault(FlowRuleDirection direction)
		{
			rules.RemoveAll(x => x.Matches(direction, null));
			return this;
		}

		/// <summary>
		/// Sets an override for a specific resource pool, replacing any existing override for that direction and resource pool.
		/// </summary>
		/// <param name="direction">The direction the override applies to.</param>
		/// <param name="targetResourcePoolId">The unique identifier of the resource pool the override applies to.</param>
		/// <param name="outcome">The outcome for the target resource pool.</param>
		/// <param name="message">An optional message that explains the outcome.</param>
		/// <returns>The current <see cref="FlowRules"/> instance.</returns>
		public FlowRules SetOverride(FlowRuleDirection direction, Guid targetResourcePoolId, FlowRuleOutcome outcome, string message = null)
		{
			Replace(new FlowRule(direction, targetResourcePoolId, outcome, message));
			return this;
		}

		/// <summary>
		/// Sets an override for a specific resource pool, replacing any existing override for that direction and resource pool.
		/// </summary>
		/// <param name="direction">The direction the override applies to.</param>
		/// <param name="targetResourcePool">The resource pool the override applies to.</param>
		/// <param name="outcome">The outcome for the target resource pool.</param>
		/// <param name="message">An optional message that explains the outcome.</param>
		/// <returns>The current <see cref="FlowRules"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="targetResourcePool"/> is <see langword="null"/>.</exception>
		public FlowRules SetOverride(FlowRuleDirection direction, ResourcePool targetResourcePool, FlowRuleOutcome outcome, string message = null)
		{
			if (targetResourcePool == null)
			{
				throw new ArgumentNullException(nameof(targetResourcePool));
			}

			return SetOverride(direction, targetResourcePool.Id, outcome, message);
		}

		/// <summary>
		/// Removes the override of a direction for a specific resource pool.
		/// </summary>
		/// <param name="direction">The direction of the override.</param>
		/// <param name="targetResourcePoolId">The unique identifier of the resource pool the override applies to.</param>
		/// <returns>The current <see cref="FlowRules"/> instance.</returns>
		public FlowRules RemoveOverride(FlowRuleDirection direction, Guid targetResourcePoolId)
		{
			rules.RemoveAll(x => x.Matches(direction, targetResourcePoolId));
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
		/// Gets the override of a direction for a specific resource pool.
		/// </summary>
		/// <param name="direction">The direction.</param>
		/// <param name="targetResourcePoolId">The unique identifier of the resource pool.</param>
		/// <returns>The override, or <see langword="null"/> when no override is configured.</returns>
		public FlowRule GetOverride(FlowRuleDirection direction, Guid targetResourcePoolId)
		{
			return rules.FirstOrDefault(x => x.Matches(direction, targetResourcePoolId));
		}

		/// <summary>
		/// Gets the rule that applies to a specific resource pool in a direction: its override when present, otherwise the default of the direction.
		/// </summary>
		/// <param name="direction">The direction.</param>
		/// <param name="resourcePoolId">The unique identifier of the resource pool.</param>
		/// <returns>The applicable rule, or <see langword="null"/> when neither an override nor a default is configured.</returns>
		public FlowRule GetApplicableRule(FlowRuleDirection direction, Guid resourcePoolId)
		{
			return GetOverride(direction, resourcePoolId) ?? GetDefault(direction);
		}

		/// <inheritdoc/>
		/// <remarks>The identifier is not part of the hash code: a resource pool that has no stored flow rules yet gets a new identifier on every read.</remarks>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + ResourcePoolId.GetHashCode();

				foreach (var rule in rules
					.OrderBy(x => x.Direction)
					.ThenBy(x => x.TargetResourcePoolId.HasValue)
					.ThenBy(x => x.TargetResourcePoolId)
					.ThenBy(x => x.Outcome)
					.ThenBy(x => x.Message, StringComparer.Ordinal))
				{
					hash = (hash * 23) + rule.GetHashCode();
				}

				return hash;
			}
		}

		/// <inheritdoc/>
		/// <remarks>Two instances are equal when they belong to the same resource pool and contain the same rules, regardless of their order.</remarks>
		public override bool Equals(object obj)
		{
			if (obj is not FlowRules other)
			{
				return false;
			}

			return ResourcePoolId == other.ResourcePoolId
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

			updatedInstance.Flowrulesinfo.OwnerId = ResourcePoolId;
			updatedInstance.Flowrulesinfo.Ownertype = StorageResourceStudio.SlcResource_StudioIds.Enums.Flowruleobjecttype.Resourcepool;

			updatedInstance.Flowrule.Clear();
			foreach (var rule in rules)
			{
				updatedInstance.Flowrule.Add(rule.GetSectionWithChanges());
			}

			return updatedInstance;
		}

		private void Replace(FlowRule rule)
		{
			rules.RemoveAll(x => x.Matches(rule.Direction, rule.TargetResourcePoolId));
			rules.Add(rule);
		}
	}
}
