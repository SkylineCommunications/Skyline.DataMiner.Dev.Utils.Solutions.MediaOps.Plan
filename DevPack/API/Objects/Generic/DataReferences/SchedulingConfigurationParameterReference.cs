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
		public SchedulingConfigurationParameterReference(Guid parameterId) : base(DataReferenceType.SchedulingConfigurationParameter)
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
			return other is SchedulingConfigurationParameterReference scpr && scpr.ParameterId == ParameterId;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 17;
				hash = hash * 23 + Type.GetHashCode();
				hash = hash * 23 + ParameterId.GetHashCode();
				return hash;
			}
		}

		internal static SchedulingConfigurationParameterReference ParseFromStorage(Storage.DOM.DataReference reference)
		{
			if (reference.ReferenceData == null || !reference.ReferenceData.TryGetValue(ParameterIdKey, out var raw))
			{
				return null;
			}

			return Guid.TryParse(raw, out var id) ? new SchedulingConfigurationParameterReference(id) : null;
		}

		internal override Storage.DOM.DataReference ToStorage()
		{
			return new Storage.DOM.DataReference
			{
				ReferenceType = Type.ToString(),
				ReferenceData = new Dictionary<string, string>
				{
					[ParameterIdKey] = ParameterId.ToString(),
				},
			};
		}
	}
}
