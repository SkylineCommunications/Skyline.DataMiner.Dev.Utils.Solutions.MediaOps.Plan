namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a reference to a configuration parameter on a workflow node.
    /// </summary>
    public sealed class ConfigurationParameterReference : ParameterReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationParameterReference"/> class.
        /// </summary>
        /// <param name="parameterId">The unique identifier of the configuration parameter.</param>
        /// <param name="nodeId">
        /// Optional identifier of the workflow node whose configuration parameter is referenced.
        /// When <see langword="null"/> the reference targets the parameter on the current node.
        /// </param>
        public ConfigurationParameterReference(Guid parameterId, string nodeId = null)
            : base(DataReferenceType.ConfigurationParameter, parameterId, nodeId)
        {
        }

        internal static ConfigurationParameterReference ParseFromStorage(Storage.DOM.DataReferenceStorage reference, string nodeId)
        {
            if (reference.ReferenceData == null || !reference.ReferenceData.TryGetValue(ParameterIdKey, out var raw))
                return null;

            return Guid.TryParse(raw, out var id) ? new ConfigurationParameterReference(id, nodeId) : null;
        }
    }
}
