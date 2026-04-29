namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a reference to a workflow property (a property defined under the MediaOps scope and
    /// assigned to a workflow / job).
    /// </summary>
    public sealed class WorkflowPropertyReference : DataReference
    {
        private const string WorkflowPropertyIdKey = "WorkflowPropertyId";

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowPropertyReference"/> class.
        /// </summary>
        /// <param name="workflowPropertyId">The unique identifier of the workflow property definition.</param>
        /// <param name="nodeId">
        /// Optional identifier of the workflow node whose workflow is referenced.
        /// When <see langword="null"/> the reference targets the workflow of the current node.
        /// </param>
        public WorkflowPropertyReference(Guid workflowPropertyId, string nodeId = null) : base(DataReferenceType.WorkflowProperty, nodeId)
        {
            WorkflowPropertyId = workflowPropertyId;
        }

        /// <summary>
        /// Gets the unique identifier of the workflow property definition.
        /// </summary>
        public Guid WorkflowPropertyId { get; }

        /// <inheritdoc/>
        public override bool Equals(DataReference other)
        {
            return base.Equals(other)
                && other is WorkflowPropertyReference wpr
                && wpr.WorkflowPropertyId == WorkflowPropertyId;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = base.GetHashCode();
                hash = hash * 23 + WorkflowPropertyId.GetHashCode();
                return hash;
            }
        }

        internal static WorkflowPropertyReference ParseFromStorage(Storage.DOM.DataReferenceStorage reference, string nodeId)
        {
            if (reference.ReferenceData == null || !reference.ReferenceData.TryGetValue(WorkflowPropertyIdKey, out var raw))
            {
                return null;
            }

            return Guid.TryParse(raw, out var id) ? new WorkflowPropertyReference(id, nodeId) : null;
        }

        internal override Dictionary<string, string> BuildReferenceData()
        {
            var data = base.BuildReferenceData() ?? new Dictionary<string, string>();
            data[WorkflowPropertyIdKey] = WorkflowPropertyId.ToString();
            return data;
        }
    }
}
