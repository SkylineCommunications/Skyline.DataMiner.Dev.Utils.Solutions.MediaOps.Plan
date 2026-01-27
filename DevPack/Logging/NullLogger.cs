namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Logging
{
    using System.Runtime.CompilerServices;

    internal class NullLogger : ILogger
    {
        public void LogDebug(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "")
        {
            // nothing to do
        }

        public void LogDebug(string message)
        {
            // nothing to do
        }

        public void LogError(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "")
        {
            // nothing to do
        }

        public void LogError(string message)
        {
            // nothing to do
        }

        public void LogInformation(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "")
        {
            // nothing to do
        }

        public void LogInformation(string message)
        {
            // nothing to do
        }

        public void LogWarning(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "")
        {
            // nothing to do
        }

        public void LogWarning(string message)
        {
            // nothing to do
        }
    }
}
