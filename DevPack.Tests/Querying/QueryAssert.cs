namespace RT_MediaOps.Plan.Querying
{
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using SLDataGateway.API.Types.Querying;

	/// <summary>
	/// Contains shared assertions to validate the querying (filtering, ordering and limiting) of a repository.
	/// </summary>
	internal static class QueryAssert
	{
		/// <summary>
		/// Asserts that reading with the specified query returns the expected objects in the expected order.
		/// </summary>
		public static void Read<T>(IRepository<T> repository, T[] expectedObjects, IQuery<T> query)
			where T : ApiObject
		{
			var expectedIds = expectedObjects.Select(x => x.Id).ToList();
			var actualIds = repository.Read(query).Select(x => x.Id).ToList();

			Assert.IsTrue(expectedIds.SequenceEqual(actualIds), Describe(query));
		}

		/// <summary>
		/// Asserts that counting with the specified query returns the expected number of objects.
		/// </summary>
		public static void Count<T>(IRepository<T> repository, T[] expectedObjects, IQuery<T> query)
			where T : ApiObject
		{
			Assert.AreEqual(expectedObjects.Length, repository.Count(query), Describe(query));
		}

		/// <summary>
		/// Asserts that reading paged with the specified query returns the expected objects in the expected order,
		/// using the default page size.
		/// </summary>
		public static void ReadPaged<T>(IRepository<T> repository, T[] expectedObjects, IQuery<T> query)
			where T : ApiObject
		{
			var expectedIds = expectedObjects.Select(x => x.Id).ToList();
			var pages = repository.ReadPaged(query).ToList();
			var actualIds = pages.SelectMany(x => x).Select(x => x.Id).ToList();

			Assert.AreEqual(1, pages.Count, Describe(query));
			Assert.IsTrue(expectedIds.SequenceEqual(actualIds), Describe(query));
		}

		/// <summary>
		/// Asserts that reading paged with the specified query returns the expected objects in the expected order,
		/// respecting the specified page size.
		/// </summary>
		public static void ReadPaged<T>(IRepository<T> repository, T[] expectedObjects, IQuery<T> query, int pageSize)
			where T : ApiObject
		{
			var expectedIds = expectedObjects.Select(x => x.Id).ToList();
			var pages = repository.ReadPaged(query, pageSize).ToList();
			var actualIds = pages.SelectMany(x => x).Select(x => x.Id).ToList();

			Assert.IsTrue(expectedIds.SequenceEqual(actualIds), Describe(query));

			foreach (var page in pages)
			{
				Assert.IsTrue(page.Count() <= pageSize, Describe(query));
			}
		}

		private static string Describe<T>(IQuery<T> query)
		{
			var order = String.Join(", ", query.Order.Elements.Select(x => $"{x.Exposer.fieldName} {x.SortOrder}"));

			return $"Filter: {query.Filter}, Order: [{order}], Limit: {query.Limit}";
		}
	}
}
