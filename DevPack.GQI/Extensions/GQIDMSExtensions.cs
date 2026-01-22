namespace Skyline.DataMiner.Solutions.MediaOps.Plan.GQI
{
    using System;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.Analytics.GenericInterface;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

    /// <summary>
    /// Defines extension methods on the <see cref="GqiDmsExtensions"/> class.
    /// </summary>
    public static class GqiDmsExtensions
    {
        /// <summary>
        /// Retrieves an instance of the <see cref="IMediaOpsPlanApi"/> interface."/>
        /// </summary>
        /// <param name="dms">The <see cref="GQIDMS"/> instance.</param>
        /// <param name="logger">The <see cref="ILogger"/> implementation.</param>
        /// <returns>Instance of the <see cref="IMediaOpsPlanApi"/> interface.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dms"/> is <see langword="null" />.</exception>
        public static IMediaOpsPlanApi GetMediaOpsPlanApi(this GQIDMS dms, ILogger logger = null)
        {
            if (dms == null)
            {
                throw new ArgumentNullException(nameof(dms));
            }

            return new MediaOpsPlanApi(dms.GetConnection(), logger);
        }
    }
}
