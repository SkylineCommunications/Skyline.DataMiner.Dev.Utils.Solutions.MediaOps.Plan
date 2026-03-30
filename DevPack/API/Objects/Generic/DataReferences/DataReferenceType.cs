namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System.ComponentModel;

    /// <summary>
    /// Specifies the type of data a <see cref="DataReference"/> points to.
    /// </summary>
    public enum DataReferenceType
    {
        /// <summary>
        /// Refers to the name of a resource.
        /// </summary>
        [Description("Resource Name")]
        ResourceName,

        /// <summary>
        /// Refers to a resource property.
        /// </summary>
        [Description("Resource Property")]
        ResourceProperty,

        /// <summary>
        /// Refers to the linked object ID of a resource.
        /// </summary>
        [Description("Resource Linked Object ID")]
        ResourceLinkedObjectID,

        /// <summary>
        /// Refers to a scheduling configuration parameter.
        /// </summary>
        [Description("Scheduling Configuration Parameter")]
        SchedulingConfigurationParameter,
    }
}
