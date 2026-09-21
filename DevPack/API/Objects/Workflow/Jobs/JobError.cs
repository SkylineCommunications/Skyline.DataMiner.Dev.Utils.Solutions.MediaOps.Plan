namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents an error that is reported on a <see cref="Job"/>.
	/// </summary>
	public class JobError
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JobError"/> class.
		/// </summary>
		/// <param name="code">The code that identifies the error.</param>
		/// <param name="message">The message that describes the error.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="code"/> is <see langword="null"/> or whitespace.</exception>
		public JobError(string code, string message)
		{
			if (string.IsNullOrWhiteSpace(code))
			{
				throw new ArgumentException("Error code cannot be null or whitespace.", nameof(code));
			}

			Code = code;
			Message = message;
		}

		/// <summary>
		/// Gets the code that identifies the error.
		/// </summary>
		public string Code { get; private set; }

		/// <summary>
		/// Gets the message that describes the error.
		/// </summary>
		public string Message { get; private set; }

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + (Code != null ? Code.GetHashCode() : 0);
				hash = (hash * 23) + (Message != null ? Message.GetHashCode() : 0);

				return hash;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not JobError other)
			{
				return false;
			}

			return Code == other.Code &&
				   Message == other.Message;
		}
	}
}
