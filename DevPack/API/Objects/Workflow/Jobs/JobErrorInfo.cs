namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents an error that is reported on a <see cref="Job"/>.
	/// </summary>
	public class JobErrorInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JobErrorInfo"/> class.
		/// </summary>
		/// <param name="errorCode">The code that identifies the error.</param>
		/// <param name="errorMessage">The message that describes the error.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="errorCode"/> is <see langword="null"/> or whitespace.</exception>
		public JobErrorInfo(string errorCode, string errorMessage)
		{
			if (string.IsNullOrWhiteSpace(errorCode))
			{
				throw new ArgumentException("Error code cannot be null or whitespace.", nameof(errorCode));
			}

			ErrorCode = errorCode;
			ErrorMessage = errorMessage;
		}

		/// <summary>
		/// Gets the code that identifies the error.
		/// </summary>
		public string ErrorCode { get; private set; }

		/// <summary>
		/// Gets the message that describes the error.
		/// </summary>
		public string ErrorMessage { get; private set; }

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + (ErrorCode != null ? ErrorCode.GetHashCode() : 0);
				hash = (hash * 23) + (ErrorMessage != null ? ErrorMessage.GetHashCode() : 0);

				return hash;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not JobErrorInfo other)
			{
				return false;
			}

			return ErrorCode == other.ErrorCode &&
				   ErrorMessage == other.ErrorMessage;
		}
	}
}
