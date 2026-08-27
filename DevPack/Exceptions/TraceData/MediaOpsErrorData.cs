namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Parent class for all ErrorData types.
	/// </summary>
	public class MediaOpsErrorData
	{
		/// <summary>
		/// Gets or sets the message that describes the error.
		/// </summary>
		public string ErrorMessage { get; internal set; }

		/// <summary>
		/// Returns the message that describes the error.
		/// </summary>
		/// <returns>The error message, or the type name when no message is available.</returns>
		public override string ToString()
		{
			return string.IsNullOrEmpty(ErrorMessage) ? GetType().Name : ErrorMessage;
		}
	}
}
