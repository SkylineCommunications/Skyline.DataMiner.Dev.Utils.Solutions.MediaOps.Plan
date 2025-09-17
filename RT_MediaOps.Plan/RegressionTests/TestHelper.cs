namespace RT_MediaOps.Plan.RegressionTests
{
    using System;

    internal static class TestHelper
    {
        public static string GetRandomName(string prefix)
        {
            return $"{prefix}{Guid.NewGuid().ToString().Replace("-", string.Empty)}";
        }
    }
}
