namespace Skyline.DataMiner.MediaOps.Plan.Exceptions
{
    using System;

    /// <summary>
    /// Thrown when a MediaOps operation failed.
    /// </summary>
    public class MediaOpsException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaOpsException"/> class with a specified error data.
        /// </summary>
        /// <param name="data">The <see cref="MediaOpsErrorData"/> that describes the error.</param>
        public MediaOpsException(MediaOpsErrorData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            TraceData = new MediaOpsTraceData();
            TraceData.ErrorData.Add(data);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaOpsException"/> class with specified trace data.
        /// </summary>
        /// <param name="data">The <see cref="MediaOpsTraceData"/> that contains the trace information of the error.</param>
        public MediaOpsException(MediaOpsTraceData data)
        {
            TraceData = data ?? new MediaOpsTraceData();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaOpsException"/> class with the specified error message.
        /// </summary>
        /// <param name="message">The error message that describes the reason for the exception.</param>
        public MediaOpsException(string message)
            : this(new MediaOpsErrorData { ErrorMessage = message })
        {
        }

        /// <summary>
        /// Gets the trace data associated with this exception.
        /// </summary>
        public MediaOpsTraceData TraceData { get; private set; }

        /// <summary>
        /// Gets the error message that explains the reason for this <see cref="MediaOpsException" />.
        /// </summary>
        public override string Message
        {
            get
            {
                if (TraceData.ErrorData.Count == 1)
                {
                    return TraceData.ErrorData[0].ErrorMessage;
                }

                return TraceData.ToString();
            }
        }

        /// <summary>
        /// Returns a string that represents the current exception.
        /// </summary>
        /// <returns>A string that represents the current exception, including the trace data.</returns>
        public override string ToString()
        {
            return $"{base.ToString()}{Environment.NewLine}Containing TraceData:{Environment.NewLine}{TraceData}";
        }
    }
}
