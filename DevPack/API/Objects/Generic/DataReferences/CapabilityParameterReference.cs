namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a reference to a capability parameter on a workflow node.
    /// </summary>
    public sealed class CapabilityParameterReference : ParameterReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CapabilityParameterReference"/> class.
        /// </summary>
        /// <param name="parameterId">The unique identifier of the capability parameter.</param>
        /// <param name="nodeId">
        /// Optional identifier of the workflow node whose capability parameter is referenced.
        /// When <see langword="null"/> the reference targets the parameter on the current node.
        /// </param>
        public CapabilityParameterReference(Guid parameterId, string nodeId = null)
            : base(DataReferenceType.CapabilityParameter, parameterId, nodeId)
        {
        }

        internal static CapabilityParameterReference ParseFromStorage(Storage.DOM.DataReferenceStorage reference, string nodeId)
        {
            if (reference.ReferenceData == null || !reference.ReferenceData.TryGetValue(ParameterIdKey, out var raw))
                return null;

            return Guid.TryParse(raw, out var id) ? new CapabilityParameterReference(id, nodeId) : null;
        }
    }
}
