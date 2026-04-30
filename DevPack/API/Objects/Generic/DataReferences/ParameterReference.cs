namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Common base class for references that target a parameter (capability, capacity or configuration)
    /// on a workflow node.
    /// </summary>
    public abstract class ParameterReference : DataReference
    {
        internal const string ParameterIdKey = "ParameterId";

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterReference"/> class.
        /// </summary>
        /// <param name="type">The concrete reference type.</param>
        /// <param name="parameterId">The unique identifier of the parameter.</param>
        /// <param name="nodeId">
        /// Optional identifier of the workflow node whose parameter is referenced.
        /// When <see langword="null"/> the reference targets the parameter on the current node.
        /// </param>
        protected ParameterReference(DataReferenceType type, Guid parameterId, string nodeId)
            : base(type, nodeId)
        {
            ParameterId = parameterId;
        }

        /// <summary>
        /// Gets the unique identifier of the parameter.
        /// </summary>
        public Guid ParameterId { get; }

        /// <inheritdoc/>
        public override bool Equals(DataReference other)
        {
            return base.Equals(other)
                && other is ParameterReference pr
                && pr.ParameterId == ParameterId;
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

        internal override Dictionary<string, string> BuildReferenceData()
        {
            var data = base.BuildReferenceData() ?? new Dictionary<string, string>();
            data[ParameterIdKey] = ParameterId.ToString();
            return data;
        }
    }
}
