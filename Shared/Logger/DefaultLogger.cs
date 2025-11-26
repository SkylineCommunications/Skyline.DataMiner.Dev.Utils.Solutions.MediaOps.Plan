namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Logger
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

    internal class DefaultLogger : ILogger<IMediaOpsPlanApi>, IDisposable
    {
        private static readonly IReadOnlyDictionary<LogLevel, string> LogLevelAbbreviations = new Dictionary<LogLevel, string>
        {
            { LogLevel.Trace, "TRC" },
            { LogLevel.Debug, "DBG" },
            { LogLevel.Information, "INF" },
            { LogLevel.Warning, "WRN" },
            { LogLevel.Error, "ERR" },
            { LogLevel.Critical, "CRT" },
            { LogLevel.None, "N/A" }
        };

        private readonly FixedFileLogger fixedFileLogger;

        private bool disposedValue;

        public DefaultLogger()
        {
            if (!Tools.UnitTestDetector.IsInUnitTest)
            {
                string path = FixedFileLogger.GenerateLogFilePath("PlanAPI");
                fixedFileLogger = new FixedFileLogger(path);
            }
        }

        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

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

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel < MinimumLogLevel || fixedFileLogger == null)
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            string logLevelAbbreviation = LogLevelAbbreviations.TryGetValue(logLevel, out var abbr) ? abbr : logLevel.ToString().ToUpperInvariant();
            string message = formatter(state, exception);

            if (exception != null)
            {
                message = $"{message} with exception:{Environment.NewLine}{exception}";
            }

            fixedFileLogger.LogLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{Thread.CurrentThread.ManagedThreadId}] [{logLevelAbbreviation}] {message}");
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= MinimumLogLevel && fixedFileLogger != null;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            throw new NotSupportedException();
        }
    }
}
