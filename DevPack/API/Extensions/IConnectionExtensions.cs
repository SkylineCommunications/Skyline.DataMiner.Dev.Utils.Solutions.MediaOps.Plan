namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Net;

	/// <summary>
	/// Defines extension methods on the <see cref="IConnection"/> class.
	/// </summary>
	public static class IConnectionExtensions
	{
		/// <summary>
		/// Retrieves an instance of the <see cref="IMediaOpsPlanApi"/> interface.
		/// </summary>
		/// <param name="connection">The <see cref="IConnection"/> instance.</param>
		/// <returns>Instance of the <see cref="IMediaOpsPlanApi"/> interface.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null" />.</exception>
		public static IMediaOpsPlanApi GetMediaOpsPlanApi(this IConnection connection)
		{
			if (connection == null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			return new MediaOpsPlanApi(connection);
		}
	}
}
