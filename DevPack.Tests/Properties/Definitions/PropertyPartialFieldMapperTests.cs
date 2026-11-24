namespace RT_MediaOps.Plan.Properties.Definitions
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
	using Propertytype = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties.SlcPropertiesIds.Enums.Propertytype;

	[TestClass]
	public sealed class PropertyPartialFieldMapperTests
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
		public void SupportedFieldNames_ContainsEveryPropertyExposer()
		{
			var exposerNames = typeof(PropertyExposers).GetFields()
				.Select(x => x.GetValue(null))
				.OfType<FieldExposer>()
				.Select(x => x.fieldName)
				.ToList();

			CollectionAssert.AreEquivalent(exposerNames, PropertyPartialFieldMapper.SupportedFieldNames.ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_SelectsEachDomFieldOnceAndAlwaysThePropertyType()
		{
			var selectedFields = PropertyPartialFieldMapper.CreateSelectedFields(new FieldExposer[] { PropertyExposers.Name, PropertyExposers.Order, PropertyExposers.Name });

			CollectionAssert.AreEquivalent(
				new[] { FieldName(PropertyInfo.PropertyType), FieldName(PropertyInfo.Name), FieldName(Layout.Order) },
				selectedFields.Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_OnlyId_SelectsThePropertyType()
		{
			CollectionAssert.AreEqual(
				new[] { FieldName(PropertyInfo.PropertyType) },
				PropertyPartialFieldMapper.CreateSelectedFields(new[] { PropertyExposers.Id }).Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_UnsupportedField_ThrowsNotSupportedException()
		{
			var unknownField = new Exposer<Property, string>(x => x.Name, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => PropertyPartialFieldMapper.CreateSelectedFields(new[] { unknownField }));
			Assert.ThrowsException<NotSupportedException>(() => PropertyPartialFieldMapper.CreateSelectedFields(new[] { ResourceExposers.Name }));
		}

		[TestMethod]
		public void CreatePartialProperty_EveryPropertyType_ConvertsValuesLikeAFullRead()
		{
			foreach (var type in new[] { Propertytype.String, Propertytype.Boolean, Propertytype.Discrete, Propertytype.File })
			{
				var id = Guid.NewGuid();

				var partialProperty = CreatePartialProperties(
					new[]
					{
						CreateDomPartialObject(
							id,
							Field(PropertyInfo.PropertyType, type),
							Field(PropertyInfo.Name, "Name"),
							Field(PropertyInfo.Scope, "global"),
							Field(Layout.SectionName, "General"),
							Field(Layout.Order, 3L)),
					},
					AllFields).Single();

				Assert.AreEqual(id, partialProperty.Id, type);
				Assert.AreEqual(id, partialProperty.GetValue(PropertyExposers.Id), type);
				Assert.AreEqual("Name", partialProperty.GetValue(PropertyExposers.Name), type);
				Assert.AreEqual("global", partialProperty.GetValue(PropertyExposers.Scope), type);
				Assert.AreEqual("General", partialProperty.GetValue(PropertyExposers.SectionName), type);
				Assert.AreEqual(3, partialProperty.GetValue(PropertyExposers.Order), type);
			}
		}

		[TestMethod]
		public void CreatePartialProperty_MissingValues_UseTheDefaultsOfAFullRead()
		{
			var partialProperty = CreatePartialProperties(
				new[] { CreateDomPartialObject(Guid.NewGuid(), Field(PropertyInfo.PropertyType, Propertytype.String)) },
				AllFields).Single();

			Assert.IsNull(partialProperty.GetValue(PropertyExposers.Name));
			Assert.IsNull(partialProperty.GetValue(PropertyExposers.Scope));
			Assert.IsNull(partialProperty.GetValue(PropertyExposers.SectionName));
			Assert.AreEqual(0, partialProperty.GetValue(PropertyExposers.Order));
			Assert.IsTrue(partialProperty.HasField(PropertyExposers.Order));
		}

		[TestMethod]
		public void CreatePartialProperties_WithoutPropertyType_SkipsTheInstanceLikeAFullRead()
		{
			var typedId = Guid.NewGuid();

			var partialProperties = CreatePartialProperties(
				new[]
				{
					CreateDomPartialObject(Guid.NewGuid(), Field(PropertyInfo.Name, "Untyped")),
					CreateDomPartialObject(typedId, Field(PropertyInfo.PropertyType, Propertytype.Boolean), Field(PropertyInfo.Name, "Typed")),
				},
				PropertyExposers.Name);

			CollectionAssert.AreEqual(new[] { typedId }, partialProperties.Select(x => x.Id).ToList());
		}

		[TestMethod]
		public void CreatePartialProperties_UnknownPropertyType_ThrowsLikeAFullRead()
		{
			var domPartialObject = CreateDomPartialObject(Guid.NewGuid(), Field(PropertyInfo.PropertyType, "unknown"));

			Assert.ThrowsException<ArgumentOutOfRangeException>(() => CreatePartialProperties(new[] { domPartialObject }, PropertyExposers.Name));
		}

		[TestMethod]
		public void CreatePartialProperties_OnlyRequestedFieldsAreExposed()
		{
			var partialProperty = CreatePartialProperties(
				new[] { CreateDomPartialObject(Guid.NewGuid(), Field(PropertyInfo.PropertyType, Propertytype.String), Field(PropertyInfo.Name, "Name")) },
				PropertyExposers.Name).Single();

			Assert.IsTrue(partialProperty.HasField(PropertyExposers.Name));
			Assert.IsFalse(partialProperty.HasField(PropertyExposers.Order));
			Assert.ThrowsException<InvalidOperationException>(() => partialProperty.GetValue(PropertyExposers.Order));
		}

		private static IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<Property>> CreatePartialProperties(IEnumerable<PartialObject<DomInstance, DomInstanceId>> domPartialObjects, params FieldExposer[] fields)
		{
			return PropertyPartialFieldMapper.CreatePartialProperties(domPartialObjects, fields);
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
