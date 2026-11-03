namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Represents an error that occurs when validating or persisting the flow rules of an object.
	/// </summary>
	/// <seealso cref="FlowRuleCollectionInvalidRuleError"/>
	/// <seealso cref="FlowRuleCollectionNotFoundError"/>
	/// <seealso cref="FlowRuleCollectionValueAlreadyChangedError"/>
	public class FlowRuleCollectionError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier of the flow rules.
		/// </summary>
		public Guid Id { get; internal set; }

		/// <summary>
		/// Gets the type of the object that owns the flow rules.
		/// </summary>
		public FlowRuleObjectType OwnerType { get; internal set; }

		/// <summary>
		/// Gets the unique identifier of the object that owns the flow rules.
		/// </summary>
		public Guid OwnerId { get; internal set; }
	}
}
