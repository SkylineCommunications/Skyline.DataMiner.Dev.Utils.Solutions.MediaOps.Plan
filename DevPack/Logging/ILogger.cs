namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Logging
{
    using System.Runtime.CompilerServices;

    public interface ILogger
    {
        void LogDebug(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "");

        void LogDebug(string message);

        void LogError(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "");

        void LogError(string message);

        void LogInformation(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "");

        void LogInformation(string message);

        void LogWarning(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "");

        void LogWarning(string message);
    }
}
