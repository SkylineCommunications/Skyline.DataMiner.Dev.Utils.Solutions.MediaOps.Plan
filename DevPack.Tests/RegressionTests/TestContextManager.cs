namespace RT_MediaOps.Plan.RegressionTests
{
	public static class TestContextManager
	{
		private static readonly IntegrationTestContext sharedTestContext = new IntegrationTestContext();

		public static IntegrationTestContext SharedTestContext
		{
			get
			{
				// Verify the connection is still alive and authenticated before every test,
				// recreating it when needed.
				sharedTestContext.EnsureConnected();
				return sharedTestContext;
			}
		}

		[AssemblyCleanup]
		public static void Cleanup()
		{
			sharedTestContext.Dispose();
		}
	}
}
