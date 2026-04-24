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
        /// Refers to a configuration parameter.
        /// </summary>
        [Description("Configuration Parameter")]
        ConfigurationParameter,

        /// <summary>
        /// Refers to the name of the workflow.
        /// </summary>
        [Description("Workflow Name")]
        WorkflowName,

        /// <summary>
        /// Refers to a workflow property (a property defined under the MediaOps scope and assigned to a workflow / job).
        /// </summary>
        [Description("Workflow Property")]
        WorkflowProperty,

        /// <summary>
        /// Refers to a configuration parameter on the workflow (or job) level rather than on a specific node.
        /// </summary>
        [Description("Workflow Configuration Parameter")]
        WorkflowConfigurationParameter,
    }
}
