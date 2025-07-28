namespace Skyline.DataMiner.MediaOps.Plan.Protocol
{
    using System;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.MediaOps.Plan.API;
    using Skyline.DataMiner.Scripting;

    /// <summary>
    /// Defines extension methods on the <see cref="SlProtocolExtensions"/> class.
    /// </summary>
    public static class SlProtocolExtensions
    {
        /// <summary>
        /// Retrieves an instance of the <see cref="IMediaOpsPlanApi"/> interface."/>
        /// </summary>
        /// <param name="protocol">The <see cref="SLProtocol"/> instance.</param>
        /// <param name="logger">The <see cref="ILogger"/> implementation.</param>
        /// <returns>Instance of the <see cref="IMediaOpsPlanApi"/> interface.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="protocol"/> is <see langword="null" />.</exception>
        public static IMediaOpsPlanApi GetMediaOpsPlanApi(this SLProtocol protocol, ILogger<IMediaOpsPlanApi> logger = null)
        {
            if (protocol == null)
            {
                throw new ArgumentNullException(nameof(protocol));
            }

            return new MediaOpsPlanApi(protocol.GetUserConnection(), logger);
        }
    }
}
