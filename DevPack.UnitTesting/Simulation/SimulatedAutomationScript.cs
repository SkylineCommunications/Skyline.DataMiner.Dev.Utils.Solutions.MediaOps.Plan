namespace Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// An in-memory Automation script, mirroring the information a real DataMiner Agent exposes about an
	/// installed script (its folder, input parameters and dummies).
	/// </summary>
	public sealed class SimulatedAutomationScript
	{
		internal SimulatedAutomationScript(string name, string folder, IEnumerable<string> parameters, IEnumerable<string> dummies)
		{
			Name = name;
			Folder = folder ?? String.Empty;
			Parameters = (parameters ?? Enumerable.Empty<string>()).ToList();
			Dummies = (dummies ?? Enumerable.Empty<string>()).ToList();
		}

		/// <summary>
		/// Gets the name of the script.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the folder the script is stored in.
		/// </summary>
		public string Folder { get; }

		/// <summary>
		/// Gets the descriptions of the input parameters of the script.
		/// </summary>
		public IReadOnlyList<string> Parameters { get; }

		/// <summary>
		/// Gets the descriptions of the input dummies of the script.
		/// </summary>
		public IReadOnlyList<string> Dummies { get; }
	}
}
