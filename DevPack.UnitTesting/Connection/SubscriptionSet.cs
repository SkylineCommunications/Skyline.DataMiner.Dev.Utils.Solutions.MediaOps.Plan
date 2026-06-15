namespace Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Connection
{
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.ToolsSpace.Collections;

	internal sealed class SubscriptionSet
	{
		public SubscriptionSet(string setId)
		{
			SetId = setId;
		}

		public string SetId { get; }

		public ConcurrentHashSet<SubscriptionFilter> Filters { get; } = new ConcurrentHashSet<SubscriptionFilter>();
	}
}
