namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a reference to a job property (a property defined under the MediaOps scope and
    /// assigned to a job).
    /// </summary>
    public sealed class JobPropertyReference : DataReference
    {
        private const string JobPropertyIdKey = "JobPropertyId";

        /// <summary>
        /// Initializes a new instance of the <see cref="JobPropertyReference"/> class.
        /// </summary>
        /// <param name="jobPropertyId">The unique identifier of the job property definition.</param>
        /// <param name="nodeId">
        /// Optional identifier of the workflow node whose job is referenced.
        /// When <see langword="null"/> the reference targets the job of the current node.
        /// </param>
        public JobPropertyReference(Guid jobPropertyId, string nodeId = null) : base(DataReferenceType.JobProperty, nodeId)
        {
            JobPropertyId = jobPropertyId;
        }

        /// <summary>
        /// Gets the unique identifier of the job property definition.
        /// </summary>
        public Guid JobPropertyId { get; }

        /// <inheritdoc/>
        public override bool Equals(DataReference other)
        {
            return base.Equals(other)
                && other is JobPropertyReference jpr
                && jpr.JobPropertyId == JobPropertyId;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = base.GetHashCode();
                hash = hash * 23 + JobPropertyId.GetHashCode();
                return hash;
            }
        }

        internal static JobPropertyReference ParseFromStorage(Storage.DOM.DataReferenceStorage reference, string nodeId)
        {
            if (reference.ReferenceData == null || !reference.ReferenceData.TryGetValue(JobPropertyIdKey, out var raw))
            {
                return null;
            }

            return Guid.TryParse(raw, out var id) ? new JobPropertyReference(id, nodeId) : null;
        }

        internal override Dictionary<string, string> BuildReferenceData()
        {
            var data = base.BuildReferenceData() ?? new Dictionary<string, string>();
            data[JobPropertyIdKey] = JobPropertyId.ToString();
            return data;
        }
    }
}
