namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a reference to a scheduling configuration parameter.
    /// </summary>
    public class SchedulingConfigurationParameterReference : DataReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SchedulingConfigurationParameterReference"/> class with the specified parameter ID.
        /// </summary>
        /// <param name="parameterId">The unique identifier of the scheduling configuration parameter.</param>
        public SchedulingConfigurationParameterReference(Guid parameterId)
            : base(DataReferenceType.SchedulingConfigurationParameter)
        {
            SchedulingConfigurationParameterId = parameterId;
        }

        /// <summary>
        /// Gets the unique identifier of the scheduling configuration parameter.
        /// </summary>
        public Guid SchedulingConfigurationParameterId { get; }
    }
}
