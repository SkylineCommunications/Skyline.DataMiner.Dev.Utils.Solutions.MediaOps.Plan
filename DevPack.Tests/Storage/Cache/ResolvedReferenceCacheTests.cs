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
			cache.Set("node-1", reference, value);

			Assert.AreEqual(1, cache.Count);
			Assert.IsTrue(cache.Contains("node-1", reference));
			Assert.IsTrue(cache.TryGetValue("node-1", reference, out var result));
			Assert.AreSame(value, result);
		}

		/// <summary>The same configured reference can resolve to a different value on each node.</summary>
		[TestMethod]
		public void SetCache_SameReferenceOnDifferentNodes_KeepsBothValues()
		{
			var reference = new ResourceNameReference();

			var cache = new ResolvedReferenceCache();
			cache.Set("node-1", reference, new StringResolvedValue("first"));
			cache.Set("node-2", reference, new StringResolvedValue("second"));

			Assert.IsTrue(cache.TryGetValue("node-1", reference, out var first));
			Assert.IsTrue(cache.TryGetValue("node-2", reference, out var second));
			Assert.AreEqual("first", ((StringResolvedValue)first).Value);
			Assert.AreEqual("second", ((StringResolvedValue)second).Value);
		}

		[TestMethod]
		public void Clear_RemovesEveryValue()
		{
			var reference = new ResourceNameReference();

			var cache = new ResolvedReferenceCache();
			cache.Set("node-1", reference, new StringResolvedValue("resolved"));

			cache.Clear();

			Assert.AreEqual(0, cache.Count);
			Assert.IsFalse(cache.TryGetValue("node-1", reference, out _));
		}

		[TestMethod]
		public void TryGetValue_UnknownReference_ReturnsFalse()
		{
			var cache = new ResolvedReferenceCache();

			Assert.IsFalse(cache.TryGetValue("node-1", new ResourcePropertyReference(Guid.NewGuid()), out var result));
			Assert.IsNull(result);
		}

		[TestMethod]
		public void TryGetValue_NullReference_ReturnsFalse()
		{
			var cache = new ResolvedReferenceCache();

			Assert.IsFalse(cache.TryGetValue("node-1", null, out var result));
			Assert.IsNull(result);
			Assert.IsFalse(cache.Contains("node-1", null));
		}

		[TestMethod]
		public void Set_NullReference_Throws()
		{
			var cache = new ResolvedReferenceCache();

			Assert.ThrowsException<ArgumentNullException>(() => cache.Set("node-1", null, new StringResolvedValue("resolved")));
		}

		[TestMethod]
		public void SetCache_CopiesTheContentOfTheOtherCache()
		{
			var reference = new ResourceNameReference();

			var source = new ResolvedReferenceCache();
			source.Set("node-1", reference, new StringResolvedValue("resolved"));

			var target = new ResolvedReferenceCache();
			target.Set(source);

			Assert.AreEqual(1, target.Count);
			Assert.IsTrue(target.TryGetValue("node-1", reference, out var value));
			Assert.AreEqual("resolved", ((StringResolvedValue)value).Value);
		}

		[TestMethod]
		public void SetCache_Null_Throws()
		{
			var cache = new ResolvedReferenceCache();

			Assert.ThrowsException<ArgumentNullException>(() => cache.Set(null));
		}
	}
}
