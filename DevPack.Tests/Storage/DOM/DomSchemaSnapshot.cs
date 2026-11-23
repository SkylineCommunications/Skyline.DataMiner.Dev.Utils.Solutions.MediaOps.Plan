namespace RT_MediaOps.Plan.Storage.DOM
{
	using System.Security.Cryptography;
	using System.Text;
	using System.Text.RegularExpressions;

	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;

	internal static class DomSchemaSnapshot
	{
		internal static string Canonicalize(string json)
		{
			var value = JObject.Parse(json);
			// These belong to the platform, not the declared application schema.
			foreach (var name in new[] { "CreatedAt", "CreatedBy", "LastModified", "LastModifiedBy", "TrackLastModified", "SyncLastModified" })
			{
				value.Remove(name);
			}

			return Normalize(value).ToString(Formatting.None);
		}

		internal static string Hash(string json)
		{
			using var sha = SHA256.Create();
			return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Canonicalize(json)))).Replace("-", String.Empty).ToLowerInvariant();
		}

		private static JToken Normalize(JToken value)
		{
			if (value is JObject obj)
			{
				// Collection implementation/assembly versions are not schema metadata.
				// Keep the concrete descriptor/action/condition type and every schema property.
				if (obj.Property("$values") is JProperty values)
				{
					return Normalize(values.Value);
				}

				var result = new JObject();
				foreach (var property in obj.Properties().OrderBy(x => x.Name, StringComparer.Ordinal))
				{
					result.Add(property.Name, property.Name is "$type" or "FieldType"
						? new JValue(Regex.Replace((string)property.Value!, @", Version=[^,\]]+|, Culture=[^,\]]+|, PublicKeyToken=[^,\]]+", String.Empty))
						: Normalize(property.Value));
				}

				return result;
			}

			if (value is JArray array)
			{
				return new JArray(array.Select(Normalize));
			}

			return value.DeepClone();
		}
	}
}
