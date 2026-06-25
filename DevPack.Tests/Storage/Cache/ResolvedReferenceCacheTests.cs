namespace RT_MediaOps.Plan.Storage.Cache
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	[TestClass]
	public sealed class ResolvedReferenceCacheTests
	{
		[TestMethod]
		public void SetCache_ThenTryGetValue_ReturnsValue()
		{
			var reference = new ResourcePropertyReference(Guid.NewGuid());
			var value = new StringResolvedValue("resolved");

			var cache = new ResolvedReferenceCache();
			cache.SetCache(new Dictionary<DataReference, ResolvedValue> { [reference] = value });

			Assert.IsTrue(cache.TryGetValue(reference, out var result));
			Assert.AreSame(value, result);
		}

		[TestMethod]
		public void TryGetValue_UnknownReference_ReturnsFalse()
		{
			var cache = new ResolvedReferenceCache();

			Assert.IsFalse(cache.TryGetValue(new ResourcePropertyReference(Guid.NewGuid()), out var result));
			Assert.IsNull(result);
		}

		[TestMethod]
		public void TryGetValue_NullReference_ReturnsFalse()
		{
			var cache = new ResolvedReferenceCache();

			Assert.IsFalse(cache.TryGetValue(null, out var result));
			Assert.IsNull(result);
		}

		[TestMethod]
		public void SetCache_Null_Throws()
		{
			var cache = new ResolvedReferenceCache();

			Assert.ThrowsException<ArgumentNullException>(() => cache.SetCache(null));
		}
	}
}
