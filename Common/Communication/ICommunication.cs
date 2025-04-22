namespace Skyline.DataMiner.MediaOps.API.Common
{
    using Skyline.DataMiner.Net;

    /// <summary>
    /// Defines properties and methods to communicate with the MediaOps solution.
    /// </summary>
    public interface ICommunication
    {
        /// <summary>
        /// Gets the <see cref="IConnection"/> interface.
        /// </summary>
        IConnection Connection { get; }
    }
}