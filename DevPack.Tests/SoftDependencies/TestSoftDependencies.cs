namespace RT_MediaOps.Plan.SoftDependencies
{
    using System;
    using System.Net;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using DMConnection = Skyline.DataMiner.Net.Connection;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class TestSoftDependencies
    {
        [TestMethod]
        public void TestCreateResource()
        {
            var credentials = CredentialCache.DefaultNetworkCredentials;

            Console.WriteLine("Connecting to DataMiner...");
            DMConnection connection = ConnectionSettings.GetConnection("slc-h67-g02.skyline.local");
            connection.Authenticate(credentials.UserName, credentials.Password, credentials.Domain);
            Console.WriteLine("Connected to DataMiner\r\n");

            var api = new MediaOpsPlanApi(connection);

            var resource = new UnmanagedResource { Name = $"Green Resource [{Guid.NewGuid()}]" };
            api.Resources.Create(resource);
            Assert.AreNotEqual(Guid.Empty, resource.Id);
            Assert.AreEqual(resource.Id, resource.Id);
        }
    }
}
