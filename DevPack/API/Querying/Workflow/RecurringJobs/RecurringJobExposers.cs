namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    /// <summary>
    /// Provides exposers for querying and filtering <see cref="RecurringJob"/> objects.
    /// </summary>
    public class RecurringJobExposers
    {
        /// <summary>
        /// Gets an exposer for the <see cref="ApiObject.Id"/> property.
        /// </summary>
        public static readonly Exposer<RecurringJob, Guid> Id = new Exposer<RecurringJob, Guid>((obj) => obj.Id, "Id");

        /// <summary>
        /// Gets an exposer for the <see cref="ApiObject.Name"/> property.
        /// </summary>
        public static readonly Exposer<RecurringJob, string> Name = new Exposer<RecurringJob, string>((obj) => obj.Name, "Name");
    }
}
