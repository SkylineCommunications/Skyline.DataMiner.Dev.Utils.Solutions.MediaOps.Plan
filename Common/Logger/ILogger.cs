namespace Skyline.DataMiner.MediaOps.Plan.Logger
{
    using System;
    using System.Runtime.CompilerServices;

	/// <summary>
	/// Defines methods for logging messages at various log levels.
	/// </summary>
	    public interface ILogger : IDisposable
	    {
		    /// <summary>
		    /// Gets or sets the minimum log level for messages to be logged.
		    /// </summary>
		    LogLevel MinimumLogLevel { get; set; }

		    /// <summary>
		    /// Logs a debug message.
		    /// </summary>
		    /// <param name="callerInstance">The instance of the calling object.</param>
		    /// <param name="message">The debug message to log.</param>
		    /// <param name="methodName">The name of the calling method. Automatically provided by the compiler.</param>
		    void Debug(object callerInstance, string message, [CallerMemberName] string methodName = "");

		    /// <summary>
		    /// Logs a debug message.
		    /// </summary>
		    /// <param name="className">The name of the class logging the message.</param>
		    /// <param name="methodName">The name of the method logging the message.</param>
		    /// <param name="message">The debug message to log.</param>
		    void Debug(string className, string methodName, string message);

		    /// <summary>
		    /// Logs an error message with an exception.
		    /// </summary>
		    /// <param name="exception">The exception to log.</param>
		    /// <param name="message">The error message to log.</param>
		    void Error(Exception exception, string message);

		    /// <summary>
		    /// Logs an error message.
		    /// </summary>
		    /// <param name="callerInstance">The instance of the calling object.</param>
		    /// <param name="message">The error message to log.</param>
		    /// <param name="methodName">The name of the calling method. Automatically provided by the compiler.</param>
		    void Error(object callerInstance, string message, [CallerMemberName] string methodName = "");

		    /// <summary>
		    /// Logs an error message with an exception.
		    /// </summary>
		    /// <param name="className">The name of the class logging the message.</param>
		    /// <param name="methodName">The name of the method logging the message.</param>
		    /// <param name="exception">The exception to log.</param>
		    /// <param name="message">The error message to log.</param>
		    void Error(string className, string methodName, Exception exception, string message);

		    /// <summary>
		    /// Logs an error message.
		    /// </summary>
		    /// <param name="className">The name of the class logging the message.</param>
		    /// <param name="methodName">The name of the method logging the message.</param>
		    /// <param name="message">The error message to log.</param>
		    void Error(string className, string methodName, string message);

		    /// <summary>
		    /// Logs an informational message.
		    /// </summary>
		    /// <param name="callerInstance">The instance of the calling object.</param>
		    /// <param name="message">The informational message to log.</param>
		    /// <param name="methodName">The name of the calling method. Automatically provided by the compiler.</param>
		    void Information(object callerInstance, string message, [CallerMemberName] string methodName = "");

		    /// <summary>
		    /// Logs an informational message.
		    /// </summary>
		    /// <param name="className">The name of the class logging the message.</param>
		    /// <param name="methodName">The name of the method logging the message.</param>
		    /// <param name="message">The informational message to log.</param>
		    void Information(string className, string methodName, string message);

		    /// <summary>
		    /// Logs a warning message with an exception.
		    /// </summary>
		    /// <param name="exception">The exception to log.</param>
		    /// <param name="message">The warning message to log.</param>
		    void Warning(Exception exception, string message);

		    /// <summary>
		    /// Logs a warning message.
		    /// </summary>
		    /// <param name="callerInstance">The instance of the calling object.</param>
		    /// <param name="message">The warning message to log.</param>
		    /// <param name="methodName">The name of the calling method. Automatically provided by the compiler.</param>
		    void Warning(object callerInstance, string message, [CallerMemberName] string methodName = "");

		    /// <summary>
		    /// Logs a warning message with an exception.
		    /// </summary>
		    /// <param name="className">The name of the class logging the message.</param>
		    /// <param name="methodName">The name of the method logging the message.</param>
		    /// <param name="exception">The exception to log.</param>
		    /// <param name="message">The warning message to log.</param>
		    void Warning(string className, string methodName, Exception exception, string message);

		    /// <summary>
		    /// Logs a warning message.
		    /// </summary>
		    /// <param name="className">The name of the class logging the message.</param>
		    /// <param name="methodName">The name of the method logging the message.</param>
		    /// <param name="message">The warning message to log.</param>
		    void Warning(string className, string methodName, string message);
	    }
}
