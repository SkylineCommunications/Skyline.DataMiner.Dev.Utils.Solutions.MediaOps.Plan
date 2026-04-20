namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents a reference to a scheduling configuration parameter.
	/// </summary>
	public sealed class SchedulingConfigurationParameterReference : DataReference
	{
		private const string ParameterIdKey = "SchedulingConfigurationParameterId";

		/// <summary>
		/// Initializes a new instance of the <see cref="SchedulingConfigurationParameterReference"/> class.
		/// </summary>
		/// <param name="parameterId">The unique identifier of the scheduling configuration parameter.</param>
		/// <param name="nodeId">
		/// Optional identifier of the workflow node whose scheduling configuration parameter is referenced.
		/// When <see langword="null"/> the reference targets the parameter on the current node.
		/// </param>
		public SchedulingConfigurationParameterReference(Guid parameterId, string nodeId = null) : base(DataReferenceType.SchedulingConfigurationParameter, nodeId)
		{
			ParameterId = parameterId;
		}

		/// <summary>
		/// Gets the unique identifier of the scheduling configuration parameter.
		/// </summary>
		public Guid ParameterId { get; }

		/// <inheritdoc/>
		public override bool Equals(DataReference other)
		{
			return base.Equals(other)
				&& other is SchedulingConfigurationParameterReference scpr
				&& scpr.ParameterId == ParameterId;
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

		internal static SchedulingConfigurationParameterReference ParseFromStorage(Storage.DOM.DataReference reference, string nodeId)
		{
			if (reference.ReferenceData == null || !reference.ReferenceData.TryGetValue(ParameterIdKey, out var raw))
			{
				return null;
			}

			return Guid.TryParse(raw, out var id) ? new SchedulingConfigurationParameterReference(id, nodeId) : null;
		}

		private protected override Dictionary<string, string> BuildReferenceData()
		{
			var data = base.BuildReferenceData() ?? new Dictionary<string, string>();
			data[ParameterIdKey] = ParameterId.ToString();
			return data;
		}
	}
}
