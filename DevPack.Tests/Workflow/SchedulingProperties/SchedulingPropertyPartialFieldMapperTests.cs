namespace RT_MediaOps.Plan.Workflow.SchedulingProperties
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.ManagerStore.Select;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using Layout = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties.SlcPropertiesIds.Sections.Layout;
	using PropertyInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties.SlcPropertiesIds.Sections.PropertyInfo;

	[TestClass]
	public sealed class SchedulingPropertyPartialFieldMapperTests
	{
		private static readonly FieldExposer[] AllFields =
		{
			PropertyExposers.Id,
			PropertyExposers.Name,
			PropertyExposers.Scope,
			PropertyExposers.SectionName,
			PropertyExposers.Order,
		};

		[TestMethod]
		public void CreateSelectedFields_SelectsEachDomFieldOnceAndAlwaysThePropertyType()
		{
			var selectedFields = SchedulingPropertyPartialFieldMapper.CreateSelectedFields(new FieldExposer[] { PropertyExposers.Name, PropertyExposers.Order, PropertyExposers.Name, PropertyExposers.Id });

			CollectionAssert.AreEquivalent(
				new[] { FieldName(PropertyInfo.PropertyType), FieldName(PropertyInfo.Name), FieldName(Layout.Order) },
				selectedFields.Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_OnlyId_SelectsPropertyType()
		{
			CollectionAssert.AreEqual(
				new[] { FieldName(PropertyInfo.PropertyType) },
				SchedulingPropertyPartialFieldMapper.CreateSelectedFields(new[] { PropertyExposers.Id }).Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_SupportsEveryExposer()
		{
			CollectionAssert.AreEquivalent(AllFields.Select(x => x.fieldName).ToList(), SchedulingPropertyPartialFieldMapper.SupportedFieldNames.ToList());
			Assert.AreEqual(AllFields.Length, SchedulingPropertyPartialFieldMapper.CreateSelectedFields(AllFields).Fields.Count());
		}

		[TestMethod]
		public void CreateSelectedFields_UnsupportedField_ThrowsNotSupportedException()
		{
			var unknownField = new Exposer<Property, string>(x => x.Name, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => SchedulingPropertyPartialFieldMapper.CreateSelectedFields(new[] { unknownField }));
			Assert.ThrowsException<NotSupportedException>(() => SchedulingPropertyPartialFieldMapper.CreateSelectedFields(new[] { JobExposers.Name }));
		}

		[TestMethod]
		public void CreatePartialProperty_ConvertsValuesLikeAFullRead()
		{
			var id = Guid.NewGuid();

			var domPartialObject = CreateDomPartialObject(
				id,
				Field(PropertyInfo.PropertyType, "Boolean"),
				Field(PropertyInfo.Name, "Name"),
				Field(PropertyInfo.Scope, "MediaOps"),
				Field(Layout.SectionName, "General"),
				Field(Layout.Order, 4L));

			var partialProperty = CreatePartialProperties(new[] { domPartialObject }, AllFields).Single();

			Assert.AreEqual(id, partialProperty.Id);
			Assert.AreEqual(id, partialProperty.GetValue(PropertyExposers.Id));
			Assert.AreEqual("Name", partialProperty.GetValue(PropertyExposers.Name));
			Assert.AreEqual("MediaOps", partialProperty.GetValue(PropertyExposers.Scope));
			Assert.AreEqual("General", partialProperty.GetValue(PropertyExposers.SectionName));
			Assert.AreEqual(4, partialProperty.GetValue(PropertyExposers.Order));
		}

		[TestMethod]
		public void CreatePartialProperty_MissingValues_UseTheDefaultsOfAFullRead()
		{
			var partialProperty = CreatePartialProperties(new[] { CreateDomPartialObject(Guid.NewGuid(), Field(PropertyInfo.PropertyType, "string")) }, AllFields).Single();

			Assert.IsNull(partialProperty.GetValue(PropertyExposers.Name));
			Assert.IsNull(partialProperty.GetValue(PropertyExposers.Scope));
			Assert.IsNull(partialProperty.GetValue(PropertyExposers.SectionName));
			Assert.AreEqual(0, partialProperty.GetValue(PropertyExposers.Order));
		}

		[TestMethod]
		public void CreatePartialProperties_SkipsInstancesAFullReadSkips()
		{
			var stringId = Guid.NewGuid();
			var discreteId = Guid.NewGuid();
			var fileId = Guid.NewGuid();

			var domPartialObjects = new[]
			{
				CreateDomPartialObject(stringId, Field(PropertyInfo.PropertyType, "string")),
				CreateDomPartialObject(Guid.NewGuid()),
				CreateDomPartialObject(discreteId, Field(PropertyInfo.PropertyType, "discrete")),
				CreateDomPartialObject(Guid.NewGuid(), Field(PropertyInfo.PropertyType, "unknown")),
				CreateDomPartialObject(fileId, Field(PropertyInfo.PropertyType, "file")),
			};

			var partialProperties = CreatePartialProperties(domPartialObjects, PropertyExposers.Id);

			CollectionAssert.AreEqual(new[] { stringId, discreteId, fileId }, partialProperties.Select(x => x.Id).ToList());
		}

		[TestMethod]
		public void CreatePartialProperty_OnlyExposesSelectedFields()
		{
			var domPartialObject = CreateDomPartialObject(Guid.NewGuid(), Field(PropertyInfo.PropertyType, "Boolean"), Field(PropertyInfo.Name, "Name"), Field(Layout.SectionName, "General"));

			var partialProperty = CreatePartialProperties(new[] { domPartialObject }, PropertyExposers.Name).Single();

			Assert.AreEqual("Name", partialProperty.GetValue(PropertyExposers.Name));
			Assert.IsFalse(partialProperty.HasField(PropertyExposers.SectionName));
			Assert.IsFalse(partialProperty.HasField(PropertyExposers.Id));
		}

		private static IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<Property>> CreatePartialProperties(IEnumerable<PartialObject<DomInstance, DomInstanceId>> domPartialObjects, params FieldExposer[] fields)
		{
			return SchedulingPropertyPartialFieldMapper.CreatePartialProperties(domPartialObjects, fields);
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
