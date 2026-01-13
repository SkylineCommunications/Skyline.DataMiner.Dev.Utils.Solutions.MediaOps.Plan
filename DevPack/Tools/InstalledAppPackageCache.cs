namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.AppPackages;
    using Skyline.DataMiner.Net.AppPackages.Messages;

    internal class InstalledAppPackageCache
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

        private readonly IConnection _connection;

        private readonly object _lock = new();
        private readonly Dictionary<string, InstalledAppInfo> _cache = new(StringComparer.OrdinalIgnoreCase);

        private DateTime _lastLoadUtc = DateTime.MinValue;

        public InstalledAppPackageCache(IConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public IEnumerable<InstalledAppInfo> GetAllInstalledPackages()
        {
            lock (_lock)
            {
                EnsureCacheLoaded();

                return _cache.Values
                    .Where(x => x.InstallState.InstallStatus == AppInstallStatus.INSTALLED)
                    .ToList();
            }
        }

        public bool IsInstalled(string appPackageName, out InstalledAppInfo installedAppInfo)
        {
            if (String.IsNullOrWhiteSpace(appPackageName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(appPackageName));
            }

            lock (_lock)
            {
                EnsureCacheLoaded();

                return _cache.TryGetValue(appPackageName, out installedAppInfo) &&
                       installedAppInfo.InstallState.InstallStatus == AppInstallStatus.INSTALLED;

            }
        }

        public bool IsInstalled(string appPackageName)
        {
            return IsInstalled(appPackageName, out _);
        }

        public void Refresh()
        {
            lock (_lock)
            {
                LoadInstalledAppPackages();
            }
        }

        private void EnsureCacheLoaded()
        {
            var now = DateTime.UtcNow;

            if (now - _lastLoadUtc < CacheDuration)
            {
                return;
            }

            LoadInstalledAppPackages();
        }

        private void LoadInstalledAppPackages()
        {
            var request = new GetInstalledAppPackagesRequest();
            var response = (GetInstalledAppPackagesResponse)_connection.HandleSingleResponseMessage(request);

            _cache.Clear();

            foreach (var appPackage in response.InstalledAppPackages)
            {
                _cache[appPackage.AppInfo.Name] = appPackage;
            }

            _lastLoadUtc = DateTime.UtcNow;
        }
    }
}
