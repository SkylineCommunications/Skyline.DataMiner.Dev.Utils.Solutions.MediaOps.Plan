namespace RT_MediaOps.Plan.RegressionTests
{
	using System;

	using Skyline.DataMiner.Solutions.Categories.Logging;

	public class ConsoleCategoriesLogger : ILogger
	{
		public void Debug(object callerInstance, string message, object[]? args = null, string methodName = "")
		{
			var formattedMessage = args != null && args.Length > 0 ? string.Format(message, args) : message;
			Console.WriteLine($"[DEBUG] {callerInstance?.GetType().Name}.{methodName}: {formattedMessage}");
		}

		public void Debug(string message)
		{
			Console.WriteLine($"[DEBUG] {message}");
		}

		public void Error(object callerInstance, string message, object[]? args = null, string methodName = "")
		{
			var formattedMessage = args != null && args.Length > 0 ? string.Format(message, args) : message;
			Console.WriteLine($"[ERROR] {callerInstance?.GetType().Name}.{methodName}: {formattedMessage}");
		}

		public void Error(string message)
		{
			Console.WriteLine($"[ERROR] {message}");
		}

		public void Information(object callerInstance, string message, object[]? args = null, string methodName = "")
		{
			var formattedMessage = args != null && args.Length > 0 ? string.Format(message, args) : message;
			Console.WriteLine($"[INFO] {callerInstance?.GetType().Name}.{methodName}: {formattedMessage}");
		}

		public void Information(string message)
		{
			Console.WriteLine($"[INFO] {message}");
		}

		public void Warning(object callerInstance, string message, object[]? args = null, string methodName = "")
		{
			var formattedMessage = args != null && args.Length > 0 ? string.Format(message, args) : message;
			Console.WriteLine($"[WARNING] {callerInstance?.GetType().Name}.{methodName}: {formattedMessage}");
		}

		public void Warning(string message)
		{
			Console.WriteLine($"[WARNING] {message}");
		}
	}
}