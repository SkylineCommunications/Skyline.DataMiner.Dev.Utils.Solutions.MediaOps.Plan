namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;

	internal partial class ResourceInternalPropertiesSection
	{
		private ResourceMetadata resourceMetadata;

		internal IEnumerable<Guid> PoolIds
		{
			get
			{
				if (string.IsNullOrWhiteSpace(Pool_Ids))
				{
					return Enumerable.Empty<Guid>();
				}

				return Pool_Ids.Split([";"], StringSplitOptions.RemoveEmptyEntries).Select(x => Guid.Parse(x));
			}

			set
			{
				if (value == null || !value.Any())
				{
					Pool_Ids = string.Empty;
				}
				else
				{
					Pool_Ids = string.Join(";", value.Select(x => x.ToString()));
				}
			}
		}

		internal ResourceMetadata Metadata
		{
			get
			{
				if (resourceMetadata != null)
				{
					return resourceMetadata;
				}

				if (SlcResource_Studio.ResourceMetadata.TryDeserialize(ResourceMetadata, out resourceMetadata))
				{
					return resourceMetadata;
				}

				resourceMetadata = new ResourceMetadata();
				return resourceMetadata;
			}

			set
			{
				resourceMetadata = value;
			}
		}

		internal void ApplyChanges()
		{
			if (resourceMetadata != null)
			{
				ResourceMetadata = resourceMetadata.Serialize();
			}
		}
	}

	internal class ResourceMetadata
	{
		public string LinkedElementInfo { get; set; }

		public string LinkedServiceInfo { get; set; }

		public Guid LinkedFunctionId { get; set; }

		public string LinkedFunctionTableIndex { get; set; }

		public static bool TryDeserialize(string json, out ResourceMetadata resourceMetadata)
		{
			resourceMetadata = null;

			if (String.IsNullOrEmpty(json))
			{
				return false;
			}

			try
			{
				resourceMetadata = SecureNewtonsoftDeserialization.DeserializeObject<ResourceMetadata>(json);
				return true;
			}
			catch (JsonException)
			{
				// Handle JSON parsing errors if necessary
				return false;
			}
		}

		public string Serialize()
		{
			return JsonConvert.SerializeObject(this);
		}
	}
}
