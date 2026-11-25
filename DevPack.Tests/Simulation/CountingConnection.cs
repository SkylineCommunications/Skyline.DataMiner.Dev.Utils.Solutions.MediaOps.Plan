namespace RT_MediaOps.Plan.Simulation
{
	using System;
	using System.Linq;
	using System.Threading;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Async;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.SubscriptionFilters;

	using Parameter = Skyline.DataMiner.Net.Profiles.Parameter;
	using WorkflowDefinitions = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Definitions;
	using ResourceStudioDefinitions = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Definitions;

	/// <summary>
	/// Wraps a connection and counts the reads of orchestration settings (configuration DOM instances) and profile
	/// parameters that pass through it, so tests can verify when these are read.
	/// </summary>
	internal sealed class CountingConnection : IConnection
	{
		private static readonly string WorkflowConfigurationDefinitionId = WorkflowDefinitions.Configuration.Id.ToString();
		private static readonly string ResourceStudioConfigurationDefinitionId = ResourceStudioDefinitions.Configuration.Id.ToString();

		private readonly IConnection inner;
		private int configurationReads;
		private int profileParameterReads;

		internal CountingConnection(IConnection inner)
		{
			this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
		}

		/// <summary>
		/// Gets the number of reads of workflow or resource studio configuration DOM instances.
		/// </summary>
		internal int ConfigurationReads => Volatile.Read(ref configurationReads);

		/// <summary>
		/// Gets the number of profile parameter reads.
		/// </summary>
		internal int ProfileParameterReads => Volatile.Read(ref profileParameterReads);

		/// <summary>
		/// Gets or sets a value indicating whether the next configuration read fails, like a read that times out.
		/// </summary>
		internal bool FailNextConfigurationRead { get; set; }

		internal void Reset()
		{
			Interlocked.Exchange(ref configurationReads, 0);
			Interlocked.Exchange(ref profileParameterReads, 0);
		}

		private void Count(DMSMessage message)
		{
			switch (message)
			{
				case ManagerStoreReadRequest<DomInstance> read when IsConfigurationFilter(read.Query?.Filter?.ToString()):
					Interlocked.Increment(ref configurationReads);
					ThrowIfConfigurationReadMustFail();
					break;

				case ManagerStoreStartPagingRequest<DomInstance> paging when IsConfigurationFilter(paging.Filter?.ToString()):
					Interlocked.Increment(ref configurationReads);
					ThrowIfConfigurationReadMustFail();
					break;

				case ManagerStoreReadRequest<Parameter> _:
				case ManagerStoreStartPagingRequest<Parameter> _:
					Interlocked.Increment(ref profileParameterReads);
					break;
			}
		}

		private void ThrowIfConfigurationReadMustFail()
		{
			if (FailNextConfigurationRead)
			{
				FailNextConfigurationRead = false;
				throw new TimeoutException("Simulated failure of a configuration read.");
			}
		}

		private static bool IsConfigurationFilter(string? filter)
		{
			return filter != null
				&& (filter.IndexOf(WorkflowConfigurationDefinitionId, StringComparison.OrdinalIgnoreCase) >= 0
					|| filter.IndexOf(ResourceStudioConfigurationDefinitionId, StringComparison.OrdinalIgnoreCase) >= 0);
		}

		#region IConnection implementation

		public string UserDomainName => inner.UserDomainName;

		public Guid ConnectionID => inner.ConnectionID;

		public bool IsShuttingDown => inner.IsShuttingDown;

		public IAsyncMessageHandler Async => inner.Async;

		public bool IsReceiving => inner.IsReceiving;

		public ServerDetails ServerDetails => inner.ServerDetails;

		public event ConnectionClosedHandler OnClose
		{
			add => inner.OnClose += value;
			remove => inner.OnClose -= value;
		}

		public event NewMessageEventHandler OnNewMessage
		{
			add => inner.OnNewMessage += value;
			remove => inner.OnNewMessage -= value;
		}

		public event AbnormalCloseEventHandler OnAbnormalClose
		{
			add => inner.OnAbnormalClose += value;
			remove => inner.OnAbnormalClose -= value;
		}

		public event EventsDroppedEventHandler OnEventsDropped
		{
			add => inner.OnEventsDropped += value;
			remove => inner.OnEventsDropped -= value;
		}

		public event SubscriptionCompleteEventHandler OnSubscriptionComplete
		{
			add => inner.OnSubscriptionComplete += value;
			remove => inner.OnSubscriptionComplete -= value;
		}

		public event AuthenticationChallengeEventHandler OnAuthenticationChallenge
		{
			add => inner.OnAuthenticationChallenge += value;
			remove => inner.OnAuthenticationChallenge -= value;
		}

		public event EventHandler<SubscriptionStateEventArgs> OnSubscriptionState
		{
			add => inner.OnSubscriptionState += value;
			remove => inner.OnSubscriptionState -= value;
		}

		public DMSMessage[] HandleMessage(DMSMessage msg)
		{
			Count(msg);
			return inner.HandleMessage(msg);
		}

		public DMSMessage[] HandleMessages(DMSMessage[] msgs)
		{
			foreach (var msg in msgs ?? Array.Empty<DMSMessage>())
			{
				Count(msg);
			}

			return inner.HandleMessages(msgs);
		}

		public DMSMessage HandleSingleResponseMessage(DMSMessage msg)
		{
			Count(msg);
			return inner.HandleSingleResponseMessage(msg);
		}

		public CreateSubscriptionResponseMessage Subscribe(params SubscriptionFilter[] filters) => inner.Subscribe(filters);

		public void Unsubscribe() => inner.Unsubscribe();

		public void AddSubscription(string setID, params SubscriptionFilter[] newFilters) => inner.AddSubscription(setID, newFilters);

		public void RemoveSubscription(string setID, params SubscriptionFilter[] deletedFilters) => inner.RemoveSubscription(setID, deletedFilters);

		public void ReplaceSubscription(string setID, params SubscriptionFilter[] newFilters) => inner.ReplaceSubscription(setID, newFilters);

		public void ClearSubscriptions(string setID) => inner.ClearSubscriptions(setID);

		public ITrackedSubscriptionUpdate TrackSubscribe(params SubscriptionFilter[] filters) => inner.TrackSubscribe(filters);

		public ITrackedSubscriptionUpdate TrackAddSubscription(string setID, params SubscriptionFilter[] newFilters) => inner.TrackAddSubscription(setID, newFilters);

		public ITrackedSubscriptionUpdate TrackRemoveSubscription(string setID, params SubscriptionFilter[] deletedFilters) => inner.TrackRemoveSubscription(setID, deletedFilters);

		public ITrackedSubscriptionUpdate TrackReplaceSubscription(string setID, params SubscriptionFilter[] newFilters) => inner.TrackReplaceSubscription(setID, newFilters);

		public ITrackedSubscriptionUpdate TrackClearSubscriptions(string setID) => inner.TrackClearSubscriptions(setID);

		public ITrackedSubscriptionUpdate TrackUpdateSubscription(UpdateSubscriptionMultiMessage multi) => inner.TrackUpdateSubscription(multi);

		public bool SupportsFeature(CompatibilityFlags flags) => inner.SupportsFeature(flags);

		public bool SupportsFeature(string name) => inner.SupportsFeature(name);

		public GetElementProtocolResponseMessage GetElementProtocol(int dmaid, int eid) => inner.GetElementProtocol(dmaid, eid);

		public GetProtocolInfoResponseMessage GetProtocol(string name, string version) => inner.GetProtocol(name, version);

		public void FireOnAsyncResponse(AsyncResponseEvent responseEvent, ref bool handled) => inner.FireOnAsyncResponse(responseEvent, ref handled);

		public DMSMessage[] UnPack(DMSMessage[] messages) => inner.UnPack(messages);

		public void SafeWait(int timeout) => inner.SafeWait(timeout);

		public void Dispose() => inner.Dispose();

		#endregion
	}
}
