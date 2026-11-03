namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Specifies the direction of a <see cref="FlowRule"/> relative to the object that owns it.
	/// </summary>
	public enum FlowRuleDirection
	{
		/// <summary>
		/// The rule applies to what is connected before the owning object (other node → owning node).
		/// </summary>
		Before = 0,

		/// <summary>
		/// The rule applies to what is connected after the owning object (owning node → other node).
		/// </summary>
		After = 1,
	}
}
