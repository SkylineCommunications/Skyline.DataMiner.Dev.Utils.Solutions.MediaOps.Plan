namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Logging
{
	using System.Runtime.CompilerServices;

	/// <summary>
	/// Defines logging methods for debug, information, warning, and error messages.
	/// </summary>
	public interface ILogger
	{
		/// <summary>
		/// Logs a debug-level message including contextual information about the caller.
		/// </summary>
		/// <param name="callerInstance">The instance of the object that triggers the log entry.</param>
		/// <param name="message">The message template to log.</param>
		/// <param name="args">Optional arguments to format into the message template.</param>
		/// <param name="methodName">The name of the calling method (automatically provided by the compiler).</param>
		void Debug(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "");

		/// <summary>
		/// Logs a debug-level message without additional caller context.
		/// </summary>
		/// <param name="message">The message to log.</param>
		void Debug(string message);

		/// <summary>
		/// Logs an error-level message including contextual information about the caller.
		/// </summary>
		/// <param name="callerInstance">The instance of the object that triggers the log entry.</param>
		/// <param name="message">The message template to log.</param>
		/// <param name="args">Optional arguments to format into the message template.</param>
		/// <param name="methodName">The name of the calling method (automatically provided by the compiler).</param>
		void Error(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "");

		/// <summary>
		/// Logs an error-level message without additional caller context.
		/// </summary>
		/// <param name="message">The message to log.</param>
		void Error(string message);

		/// <summary>
		/// Logs an information-level message including contextual information about the caller.
		/// </summary>
		/// <param name="callerInstance">The instance of the object that triggers the log entry.</param>
		/// <param name="message">The message template to log.</param>
		/// <param name="args">Optional arguments to format into the message template.</param>
		/// <param name="methodName">The name of the calling method (automatically provided by the compiler).</param>
		void Information(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "");

		/// <summary>
		/// Logs an information-level message without additional caller context.
		/// </summary>
		/// <param name="message">The message to log.</param>
		void Information(string message);

		/// <summary>
		/// Logs a warning-level message including contextual information about the caller.
		/// </summary>
		/// <param name="callerInstance">The instance of the object that triggers the log entry.</param>
		/// <param name="message">The message template to log.</param>
		/// <param name="args">Optional arguments to format into the message template.</param>
		/// <param name="methodName">The name of the calling method (automatically provided by the compiler).</param>
		void Warning(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "");

		/// <summary>
		/// Logs a warning-level message without additional caller context.
		/// </summary>
		/// <param name="message">The message to log.</param>
		void Warning(string message);
	}
}
