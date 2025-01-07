namespace Skyline.DataMiner.MediaOps.API.GQI
{
    using System;

    using Skyline.DataMiner.Analytics.GenericInterface;
    using Skyline.DataMiner.MediaOps.API.Common.MediaOpsHelpers;

    /// <summary>
    /// Defines extension methods on the <see cref="GQIDMS"/> class.
    /// </summary>
    public class GQIDMSExtensions
    {
        /// <summary>
        /// Retrieves an instance of the <see cref="Helpers"/> class.
        /// </summary>
        /// <param name="dms">The <see cref="GQIDMS"/> instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dms"/> is <see langword="null" />.</exception>
        /// <returns>Instance of the <see cref="Helpers"/> class.</returns>
        public static Helpers GetMediaOpsHelpers(GQIDMS dms)
        {
            if (dms == null)
            {
                throw new ArgumentNullException(nameof(dms));
            }

            return new Helpers(new ConnectionCommunication(dms.GetConnection()));
        }
    }
}
