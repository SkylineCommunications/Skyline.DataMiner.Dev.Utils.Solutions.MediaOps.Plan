namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Helper;

	/// <summary>
	/// Lightweight record of the content of a <see cref="PropertySettingCollection"/>, used to detect changes without copying the settings (and their file content).
	/// </summary>
	internal sealed class PropertySettingsSnapshot
	{
		private readonly List<object> entries;

		private PropertySettingsSnapshot(List<object> entries)
		{
			this.entries = entries;
		}

		internal static PropertySettingsSnapshot Capture(PropertySettingCollection collection)
			=> new PropertySettingsSnapshot(ToEntries(collection));

		internal bool Matches(PropertySettingCollection collection)
			=> entries.ScrambledEquals(ToEntries(collection));

		private static List<object> ToEntries(PropertySettingCollection collection)
		{
			var result = new List<object>(collection.Count);
			result.AddRange(collection.CustomSettings.Select(x => (object)("custom", x.Name, x.Value)));
			result.AddRange(collection.StringSettings.Select(x => (object)("string", x.Id, x.Value)));
			result.AddRange(collection.BooleanSettings.Select(x => (object)("boolean", x.Id, x.Value)));
			result.AddRange(collection.DiscreteSettings.Select(x => (object)("discrete", x.Id, x.Value)));
			result.AddRange(collection.FileSettings.Select(x => (object)new FileEntry(x)));
			return result;
		}

		private sealed class FileEntry
		{
			private readonly Guid id;
			private readonly string[] files;
			private readonly string[] filesToDelete;
			private readonly KeyValuePair<string, byte[]>[] filesToUpload;

			internal FileEntry(FilePropertySetting setting)
			{
				id = setting.Id;
				files = setting.Files.OrderBy(x => x, StringComparer.Ordinal).ToArray();
				filesToDelete = setting.FilesToDelete.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
				filesToUpload = setting.FilesToUpload.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToArray();
			}

			public override bool Equals(object obj)
			{
				// AddFile always stores a new array, so replaced content shows up as a different reference.
				return obj is FileEntry other
					&& id == other.id
					&& files.SequenceEqual(other.files, StringComparer.Ordinal)
					&& filesToDelete.SequenceEqual(other.filesToDelete, StringComparer.OrdinalIgnoreCase)
					&& filesToUpload.Length == other.filesToUpload.Length
					&& filesToUpload.Zip(other.filesToUpload, (x, y) => String.Equals(x.Key, y.Key, StringComparison.OrdinalIgnoreCase) && ReferenceEquals(x.Value, y.Value)).All(x => x);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (id.GetHashCode() * 23) + files.Length;
				}
			}
		}
	}
}
