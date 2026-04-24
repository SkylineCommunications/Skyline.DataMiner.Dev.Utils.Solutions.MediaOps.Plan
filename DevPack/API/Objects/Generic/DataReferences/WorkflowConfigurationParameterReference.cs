namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a reference to a configuration parameter defined on the workflow (or job) level
    /// rather than on a specific node.
    /// </summary>
    public sealed class WorkflowConfigurationParameterReference : DataReference
    {
        private const string ParameterIdKey = "WorkflowConfigurationParameterId";

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowConfigurationParameterReference"/> class.
        /// </summary>
        /// <param name="parameterId">The unique identifier of the workflow-level configuration parameter.</param>
        public WorkflowConfigurationParameterReference(Guid parameterId) : base(DataReferenceType.WorkflowConfigurationParameter)
        {
            ParameterId = parameterId;
        }

        /// <summary>
        /// Gets the unique identifier of the workflow-level configuration parameter.
        /// </summary>
        public Guid ParameterId { get; }

        /// <inheritdoc/>
        public override bool Equals(DataReference other)
        {
            return base.Equals(other)
                && other is WorkflowConfigurationParameterReference wcpr
                && wcpr.ParameterId == ParameterId;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = base.GetHashCode();
                hash = hash * 23 + ParameterId.GetHashCode();
                return hash;
            }
        }

        internal static WorkflowConfigurationParameterReference ParseFromStorage(Storage.DOM.DataReference reference)
        {
            if (reference.ReferenceData == null || !reference.ReferenceData.TryGetValue(ParameterIdKey, out var raw))
            {
                return null;
            }

            return Guid.TryParse(raw, out var id) ? new WorkflowConfigurationParameterReference(id) : null;
        }

        private protected override Dictionary<string, string> BuildReferenceData()
        {
            var data = base.BuildReferenceData() ?? new Dictionary<string, string>();
            data[ParameterIdKey] = ParameterId.ToString();
            return data;
        }
    }
}
