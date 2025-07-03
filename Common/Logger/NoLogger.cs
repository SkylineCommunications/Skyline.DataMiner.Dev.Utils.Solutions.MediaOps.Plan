namespace Skyline.DataMiner.MediaOps.Plan.Logger
{
    using System;

    internal class NoLogger : ILogger
    {
        private LogLevel minimumLogLevel;
        private bool disposedValue;

        public NoLogger()
        {
            minimumLogLevel = LogLevel.Debug; // Default log level
        }

        LogLevel ILogger.MinimumLogLevel
        {
            get => minimumLogLevel;
            set
            {
                minimumLogLevel = value;
            }
        }

        void ILogger.Debug(object callerInstance, string message, string methodName)
        {
            // No logic
        }

        void ILogger.Debug(string className, string methodName, string message)
        {
            // No logic
        }

        void ILogger.Error(Exception exception, string message)
        {
            // No logic
        }

        void ILogger.Error(object callerInstance, string message, string methodName)
        {
            // No logic
        }

        void ILogger.Error(string className, string methodName, Exception exception, string message)
        {
            // No logic
        }

        void ILogger.Error(string className, string methodName, string message)
        {
            // No logic
        }

        void ILogger.Information(object callerInstance, string message, string methodName)
        {
            // No logic
        }

        void ILogger.Information(string className, string methodName, string message)
        {
            // No logic
        }

        void ILogger.Warning(Exception exception, string message)
        {
            // No logic
        }

        void ILogger.Warning(object callerInstance, string message, string methodName)
        {
            // No logic
        }

        void ILogger.Warning(string className, string methodName, Exception exception, string message)
        {
            // No logic
        }

        void ILogger.Warning(string className, string methodName, string message)
        {
            // No logic
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Nothing to dispose
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
