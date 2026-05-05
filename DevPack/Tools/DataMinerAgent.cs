namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Tools
{
	using System;
	using System.Diagnostics;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Logging;

	internal static class DataMinerAgentHelper
	{
		private static bool? isRunningOnDataMinerAgent;

		private static readonly string[] DataMinerProcessNames = new[]
		{
			"DataMiner",
			"SLAutomation",
			"SLScripting",
		};

		public static bool IsRunningOnDataMinerAgent(ILogger _logger)
		{
			if (!isRunningOnDataMinerAgent.HasValue)
			{
				string currentProcessName = Process.GetCurrentProcess().ProcessName;
				isRunningOnDataMinerAgent = DataMinerProcessNames.Any(x => currentProcessName.StartsWith(x, StringComparison.InvariantCultureIgnoreCase));
				if (!isRunningOnDataMinerAgent.Value )
				{
					_logger.Warning("This code isn't running on a DataMiner agent, unable to communicate with Lock Manager as NATS communication will fail, keeping locks in memory");
				}
			}
			
			return isRunningOnDataMinerAgent.Value;
		}
	}
}
