namespace RT_MediaOps.Plan.Relationships
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.ManagerStore.Select;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using LinkInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcRelationships.SlcRelationshipsIds.Sections.LinkInfo;

	[TestClass]
	public sealed class RelationshipPartialFieldMapperTests
	{
		private static readonly FieldExposer[] AllFields =
		{
			RelationshipExposers.Id,
			RelationshipExposers.Parent.ObjectTypeId,
			RelationshipExposers.Parent.ObjectId,
			RelationshipExposers.Parent.ObjectName,
			RelationshipExposers.Parent.Url,
			RelationshipExposers.Parent.Order,
			RelationshipExposers.Child.ObjectTypeId,
			RelationshipExposers.Child.ObjectId,
			RelationshipExposers.Child.ObjectName,
			RelationshipExposers.Child.Url,
			RelationshipExposers.Child.Order,
		};

		[TestMethod]
		public void CreateSelectedFields_SelectsEachDomFieldOnce()
		{
			var selectedFields = RelationshipPartialFieldMapper.CreateSelectedFields(new FieldExposer[] { RelationshipExposers.Parent.ObjectName, RelationshipExposers.Child.Order, RelationshipExposers.Parent.ObjectName, RelationshipExposers.Id });

			CollectionAssert.AreEquivalent(
				new[] { FieldName(LinkInfo.ParentObjectName), FieldName(LinkInfo.ChildOrder) },
				selectedFields.Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_OnlyId_SelectsStatus()
		{
			CollectionAssert.AreEqual(new[] { "StatusId" }, RelationshipPartialFieldMapper.CreateSelectedFields(new[] { RelationshipExposers.Id }).Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_SupportsEveryExposer()
		{
			CollectionAssert.AreEquivalent(AllFields.Select(x => x.fieldName).ToList(), RelationshipPartialFieldMapper.SupportedFieldNames.ToList());
			Assert.AreEqual(AllFields.Length - 1, RelationshipPartialFieldMapper.CreateSelectedFields(AllFields).Fields.Count());
		}

		[TestMethod]
		public void CreateSelectedFields_UnsupportedField_ThrowsNotSupportedException()
		{
			var unknownField = new Exposer<Relationship, string>(x => x.Parent.ObjectId, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => RelationshipPartialFieldMapper.CreateSelectedFields(new[] { unknownField }));
			Assert.ThrowsException<NotSupportedException>(() => RelationshipPartialFieldMapper.CreateSelectedFields(new[] { RelationshipObjectTypeExposers.Name }));
		}

		[TestMethod]
		public void CreatePartialRelationship_ConvertsValuesLikeAFullRead()
		{
			var id = Guid.NewGuid();
			var parentTypeId = Guid.NewGuid();
			var childTypeId = Guid.NewGuid();

			var domPartialObject = CreateDomPartialObject(
				id,
				Field(LinkInfo.ParentObjectType, parentTypeId),
				Field(LinkInfo.ParentObjectID, "parent"),
				Field(LinkInfo.ParentObjectName, "Parent"),
				Field(LinkInfo.ParentURL, "https://parent"),
				Field(LinkInfo.ParentOrder, 3L),
				Field(LinkInfo.ChildObjectType, childTypeId),
				Field(LinkInfo.ChildObjectID, "child"),
				Field(LinkInfo.ChildObjectName, "Child"),
				Field(LinkInfo.ChildURL, "https://child"),
				Field(LinkInfo.ChildOrder, 7L));

			var partialRelationship = CreatePartialRelationship(domPartialObject, AllFields);

			Assert.AreEqual(id, partialRelationship.Id);
			Assert.AreEqual(id, partialRelationship.GetValue(RelationshipExposers.Id));
			Assert.AreEqual(parentTypeId, partialRelationship.GetValue(RelationshipExposers.Parent.ObjectTypeId));
			Assert.AreEqual("parent", partialRelationship.GetValue(RelationshipExposers.Parent.ObjectId));
			Assert.AreEqual("Parent", partialRelationship.GetValue(RelationshipExposers.Parent.ObjectName));
			Assert.AreEqual("https://parent", partialRelationship.GetValue(RelationshipExposers.Parent.Url));
			Assert.AreEqual(3L, partialRelationship.GetValue(RelationshipExposers.Parent.Order));
			Assert.AreEqual(childTypeId, partialRelationship.GetValue(RelationshipExposers.Child.ObjectTypeId));
			Assert.AreEqual("child", partialRelationship.GetValue(RelationshipExposers.Child.ObjectId));
			Assert.AreEqual("Child", partialRelationship.GetValue(RelationshipExposers.Child.ObjectName));
			Assert.AreEqual("https://child", partialRelationship.GetValue(RelationshipExposers.Child.Url));
			Assert.AreEqual(7L, partialRelationship.GetValue(RelationshipExposers.Child.Order));
		}

		[TestMethod]
		public void CreatePartialRelationship_MissingValues_UseTheDefaultsOfAFullRead()
		{
			var partialRelationship = CreatePartialRelationship(CreateDomPartialObject(Guid.NewGuid()), AllFields);

			Assert.AreEqual(Guid.Empty, partialRelationship.GetValue(RelationshipExposers.Parent.ObjectTypeId));
			Assert.IsNull(partialRelationship.GetValue(RelationshipExposers.Parent.ObjectId));
			Assert.IsNull(partialRelationship.GetValue(RelationshipExposers.Parent.ObjectName));
			Assert.IsNull(partialRelationship.GetValue(RelationshipExposers.Parent.Url));
			Assert.AreEqual(0L, partialRelationship.GetValue(RelationshipExposers.Parent.Order));
			Assert.AreEqual(Guid.Empty, partialRelationship.GetValue(RelationshipExposers.Child.ObjectTypeId));
			Assert.IsNull(partialRelationship.GetValue(RelationshipExposers.Child.ObjectId));
			Assert.IsNull(partialRelationship.GetValue(RelationshipExposers.Child.ObjectName));
			Assert.IsNull(partialRelationship.GetValue(RelationshipExposers.Child.Url));
			Assert.AreEqual(0L, partialRelationship.GetValue(RelationshipExposers.Child.Order));
		}

		[TestMethod]
		public void CreatePartialRelationship_OnlyExposesSelectedFields()
		{
			var domPartialObject = CreateDomPartialObject(Guid.NewGuid(), Field(LinkInfo.ParentObjectName, "Parent"), Field(LinkInfo.ChildObjectName, "Child"));

			var partialRelationship = CreatePartialRelationship(domPartialObject, RelationshipExposers.Parent.ObjectName);

			Assert.AreEqual("Parent", partialRelationship.GetValue(RelationshipExposers.Parent.ObjectName));
			Assert.IsFalse(partialRelationship.HasField(RelationshipExposers.Child.ObjectName));
			Assert.IsFalse(partialRelationship.HasField(RelationshipExposers.Id));
		}

		private static Skyline.DataMiner.SDM.PartialObject<Relationship> CreatePartialRelationship(PartialObject<DomInstance, DomInstanceId> domPartialObject, params FieldExposer[] fields)
		{
			return RelationshipPartialFieldMapper.CreatePartialRelationships(new[] { domPartialObject }, fields).Single();
		}

		private static string FieldName(FieldDescriptorID descriptor)
		{
			return $"FieldValues.{descriptor.Id}";
		}

		// The DOM stores a selected field value as a list under the name of its field exposer, to support multiple sections.
		private static (string Key, IPartialObjectValue Value) Field<T>(FieldDescriptorID descriptor, T value)
		{
			return (FieldName(descriptor), new PartialObjectValue<List<T>> { Value = new List<T> { value } });
		}

		private static PartialObject<DomInstance, DomInstanceId> CreateDomPartialObject(Guid id, params (string Key, IPartialObjectValue Value)[] values)
		{
			return new PartialObject<DomInstance, DomInstanceId>(new DomInstanceId(id), values.ToDictionary(x => x.Key, x => x.Value));
		}
	}
}
