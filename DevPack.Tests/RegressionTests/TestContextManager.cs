namespace RT_MediaOps.Plan.RegressionTests
{
	public static class TestContextManager
	{
		private static readonly Lazy<IntegrationTestContext> sharedTestContext = new Lazy<IntegrationTestContext>(() => new IntegrationTestContext());

		public static IntegrationTestContext SharedTestContext
		{
			get
			{
				// Verify the connection is still alive and authenticated before every test,
				// recreating it when needed.
				var context = sharedTestContext.Value;
				context.EnsureConnected();
				return context;
			}
		}

		[AssemblyCleanup]
		public static void Cleanup()
		{
			// A unit-only run must not establish an integration connection just to dispose it.
			if (sharedTestContext.IsValueCreated)
			{
				sharedTestContext.Value.Dispose();
			}
		}
	}
}
