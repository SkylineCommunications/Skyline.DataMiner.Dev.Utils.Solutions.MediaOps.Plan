namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="FlowRuleCollection"/> objects.
	/// </summary>
	public static class FlowRuleCollectionExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="ApiObject.Id"/> property.
		/// </summary>
		public static readonly Exposer<FlowRuleCollection, Guid> Id = new Exposer<FlowRuleCollection, Guid>((obj) => obj.Id, "Id");

		/// <summary>
		/// Gets an exposer for the <see cref="FlowRuleCollection.OwnerType"/> property.
		/// </summary>
		public static readonly Exposer<FlowRuleCollection, FlowRuleObjectType> OwnerType = new Exposer<FlowRuleCollection, FlowRuleObjectType>((obj) => obj.OwnerType, "OwnerType");

		/// <summary>
		/// Gets an exposer for the <see cref="FlowRuleCollection.OwnerId"/> property.
		/// </summary>
		public static readonly Exposer<FlowRuleCollection, Guid> OwnerId = new Exposer<FlowRuleCollection, Guid>((obj) => obj.OwnerId, "OwnerId");

		/// <summary>
		/// Provides exposers for querying and filtering the rules of <see cref="FlowRuleCollection"/> objects.
		/// </summary>
		public static class Rules
		{
			/// <summary>
			/// Gets a dynamic list exposer for the <see cref="FlowRule.TargetId"/> of the overrides.
			/// </summary>
			public static readonly DynamicListExposer<FlowRuleCollection, Guid> TargetId = DynamicListExposer<FlowRuleCollection, Guid>.CreateFromListExposer(new Exposer<FlowRuleCollection, IEnumerable>((obj) => obj.Rules.Where(x => x != null && x.TargetId.HasValue).Select(x => x.TargetId.Value), "Rules.TargetId"));
		}
	}
}
