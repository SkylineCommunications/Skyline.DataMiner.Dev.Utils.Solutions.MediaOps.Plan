namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Common base class for references that target a parameter (capability, capacity or configuration)
	/// on a workflow node.
	/// </summary>
	/// <seealso cref="CapabilityParameterReference"/>
	/// <seealso cref="CapacityParameterReference"/>
	/// <seealso cref="ConfigurationParameterReference"/>
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

		/// <summary>
		/// Determines whether this reference targets a capability and, if so, returns it as a <see cref="CapabilityParameterReference"/>.
		/// </summary>
		/// <param name="reference">When this method returns, contains the current reference as a <see cref="CapabilityParameterReference"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this reference is a <see cref="CapabilityParameterReference"/>; otherwise, <c>false</c>.</returns>
		public bool IsCapabilityParameterReference(out CapabilityParameterReference reference)
		{
			reference = this as CapabilityParameterReference;
			return reference != null;
		}

		/// <summary>
		/// Determines whether this reference targets a capacity and, if so, returns it as a <see cref="CapacityParameterReference"/>.
		/// </summary>
		/// <param name="reference">When this method returns, contains the current reference as a <see cref="CapacityParameterReference"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this reference is a <see cref="CapacityParameterReference"/>; otherwise, <c>false</c>.</returns>
		public bool IsCapacityParameterReference(out CapacityParameterReference reference)
		{
			reference = this as CapacityParameterReference;
			return reference != null;
		}

		/// <summary>
		/// Determines whether this reference targets a configuration and, if so, returns it as a <see cref="ConfigurationParameterReference"/>.
		/// </summary>
		/// <param name="reference">When this method returns, contains the current reference as a <see cref="ConfigurationParameterReference"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this reference is a <see cref="ConfigurationParameterReference"/>; otherwise, <c>false</c>.</returns>
		public bool IsConfigurationParameterReference(out ConfigurationParameterReference reference)
		{
			reference = this as ConfigurationParameterReference;
			return reference != null;
		}

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

		/// <inheritdoc/>
		public override string ToString()
		{
			return NodeId != null
				? $"{Type} (ParameterId: {ParameterId}, NodeId: {NodeId})"
				: $"{Type} (ParameterId: {ParameterId})";
		}

		internal override Dictionary<string, string> BuildReferenceData()
		{
			var data = base.BuildReferenceData() ?? new Dictionary<string, string>();
			data[ParameterIdKey] = ParameterId.ToString();
			return data;
		}
	}
}
