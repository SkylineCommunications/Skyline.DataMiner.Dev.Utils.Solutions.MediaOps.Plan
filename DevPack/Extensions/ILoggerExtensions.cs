namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions
{
    using System;
    using System.Runtime.CompilerServices;
    using Microsoft.Extensions.Logging;

    internal static class ILoggerExtensions
    {
        public static void LogDebug(this ILogger logger, object callerInstance, string message, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            logger.LogDebug("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, message ?? "<null message>"]);
        }

        public static void LogDebug(this ILogger logger, object callerInstance, string message, object arg, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, arg);
            logger.LogDebug("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        public static void LogDebug(this ILogger logger, object callerInstance, string message, object[] args, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, args ?? Array.Empty<object>());
            logger.LogDebug("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        public static void LogError(this ILogger logger, object callerInstance, string message, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            logger.LogError("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, message ?? "<null message>"]);
        }

        public static void LogError(this ILogger logger, object callerInstance, string message, object arg, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, arg);
            logger.LogError("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        public static void LogError(this ILogger logger, object callerInstance, string message, object[] args, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, args ?? Array.Empty<object>());
            logger.LogError("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        public static void LogInformation(this ILogger logger, object callerInstance, string message, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            logger.LogInformation("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, message ?? "<null message>"]);
        }

        public static void LogInformation(this ILogger logger, object callerInstance, string message, object arg, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, arg);
            logger.LogInformation("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        public static void LogInformation(this ILogger logger, object callerInstance, string message, object[] args, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, args ?? Array.Empty<object>());
            logger.LogInformation("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        public static void LogTrace(this ILogger logger, object callerInstance, string message, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            logger.LogTrace("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, message ?? "<null message>"]);
        }

        public static void LogTrace(this ILogger logger, object callerInstance, string message, object arg, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, arg);
            logger.LogTrace("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        public static void LogTrace(this ILogger logger, object callerInstance, string message, object[] args, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, args ?? Array.Empty<object>());
            logger.LogTrace("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        public static void LogWarning(this ILogger logger, object callerInstance, string message, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            logger.LogWarning("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, message ?? "<null message>"]);
        }

        public static void LogWarning(this ILogger logger, object callerInstance, string message, object arg, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, arg);
            logger.LogWarning("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
        }

        public static void LogWarning(this ILogger logger, object callerInstance, string message, object[] args, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            string formattedMessage = SafeFormat(message, args ?? Array.Empty<object>());
            logger.LogWarning("{0}.{1}|{2}", [callerInstance?.GetType().Name ?? "<null caller>", methodName, formattedMessage]);
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
    }
}
