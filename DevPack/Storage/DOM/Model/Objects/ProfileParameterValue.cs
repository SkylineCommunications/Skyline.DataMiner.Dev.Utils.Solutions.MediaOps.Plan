namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System;

	using Newtonsoft.Json;

	[JsonObject(MemberSerialization.OptIn)]
	internal class ProfileParameterValue : IEquatable<ProfileParameterValue>
	{
		[JsonProperty("id")]
		public Guid ProfileParameterId { get; set; }

		[JsonProperty("stringValue", NullValueHandling = NullValueHandling.Ignore)]
		public string StringValue { get; set; }

		[JsonProperty("doubleMaxValue", NullValueHandling = NullValueHandling.Ignore)]
		public double? DoubleMaxValue { get; set; }

		[JsonProperty("doubleMinValue", NullValueHandling = NullValueHandling.Ignore)]
		public double? DoubleMinValue { get; set; }

		[JsonProperty("reference", NullValueHandling = NullValueHandling.Ignore)]
		public DataReferenceStorage Reference { get; set; }

		public bool Equals(ProfileParameterValue other)
		{
			if (other == null)
			{
				return false;
			}

			return ProfileParameterId == other.ProfileParameterId
				&& StringValue == other.StringValue
				&& DoubleMaxValue == other.DoubleMaxValue
				&& DoubleMinValue == other.DoubleMinValue
				&& Equals(Reference, other.Reference);
		}

		public override bool Equals(object obj) => Equals(obj as ProfileParameterValue);

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + ProfileParameterId.GetHashCode();
				hash = (hash * 23) + (StringValue != null ? StringValue.GetHashCode() : 0);
				hash = (hash * 23) + (DoubleMaxValue.HasValue ? DoubleMaxValue.Value.GetHashCode() : 0);
				hash = (hash * 23) + (DoubleMinValue.HasValue ? DoubleMinValue.Value.GetHashCode() : 0);
				hash = (hash * 23) + (Reference != null ? Reference.GetHashCode() : 0);
				return hash;
			}
		}
	}
}
