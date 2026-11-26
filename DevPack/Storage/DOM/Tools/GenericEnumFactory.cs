namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Tools
{
	using Skyline.DataMiner.Net.GenericEnums;

	internal static class GenericEnumFactory
	{
		// Persisted labels and values are explicit: generated C# member names do
		// not necessarily preserve capitalization, spaces, or string enum values.
		internal static GenericEnum<int> Create(string name, params (string Label, int Value)[] entries)
		{
			var result = new GenericEnum<int> { EnumName = name };
			foreach (var entry in entries)
			{
				result.AddEntry(entry.Label, entry.Value);
			}

			return result;
		}

		internal static GenericEnum<string> Create(string name, params (string Label, string Value)[] entries)
		{
			var result = new GenericEnum<string> { EnumName = name };
			foreach (var entry in entries)
			{
				result.AddEntry(entry.Label, entry.Value);
			}

			return result;
		}
	}
}
