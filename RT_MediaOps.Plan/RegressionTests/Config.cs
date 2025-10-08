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

            BaseUrl = configuration["DATAMINER_HOST"] ?? "slc-h67-g03.skyline.local";
        }

        public string BaseUrl { get; }

        public static Config Load()
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets(Assembly.GetExecutingAssembly());

            return new Config(builder.Build());
        }
    }
}
