namespace Skyline.DataMiner.MediaOps.Plan.Logger
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;

    internal class FixedFileLogger : IDisposable
    {
        public static readonly string SkylineDataFilePath = @"C:\Skyline DataMiner\Logging\MediaOps\";
        public static readonly string TextFileExtension = ".txt";

        private const long MaxLogFileSize = 10000000; // 10 mb

        private readonly StringBuilder buffer = new StringBuilder();

        private bool disposedValue;

        public FixedFileLogger(params string[] logFilePaths)
        {
            LogfilePaths = new HashSet<string>(logFilePaths ?? throw new ArgumentNullException(nameof(logFilePaths)));

            foreach (var logFilePath in logFilePaths)
            {
                if (!logFilePath.StartsWith(SkylineDataFilePath)) throw new ArgumentException($"Argument does not start with {SkylineDataFilePath}", nameof(logFilePaths));
                if (!logFilePath.EndsWith(TextFileExtension)) throw new ArgumentException($"Argument does not end with {TextFileExtension}", nameof(logFilePaths));
            }

            TryCreateNewFiles();
        }

        public HashSet<string> LogfilePaths { get; private set; }

        public static string GenerateLogFilePath(string fileName)
        {
            return $"{SkylineDataFilePath}{fileName}{TextFileExtension}";
        }

        public void LogLine(string message)
        {
            buffer.AppendLine(message);
        }

        private static void WriteFileContentWithRetry(string logfilePath, string contentToKeep)
        {
            int retry = 0;
            bool successful = false;
            IOException exception = new IOException("Unable to write content to log file.");

            while (retry < 10 && !successful)
            {
                try
                {
                    File.WriteAllText(logfilePath, contentToKeep);
                    successful = true;
                }
                catch (IOException ex)
                {
                    exception = new IOException("Unable to write content to log file.", ex);

                    successful = false;
                    retry++;
                    Thread.Sleep(50);
                }
            }

            if (!successful)
            {
                throw exception;
            }
        }

        private static void AppendFileContentWithRetry(string logfilePath, string contentToAppend)
        {
            int retry = 0;
            bool successful = false;

            while (retry < 10 && !successful)
            {
                try
                {
                    File.AppendAllText(logfilePath, contentToAppend);
                    successful = true;
                }
                catch (IOException)
                {
                    successful = false;
                    retry++;
                    Thread.Sleep(50);
                }
            }
        }

        private static string GetFileContent(string filename, long amountOfBytesToSkip = 0)
        {
            using (var stream = File.OpenRead(filename))
            {
                stream.Seek(amountOfBytesToSkip, SeekOrigin.Begin);

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private void TryCreateNewFiles()
        {
            foreach (var logfilePath in LogfilePaths)
            {
                int retries = 0;
                bool successful = false;
                while (!successful && retries < 5)
                {
                    try
                    {
                        bool logFileExists = File.Exists(logfilePath);

                        if (!logFileExists)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(logfilePath));

                            var newFile = File.Create(logfilePath);
                            newFile.Close();
                        }

                        successful = true;
                    }
                    catch (Exception)
                    {
                        successful = false;
                        retries++;
                        Thread.Sleep(100);
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                try
                {
                    TryCreateNewFiles();

                    long bufferSize = buffer.Length;

                    foreach (var logfilePath in LogfilePaths)
                    {
                        long currentFileSize = new FileInfo(logfilePath).Length; // 0 in case file doesn't exist

                        long potentialFileSize = currentFileSize + bufferSize;

                        if (potentialFileSize > MaxLogFileSize)
                        {
                            long amountOfBytesOverTheMax = currentFileSize - MaxLogFileSize;
                            long extraAmountOfBytesToRemoveToHaveSomeMargin = MaxLogFileSize / 4;
                            long totalAmountOfBytesToSkip = amountOfBytesOverTheMax + extraAmountOfBytesToRemoveToHaveSomeMargin;

                            var fileContentToKeep = new StringBuilder(GetFileContent(logfilePath, totalAmountOfBytesToSkip));
                            fileContentToKeep.AppendLine("**********");
                            fileContentToKeep.Append(buffer);

                            WriteFileContentWithRetry(logfilePath, fileContentToKeep.ToString());
                        }
                        else
                        {
                            AppendFileContentWithRetry(logfilePath, buffer.ToString());
                        }
                    }

                    buffer.Clear();
                }
                catch (Exception)
                {
                    // silently handle exceptions
                }

                disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~FixedFileLogger()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
