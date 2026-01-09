namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions
{
    using System.Runtime.CompilerServices;
    using Microsoft.Extensions.Logging;

    internal static class ILoggerExtensions
    {
        public static void LogTrace(this ILogger logger, object callerInstance, string message, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            logger.LogTrace("{0}.{1}|{2}", callerInstance.GetType().Name, methodName, message);
        }

        public static void LogDebug(this ILogger logger, object callerInstance, string message, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            logger.LogDebug("{0}.{1}|{2}", callerInstance.GetType().Name, methodName, message);
        }

        public static void LogInformation(this ILogger logger, object callerInstance, string message, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            logger.LogInformation("{0}.{1}|{2}", callerInstance.GetType().Name, methodName, message);
        }

        public static void LogError(this ILogger logger, object callerInstance, string message, [CallerMemberName] string methodName = "")
        {
            if (logger == null)
                return;

            logger.LogError("{0}.{1}|{2}", callerInstance.GetType().Name, methodName, message);
        }
    }
}
