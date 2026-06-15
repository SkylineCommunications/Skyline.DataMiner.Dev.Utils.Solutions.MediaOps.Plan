namespace Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Connection
{
	using System;
	using System.Linq;
	using System.Reflection;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Async;
	using Skyline.DataMiner.Net.Messages;

	internal sealed class AsyncMessageHandlerMock : IAsyncMessageHandler
	{
		private readonly IConnection _connection;

		public AsyncMessageHandlerMock(IConnection connection)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		public int UnclaimedAsyncResponsesCount { get; }

		public int TrackedActiveAsyncRequestCount { get; }

		public IConnection Connection { get; }

		public AsyncProgress Launch(params DMSMessage[] messages)
		{
			AsyncProgress progress = AsyncProgressBuilder.CreateInstance(this, messages, 0, null, null, 250);

			AsyncResponseEvent response = new AsyncResponseEvent
			{
				Messages = _connection.HandleMessages(messages),
			};

			MethodInfo dynMethod = GetType().GetMethod("SetResponse", BindingFlags.NonPublic | BindingFlags.Instance);
			dynMethod.Invoke(this, new object[] { response });

			return progress;
		}

		public AsyncProgress Launch(DMSMessage message, AsyncResponseEventHandler onCompleteHandler = null, AsyncProgressEventHandler onProgressHandler = null, int pageSize = 250)
		{
			AsyncProgress progress = AsyncProgressBuilder.CreateInstance(this, new[] { message }, 0, onCompleteHandler, onProgressHandler, pageSize);

			AsyncResponseEvent response = new AsyncResponseEvent
			{
				Messages = _connection.HandleMessage(message),
			};

			MethodInfo dynMethod = progress.GetType().GetMethod("SetResponse", BindingFlags.NonPublic | BindingFlags.Instance);
			dynMethod.Invoke(progress, new object[] { response });

			return progress;
		}

		public AsyncProgress Launch(DMSMessage[] messages, AsyncResponseEventHandler onCompleteHandler = null, AsyncProgressEventHandler onProgressHandler = null, int pageSize = 250)
		{
			throw new NotImplementedException();
		}

		public AsyncProgress Launch(DMSMessage[] messages, AsyncResponseEventHandler onCompleteHandler, AsyncProgressEventHandler onProgressHandler, int compatClientCookie, int pageSize)
		{
			throw new NotImplementedException();
		}

		public void Launch(AsyncProgress progress)
		{
			throw new NotImplementedException();
		}

		public AsyncProgress FindRequestInfoByCompatClientCookie(int compatClientCookie)
		{
			throw new NotImplementedException();
		}

		public void HandleAsyncResponseEvent(AsyncResponseEvent responseEvent)
		{
			throw new NotImplementedException();
		}

		public void HandleAsyncProgressEvent(AsyncProgressEvent progressEvent)
		{
			throw new NotImplementedException();
		}

		public void ClearExpiredAsyncRequestResponses()
		{
			// No logic
		}

		public void Remove(AsyncProgress progress)
		{
			// No logic
		}

		public AsyncProgress CreateProgressHandle(DMSMessage[] messages, AsyncResponseEventHandler onCompleteHandler, AsyncProgressEventHandler onProgressHandler, int compatClientCookie = -1, int pageSize = 250)
		{
			throw new NotImplementedException();
		}
	}
}
