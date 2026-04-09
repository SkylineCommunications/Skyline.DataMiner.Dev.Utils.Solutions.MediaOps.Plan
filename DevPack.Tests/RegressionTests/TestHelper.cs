namespace RT_MediaOps.Plan.RegressionTests
{
	using System;

	internal static class TestHelper
	{
		public static string GetRandomName(string prefix, Guid? id = null)
		{
			var guid = id ?? Guid.NewGuid();
			return $"{prefix}{guid.ToString().Replace("-", string.Empty)}";
		}
	}
}
