namespace RT_MediaOps.Plan.SDM
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;

	[TestClass]
	public sealed class PartialReadTests
	{
		private static readonly FilterElement<Job> NameFilter = JobExposers.Name.Equal("Job");

		[TestMethod]
		public void WithFields_Filter_KeepsFilterAndIgnoresDuplicateFields()
		{
			var equivalentNameExposer = new Exposer<Job, string>(x => x.Name, "Name");

			var partialFilter = NameFilter.WithFields(JobExposers.Name, JobExposers.Start, equivalentNameExposer);

			Assert.AreSame(NameFilter, partialFilter.Filter);
			CollectionAssert.AreEqual(new FieldExposer[] { JobExposers.Name, JobExposers.Start }, partialFilter.Fields.ToList());
		}

		[TestMethod]
		public void WithFields_InvalidFields_Throws()
		{
			Assert.ThrowsException<ArgumentException>(() => NameFilter.WithFields());
			Assert.ThrowsException<ArgumentNullException>(() => NameFilter.WithFields(JobExposers.Name, null));
			Assert.ThrowsException<ArgumentException>(() => NameFilter.WithFields(ResourceExposers.Name));
		}

		[TestMethod]
		public void WithFields_Query_KeepsFieldsWhenQueryIsModified()
		{
			var partialQuery = NameFilter.ToQuery().WithFields(JobExposers.Name);

			var modified = partialQuery.OrderBy(JobExposers.Start).Limit(10);

			var modifiedPartialQuery = modified as IPartialQuery<Job>;
			Assert.IsNotNull(modifiedPartialQuery);
			CollectionAssert.AreEqual(new FieldExposer[] { JobExposers.Name }, modifiedPartialQuery.Fields.ToList());
			Assert.AreEqual(10, modifiedPartialQuery.Limit.Limit);
			Assert.AreEqual(JobExposers.Start, modifiedPartialQuery.Order.Elements.Single().Exposer);
			Assert.AreEqual(NameFilter.ToString(), modifiedPartialQuery.Filter.ToString());
		}

		[TestMethod]
		public void WithFields_PartialQuery_AddsFields()
		{
			var partialQuery = NameFilter.ToQuery().WithFields(JobExposers.Name).WithFields(JobExposers.Name, JobExposers.End);

			CollectionAssert.AreEqual(new FieldExposer[] { JobExposers.Name, JobExposers.End }, partialQuery.Fields.ToList());
		}

		[TestMethod]
		public void PartialObject_SelectedFields_ReturnValues()
		{
			var start = DateTimeOffset.UtcNow;
			var partial = CreatePartialObject(
				(JobExposers.Name, new object[] { "Job" }),
				(JobExposers.Start, new object[] { start }),
				(JobExposers.Notes, Array.Empty<object>()));

			Assert.IsTrue(partial.HasField(new Exposer<Job, string>(x => x.Name, "Name")));
			Assert.AreEqual("Job", partial.GetValue(JobExposers.Name));
			Assert.AreEqual(start, partial.GetValue(JobExposers.Start));
			Assert.IsNull(partial.GetValue(JobExposers.Notes));
			Assert.IsTrue(partial.TryGetValue(JobExposers.Start, out var tryStart));
			Assert.AreEqual(start, tryStart);
			Assert.IsFalse(partial.TryGetValue(JobExposers.Notes, out _));
			Assert.IsFalse(partial.TryGetValues(JobExposers.Notes, out var notes));
			Assert.AreEqual(0, notes.Count());
			Assert.AreEqual(0, partial.GetValues(JobExposers.Notes).Count());
		}

		[TestMethod]
		public void PartialObject_FieldNotSelected_GetThrowsAndTryGetReturnsFalse()
		{
			var partial = CreatePartialObject((JobExposers.Name, new object[] { "Job" }));

			Assert.IsFalse(partial.HasField(JobExposers.Start));
			Assert.ThrowsException<InvalidOperationException>(() => partial.GetValue(JobExposers.Start));
			Assert.ThrowsException<InvalidOperationException>(() => partial.GetValues(JobExposers.Start));
			Assert.IsFalse(partial.TryGetValue(JobExposers.Start, out _));
			Assert.IsFalse(partial.TryGetValues(JobExposers.Start, out _));
		}

		[TestMethod]
		public void PartialObject_MultipleValues_OnlyAvailableThroughGetValues()
		{
			var partial = CreatePartialObject((JobExposers.Name, new object[] { "First", "Second" }));

			Assert.ThrowsException<InvalidOperationException>(() => partial.GetValue(JobExposers.Name));
			Assert.IsFalse(partial.TryGetValue(JobExposers.Name, out _));
			CollectionAssert.AreEqual(new[] { "First", "Second" }, partial.GetValues(JobExposers.Name).ToList());
			Assert.IsTrue(partial.TryGetValues(JobExposers.Name, out var names));
			CollectionAssert.AreEqual(new[] { "First", "Second" }, names.ToList());
		}

		[TestMethod]
		public void PartialObject_ValueOfWrongType_GetThrowsAndTryGetReturnsFalse()
		{
			var partial = CreatePartialObject((JobExposers.Start, new object[] { "not a date" }));

			Assert.ThrowsException<InvalidOperationException>(() => partial.GetValue(JobExposers.Start));
			Assert.ThrowsException<InvalidOperationException>(() => partial.GetValues(JobExposers.Start));
			Assert.IsFalse(partial.TryGetValue(JobExposers.Start, out _));
			Assert.IsFalse(partial.TryGetValues(JobExposers.Start, out _));
		}

		[TestMethod]
		public void PartialObject_InvalidFields_Throws()
		{
			Assert.ThrowsException<ArgumentException>(() => CreatePartialObject((ResourceExposers.Name, new object[] { "Resource" })));
			Assert.ThrowsException<ArgumentException>(() => CreatePartialObject(
				(JobExposers.Name, new object[] { "Job" }),
				(new Exposer<Job, string>(x => x.Name, "Name"), new object[] { "Job" })));
		}

		[TestMethod]
		public void PartialObject_Id_AlwaysAvailableWithoutSelectingIt()
		{
			var id = Guid.NewGuid();

			var partial = new PartialObject<Job>(id, new[] { new KeyValuePair<FieldExposer, IEnumerable<object>>(JobExposers.Name, new object[] { "Job" }) });

			Assert.AreEqual(id, partial.Id);
			Assert.IsFalse(partial.HasField(JobExposers.Id));
			Assert.ThrowsException<ArgumentException>(() => new PartialObject<Job>(Guid.Empty, Array.Empty<KeyValuePair<FieldExposer, IEnumerable<object>>>()));
		}

		private static PartialObject<Job> CreatePartialObject(params (FieldExposer Field, object[] Values)[] fields)
		{
			return new PartialObject<Job>(Guid.NewGuid(), fields.Select(x => new KeyValuePair<FieldExposer, IEnumerable<object>>(x.Field, x.Values)));
		}
	}
}
