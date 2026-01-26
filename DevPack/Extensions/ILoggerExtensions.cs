namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions
{
    using System;
    using System.Runtime.CompilerServices;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Extension methods for <see cref="ILogger"/> to provide standardized logging
    /// with caller type and method information, and safe message formatting.
    /// </summary>
    public static class ILoggerExtensions
    {
        /// <summary>
        /// Writes a debug log message including the caller type and method name.
        /// </summary>
        /// <param name="logger">The logger instance used to write the message.</param>
        /// <param name="callerInstance">The instance of the caller, used to derive the caller type name.</param>
        /// <param name="message">The log message.</param>
        /// <param name="methodName">The calling member name (automatically supplied by the compiler).</param>
        public static void LogDebug(this ILogger logger, object callerInstance, string message, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            logger.LogDebug("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, message ?? "<null message>"]);
        }

        /// <summary>
        /// Writes a parameterized debug log message including the caller type and method name.
        /// </summary>
        /// <param name="logger">The logger instance used to write the message.</param>
        /// <param name="callerInstance">The instance of the caller, used to derive the caller type name.</param>
        /// <param name="message">The composite format string for the log message.</param>
        /// <param name="arg">A single argument to format into the message.</param>
        /// <param name="methodName">The calling member name (automatically supplied by the compiler).</param>
        public static void LogDebug(this ILogger logger, object callerInstance, string message, object arg, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, arg);
            logger.LogDebug("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        /// <summary>
        /// Writes a parameterized debug log message including the caller type and method name.
        /// </summary>
        /// <param name="logger">The logger instance used to write the message.</param>
        /// <param name="callerInstance">The instance of the caller, used to derive the caller type name.</param>
        /// <param name="message">The composite format string for the log message.</param>
        /// <param name="args">An array of arguments to format into the message.</param>
        /// <param name="methodName">The calling member name (automatically supplied by the compiler).</param>
        public static void LogDebug(this ILogger logger, object callerInstance, string message, object[] args, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, args ?? Array.Empty<object>());
            logger.LogDebug("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        /// <summary>
        /// Writes an error log message including the caller type and method name.
        /// </summary>
        /// <param name="logger">The logger instance used to write the message.</param>
        /// <param name="callerInstance">The instance of the caller, used to derive the caller type name.</param>
        /// <param name="message">The log message.</param>
        /// <param name="methodName">The calling member name (automatically supplied by the compiler).</param>
        public static void LogError(this ILogger logger, object callerInstance, string message, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            logger.LogError("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, message ?? "<null message>"]);
        }

        /// <summary>
        /// Writes a parameterized error log message including the caller type and method name.
        /// </summary>
        /// <param name="logger">The logger instance used to write the message.</param>
        /// <param name="callerInstance">The instance of the caller, used to derive the caller type name.</param>
        /// <param name="message">The composite format string for the log message.</param>
        /// <param name="arg">A single argument to format into the message.</param>
        /// <param name="methodName">The calling member name (automatically supplied by the compiler).</param>
        public static void LogError(this ILogger logger, object callerInstance, string message, object arg, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, arg);
            logger.LogError("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        /// <summary>
        /// Writes a parameterized error log message including the caller type and method name.
        /// </summary>
        /// <param name="logger">The logger instance used to write the message.</param>
        /// <param name="callerInstance">The instance of the caller, used to derive the caller type name.</param>
        /// <param name="message">The composite format string for the log message.</param>
        /// <param name="args">An array of arguments to format into the message.</param>
        /// <param name="methodName">The calling member name (automatically supplied by the compiler).</param>
        public static void LogError(this ILogger logger, object callerInstance, string message, object[] args, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, args ?? Array.Empty<object>());
            logger.LogError("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        /// <summary>
        /// Writes an informational log message including the caller type and method name.
        /// </summary>
        /// <param name="logger">The logger instance used to write the message.</param>
        /// <param name="callerInstance">The instance of the caller, used to derive the caller type name.</param>
        /// <param name="message">The log message.</param>
        /// <param name="methodName">The calling member name (automatically supplied by the compiler).</param>
        public static void LogInformation(this ILogger logger, object callerInstance, string message, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            logger.LogInformation("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, message ?? "<null message>"]);
        }

        /// <summary>
        /// Writes a parameterized informational log message including the caller type and method name.
        /// </summary>
        /// <param name="logger">The logger instance used to write the message.</param>
        /// <param name="callerInstance">The instance of the caller, used to derive the caller type name.</param>
        /// <param name="message">The composite format string for the log message.</param>
        /// <param name="arg">A single argument to format into the message.</param>
        /// <param name="methodName">The calling member name (automatically supplied by the compiler).</param>
        public static void LogInformation(this ILogger logger, object callerInstance, string message, object arg, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, arg);
            logger.LogInformation("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        /// <summary>
        /// Writes a parameterized informational log message including the caller type and method name.
        /// </summary>
        /// <param name="logger">The logger instance used to write the message.</param>
        /// <param name="callerInstance">The instance of the caller, used to derive the caller type name.</param>
        /// <param name="message">The composite format string for the log message.</param>
        /// <param name="args">An array of arguments to format into the message.</param>
        /// <param name="methodName">The calling member name (automatically supplied by the compiler).</param>
        public static void LogInformation(this ILogger logger, object callerInstance, string message, object[] args, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, args ?? Array.Empty<object>());
            logger.LogInformation("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        /// <summary>
        /// Writes a trace log message including the caller type and method name.
        /// </summary>
        /// <param name="logger">The logger instance used to write the message.</param>
        /// <param name="callerInstance">The instance of the caller, used to derive the caller type name.</param>
        /// <param name="message">The log message.</param>
        /// <param name="methodName">The calling member name (automatically supplied by the compiler).</param>
        public static void LogTrace(this ILogger logger, object callerInstance, string message, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            logger.LogTrace("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, message ?? "<null message>"]);
        }

        /// <summary>
        /// Writes a parameterized trace log message including the caller type and method name.
        /// </summary>
        /// <param name="logger">The logger instance used to write the message.</param>
        /// <param name="callerInstance">The instance of the caller, used to derive the caller type name.</param>
        /// <param name="message">The composite format string for the log message.</param>
        /// <param name="arg">A single argument to format into the message.</param>
        /// <param name="methodName">The calling member name (automatically supplied by the compiler).</param>
        public static void LogTrace(this ILogger logger, object callerInstance, string message, object arg, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, arg);
            logger.LogTrace("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        /// <summary>
        /// Writes a parameterized trace log message including the caller type and method name.
        /// </summary>
        /// <param name="logger">The logger instance used to write the message.</param>
        /// <param name="callerInstance">The instance of the caller, used to derive the caller type name.</param>
        /// <param name="message">The composite format string for the log message.</param>
        /// <param name="args">An array of arguments to format into the message.</param>
        /// <param name="methodName">The calling member name (automatically supplied by the compiler).</param>
        public static void LogTrace(this ILogger logger, object callerInstance, string message, object[] args, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, args ?? Array.Empty<object>());
            logger.LogTrace("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        /// <summary>
        /// Writes a warning log message including the caller type and method name.
        /// </summary>
        /// <param name="logger">The logger instance used to write the message.</param>
        /// <param name="callerInstance">The instance of the caller, used to derive the caller type name.</param>
        /// <param name="message">The log message.</param>
        /// <param name="methodName">The calling member name (automatically supplied by the compiler).</param>
        public static void LogWarning(this ILogger logger, object callerInstance, string message, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            logger.LogWarning("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, message ?? "<null message>"]);
        }

        /// <summary>
        /// Writes a parameterized warning log message including the caller type and method name.
        /// </summary>
        /// <param name="logger">The logger instance used to write the message.</param>
        /// <param name="callerInstance">The instance of the caller, used to derive the caller type name.</param>
        /// <param name="message">The composite format string for the log message.</param>
        /// <param name="arg">A single argument to format into the message.</param>
        /// <param name="methodName">The calling member name (automatically supplied by the compiler).</param>
        public static void LogWarning(this ILogger logger, object callerInstance, string message, object arg, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, arg);
            logger.LogWarning("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        /// <summary>
        /// Writes a parameterized warning log message including the caller type and method name.
        /// </summary>
        /// <param name="logger">The logger instance used to write the message.</param>
        /// <param name="callerInstance">The instance of the caller, used to derive the caller type name.</param>
        /// <param name="message">The composite format string for the log message.</param>
        /// <param name="args">An array of arguments to format into the message.</param>
        /// <param name="methodName">The calling member name (automatically supplied by the compiler).</param>
        public static void LogWarning(this ILogger logger, object callerInstance, string message, object[] args, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, args ?? Array.Empty<object>());
            logger.LogWarning("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        /// <summary>
        /// Safely formats a message using the provided arguments, preventing format exceptions
        /// from propagating and returning a fallback message when formatting fails.
        /// </summary>
        /// <param name="message">The composite format string.</param>
        /// <param name="args">The arguments to format into the message.</param>
        /// <returns>
        /// The formatted message, the original message when there are no arguments,
        /// a placeholder when the message is <c>null</c>, or a message annotated with
        /// format error details if formatting fails.
        /// </returns>
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
    }
}
