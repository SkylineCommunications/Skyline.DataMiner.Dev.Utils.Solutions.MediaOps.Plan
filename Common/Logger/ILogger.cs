namespace Skyline.DataMiner.MediaOps.Plan.Logger
{
    using System;
    using System.Runtime.CompilerServices;

    public interface ILogger : IDisposable
    {
        LogLevel MinimumLogLevel { get; set; }

        void Debug(object callerInstance, string message, [CallerMemberName] string methodName = "");

        void Debug(string className, string methodName, string message);

        void Error(Exception exception, string message);

        void Error(object callerInstance, string message, [CallerMemberName] string methodName = "");

        void Error(string className, string methodName, Exception exception, string message);

        void Error(string className, string methodName, string message);

        void Information(object callerInstance, string message, [CallerMemberName] string methodName = "");

        void Information(string className, string methodName, string message);

        void Warning(Exception exception, string message);

        void Warning(object callerInstance, string message, [CallerMemberName] string methodName = "");

        void Warning(string className, string methodName, Exception exception, string message);

        void Warning(string className, string methodName, string message);
    }
}
