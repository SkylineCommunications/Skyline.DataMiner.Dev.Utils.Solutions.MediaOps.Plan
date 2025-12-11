namespace RT_MediaOps.Plan.RegressionTests
{
    using System;
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

            // To set the username locally, use the following command from the 'DevPack.Tests' folder:
            // dotnet user-secrets set "DATAMINER_USERNAME" "your_username"
            Username = configuration["DATAMINER_USERNAME"] ?? throw new ArgumentException("Unable to retrieve the DATAMINER_USERNAME environment variable");

            // To set the password locally, use the following command from the 'DevPack.Tests' folder:
            // dotnet user-secrets set "DATAMINER_PASSWORD" "your_password"
            Password = configuration["DATAMINER_PASSWORD"] ?? throw new ArgumentException("Unable to retrieve the DATAMINER_PASSWORD environment variable");

            Domain = configuration["DATAMINER_DOMAIN"] ?? string.Empty;

            BaseUrl = configuration["DATAMINER_HOST"] ?? "slc-h67-g03.skyline.local";
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
