namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	/// <summary>
	/// Represents a single flow rule. A rule without a target is the default rule for its direction; a rule with a target
	/// overrides that default for the target object.
	/// </summary>
	public sealed class FlowRule : IEquatable<FlowRule>
	{
		private readonly StorageResourceStudio.FlowruleSection originalSection;

		/// <summary>
		/// Initializes a new instance of the <see cref="FlowRule"/> class that represents the default rule of a direction.
		/// </summary>
		/// <param name="direction">The direction the rule applies to.</param>
		/// <param name="outcome">The outcome for any object that is not covered by an override.</param>
		/// <param name="message">An optional message that explains the outcome.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> or <paramref name="outcome"/> is not a defined value.</exception>
		public FlowRule(FlowRuleDirection direction, FlowRuleOutcome outcome, string message = null)
			: this(direction, null, outcome, message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FlowRule"/> class that overrides the default rule of a direction for a specific object.
		/// </summary>
		/// <param name="direction">The direction the rule applies to.</param>
		/// <param name="targetType">The type of the object the override applies to.</param>
		/// <param name="targetId">The unique identifier of the object the override applies to.</param>
		/// <param name="outcome">The outcome for the target object.</param>
		/// <param name="message">An optional message that explains the outcome.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="targetId"/> is <see cref="Guid.Empty"/>, or when <paramref name="direction"/>, <paramref name="targetType"/> or <paramref name="outcome"/> is not a defined value.</exception>
		public FlowRule(FlowRuleDirection direction, FlowRuleObjectType targetType, Guid targetId, FlowRuleOutcome outcome, string message = null)
			: this(direction, ValidateTarget(targetType, targetId), outcome, message)
		{
		}

		internal FlowRule(StorageResourceStudio.FlowruleSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Direction = section.Direction.HasValue
				? section.Direction.Value.MapEnum<StorageResourceStudio.SlcResource_StudioIds.Enums.Flowruledirection, FlowRuleDirection>()
				: FlowRuleDirection.Before;
			Target = section.TargetId.HasValue
				? new FlowRuleObject(FlowRuleObject.FromStorage(section.Targettype), section.TargetId.Value)
				: (FlowRuleObject?)null;
			Outcome = section.Outcome.HasValue
				? section.Outcome.Value.MapEnum<StorageResourceStudio.SlcResource_StudioIds.Enums.Flowruleoutcome, FlowRuleOutcome>()
				: FlowRuleOutcome.Allowed;
			Message = section.Message;
		}

		private FlowRule(FlowRuleDirection direction, FlowRuleObject? target, FlowRuleOutcome outcome, string message)
		{
			if (!Enum.IsDefined(typeof(FlowRuleDirection), direction))
			{
				throw new ArgumentException($"Value '{direction}' is not a valid flow rule direction.", nameof(direction));
			}

			if (!Enum.IsDefined(typeof(FlowRuleOutcome), outcome))
			{
				throw new ArgumentException($"Value '{outcome}' is not a valid flow rule outcome.", nameof(outcome));
			}

			Direction = direction;
			Target = target;
			Outcome = outcome;
			Message = message;
		}

		/// <summary>
		/// Gets the direction the rule applies to.
		/// </summary>
		public FlowRuleDirection Direction { get; }

		/// <summary>
		/// Gets the type of the object this rule applies to, or <see langword="null"/> when this is the default rule of its direction.
		/// </summary>
		public FlowRuleObjectType? TargetType => Target?.Type;

		/// <summary>
		/// Gets the unique identifier of the object this rule applies to, or <see langword="null"/> when this is the default rule of its direction.
		/// </summary>
		public Guid? TargetId => Target?.Id;

		/// <summary>
		/// Gets the outcome of the rule.
		/// </summary>
		public FlowRuleOutcome Outcome { get; }

		/// <summary>
		/// Gets the optional message that explains the outcome.
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Gets a value indicating whether this rule is the default rule of its direction.
		/// </summary>
		public bool IsDefault => !Target.HasValue;

		internal FlowRuleObject? Target { get; }

		/// <inheritdoc/>
		public bool Equals(FlowRule other)
		{
			if (other is null)
			{
				return false;
			}

			return Direction == other.Direction
				&& Target == other.Target
				&& Outcome == other.Outcome
				&& String.Equals(Message, other.Message, StringComparison.Ordinal);
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			return obj is FlowRule other && Equals(other);
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + Direction.GetHashCode();
				hash = (hash * 23) + Target.GetHashCode();
				hash = (hash * 23) + Outcome.GetHashCode();
				hash = (hash * 23) + (Message != null ? StringComparer.Ordinal.GetHashCode(Message) : 0);
				return hash;
			}
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			var target = IsDefault ? "default" : Target.Value.ToString();
			return $"{Direction} {target}: {Outcome}";
		}

		internal bool Matches(FlowRuleDirection direction, FlowRuleObject? target)
		{
			return Direction == direction && Target == target;
		}

		internal StorageResourceStudio.FlowruleSection GetSectionWithChanges()
		{
			var section = originalSection != null ? originalSection.Clone() : new StorageResourceStudio.FlowruleSection();

			section.Direction = Direction.MapEnum<FlowRuleDirection, StorageResourceStudio.SlcResource_StudioIds.Enums.Flowruledirection>();
			section.TargetId = TargetId;
			section.Targettype = Target.HasValue ? FlowRuleObject.ToStorage(Target.Value.Type) : null;
			section.Outcome = Outcome.MapEnum<FlowRuleOutcome, StorageResourceStudio.SlcResource_StudioIds.Enums.Flowruleoutcome>();
			section.Message = String.IsNullOrEmpty(Message) ? null : Message;

			return section;
		}

		private static FlowRuleObject? ValidateTarget(FlowRuleObjectType targetType, Guid targetId)
		{
			FlowRuleObject.ValidateType(targetType, nameof(targetType));

			if (targetId == Guid.Empty)
			{
				throw new ArgumentException("Target ID cannot be empty.", nameof(targetId));
			}

			return new FlowRuleObject(targetType, targetId);
		}
	}
}
