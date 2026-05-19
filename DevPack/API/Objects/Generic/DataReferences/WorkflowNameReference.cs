namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    /// <summary>
    /// Represents a reference to the name of a workflow.
    /// </summary>
    public sealed class WorkflowNameReference : DataReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowNameReference"/> class.
        /// </summary>
        /// <param name="nodeId">
        /// Optional identifier of the workflow node whose workflow name is referenced.
        /// When <see langword="null"/> the reference targets the workflow of the current node.
        /// </param>
        public WorkflowNameReference(string nodeId = null) : base(DataReferenceType.WorkflowName, nodeId)
        {
        }
    }
}
