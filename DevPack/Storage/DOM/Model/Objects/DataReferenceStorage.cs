namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System;
	using System.Collections.Generic;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Tools;
	using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;

	[JsonObject(MemberSerialization.OptIn)]
	internal class DataReferenceStorage : IEquatable<DataReferenceStorage>
	{
		[JsonProperty("referenceType")]
		public string ReferenceType { get; set; }

		[JsonProperty("referenceData")]
		public Dictionary<string, string> ReferenceData { get; set; }

		public static DataReferenceStorage FromDataReference(DataReference reference)
		{
			if (reference == null)
			{
				return null;
			}

			return new DataReferenceStorage
			{
				ReferenceType = reference.Type.ToString(),
				ReferenceData = reference.BuildReferenceData(),
			};
		}

		public DataReference ToDataReference()
		{
			if (!Enum.TryParse<DataReferenceType>(ReferenceType, out var type))
			{
				return null;
			}

			var nodeId = DataReference.ReadNodeId(this);

			switch (type)
			{
				case DataReferenceType.ResourceName: return new ResourceNameReference(nodeId);
				case DataReferenceType.ResourceLinkedObjectID: return new ResourceLinkedObjectIdReference(nodeId);
				case DataReferenceType.ResourceProperty: return ResourcePropertyReference.ParseFromStorage(this, nodeId);
				case DataReferenceType.CapabilityParameter: return CapabilityParameterReference.ParseFromStorage(this, nodeId);
				case DataReferenceType.CapacityParameter: return CapacityParameterReference.ParseFromStorage(this, nodeId);
				case DataReferenceType.ConfigurationParameter: return ConfigurationParameterReference.ParseFromStorage(this, nodeId);
				case DataReferenceType.WorkflowName: return new WorkflowNameReference(nodeId);
				case DataReferenceType.WorkflowProperty: return WorkflowPropertyReference.ParseFromStorage(this, nodeId);
				default: return null;
			}
		}

		public string Serialize()
		{
			return JsonConvert.SerializeObject(this);
		}

		public static bool TryDeserialize(string json, out DataReferenceStorage reference)
		{
			if (!String.IsNullOrEmpty(json))
			{
				try
				{
					reference = SecureNewtonsoftDeserialization.DeserializeObject<DataReferenceStorage>(json);
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

		public bool Equals(DataReferenceStorage other)
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

		public override bool Equals(object obj) => Equals(obj as DataReferenceStorage);

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
