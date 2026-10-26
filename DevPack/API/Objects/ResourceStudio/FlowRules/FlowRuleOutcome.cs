namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Specifies the outcome of a <see cref="FlowRule"/> or of the evaluation of a connection.
	/// </summary>
	/// <remarks>The values are ordered by restrictiveness, so the most restrictive outcome has the highest value.</remarks>
	public enum FlowRuleOutcome
	{
		/// <summary>
		/// The connection is allowed.
		/// </summary>
		Allowed = 0,

		/// <summary>
		/// The connection is allowed, but should be flagged with a warning.
		/// </summary>
		Warning = 1,

		/// <summary>
		/// The connection is not allowed.
		/// </summary>
		Forbidden = 2,
	}
}
