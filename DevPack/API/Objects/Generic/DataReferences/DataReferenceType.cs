namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System.ComponentModel;

    /// <summary>
    /// Specifies the type of data a <see cref="DataReference"/> points to.
    /// </summary>
    public enum DataReferenceType
    {
        /// <summary>
        /// Refers to the name of a resource assigned to a node.
        /// </summary>
        [Description("Resource Name")]
        ResourceName,

        /// <summary>
        /// Refers to a property of a resource assigned to a node.
        /// </summary>
        [Description("Resource Property")]
        ResourceProperty,

        /// <summary>
        /// Refers to the linked object ID of a resource assigned to a node (e.g. the element or service ID).
        /// </summary>
        [Description("Resource Linked Object ID")]
        ResourceLinkedObjectID,

        /// <summary>
        /// Refers to a capability parameter.
        /// </summary>
        [Description("Capability")]
        CapabilityParameter,

        /// <summary>
        /// Refers to a capacity parameter.
        /// </summary>
        [Description("Capacity")]
        CapacityParameter,

        /// <summary>
        /// Refers to a configuration parameter.
        /// </summary>
        [Description("Configuration")]
        ConfigurationParameter,

        /// <summary>
        /// Refers to the name of the workflow.
        /// </summary>
        [Description("Workflow Name")]
        WorkflowName,

        /// <summary>
        /// Refers to a workflow property.
        /// </summary>
        [Description("Workflow Property")]
        WorkflowProperty,

        /// <summary>
        /// Refers to the name of a job.
        /// </summary>
        [Description("Job Name")]
        JobName,

        /// <summary>
        /// Refers to a job property.
        /// </summary>
        [Description("Job Property")]
        JobProperty,
    }
}
