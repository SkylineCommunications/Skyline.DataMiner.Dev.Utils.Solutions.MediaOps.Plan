namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	/// <summary>
	/// Contains all kinds of data that MediaOps could generate while handling a request.
	/// </summary>
	public sealed class MediaOpsTraceData
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MediaOpsTraceData"/> class.
		/// </summary>
		public MediaOpsTraceData()
		{
		}

		/// <summary>
		/// Gets or sets the error data that was generated while handling the request.
		/// </summary>
		/// <returns>Never null.</returns>
		public List<MediaOpsErrorData> ErrorData { get; internal set; } = new List<MediaOpsErrorData>();

		/// <summary>
		/// Returns all the data contained in the object in a readable format.
		/// Is also log-friendly.
		/// </summary>
		/// <returns>A string representation of the trace data.</returns>
		public override string ToString()
		{
			var info = new StringBuilder();
			info.Append($"TraceData: (amount = {ErrorData.Count})\n");

			if (ErrorData.Count != 0)
			{
				info.Append($"  - ErrorData: (amount = {ErrorData.Count})\n");
				info.Append($"      - {string.Join("\n      - ", ErrorData)}\n");
			}

			return info.ToString();
		}

		/// <summary>
		/// Returns true if the object does not contain any errors indicating failure of a operation.
		/// </summary>
		/// <returns><c>true</c> if the object has no errors; otherwise, <c>false</c>.</returns>
		public bool HasSucceeded()
		{
			return ErrorData.Count == 0;
		}

		/// <summary>
		/// Adds error data to the trace.
		/// </summary>
		/// <param name="errorData">The error data to add. Cannot be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="errorData"/> is <see langword="null"/>.</exception>
		public void Add(MediaOpsErrorData errorData)
		{
			if (errorData == null)
			{
				throw new ArgumentNullException(nameof(errorData));
			}

			ErrorData.Add(errorData);
		}
	}
}
