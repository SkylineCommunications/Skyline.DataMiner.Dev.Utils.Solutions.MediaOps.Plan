namespace RT_MediaOps.Plan.RegressionTests
{
	using System;
	using System.Net;
	using System.Reflection;

	using Microsoft.Extensions.Configuration;
	using Skyline.DataMiner.CICD.Tools.WinEncryptedKeys.Lib;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Tools;

	public class Config
	{
		private Config(IConfiguration configuration)
		{
			if (configuration is null)
			{
				throw new ArgumentNullException(nameof(configuration));
			}

			if (TryLoadQaOpsCredentials(out var qaOpsConfig))
			{
				Username = qaOpsConfig.Username;
				Password = qaOpsConfig.Password;
				Domain = qaOpsConfig.Domain;
				BaseUrl = qaOpsConfig.BaseUrl;
				return;
			}

			// To set the credentials prefix locally, use the following command from the 'DevPack.Tests' folder:
			// dotnet user-secrets set "CRED_PREFIX" "DATAMINER"
			var prefixCredentials = configuration["CRED_PREFIX"];

			if (prefixCredentials is null)
			{
				var credentials = CredentialCache.DefaultNetworkCredentials;
				Username = credentials.UserName;
				Password = credentials.Password;
				Domain = credentials.Domain;
				BaseUrl = configuration["DATAMINER_HOST"] ?? "localhost";
			}
			else
			{
				// To set the username locally, use the following command from the 'DevPack.Tests' folder:
				// dotnet user-secrets set "DATAMINER_USERNAME" "your_username"
				Username = configuration[prefixCredentials + "_USERNAME"] ?? throw new ArgumentException("Unable to retrieve the DATAMINER_USERNAME environment variable");

				// To set the password locally, use the following command from the 'DevPack.Tests' folder:
				// dotnet user-secrets set "DATAMINER_PASSWORD" "your_password"
				Password = configuration[prefixCredentials + "_PASSWORD"] ?? throw new ArgumentException("Unable to retrieve the DATAMINER_PASSWORD environment variable");

				Domain = configuration[prefixCredentials + "_DOMAIN"] ?? string.Empty;

				BaseUrl = configuration[prefixCredentials + "_HOST"] ?? "localhost";
			}
		}

		public string BaseUrl { get; }

		public string Username { get; }

		public string Password { get; }

		public string Domain { get; }

		public static bool IsQaOps { get; private set; }

		public static Config Load()
		{
			var builder = new ConfigurationBuilder()
				.AddUserSecrets(Assembly.GetExecutingAssembly())
				.AddEnvironmentVariables();

			return new Config(builder.Build());
		}

		private static bool TryLoadQaOpsCredentials(out QaOpsConfig config)
		{
			config = default;

			if (!Keys.TryRetrieveKey("BridgeId", out var bridgeId) || string.IsNullOrWhiteSpace(bridgeId))
			{
				return false;
			}

			if (!Keys.TryRetrieveKey("QAOpsDataMinerUser", out var userName) || string.IsNullOrWhiteSpace(userName))
			{
				throw new InvalidOperationException("QAOps Bridge detected, but encrypted key 'QAOpsDataMinerUser' is missing.");
			}

			if (!Keys.TryRetrieveKey("QAOpsDataMinerPassword", out var password) || string.IsNullOrWhiteSpace(password))
			{
				throw new InvalidOperationException("QAOps Bridge detected, but encrypted key 'QAOpsDataMinerPassword' is missing.");
			}

			IsQaOps = true;
			DataMinerAgentHelper.UseInMemoryLocksForCurrentProcess();
			config = new QaOpsConfig("localhost", userName, password, string.Empty);
			return true;
		}

		private readonly struct QaOpsConfig
		{
			public QaOpsConfig(string baseUrl, string username, string password, string domain)
			{
				BaseUrl = baseUrl;
				Username = username;
				Password = password;
				Domain = domain;
			}

			public string BaseUrl { get; }

			public string Username { get; }

			public string Password { get; }

			public string Domain { get; }
		}
	}
}
