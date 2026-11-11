namespace RT_MediaOps.Plan.Storage.DOM
{
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.Async;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.SubscriptionFilters;
	using Skyline.DataMiner.Utils.DOM.UnitTesting;

	// An isolated SLNet test double. It never constructs the regression-test
	// context, reads credentials, or opens a connection to a DataMiner Agent.
	internal sealed class InMemoryDomSchemaStore
	{
		internal DomSLNetMessageHandler Handler { get; } = new DomSLNetMessageHandler(validateAgainstDefinition: true);

		internal List<ModuleSettings> ModuleSettings { get; } = new List<ModuleSettings>();

		internal List<DMSMessage> Messages { get; } = new List<DMSMessage>();

		internal Func<DMSMessage, DMSMessage?>? OverrideResponse { get; set; }

		internal IConnection CreateConnection() => new SchemaStoreConnection(this);

		internal DMSMessage[] HandleMessages(DMSMessage[] messages)
		{
			return messages.Select(HandleMessage).ToArray();
		}

		private DMSMessage HandleMessage(DMSMessage message)
		{
			Messages.Add(message);
			var overridden = OverrideResponse?.Invoke(message);
			if (overridden != null)
			{
				return overridden;
			}

			switch (message)
			{
				case ManagerStoreReadRequest<ModuleSettings> request:
					return new ManagerStoreCrudResponse<ModuleSettings>(request.Query.ExecuteInMemory(ModuleSettings).ToList());
				case ManagerStoreCreateRequest<ModuleSettings> request:
					ModuleSettings.Add(request.Object);
					return new ManagerStoreCrudResponse<ModuleSettings>(request.Object);
				case ManagerStoreUpdateRequest<ModuleSettings> request:
					ModuleSettings.RemoveAll(x => x.ModuleId == request.Object.ModuleId);
					ModuleSettings.Add(request.Object);
					return new ManagerStoreCrudResponse<ModuleSettings>(request.Object);
				case ManagerStoreReinitializeCustomManagerRequest:
					return new ManagerStoreReinitializeCustomManagerResponse { TraceData = new TraceData() };
				default:
					return Handler.HandleMessage(message);
			}
		}

		// Only synchronous message handling is supported. Any accidental use of another
		// connection capability fails locally rather than reaching an integration fixture.
		private sealed class SchemaStoreConnection : IConnection
		{
			private readonly InMemoryDomSchemaStore store;

			internal SchemaStoreConnection(InMemoryDomSchemaStore store)
			{
				this.store = store;
			}

			public string UserDomainName => "Schema unit test";
			public Guid ConnectionID => Guid.Empty;
			public bool IsShuttingDown => false;
			public bool IsReceiving => true;
			public IAsyncMessageHandler Async => throw new NotSupportedException();
			public ServerDetails ServerDetails => throw new NotSupportedException();

			public event ConnectionClosedHandler OnClose { add { } remove { } }
			public event NewMessageEventHandler OnNewMessage { add { } remove { } }
			public event AbnormalCloseEventHandler OnAbnormalClose { add { } remove { } }
			public event EventsDroppedEventHandler OnEventsDropped { add { } remove { } }
			public event SubscriptionCompleteEventHandler OnSubscriptionComplete { add { } remove { } }
			public event AuthenticationChallengeEventHandler OnAuthenticationChallenge { add { } remove { } }
			public event EventHandler<SubscriptionStateEventArgs> OnSubscriptionState { add { } remove { } }

			public DMSMessage[] HandleMessage(DMSMessage message) => HandleMessages(new[] { message });
			public DMSMessage[] HandleMessages(DMSMessage[] messages) => store.HandleMessages(messages);
			public DMSMessage HandleSingleResponseMessage(DMSMessage message) => HandleMessage(message).Single();
			public DMSMessage[] UnPack(DMSMessage[] messages) => messages;
			public void Dispose() { }

			public CreateSubscriptionResponseMessage Subscribe(params SubscriptionFilter[] filters) => throw new NotSupportedException();
			public void Unsubscribe() => throw new NotSupportedException();
			public void AddSubscription(string setID, params SubscriptionFilter[] filters) => throw new NotSupportedException();
			public void RemoveSubscription(string setID, params SubscriptionFilter[] filters) => throw new NotSupportedException();
			public void ReplaceSubscription(string setID, params SubscriptionFilter[] filters) => throw new NotSupportedException();
			public void ClearSubscriptions(string setID) => throw new NotSupportedException();
			public ITrackedSubscriptionUpdate TrackSubscribe(params SubscriptionFilter[] filters) => throw new NotSupportedException();
			public ITrackedSubscriptionUpdate TrackAddSubscription(string setID, params SubscriptionFilter[] filters) => throw new NotSupportedException();
			public ITrackedSubscriptionUpdate TrackRemoveSubscription(string setID, params SubscriptionFilter[] filters) => throw new NotSupportedException();
			public ITrackedSubscriptionUpdate TrackReplaceSubscription(string setID, params SubscriptionFilter[] filters) => throw new NotSupportedException();
			public ITrackedSubscriptionUpdate TrackClearSubscriptions(string setID) => throw new NotSupportedException();
			public ITrackedSubscriptionUpdate TrackUpdateSubscription(UpdateSubscriptionMultiMessage multi) => throw new NotSupportedException();
			public bool SupportsFeature(CompatibilityFlags flags) => throw new NotSupportedException();
			public bool SupportsFeature(string name) => throw new NotSupportedException();
			public GetElementProtocolResponseMessage GetElementProtocol(int dmaId, int elementId) => throw new NotSupportedException();
			public GetProtocolInfoResponseMessage GetProtocol(string name, string version) => throw new NotSupportedException();
			public void FireOnAsyncResponse(AsyncResponseEvent responseEvent, ref bool handled) => throw new NotSupportedException();
			public void SafeWait(int timeout) => throw new NotSupportedException();
		}
	}
}
