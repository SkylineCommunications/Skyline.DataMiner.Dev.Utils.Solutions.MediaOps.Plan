namespace Skyline.DataMiner.Solutions.MediaOps.Plan.GQI
{
	using System;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Extensions;

	/// <summary>
	/// Defines extension methods on the <see cref="GqiDmsExtensions"/> class.
	/// </summary>
	public static class GqiDmsExtensions
	{
		/// <summary>
		/// Retrieves an instance of the <see cref="IMediaOpsPlanApi"/> interface."/>
		/// </summary>
		/// <param name="dms">The <see cref="GQIDMS"/> instance.</param>
		/// <returns>Instance of the <see cref="IMediaOpsPlanApi"/> interface.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="dms"/> is <see langword="null" />.</exception>
		public static IMediaOpsPlanApi GetMediaOpsPlanApi(this GQIDMS dms)
		{
			if (dms == null)
			{
				throw new ArgumentNullException(nameof(dms));
			}

			return dms.GetConnection().GetMediaOpsPlanApi();
		}
	}
}
