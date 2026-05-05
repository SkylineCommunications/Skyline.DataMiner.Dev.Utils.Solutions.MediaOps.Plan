namespace RT_MediaOps.Plan.RegressionTests
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Logging;

	internal class ConsolePlanLogger : ILogger
	{
		private static readonly IReadOnlyDictionary<LogLevel, string> LogLevelAbbreviations = new Dictionary<LogLevel, string>
		{
			{ LogLevel.Debug, "DBG" },
			{ LogLevel.Information, "INF" },
			{ LogLevel.Warning, "WRN" },
			{ LogLevel.Error, "ERR" },
		};

		public enum LogLevel
		{
			Debug = 0,
			Information = 1,
			Warning = 2,
			Error = 3,
		}

		public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

		public void Debug(string className, string methodName, string message)
		{
			Log(LogLevel.Debug, className, methodName, message);
		}

		public void Debug(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "")
		{
			Debug(callerInstance.GetType().Name, SafeFormat(message, args), methodName);
		}

		public void Debug(string message)
		{
			Log(LogLevel.Debug, message);
		}

		public void Error(string className, string methodName, string message)
		{
			Log(LogLevel.Error, className, methodName, message);
		}

		public void Error(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "")
		{
			Error(callerInstance.GetType().Name, methodName, SafeFormat(message, args));
		}

		public void Error(string message)
		{
			Log(LogLevel.Error, message);
		}

		public void Information(string className, string methodName, string message)
		{
			Log(LogLevel.Information, className, methodName, message);
		}

		public void Information(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "")
		{
			Information(callerInstance.GetType().Name, SafeFormat(message, args), methodName);
		}

		public void Information(string message)
		{
			Log(LogLevel.Information, message);
		}

		public void Warning(string className, string methodName, string message)
		{
			Log(LogLevel.Warning, className, methodName, message);
		}

		public void Warning(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "")
		{
			Warning(callerInstance.GetType().Name, SafeFormat(message, args), methodName);
		}

		public void Warning(string message)
		{
			Log(LogLevel.Warning, message);
		}

		private static void Log(LogLevel logLevel, string message)
		{
			Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{Thread.CurrentThread.ManagedThreadId}] [{LogLevelAbbreviations[logLevel]}] {message}");
		}

		private static string SafeFormat(string message, params object[] args)
		{
			if (message == null)
			{
				return "<null message>";
			}

			if (args == null || args.Length == 0)
			{
				return message;
			}

			try
			{
				return String.Format(message, args);
			}
			catch (FormatException)
			{
				// Fallback: include info about bad format but never throw
				return message + " | [Format error] Args: " + String.Join(", ", args);
			}
		}

		private void Log(LogLevel logLevel, string className, string methodName, string message)
		{
			if (logLevel < MinimumLogLevel)
			{
				return;
			}

			Log(logLevel, $"{className}|{methodName}|{message}");
		}
	}
}