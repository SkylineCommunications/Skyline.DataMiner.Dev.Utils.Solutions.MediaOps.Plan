namespace Skyline.DataMiner.MediaOps.API.Protocol.Extensions
{
    using System;
    using Skyline.DataMiner.MediaOps.API.Common.MediaOpsHelpers;
    using Skyline.DataMiner.Scripting;

    /// <summary>
    /// Defines extension methods on the <see cref="SLProtocol"/> class.
    /// </summary>
    public static class SlProtocolExtensions
    {
        /// <summary>
        /// Retrieves an instance of the <see cref="Helpers"/> class.
        /// </summary>
        /// <param name="protocol">The <see cref="SLProtocol"/> instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="protocol"/> is <see langword="null" />.</exception>
        /// <returns>Instance of the <see cref="Helpers"/> class.</returns>
        public static Helpers GetMediaOpsHelpers(this SLProtocol protocol)
        {
            if (protocol == null)
            {
                throw new ArgumentNullException(nameof(protocol));
            }

            return new Helpers(new ConnectionCommunication(protocol.GetUserConnection()));
        }
    }
}
