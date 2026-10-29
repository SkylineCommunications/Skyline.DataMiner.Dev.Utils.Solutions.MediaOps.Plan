namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="FlowRules"/> objects.
	/// </summary>
	public static class FlowRulesExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="ApiObject.Id"/> property.
		/// </summary>
		public static readonly Exposer<FlowRules, Guid> Id = new Exposer<FlowRules, Guid>((obj) => obj.Id, "Id");

		/// <summary>
		/// Gets an exposer for the <see cref="FlowRules.OwnerType"/> property.
		/// </summary>
		public static readonly Exposer<FlowRules, FlowRuleObjectType> OwnerType = new Exposer<FlowRules, FlowRuleObjectType>((obj) => obj.OwnerType, "OwnerType");

		/// <summary>
		/// Gets an exposer for the <see cref="FlowRules.OwnerId"/> property.
		/// </summary>
		public static readonly Exposer<FlowRules, Guid> OwnerId = new Exposer<FlowRules, Guid>((obj) => obj.OwnerId, "OwnerId");

		/// <summary>
		/// Provides exposers for querying and filtering the rules of <see cref="FlowRules"/> objects.
		/// </summary>
		public static class Rules
		{
			/// <summary>
			/// Gets a dynamic list exposer for the <see cref="FlowRule.TargetId"/> of the overrides.
			/// </summary>
			public static readonly DynamicListExposer<FlowRules, Guid> TargetId = DynamicListExposer<FlowRules, Guid>.CreateFromListExposer(new Exposer<FlowRules, IEnumerable>((obj) => obj.Rules.Where(x => x != null && x.TargetId.HasValue).Select(x => x.TargetId.Value), "Rules.TargetId"));
		}
	}
}
