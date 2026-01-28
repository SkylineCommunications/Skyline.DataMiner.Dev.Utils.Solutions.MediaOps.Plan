namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Logging
{
    using System.Runtime.CompilerServices;

    internal class NullLogger : ILogger
    {
        public void Debug(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "")
        {
            // nothing to do
        }

        public void Debug(string message)
        {
            // nothing to do
        }

        public void Error(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "")
        {
            // nothing to do
        }

        public void Error(string message)
        {
            // nothing to do
        }

        public void Information(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "")
        {
            // nothing to do
        }

        public void Information(string message)
        {
            // nothing to do
        }

        public void Warning(object callerInstance, string message, object[] args = null, [CallerMemberName] string methodName = "")
        {
            // nothing to do
        }

        public void Warning(string message)
        {
            // nothing to do
        }
    }
}
