namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System;
	using System.Collections.Generic;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Tools;
	using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;

	[JsonObject(MemberSerialization.OptIn)]
	internal class DataReference : IEquatable<DataReference>
	{
		[JsonProperty("referenceType")]
		public string ReferenceType { get; set; }

		[JsonProperty("referenceData")]
		public Dictionary<string, string> ReferenceData { get; set; }

		public string Serialize()
		{
			return JsonConvert.SerializeObject(this);
		}

		public static bool TryDeserialize(string json, out DataReference reference)
		{
			if (!String.IsNullOrEmpty(json))
			{
				try
				{
					reference = SecureNewtonsoftDeserialization.DeserializeObject<DataReference>(json);
					return true;
				}
				catch (JsonException)
				{
					// Handle JSON parsing errors if necessary
				}
			}

			reference = null;
			return false;
		}

		public bool Equals(DataReference other)
		{
			if (other == null)
			{
				return false;
			}

			if (ReferenceType != other.ReferenceType)
			{
				return false;
			}

			if (!DictionaryComparer<string, string>.Default.Equals(ReferenceData, other.ReferenceData))
			{
				return false;
			}

			return true;
		}

		public override bool Equals(object obj) => Equals(obj as DataReference);

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + (ReferenceType != null ? ReferenceType.GetHashCode() : 0);
				hash = (hash * 23) + (DictionaryComparer<string, string>.Default.GetHashCode(ReferenceData));

				return hash;
			}
		}
	}
}
