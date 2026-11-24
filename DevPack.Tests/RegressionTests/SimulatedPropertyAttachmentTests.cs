namespace RT_MediaOps.Plan.RegressionTests
{
	using System;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class SimulatedPropertyAttachmentTests
	{
		[TestMethod]
		public void FileSettings_AreSharedAcrossSimulatedApiHelpers()
		{
			var context = TestContextManager.SharedTestContext;
			if (context.UseRealDma)
			{
				Assert.Inconclusive("This test verifies the simulated regression-test context.");
			}

			using var creator = new TestObjectCreator(context);
			var property = creator.CreateProperty(new FileProperty(new PropertyData { Scope = "global" })
			{
				Name = $"{Guid.NewGuid()}_File",
				SectionName = "General",
			});
			var collection = new PropertySettingCollection(new PropertySettingCollectionData
			{
				LinkedObjectId = Guid.NewGuid().ToString(),
				Scope = "global",
				SubId = String.Empty,
			});
			collection.Add(new FilePropertySetting(property));

			var created = creator.CreatePropertySettingCollection(collection);
			Assert.AreEqual(0, created.FileSettings.Single().Files.Count);

			var content = new byte[] { 1, 2, 3 };
			created.FileSettings.Single().AddFile("test.txt", content);
			context.Api.PropertySettingCollections.Update(created);

			Task<PropertySettingCollection> readTask;
			using (ExecutionContext.SuppressFlow())
			{
				readTask = Task.Run(() => context.Api.PropertySettingCollections.Read(created.Id));
			}

			var read = readTask.GetAwaiter().GetResult();
			CollectionAssert.AreEqual(content, read.FileSettings.Single().ReadContent("test.txt"));
			read.FileSettings.Single().RemoveFile("test.txt");
			context.Api.PropertySettingCollections.Update(read);

			var attachments = ((MediaOpsPlanApi)context.Api).PropertyAttachments;
			Assert.AreEqual(0, attachments.GetNames(new DomInstanceId(created.Id)).Count);
		}
	}
}
