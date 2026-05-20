namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    /// <summary>
    /// Represents a reference to the name of a job.
    /// </summary>
    public sealed class JobNameReference : DataReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobNameReference"/> class.
        /// </summary>
        /// <param name="nodeId">
        /// Optional identifier of the workflow node whose job name is referenced.
        /// When <see langword="null"/> the reference targets the job of the current node.
        /// </param>
        public JobNameReference(string nodeId = null) : base(DataReferenceType.JobName, nodeId)
        {
        }
    }
}
