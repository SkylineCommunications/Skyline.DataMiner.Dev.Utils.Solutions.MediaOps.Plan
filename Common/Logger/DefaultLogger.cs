namespace Skyline.DataMiner.MediaOps.Plan.Logger
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class DefaultLogger : ILogger
    {
        private static readonly IReadOnlyDictionary<LogLevel, string> LogLevelAbbreviations = new Dictionary<LogLevel, string>
        {
            { LogLevel.Debug, "DBG" },
            { LogLevel.Information, "INF" },
            { LogLevel.Warning, "WRN" },
            { LogLevel.Error, "ERR" },
        };

        private readonly FixedFileLogger fixedFileLogger;

        private bool disposedValue;

        public DefaultLogger()
        {
            if (!Tools.UnitTestDetector.IsInUnitTest)
            {
                string path = FixedFileLogger.GenerateLogFilePath("PLAN API");
                fixedFileLogger = new FixedFileLogger(path);
            }
        }

        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

        public void Debug(string className, string methodName, string message)
        {
            Log(LogLevel.Debug, className, methodName, message);
        }

        public void Debug(object callerInstance, string message, [CallerMemberName] string methodName = "")
        {
            Debug(callerInstance.GetType().Name, methodName, message);
        }

        public void Error(object callerInstance, Exception exception, string message, [CallerMemberName] string methodName = "")
        {
            Error(callerInstance.GetType().Name, methodName, $"{message} with exception:{Environment.NewLine}{exception}");
        }

        public void Error(Exception exception, string message)
        {
            Log("ERR", $"{message} with exception:{Environment.NewLine}{exception}");
        }

        public void Error(string className, string methodName, Exception exception, string message)
        {
            Log(LogLevel.Error, className, methodName, $"{message} with exception:{Environment.NewLine}{exception}");
        }

        public void Error(string className, string methodName, string message)
        {
            Log(LogLevel.Error, className, methodName, message);
        }

        public void Error(object callerInstance, string message, [CallerMemberName] string methodName = "")
        {
            Error(callerInstance.GetType().Name, methodName, message);
        }

        public void Information(string className, string methodName, string message)
        {
            Log(LogLevel.Information, className, methodName, message);
        }

        public void Information(object callerInstance, string message, [CallerMemberName] string methodName = "")
        {
            Information(callerInstance.GetType().Name, methodName, message);
        }

        public void Warning(Exception exception, string message)
        {
            Log("WRN", $"{message} with exception:{Environment.NewLine}{exception}");
        }

        public void Warning(string className, string methodName, Exception exception, string message)
        {
            Log(LogLevel.Warning, className, methodName, $"{message} with exception:{Environment.NewLine}{exception}");
        }

        public void Warning(string className, string methodName, string message)
        {
            Log(LogLevel.Warning, className, methodName, message);
        }

        public void Warning(object callerInstance, string message, [CallerMemberName] string methodName = "")
        {
            Warning(callerInstance.GetType().Name, methodName, message);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue)
            {
                return;
            }

            if (disposing)
            {
                // dispose managed state (managed objects)
                fixedFileLogger?.Dispose();
                disposedValue = true;
            }
        }

        private void Log(LogLevel logLevel, string className, string methodName, string message)
        {
            if (logLevel < MinimumLogLevel)
            {
                return;
            }

            Log(LogLevelAbbreviations[logLevel], $"{className}|{methodName}|{message}");
        }

        private void Log(string logLevel, string message)
        {
            fixedFileLogger?.LogLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{Thread.CurrentThread.ManagedThreadId}] [{logLevel}] {message}");
        }
    }
}
