namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a reference to a capacity parameter on a workflow node.
    /// </summary>
    public sealed class CapacityParameterReference : ParameterReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CapacityParameterReference"/> class.
        /// </summary>
        /// <param name="parameterId">The unique identifier of the capacity parameter.</param>
        /// <param name="nodeId">
        /// Optional identifier of the workflow node whose capacity parameter is referenced.
        /// When <see langword="null"/> the reference targets the parameter on the current node.
        /// </param>
        public CapacityParameterReference(Guid parameterId, string nodeId = null)
            : base(DataReferenceType.CapacityParameter, parameterId, nodeId)
        {
        }

        internal static CapacityParameterReference ParseFromStorage(Storage.DOM.DataReferenceStorage reference, string nodeId)
        {
            if (reference.ReferenceData == null || !reference.ReferenceData.TryGetValue(ParameterIdKey, out var raw))
                return null;

            return Guid.TryParse(raw, out var id) ? new CapacityParameterReference(id, nodeId) : null;
        }
    }
}
