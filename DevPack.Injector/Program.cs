namespace MediaOps_Injector
{
	using System;
	using System.Net;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Extensions;

	using DMConnection = Skyline.DataMiner.Net.Connection;

	internal class Program
	{
		static void Main(string[] args)
		{
			var credentials = CredentialCache.DefaultNetworkCredentials;

			Console.WriteLine("Connecting to DataMiner...");
			DMConnection connection = ConnectionSettings.GetConnection("slc-h62-g04.skyline.local");
			connection.Authenticate(credentials.UserName, credentials.Password, credentials.Domain);
			Console.WriteLine("Connected to DataMiner\r\n");

			var api = connection.GetMediaOpsPlanApi();

			var resource = new UnmanagedResource()
			{
				Name = "Injected Resource",
				Concurrency = 1,
			};

			api.Resources.Create(resource);

			var r = api.Resources.Read(resource.ID);
			r.Name = "Updated Injected Resource";

			api.Resources.Update(r);
		}
	}
}
