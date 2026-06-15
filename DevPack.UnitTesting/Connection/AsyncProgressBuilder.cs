namespace Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Connection
{
	using System;
	using System.Reflection;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Async;
	using Skyline.DataMiner.Net.Messages;

	internal static class AsyncProgressBuilder
	{
		public static AsyncProgress CreateInstance(
			IAsyncMessageHandler parent,
			DMSMessage[] messages,
			int compatClientCookie,
			AsyncResponseEventHandler onCompleteHandler,
			AsyncProgressEventHandler onProgressHandler,
			int pageSize)
		{
			var type = typeof(AsyncProgress);

			// Find the internal constructor
			var ctor = type.GetConstructor(
				BindingFlags.Instance | BindingFlags.NonPublic,
				null,
				new Type[]
				{
					typeof(IAsyncMessageHandler),
					typeof(DMSMessage[]),
					typeof(int),
					typeof(AsyncResponseEventHandler),
					typeof(AsyncProgressEventHandler),
					typeof(int),
				},
				null);

			if (ctor == null)
			{
				throw new InvalidOperationException("Could not find the internal constructor for AsyncProgress.");
			}

			// Invoke the constructor
			var instance = (AsyncProgress)ctor.Invoke(new object[]
			{
				parent,
				messages,
				compatClientCookie,
				onCompleteHandler,
				onProgressHandler,
				pageSize,
			});

			return instance;
		}
	}
}
