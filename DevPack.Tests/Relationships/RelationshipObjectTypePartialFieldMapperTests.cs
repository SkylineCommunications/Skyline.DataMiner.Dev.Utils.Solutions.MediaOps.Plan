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

	using ObjectTypeInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcRelationships.SlcRelationshipsIds.Sections.ObjectTypeInfo;

	[TestClass]
	public sealed class RelationshipObjectTypePartialFieldMapperTests
	{
		private static readonly FieldExposer[] AllFields =
		{
			RelationshipObjectTypeExposers.Id,
			RelationshipObjectTypeExposers.Name,
		};

		[TestMethod]
		public void CreateSelectedFields_SelectsEachDomFieldOnce()
		{
			var selectedFields = RelationshipObjectTypePartialFieldMapper.CreateSelectedFields(new FieldExposer[] { RelationshipObjectTypeExposers.Name, RelationshipObjectTypeExposers.Name, RelationshipObjectTypeExposers.Id });

			CollectionAssert.AreEqual(new[] { FieldName(ObjectTypeInfo.ObjectName) }, selectedFields.Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_OnlyId_SelectsStatus()
		{
			CollectionAssert.AreEqual(new[] { "StatusId" }, RelationshipObjectTypePartialFieldMapper.CreateSelectedFields(new[] { RelationshipObjectTypeExposers.Id }).Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_SupportsEveryExposer()
		{
			CollectionAssert.AreEquivalent(AllFields.Select(x => x.fieldName).ToList(), RelationshipObjectTypePartialFieldMapper.SupportedFieldNames.ToList());
			Assert.AreEqual(AllFields.Length - 1, RelationshipObjectTypePartialFieldMapper.CreateSelectedFields(AllFields).Fields.Count());
		}

		[TestMethod]
		public void CreateSelectedFields_UnsupportedField_ThrowsNotSupportedException()
		{
			var unknownField = new Exposer<RelationshipObjectType, string>(x => x.Name, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => RelationshipObjectTypePartialFieldMapper.CreateSelectedFields(new[] { unknownField }));
			Assert.ThrowsException<NotSupportedException>(() => RelationshipObjectTypePartialFieldMapper.CreateSelectedFields(new[] { RelationshipExposers.Id }));
		}

		[TestMethod]
		public void CreatePartialObjectType_ConvertsValuesLikeAFullRead()
		{
			var id = Guid.NewGuid();

			var partialObjectType = CreatePartialObjectType(CreateDomPartialObject(id, Field(ObjectTypeInfo.ObjectName, "Job")), AllFields);

			Assert.AreEqual(id, partialObjectType.Id);
			Assert.AreEqual(id, partialObjectType.GetValue(RelationshipObjectTypeExposers.Id));
			Assert.AreEqual("Job", partialObjectType.GetValue(RelationshipObjectTypeExposers.Name));
		}

		[TestMethod]
		public void CreatePartialObjectType_MissingValues_UseTheDefaultsOfAFullRead()
		{
			var partialObjectType = CreatePartialObjectType(CreateDomPartialObject(Guid.NewGuid()), AllFields);

			Assert.IsNull(partialObjectType.GetValue(RelationshipObjectTypeExposers.Name));
		}

		[TestMethod]
		public void CreatePartialObjectType_OnlyExposesSelectedFields()
		{
			var partialObjectType = CreatePartialObjectType(CreateDomPartialObject(Guid.NewGuid(), Field(ObjectTypeInfo.ObjectName, "Job")), RelationshipObjectTypeExposers.Name);

			Assert.AreEqual("Job", partialObjectType.GetValue(RelationshipObjectTypeExposers.Name));
			Assert.IsFalse(partialObjectType.HasField(RelationshipObjectTypeExposers.Id));
		}

		private static Skyline.DataMiner.SDM.PartialObject<RelationshipObjectType> CreatePartialObjectType(PartialObject<DomInstance, DomInstanceId> domPartialObject, params FieldExposer[] fields)
		{
			return RelationshipObjectTypePartialFieldMapper.CreatePartialObjectTypes(new[] { domPartialObject }, fields).Single();
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
