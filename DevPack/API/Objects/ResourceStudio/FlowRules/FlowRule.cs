namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	/// <summary>
	/// Represents a single flow rule. A rule without a target is the default rule for its direction; a rule with a target
	/// overrides that default for the target object.
	/// </summary>
	public class FlowRule : TrackableObject
	{
		private readonly StorageResourceStudio.FlowruleSection originalSection;

		private StorageResourceStudio.FlowruleSection updatedSection;

		private FlowRuleOutcome outcome;

		/// <summary>
		/// Initializes a new instance of the <see cref="FlowRule"/> class that represents the default rule of a direction.
		/// </summary>
		/// <param name="direction">The direction the rule applies to.</param>
		/// <param name="outcome">The outcome for any object that is not covered by an override.</param>
		/// <param name="message">An optional message that explains the outcome.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> or <paramref name="outcome"/> is not a defined value.</exception>
		public FlowRule(FlowRuleDirection direction, FlowRuleOutcome outcome, string message = null)
		{
			Initialize(direction, null, outcome, message);
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
		{
			FlowRuleObject.ValidateType(targetType, nameof(targetType));

			if (targetId == Guid.Empty)
			{
				throw new ArgumentException("Target ID cannot be empty.", nameof(targetId));
			}

			Initialize(direction, new FlowRuleObject(targetType, targetId), outcome, message);
		}

		internal FlowRule(FlowRule flowRule)
		{
			if (flowRule == null)
			{
				throw new ArgumentNullException(nameof(flowRule));
			}

			Initialize(flowRule.Direction, flowRule.Target, flowRule.Outcome, flowRule.Message);
		}

		internal FlowRule(StorageResourceStudio.FlowruleSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			ParseSection();
			InitTracking();
		}

		/// <summary>
		/// Gets the direction the rule applies to.
		/// </summary>
		public FlowRuleDirection Direction { get; private set; }

		/// <summary>
		/// Gets the type of the object this rule applies to, or <see langword="null"/> when this is the default rule of its direction.
		/// </summary>
		public FlowRuleObjectType? TargetType => Target?.Type;

		/// <summary>
		/// Gets the unique identifier of the object this rule applies to, or <see langword="null"/> when this is the default rule of its direction.
		/// </summary>
		public Guid? TargetId => Target?.Id;

		/// <summary>
		/// Gets or sets the outcome of the rule.
		/// </summary>
		/// <exception cref="ArgumentException">Thrown when the value is not a defined <see cref="FlowRuleOutcome"/>.</exception>
		public FlowRuleOutcome Outcome
		{
			get => outcome;

			set
			{
				if (!Enum.IsDefined(typeof(FlowRuleOutcome), value))
				{
					throw new ArgumentException($"Value '{value}' is not a valid flow rule outcome.", nameof(value));
				}

				outcome = value;
			}
		}

		/// <summary>
		/// Gets or sets the optional message that explains the outcome.
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// Gets a value indicating whether this rule is the default rule of its direction.
		/// </summary>
		public bool IsDefault => !Target.HasValue;

		internal FlowRuleObject? Target { get; private set; }

		internal StorageResourceStudio.FlowruleSection OriginalSection => originalSection;

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + Direction.GetHashCode();
				hash = (hash * 23) + Target.GetHashCode();
				hash = (hash * 23) + Outcome.GetHashCode();
				hash = (hash * 23) + (Message != null ? Message.GetHashCode() : 0);
				return hash;
			}
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current <see cref="FlowRule"/> instance.
		/// </summary>
		/// <param name="obj">The object to compare with the current instance.</param>
		/// <returns><see langword="true"/> if the specified object is a <see cref="FlowRule"/> with the same direction, target, outcome and message; otherwise, <see langword="false"/>.</returns>
		public override bool Equals(object obj)
		{
			if (obj is not FlowRule other)
			{
				return false;
			}

			return Direction == other.Direction
				&& Target == other.Target
				&& Outcome == other.Outcome
				&& Message == other.Message;
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
			if (updatedSection == null)
			{
				updatedSection = IsNew ? new StorageResourceStudio.FlowruleSection() : originalSection.Clone();
			}

			updatedSection.Direction = Direction.MapEnum<FlowRuleDirection, StorageResourceStudio.SlcResource_StudioIds.Enums.Flowruledirection>();
			updatedSection.TargetId = TargetId;
			updatedSection.Targettype = Target.HasValue ? FlowRuleObject.ToStorage(Target.Value.Type) : null;
			updatedSection.Outcome = Outcome.MapEnum<FlowRuleOutcome, StorageResourceStudio.SlcResource_StudioIds.Enums.Flowruleoutcome>();
			updatedSection.Message = String.IsNullOrEmpty(Message) ? null : Message;

			return updatedSection;
		}

		private void Initialize(FlowRuleDirection direction, FlowRuleObject? target, FlowRuleOutcome outcome, string message)
		{
			if (!Enum.IsDefined(typeof(FlowRuleDirection), direction))
			{
				throw new ArgumentException($"Value '{direction}' is not a valid flow rule direction.", nameof(direction));
			}

			Direction = direction;
			Target = target;
			Outcome = outcome;
			Message = message;

			IsNew = true;
		}

		private void ParseSection()
		{
			Direction = originalSection.Direction.HasValue
				? originalSection.Direction.Value.MapEnum<StorageResourceStudio.SlcResource_StudioIds.Enums.Flowruledirection, FlowRuleDirection>()
				: FlowRuleDirection.Before;
			Target = originalSection.TargetId.HasValue
				? new FlowRuleObject(FlowRuleObject.FromStorage(originalSection.Targettype), originalSection.TargetId.Value)
				: (FlowRuleObject?)null;
			outcome = originalSection.Outcome.HasValue
				? originalSection.Outcome.Value.MapEnum<StorageResourceStudio.SlcResource_StudioIds.Enums.Flowruleoutcome, FlowRuleOutcome>()
				: FlowRuleOutcome.Allowed;
			Message = originalSection.Message;
		}
	}
}
