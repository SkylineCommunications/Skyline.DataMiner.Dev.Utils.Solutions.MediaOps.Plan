namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Tools
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    internal static class DataMinerAgentHelper
    {
        private static readonly string[] DataMinerProcessNames = new[]
        {
            "DataMiner",
            "SLAutomation",
            "SLScripting",
        };

        public static bool IsRunningOnDataMinerAgent()
        {
            string currentProcessName = Process.GetCurrentProcess().ProcessName;
            return DataMinerProcessNames.Any(x => currentProcessName.StartsWith(x, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
