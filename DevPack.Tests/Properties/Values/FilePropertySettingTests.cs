namespace RT_MediaOps.Plan.Properties.Values
{
	using System;
	using System.Linq;
	using System.Text;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class FilePropertySettingTests
	{
		private static byte[] Content(string value) => Encoding.UTF8.GetBytes(value);

		[TestMethod]
		public void Constructor_SetsPropertyId()
		{
			var id = Guid.NewGuid();

			var setting = new FilePropertySetting(new FileProperty(id));

			Assert.AreEqual(id, setting.Id);
		}

		[TestMethod]
		public void Constructor_NullProperty_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new FilePropertySetting((FileProperty)null!));
		}

		[TestMethod]
		public void NewSetting_HasNoValue()
		{
			var setting = new FilePropertySetting(new FileProperty());

			Assert.IsFalse(setting.HasValue);
			Assert.AreEqual(0, setting.Files.Count);
		}

		[TestMethod]
		public void AddFile_FileIsTrackedForUpload()
		{
			var setting = new FilePropertySetting(new FileProperty());

			setting.AddFile("document.pdf", Content("abc"));

			Assert.IsTrue(setting.HasValue);
			CollectionAssert.AreEqual(new[] { "document.pdf" }, setting.Files.ToArray());
		}

		[TestMethod]
		public void AddFile_StripsDirectoryInformation()
		{
			var setting = new FilePropertySetting(new FileProperty());

			setting.AddFile(@"C:\temp\..\document.pdf", Content("abc"));

			CollectionAssert.AreEqual(new[] { "document.pdf" }, setting.Files.ToArray());
		}

		[TestMethod]
		public void AddFile_SameNameTwice_KeepsSingleEntry()
		{
			var setting = new FilePropertySetting(new FileProperty());

			setting.AddFile("document.pdf", Content("first"));
			setting.AddFile("document.pdf", Content("second"));

			Assert.AreEqual(1, setting.Files.Count);
		}

		[TestMethod]
		public void AddFile_NullContent_Throws()
		{
			var setting = new FilePropertySetting(new FileProperty());

			Assert.ThrowsException<ArgumentNullException>(() => setting.AddFile("document.pdf", null!));
		}

		[TestMethod]
		public void AddFile_EmptyName_Throws()
		{
			var setting = new FilePropertySetting(new FileProperty());

			Assert.ThrowsException<ArgumentException>(() => setting.AddFile(" ", Content("abc")));
		}

		[TestMethod]
		public void RemoveFile_RemovesFile()
		{
			var setting = new FilePropertySetting(new FileProperty());
			setting.AddFile("document.pdf", Content("abc"));

			setting.RemoveFile("document.pdf");

			Assert.IsFalse(setting.HasValue);
			Assert.AreEqual(0, setting.Files.Count);
		}

		[TestMethod]
		public void RemoveFile_UnknownFile_DoesNothing()
		{
			var setting = new FilePropertySetting(new FileProperty());
			setting.AddFile("document.pdf", Content("abc"));

			setting.RemoveFile("other.pdf");

			CollectionAssert.AreEqual(new[] { "document.pdf" }, setting.Files.ToArray());
		}

		[TestMethod]
		public void ClearFiles_RemovesAllFiles()
		{
			var setting = new FilePropertySetting(new FileProperty());
			setting.AddFile("first.pdf", Content("abc"));
			setting.AddFile("second.pdf", Content("def"));

			setting.ClearFiles();

			Assert.AreEqual(0, setting.Files.Count);
		}

		[TestMethod]
		public void Equals_SameFiles_ReturnsTrue()
		{
			var property = new FileProperty(Guid.NewGuid());

			var first = new FilePropertySetting(property).AddFile("a.pdf", Content("a"));
			var second = new FilePropertySetting(property).AddFile("a.pdf", Content("a"));

			Assert.AreEqual(first, second);
			Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
		}

		[TestMethod]
		public void Equals_DifferentFiles_ReturnsFalse()
		{
			var property = new FileProperty(Guid.NewGuid());

			var first = new FilePropertySetting(property).AddFile("a.pdf", Content("a"));
			var second = new FilePropertySetting(property).AddFile("b.pdf", Content("a"));

			Assert.AreNotEqual(first, second);
		}

		[TestMethod]
		public void Equals_IgnoresFileOrder()
		{
			var property = new FileProperty(Guid.NewGuid());

			var first = new FilePropertySetting(property).AddFile("a.pdf", Content("a")).AddFile("b.pdf", Content("b"));
			var second = new FilePropertySetting(property).AddFile("b.pdf", Content("b")).AddFile("a.pdf", Content("a"));

			Assert.AreEqual(first, second);
		}
	}
}
