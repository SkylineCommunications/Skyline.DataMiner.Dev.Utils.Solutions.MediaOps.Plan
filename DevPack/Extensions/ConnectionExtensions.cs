namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API.Extensions
{
	using System;

	using Skyline.DataMiner.Net;

	/// <summary>
	/// Provides extension methods for the <see cref="IConnection"/> interface.
	/// </summary>
	public static class ConnectionExtensions
	{
		/// <summary>
		/// Creates a new <see cref="IMediaOpsPlanApi"/> instance that uses the specified <see cref="IConnection"/>.
		/// </summary>
		/// <param name="connection">The DataMiner connection to be used by the MediaOps Plan API.</param>
		/// <returns>An <see cref="IMediaOpsPlanApi"/> instance that uses the specified connection.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is <c>null</c>.</exception>
		public static IMediaOpsPlanApi GetMediaOpsPlanApi(this IConnection connection)
		{
			if (connection == null)
			{
				throw new ArgumentNullException("connection");
			}

			return new MediaOpsPlanApi(connection);
		}
	}
}
