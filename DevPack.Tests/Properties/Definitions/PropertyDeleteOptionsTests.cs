namespace RT_MediaOps.Plan.Properties.Definitions
{
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class PropertyDeleteOptionsTests
	{
		[TestMethod]
		public void DefaultConstructor_ForceDeleteIsFalse()
		{
			var options = new PropertyDeleteOptions();

			Assert.IsFalse(options.ForceDelete);
		}

		[TestMethod]
		public void SetForceDelete_ValueIsSet()
		{
			var options = new PropertyDeleteOptions { ForceDelete = true };

			Assert.IsTrue(options.ForceDelete);
		}
	}
}
