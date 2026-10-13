namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Logging;

	/// <summary>
	/// Passes every information and warning message to a callback. The simulation has no SRM engine, so a test uses the
	/// log message of a step to change the reservation at that exact moment, as SRM could do on a real agent.
	/// </summary>
	internal sealed class CallbackLogger : ILogger
	{
		private readonly Action<string> onMessage;

		public CallbackLogger(Action<string> onMessage)
		{
			this.onMessage = onMessage ?? throw new ArgumentNullException(nameof(onMessage));
		}

		public void Debug(object callerInstance, string message, object[] args = null, string methodName = "")
		{
		}

		public void Debug(string message)
		{
		}

		public void Error(object callerInstance, string message, object[] args = null, string methodName = "")
		{
		}

		public void Error(string message)
		{
		}

		public void Information(object callerInstance, string message, object[] args = null, string methodName = "")
		{
			onMessage(message);
		}

		public void Information(string message)
		{
			onMessage(message);
		}

		public void Warning(object callerInstance, string message, object[] args = null, string methodName = "")
		{
			onMessage(message);
		}

		public void Warning(string message)
		{
			onMessage(message);
		}
	}
}
