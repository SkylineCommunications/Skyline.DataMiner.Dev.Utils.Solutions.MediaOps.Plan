namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Represents the result of evaluating the flow rules for a connection from one resource pool to another.
	/// </summary>
	/// <remarks>
	/// A connection <c>From → To</c> is evaluated against the <see cref="FlowRuleDirection.After"/> rules of the
	/// <c>From</c> resource pool and the <see cref="FlowRuleDirection.Before"/> rules of the <c>To</c> resource pool. The most
	/// restrictive of both outcomes is the resulting <see cref="Outcome"/>.
	/// </remarks>
	public class FlowRuleEvaluation
	{
		internal FlowRuleEvaluation(Guid fromResourcePoolId, Guid toResourcePoolId, FlowRule fromRule, FlowRule toRule)
		{
			FromResourcePoolId = fromResourcePoolId;
			ToResourcePoolId = toResourcePoolId;
			FromRule = fromRule;
			ToRule = toRule;

			FromOutcome = fromRule?.Outcome ?? FlowRuleOutcome.Allowed;
			ToOutcome = toRule?.Outcome ?? FlowRuleOutcome.Allowed;
			Outcome = FromOutcome >= ToOutcome ? FromOutcome : ToOutcome;

			Messages = Outcome == FlowRuleOutcome.Allowed
				? Array.Empty<string>()
				: new[] { FromOutcome == Outcome ? fromRule?.Message : null, ToOutcome == Outcome ? toRule?.Message : null }
					.Where(x => !String.IsNullOrWhiteSpace(x))
					.Distinct(StringComparer.Ordinal)
					.ToArray();
		}

		/// <summary>
		/// Gets the unique identifier of the resource pool the connection starts from.
		/// </summary>
		public Guid FromResourcePoolId { get; }

		/// <summary>
		/// Gets the unique identifier of the resource pool the connection goes to.
		/// </summary>
		public Guid ToResourcePoolId { get; }

		/// <summary>
		/// Gets the resulting outcome: the most restrictive of <see cref="FromOutcome"/> and <see cref="ToOutcome"/>.
		/// </summary>
		public FlowRuleOutcome Outcome { get; }

		/// <summary>
		/// Gets the outcome of the <see cref="FlowRuleDirection.After"/> rules of the resource pool the connection starts from.
		/// </summary>
		public FlowRuleOutcome FromOutcome { get; }

		/// <summary>
		/// Gets the <see cref="FlowRuleDirection.After"/> rule of the resource pool the connection starts from that was applied, or <see langword="null"/> when no rule applies.
		/// </summary>
		public FlowRule FromRule { get; }

		/// <summary>
		/// Gets the outcome of the <see cref="FlowRuleDirection.Before"/> rules of the resource pool the connection goes to.
		/// </summary>
		public FlowRuleOutcome ToOutcome { get; }

		/// <summary>
		/// Gets the <see cref="FlowRuleDirection.Before"/> rule of the resource pool the connection goes to that was applied, or <see langword="null"/> when no rule applies.
		/// </summary>
		public FlowRule ToRule { get; }

		/// <summary>
		/// Gets the messages of the rules that determined the resulting <see cref="Outcome"/>. Empty when the outcome is <see cref="FlowRuleOutcome.Allowed"/>.
		/// </summary>
		public IReadOnlyCollection<string> Messages { get; }
	}
}
