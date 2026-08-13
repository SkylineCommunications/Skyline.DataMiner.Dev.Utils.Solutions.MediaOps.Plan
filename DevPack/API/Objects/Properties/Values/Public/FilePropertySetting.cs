namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Helper;

	/// <summary>
	/// Represents a property value that holds one or more files.
	/// </summary>
	/// <remarks>
	/// The file content is stored as an attachment on the property value collection. Content that is added or removed is
	/// only persisted when the property value collection is saved.
	/// </remarks>
	public class FilePropertySetting : PropertySetting
	{
		// The file names are stored as a single separated value, so the separator cannot be part of a file name.
		internal const char FileSeparator = '|';

		private static readonly char[] DirectorySeparators = new[] { '/', '\\' };

		// Checked explicitly instead of through Path, because those characters depend on the platform this runs on.
		private static readonly char[] InvalidFileNameCharacters = new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };

		private readonly List<string> files = new List<string>();

		private readonly Dictionary<string, byte[]> filesToUpload = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

		private readonly HashSet<string> filesToDelete = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// The files that are known to be stored as an attachment, so a removal knows whether it has to delete one.
		private readonly HashSet<string> storedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		private MediaOpsPlanApi planApi;

		private Guid settingCollectionId;

		/// <summary>
		/// Initializes a new instance of the <see cref="FilePropertySetting"/> class linked to the specified file property.
		/// </summary>
		/// <param name="property">The <see cref="FileProperty"/> definition to link to.</param>
		public FilePropertySetting(FileProperty property)
			: base(property)
		{
		}

		internal FilePropertySetting()
		{
		}

		internal FilePropertySetting(FilePropertySetting filePropertySetting, Guid destinationCollectionId)
			: base(filePropertySetting)
		{
			files.AddRange(filePropertySetting.files);

			foreach (var fileToUpload in filePropertySetting.filesToUpload)
			{
				filesToUpload[fileToUpload.Key] = fileToUpload.Value;
			}

			foreach (var fileToDelete in filePropertySetting.filesToDelete)
			{
				filesToDelete.Add(fileToDelete);
			}

			CopyStoredFiles(filePropertySetting, destinationCollectionId);
		}

		/// <summary>
		/// Gets the names of the files of this property.
		/// </summary>
		public IReadOnlyCollection<string> Files => files;

		/// <inheritdoc/>
		public override bool HasValue => files.Count > 0;

		internal IReadOnlyDictionary<string, byte[]> FilesToUpload => filesToUpload;

		internal IReadOnlyCollection<string> FilesToDelete => filesToDelete;

		internal IReadOnlyCollection<string> StoredFiles => storedFiles;

		// The attachment holding the content of a file is named after the property it belongs to.
		internal static string GetAttachmentName(Guid propertyId, string fileName)
		{
			return $"{propertyId}_{fileName}";
		}

		/// <summary>
		/// Reads the content of the specified file.
		/// </summary>
		/// <param name="fileName">The name of the file to read.</param>
		/// <returns>The content of the file.</returns>
		/// <remarks>The content of a stored file is retrieved on demand, so it is not held in memory while the file names are used.</remarks>
		/// <exception cref="ArgumentException">Thrown when <paramref name="fileName"/> is <see langword="null"/> or white space, or is not a file of this property.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the property value collection holding this file was never read or saved.</exception>
		public byte[] ReadContent(string fileName)
		{
			var name = NormalizeFileName(fileName);

			// A file that is not stored yet is still held in memory.
			if (filesToUpload.TryGetValue(name, out var content))
			{
				return content;
			}

			if (!files.Contains(name, StringComparer.OrdinalIgnoreCase))
			{
				throw new ArgumentException($"File '{name}' is not a file of this property.", nameof(fileName));
			}

			if (planApi == null || settingCollectionId == Guid.Empty)
			{
				throw new InvalidOperationException("The content can only be read for a property value collection that was read or saved.");
			}

			return planApi.PropertyAttachments.Get(new DomInstanceId(settingCollectionId), GetAttachmentName(Id, name));
		}

		/// <summary>
		/// Adds a file to this property, or replaces the content when a file with the same name was already added.
		/// </summary>
		/// <param name="fileName">The name of the file.</param>
		/// <param name="content">The content of the file.</param>
		/// <returns>This <see cref="FilePropertySetting"/>, so calls can be chained.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="fileName"/> is <see langword="null"/> or white space.</exception>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <see langword="null"/>.</exception>
		public FilePropertySetting AddFile(string fileName, byte[] content)
		{
			var name = NormalizeFileName(fileName);

			if (content == null)
			{
				throw new ArgumentNullException(nameof(content));
			}

			filesToDelete.Remove(name);
			filesToUpload[name] = content;

			if (!files.Contains(name, StringComparer.OrdinalIgnoreCase))
			{
				files.Add(name);
			}

			return this;
		}

		/// <summary>
		/// Removes the specified file from this property.
		/// </summary>
		/// <param name="fileName">The name of the file to remove.</param>
		/// <returns>This <see cref="FilePropertySetting"/>, so calls can be chained.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="fileName"/> is <see langword="null"/> or white space.</exception>
		public FilePropertySetting RemoveFile(string fileName)
		{
			var name = NormalizeFileName(fileName);

			var storedName = files.FirstOrDefault(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
			if (storedName == null)
			{
				return this;
			}

			files.Remove(storedName);
			filesToUpload.Remove(storedName);

			// Only a file that is actually stored has an attachment that must be deleted.
			if (storedFiles.Contains(storedName))
			{
				filesToDelete.Add(storedName);
			}

			return this;
		}

		/// <summary>
		/// Removes all files from this property.
		/// </summary>
		/// <returns>This <see cref="FilePropertySetting"/>, so calls can be chained.</returns>
		public FilePropertySetting ClearFiles()
		{
			foreach (var fileName in files.ToArray())
			{
				RemoveFile(fileName);
			}

			return this;
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = base.GetHashCode();

				foreach (var fileName in files.OrderBy(x => x).ToArray())
				{
					hash = (hash * 23) + (fileName != null ? fileName.GetHashCode() : 0);
				}

				return hash;
			}
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (obj is not FilePropertySetting other)
			{
				return false;
			}

			return base.Equals(other)
				&& files.ScrambledEquals(other.files);
		}

		internal void AddParsedFile(string fileName)
		{
			if (!files.Contains(fileName, StringComparer.OrdinalIgnoreCase))
			{
				files.Add(fileName);
			}

			storedFiles.Add(fileName);
		}

		internal void ClearPendingFileChanges()
		{
			filesToUpload.Clear();
			filesToDelete.Clear();

			// Everything that is left is stored from here on.
			storedFiles.Clear();
			foreach (var fileName in files)
			{
				storedFiles.Add(fileName);
			}
		}

		// Captures where the content of the files is stored, so it can be read on demand.
		internal void SetStorageContext(MediaOpsPlanApi planApi, Guid settingCollectionId)
		{
			this.planApi = planApi;
			this.settingCollectionId = settingCollectionId;
		}

		// A setting that stays in the collection holding its attachments keeps them; anywhere else the content has to travel along.
		private void CopyStoredFiles(FilePropertySetting source, Guid destinationCollectionId)
		{
			if (source.storedFiles.Count == 0)
			{
				return;
			}

			if (source.settingCollectionId == destinationCollectionId)
			{
				foreach (var storedFile in source.storedFiles)
				{
					storedFiles.Add(storedFile);
				}

				SetStorageContext(source.planApi, source.settingCollectionId);
				return;
			}

			foreach (var storedFile in source.storedFiles.Where(x => source.files.Contains(x, StringComparer.OrdinalIgnoreCase) && !filesToUpload.ContainsKey(x)))
			{
				filesToUpload[storedFile] = source.ReadContent(storedFile);
			}
		}

		// The file name is used as an attachment name, so any directory information is stripped off.
		private static string NormalizeFileName(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
			{
				throw new ArgumentException("The file name cannot be null or white space.", nameof(fileName));
			}

			var trimmed = fileName.Trim();

			// Both separators are handled explicitly, because the attachment is stored on the server regardless of the platform this runs on.
			var separatorIndex = trimmed.LastIndexOfAny(DirectorySeparators);
			var name = separatorIndex == -1 ? trimmed : trimmed.Substring(separatorIndex + 1);

			if (string.IsNullOrWhiteSpace(name) || name == "." || name == "..")
			{
				throw new ArgumentException($"'{fileName}' is not a valid file name.", nameof(fileName));
			}

			if (name.IndexOf(FileSeparator) != -1)
			{
				throw new ArgumentException($"The file name cannot contain '{FileSeparator}'.", nameof(fileName));
			}

			if (name.IndexOfAny(InvalidFileNameCharacters) != -1 || name.Any(char.IsControl))
			{
				throw new ArgumentException($"'{fileName}' is not a valid file name.", nameof(fileName));
			}

			return name;
		}
	}
}
