namespace Skyline.DataMiner.MediaOps.Plan.Logger
{
	/// <summary>
	/// Specifies the severity level of a log message.
	/// </summary>
	public enum LogLevel
	{
		/// <summary>
		/// Debug-level messages, typically used for development and troubleshooting.
		/// </summary>
		Debug = 0,

		/// <summary>
		/// Informational messages that highlight the progress of the application.
		/// </summary>
		Information = 1,

		/// <summary>
		/// Warning messages that indicate a potential issue or important situation.
		/// </summary>
		Warning = 2,

		/// <summary>
		/// Error messages that indicate a failure in the application.
		/// </summary>
		Error = 3,
	}
}
