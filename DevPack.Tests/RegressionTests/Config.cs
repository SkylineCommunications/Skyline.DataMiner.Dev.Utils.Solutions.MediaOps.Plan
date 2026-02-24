namespace RT_MediaOps.Plan.RegressionTests
{
    using System;
    using System.Net;
    using System.Reflection;

    using Microsoft.Extensions.Configuration;

    public class Config
    {
        private Config(IConfiguration configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
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

        public static Config Load()
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets(Assembly.GetExecutingAssembly())
                .AddEnvironmentVariables();

            return new Config(builder.Build());
        }
    }
}
