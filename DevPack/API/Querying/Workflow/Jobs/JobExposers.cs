namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    /// <summary>
    /// Provides exposers for querying and filtering <see cref="Job"/> objects.
    /// </summary>
    public class JobExposers
    {
        /// <summary>
        /// Gets an exposer for the <see cref="ApiObject.ID"/> property.
        /// </summary>
        public static readonly Exposer<Job, Guid> Id = new Exposer<Job, Guid>((obj) => obj.ID, "Id");

        /// <summary>
        /// Gets an exposer for the <see cref="ApiObject.Name"/> property.
        /// </summary>
        public static readonly Exposer<Job, string> Name = new Exposer<Job, string>((obj) => obj.Name, "Name");
    }
}
