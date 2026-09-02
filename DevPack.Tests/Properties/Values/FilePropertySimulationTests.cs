namespace RT_MediaOps.Plan.Properties.Values
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	/// <summary>
	/// Deterministic, simulation-backed tests for file property values. The file content is stored through the attachment
	/// API, which cannot be simulated, so a fake attachment store is used to verify what is uploaded and deleted.
	/// </summary>
	[TestClass]
	public sealed class FilePropertySimulationTests
	{
		private const string Scope = "global";

		private static byte[] Content(string value) => Encoding.UTF8.GetBytes(value);

		private static (IMediaOpsPlanApi Api, FakePropertyAttachmentStore Attachments) CreateContext(int? maxDocumentSizeInMegaBytes = null)
		{
			var dms = MediaOpsPlanSimulation.Create();

			if (maxDocumentSizeInMegaBytes.HasValue)
			{
				dms.MaxDocumentSizeInMegaBytes = maxDocumentSizeInMegaBytes.Value;
			}

			var connection = dms.CreateConnection();
			var api = connection.GetMediaOpsPlanApi();

			var attachments = new FakePropertyAttachmentStore();
			((MediaOpsPlanApi)api).PropertyAttachments = attachments;

			return (api, attachments);
		}

		private static FileProperty CreateFileProperty(IMediaOpsPlanApi api, bool allowMultiple = false, bool hasSizeLimit = false, long sizeLimit = 20)
		{
			var property = new FileProperty(new PropertyData { Scope = Scope })
			{
				Name = $"{Guid.NewGuid()}_Prop",
				SectionName = "General",
				AllowMultiple = allowMultiple,
				HasSizeLimit = hasSizeLimit,
				SizeLimit = sizeLimit,
			};

			return (FileProperty)api.Properties.Create(property);
		}

		private static PropertySettingCollection CreateCollection()
		{
			return new PropertySettingCollection(new PropertySettingCollectionData
			{
				LinkedObjectId = $"obj-{Guid.NewGuid()}",
				Scope = Scope,
				SubId = string.Empty,
			});
		}

		[TestMethod]
		public void Create_WithFile_UploadsAttachmentAndStoresFileName()
		{
			var (api, attachments) = CreateContext();
			var property = CreateFileProperty(api);

			var collection = CreateCollection();
			collection.Add(new FilePropertySetting(property).AddFile("document.pdf", Content("hello")));

			var created = api.PropertySettingCollections.Create(collection);

			Assert.AreEqual(1, created.FileSettings.Count, "Expected the file setting to be stored.");
			CollectionAssert.AreEqual(new[] { "document.pdf" }, created.FileSettings.Single().Files.ToArray());

			var attachmentName = $"{property.Id}_document.pdf";
			Assert.IsTrue(attachments.Contains(collection.Id, attachmentName), "Expected the file content to be uploaded as an attachment.");
			CollectionAssert.AreEqual(Content("hello"), attachments.Get(new DomInstanceId(collection.Id), attachmentName));
		}

		[TestMethod]
		public void Read_AfterCreate_ReturnsFileNameWithoutAttachmentPrefix()
		{
			var (api, _) = CreateContext();
			var property = CreateFileProperty(api);

			var collection = CreateCollection();
			collection.Add(new FilePropertySetting(property).AddFile("document.pdf", Content("hello")));
			api.PropertySettingCollections.Create(collection);

			var read = api.PropertySettingCollections.Read(collection.Id);

			Assert.IsNotNull(read);
			CollectionAssert.AreEqual(new[] { "document.pdf" }, read.FileSettings.Single().Files.ToArray());
		}

		[TestMethod]
		public void ReadContent_ReturnsUploadedContent()
		{
			var (api, _) = CreateContext();
			var property = CreateFileProperty(api);

			var collection = CreateCollection();
			collection.Add(new FilePropertySetting(property).AddFile("document.pdf", Content("hello")));
			api.PropertySettingCollections.Create(collection);

			var read = api.PropertySettingCollections.Read(collection.Id);
			var setting = read.FileSettings.Single();

			CollectionAssert.AreEqual(Content("hello"), setting.ReadContent("document.pdf"));
		}

		[TestMethod]
		public void ReadContent_BeforeSaving_ReturnsPendingContent()
		{
			var (api, _) = CreateContext();
			var property = CreateFileProperty(api);

			var setting = new FilePropertySetting(property).AddFile("document.pdf", Content("hello"));

			CollectionAssert.AreEqual(Content("hello"), setting.ReadContent("document.pdf"), "Expected content that is not stored yet to be returned from memory.");
		}

		[TestMethod]
		public void ReadContent_UnknownFile_Throws()
		{
			var (api, _) = CreateContext();
			var property = CreateFileProperty(api);

			var setting = new FilePropertySetting(property).AddFile("document.pdf", Content("hello"));

			Assert.ThrowsException<ArgumentException>(() => setting.ReadContent("other.pdf"));
		}

		[TestMethod]
		public void ReadContent_OnReturnedCollection_UsesStoredContent()
		{
			var (api, attachments) = CreateContext();
			var property = CreateFileProperty(api);

			var collection = CreateCollection();
			collection.Add(new FilePropertySetting(property).AddFile("document.pdf", Content("hello")));

			var created = api.PropertySettingCollections.Create(collection);

			// The pending content is cleared once it is stored, so this has to come from the attachment.
			CollectionAssert.AreEqual(Content("hello"), created.FileSettings.Single().ReadContent("document.pdf"));
			Assert.IsTrue(attachments.Contains(collection.Id, $"{property.Id}_document.pdf"));
		}

		[TestMethod]
		public void Update_RemovingFile_DeletesAttachment()
		{
			var (api, attachments) = CreateContext();
			var property = CreateFileProperty(api);

			var collection = CreateCollection();
			collection.Add(new FilePropertySetting(property).AddFile("document.pdf", Content("hello")));
			api.PropertySettingCollections.Create(collection);

			var read = api.PropertySettingCollections.Read(collection.Id);
			read.FileSettings.Single().RemoveFile("document.pdf");
			var updated = api.PropertySettingCollections.Update(read);

			Assert.AreEqual(0, updated.FileSettings.Single().Files.Count, "Expected the file to be removed from the property.");
			Assert.IsFalse(attachments.Contains(collection.Id, $"{property.Id}_document.pdf"), "Expected the attachment to be deleted.");
		}

		[TestMethod]
		public void Update_RetainingSettingsOfSameCollection_KeepsAttachmentsWithoutReupload()
		{
			var (api, attachments) = CreateContext();
			var property = CreateFileProperty(api);

			var collection = CreateCollection();
			collection.Add(new FilePropertySetting(property).AddFile("document.pdf", Content("hello")));
			api.PropertySettingCollections.Create(collection);

			var read = api.PropertySettingCollections.Read(collection.Id);
			read.SetPropertySettings(read.PropertySettings.ToList());

			var updated = api.PropertySettingCollections.Update(read);

			CollectionAssert.AreEqual(new[] { "document.pdf" }, updated.FileSettings.Single().Files.ToArray());
			CollectionAssert.AreEqual(Content("hello"), updated.FileSettings.Single().ReadContent("document.pdf"));
			Assert.IsTrue(attachments.Contains(collection.Id, $"{property.Id}_document.pdf"));
		}

		[TestMethod]
		public void Create_CopyingSettingFromOtherCollection_CopiesStoredContent()
		{
			var (api, attachments) = CreateContext();
			var property = CreateFileProperty(api);

			var source = CreateCollection();
			source.Add(new FilePropertySetting(property).AddFile("document.pdf", Content("hello")));
			api.PropertySettingCollections.Create(source);

			var readSource = api.PropertySettingCollections.Read(source.Id);

			var destination = CreateCollection();
			destination.Add(readSource.FileSettings.Single());
			var created = api.PropertySettingCollections.Create(destination);

			Assert.IsTrue(
				attachments.Contains(destination.Id, $"{property.Id}_document.pdf"),
				"Expected the stored content to be copied to the destination collection.");
			CollectionAssert.AreEqual(Content("hello"), created.FileSettings.Single().ReadContent("document.pdf"));
		}

		[TestMethod]
		public void Update_ReplacedThenRemovedFile_DeletesStoredAttachment()
		{
			var (api, attachments) = CreateContext();
			var property = CreateFileProperty(api);

			var collection = CreateCollection();
			collection.Add(new FilePropertySetting(property).AddFile("document.pdf", Content("hello")));
			api.PropertySettingCollections.Create(collection);

			var read = api.PropertySettingCollections.Read(collection.Id);
			var setting = read.FileSettings.Single();

			// Replacing the content and removing it again must still delete the file that is already stored.
			setting.AddFile("document.pdf", Content("replaced"));
			setting.RemoveFile("document.pdf");

			api.PropertySettingCollections.Update(read);

			Assert.IsFalse(attachments.Contains(collection.Id, $"{property.Id}_document.pdf"), "Expected the stored attachment to be deleted.");
		}

		[TestMethod]
		public void Update_RemovingWholeFileSetting_DeletesAttachments()
		{
			var (api, attachments) = CreateContext();
			var property = CreateFileProperty(api);

			var collection = CreateCollection();
			collection.Add(new FilePropertySetting(property).AddFile("document.pdf", Content("hello")));
			api.PropertySettingCollections.Create(collection);

			var read = api.PropertySettingCollections.Read(collection.Id);
			read.Remove(read.FileSettings.Single());

			api.PropertySettingCollections.Update(read);

			Assert.IsFalse(attachments.Contains(collection.Id, $"{property.Id}_document.pdf"), "Expected the attachments of a removed file setting to be deleted.");
		}

		[TestMethod]
		public void Update_ClearingSettings_DeletesAttachments()
		{
			var (api, attachments) = CreateContext();
			var property = CreateFileProperty(api);

			var collection = CreateCollection();
			collection.Add(new FilePropertySetting(property).AddFile("document.pdf", Content("hello")));
			api.PropertySettingCollections.Create(collection);

			var read = api.PropertySettingCollections.Read(collection.Id);
			read.SetPropertySettings(null);

			api.PropertySettingCollections.Update(read);

			Assert.IsFalse(attachments.Contains(collection.Id, $"{property.Id}_document.pdf"), "Expected the attachments to be deleted when the settings are replaced.");
		}

		[TestMethod]
		public void Create_WhenAttachmentUploadFails_ReportsErrorInsteadOfThrowingUnexpectedly()
		{
			var (api, attachments) = CreateContext();
			var property = CreateFileProperty(api);

			attachments.FailOnAdd = true;

			var collection = CreateCollection();
			collection.Add(new FilePropertySetting(property).AddFile("document.pdf", Content("hello")));

			var exception = Assert.ThrowsException<MediaOpsException>(() => api.PropertySettingCollections.Create(collection));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<PropertySettingCollectionInvalidPropertySettingsError>().Any(),
				"Expected the attachment failure to be reported as a structured error.");
		}

		[TestMethod]
		public void Create_MultipleFilesWhileNotAllowed_ThrowsInvalidPropertySettingsError()
		{
			var (api, _) = CreateContext();
			var property = CreateFileProperty(api, allowMultiple: false);

			var collection = CreateCollection();
			collection.Add(new FilePropertySetting(property)
				.AddFile("first.pdf", Content("a"))
				.AddFile("second.pdf", Content("b")));

			var exception = Assert.ThrowsException<MediaOpsException>(() => api.PropertySettingCollections.Create(collection));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<PropertySettingCollectionInvalidPropertySettingsError>().Any(),
				"Expected an invalid property settings error when multiple files are not allowed.");
		}

		[TestMethod]
		public void Create_FileExceedingSizeLimit_ThrowsInvalidPropertySettingsError()
		{
			var (api, _) = CreateContext();
			var property = CreateFileProperty(api, hasSizeLimit: true, sizeLimit: 1);

			var collection = CreateCollection();
			collection.Add(new FilePropertySetting(property).AddFile("big.bin", new byte[2 * 1024 * 1024]));

			var exception = Assert.ThrowsException<MediaOpsException>(() => api.PropertySettingCollections.Create(collection));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<PropertySettingCollectionInvalidPropertySettingsError>().Any(),
				"Expected an invalid property settings error when a file exceeds the size limit.");
		}

		[TestMethod]
		public void Create_FileExceedingServerMaximum_ThrowsFileSizeExceededErrorWithoutUploading()
		{
			var (api, attachments) = CreateContext(maxDocumentSizeInMegaBytes: 1);
			var property = CreateFileProperty(api, hasSizeLimit: false);

			var collection = CreateCollection();
			collection.Add(new FilePropertySetting(property).AddFile("big.bin", new byte[2 * 1024 * 1024]));

			var exception = Assert.ThrowsException<MediaOpsException>(() => api.PropertySettingCollections.Create(collection));

			var error = exception.TraceData.ErrorData.OfType<PropertySettingCollectionFileSizeExceededError>().SingleOrDefault();
			Assert.IsNotNull(error, "Expected a file size exceeded error when the file is larger than the server maximum.");
			Assert.AreEqual("big.bin", error.FileName);
			Assert.AreEqual(2L * 1024 * 1024, error.FileSize);
			Assert.AreEqual(1L * 1024 * 1024, error.MaxFileSize);
			Assert.AreEqual(0, attachments.AddCallCount, "Expected the upload to be blocked by validation.");
		}

		[TestMethod]
		public void Create_UploadRejectedByServerFileSizeLimit_ThrowsFileSizeExceededError()
		{
			var (api, attachments) = CreateContext();
			var property = CreateFileProperty(api);

			attachments.AddException = new DataMinerException(
				"The document to upload is larger than the max configured document size.",
				new ArgumentException("The document to upload is larger than the max configured document size.", "FileSize"));

			var collection = CreateCollection();
			collection.Add(new FilePropertySetting(property).AddFile("document.pdf", Content("hello")));

			var exception = Assert.ThrowsException<MediaOpsException>(() => api.PropertySettingCollections.Create(collection));

			var error = exception.TraceData.ErrorData.OfType<PropertySettingCollectionFileSizeExceededError>().SingleOrDefault();
			Assert.IsNotNull(error, "Expected the server rejection to be translated into a file size exceeded error.");
			Assert.AreEqual("document.pdf", error.FileName);
			Assert.AreEqual(Content("hello").LongLength, error.FileSize);
			Assert.AreEqual(property.Id, error.PropertyId);
		}

		[TestMethod]
		public void CreateProperty_SizeLimitBelowOne_ThrowsInvalidFileSizeLimitError()
		{
			var (api, _) = CreateContext();

			var property = new FileProperty(new PropertyData { Scope = Scope })
			{
				Name = $"{Guid.NewGuid()}_Prop",
				SectionName = "General",
				HasSizeLimit = true,
				SizeLimit = 0,
			};

			var exception = Assert.ThrowsException<MediaOpsException>(() => api.Properties.Create(property));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<PropertyInvalidFileSizeLimitError>().Any(),
				"Expected a PropertyInvalidFileSizeLimitError when the size limit is not positive.");
		}

		[TestMethod]
		public void CreateProperty_SizeLimitAboveServerMaximum_ThrowsInvalidFileSizeLimitError()
		{
			var (api, _) = CreateContext();

			var property = new FileProperty(new PropertyData { Scope = Scope })
			{
				Name = $"{Guid.NewGuid()}_Prop",
				SectionName = "General",
				HasSizeLimit = true,
				SizeLimit = 10000,
			};

			var exception = Assert.ThrowsException<MediaOpsException>(() => api.Properties.Create(property));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<PropertyInvalidFileSizeLimitError>().Any(),
				"Expected a PropertyInvalidFileSizeLimitError when the size limit exceeds the server maximum.");
		}

		[TestMethod]
		public void CreateProperty_WithoutSizeLimit_RoundTripsHasSizeLimit()
		{
			var (api, _) = CreateContext();

			var property = CreateFileProperty(api, hasSizeLimit: false);

			var read = (FileProperty)api.Properties.Read(property.Id);

			Assert.IsFalse(read.HasSizeLimit, "Expected a property without its own size limit to keep using the server limit.");
		}

		private sealed class FakePropertyAttachmentStore : IPropertyAttachmentStore
		{
			private readonly Dictionary<Guid, Dictionary<string, byte[]>> attachments = new Dictionary<Guid, Dictionary<string, byte[]>>();

			public bool FailOnAdd { get; set; }

			public Exception AddException { get; set; }

			public int AddCallCount { get; private set; }

			public void Add(DomInstanceId instanceId, string attachmentName, byte[] content)
			{
				AddCallCount++;

				if (AddException != null)
				{
					throw AddException;
				}

				if (FailOnAdd)
				{
					throw new InvalidOperationException("Simulated upload failure.");
				}

				if (!attachments.TryGetValue(instanceId.Id, out var perInstance))
				{
					perInstance = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
					attachments[instanceId.Id] = perInstance;
				}

				perInstance[attachmentName] = content;
			}

			public byte[] Get(DomInstanceId instanceId, string attachmentName)
			{
				return attachments.TryGetValue(instanceId.Id, out var perInstance) && perInstance.TryGetValue(attachmentName, out var content)
					? content
					: throw new InvalidOperationException($"Attachment '{attachmentName}' was not found.");
			}

			public void Delete(DomInstanceId instanceId, string attachmentName)
			{
				if (attachments.TryGetValue(instanceId.Id, out var perInstance))
				{
					perInstance.Remove(attachmentName);
				}
			}

			public IReadOnlyCollection<string> GetNames(DomInstanceId instanceId)
			{
				return attachments.TryGetValue(instanceId.Id, out var perInstance) ? perInstance.Keys.ToList() : new List<string>();
			}

			public bool Contains(Guid instanceId, string attachmentName)
			{
				return attachments.TryGetValue(instanceId, out var perInstance) && perInstance.ContainsKey(attachmentName);
			}
		}
	}
}
