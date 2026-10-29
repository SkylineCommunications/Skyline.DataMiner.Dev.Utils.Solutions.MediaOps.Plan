namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents the result of evaluating the flow rules for a connection in a node graph.
	/// </summary>
	public sealed class FlowRuleConnectionEvaluation : FlowRuleEvaluation
	{
		internal FlowRuleConnectionEvaluation(string connectionId, string fromNodeId, string toNodeId, FlowRuleEvaluation evaluation)
			: base(
				  evaluation?.From ?? throw new ArgumentNullException(nameof(evaluation)),
				  evaluation.To,
				  evaluation.FromRule,
				  evaluation.ToRule)
		{
			ConnectionId = connectionId;
			FromNodeId = fromNodeId;
			ToNodeId = toNodeId;
		}

		/// <summary>
		/// Gets the unique identifier of the connection.
		/// </summary>
		public string ConnectionId { get; }

		/// <summary>
		/// Gets the unique identifier of the node the connection starts from.
		/// </summary>
		public string FromNodeId { get; }

		/// <summary>
		/// Gets the unique identifier of the node the connection goes to.
		/// </summary>
		public string ToNodeId { get; }
	}
}
