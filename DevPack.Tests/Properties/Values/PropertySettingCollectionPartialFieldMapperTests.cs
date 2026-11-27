namespace RT_MediaOps.Plan.Properties.Values
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.ManagerStore.Select;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using PropertyValue = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties.SlcPropertiesIds.Sections.PropertyValue;
	using PropertyValueInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties.SlcPropertiesIds.Sections.PropertyValueInfo;

	[TestClass]
	public sealed class PropertySettingCollectionPartialFieldMapperTests
	{
		[TestMethod]
		public void CreateSelectedFields_SelectsEachDomFieldOnce()
		{
			var selectedFields = PropertySettingCollectionPartialFieldMapper.CreateSelectedFields(new FieldExposer[]
			{
				PropertySettingCollectionExposers.LinkedObjectId,
				PropertySettingCollectionExposers.CustomPropertySettings.Name,
				PropertySettingCollectionExposers.StringPropertySettings.Value,
				PropertySettingCollectionExposers.BooleanPropertySettings.Value,
			});

			CollectionAssert.AreEquivalent(
				new[] { FieldName(PropertyValueInfo.LinkedObjectID), FieldName(PropertyValue.PropertyID), FieldName(PropertyValue.PropertyName), FieldName(PropertyValue.Value) },
				selectedFields.Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_OnlyId_SelectsStatus()
		{
			CollectionAssert.AreEqual(new[] { "StatusId" }, PropertySettingCollectionPartialFieldMapper.CreateSelectedFields(new[] { PropertySettingCollectionExposers.Id }).Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_UnsupportedField_ThrowsNotSupportedException()
		{
			var unknownField = new Exposer<PropertySettingCollection, string>(x => x.LinkedObjectId, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => PropertySettingCollectionPartialFieldMapper.CreateSelectedFields(new[] { unknownField }));
			Assert.ThrowsException<NotSupportedException>(() => PropertySettingCollectionPartialFieldMapper.CreateSelectedFields(new[] { JobExposers.Name }));
		}

		[TestMethod]
		public void CreateSelectedFields_WrongExposerType_ThrowsNotSupportedException()
		{
			var wrongType = new Exposer<PropertySettingCollection, int>(x => x.LinkedObjectId.Length, PropertySettingCollectionExposers.LinkedObjectId.fieldName);

			Assert.ThrowsException<NotSupportedException>(() => PropertySettingCollectionPartialFieldMapper.CreateSelectedFields(new[] { wrongType }));
		}

		[TestMethod]
		public void CreatePartialCollection_OnlyCustomSettings_AreCombinedPerSection()
		{
			var id = Guid.NewGuid();

			// The second custom setting has no value.
			var domPartialObject = CreateDomPartialObject(
				id,
				Fields(PropertyValueInfo.LinkedObjectID, "Object"),
				Fields(PropertyValueInfo.Scope, "global"),
				Fields(PropertyValue.PropertyName, "Name 1", "Name 2", "Name 3"),
				Fields(PropertyValue.Value, "Value 1", "Value 3"));

			var partialCollection = CreatePartialCollection(
				domPartialObject,
				PropertySettingCollectionExposers.Id,
				PropertySettingCollectionExposers.LinkedObjectId,
				PropertySettingCollectionExposers.Scope,
				PropertySettingCollectionExposers.SubId,
				PropertySettingCollectionExposers.CustomPropertySettings.Name,
				PropertySettingCollectionExposers.CustomPropertySettings.Value);

			Assert.AreEqual(id, partialCollection.GetValue(PropertySettingCollectionExposers.Id));
			Assert.AreEqual("Object", partialCollection.GetValue(PropertySettingCollectionExposers.LinkedObjectId));
			Assert.AreEqual("global", partialCollection.GetValue(PropertySettingCollectionExposers.Scope));
			Assert.IsNull(partialCollection.GetValue(PropertySettingCollectionExposers.SubId));
			CollectionAssert.AreEqual(new[] { "Name 1", "Name 2", "Name 3" }, partialCollection.GetValues(PropertySettingCollectionExposers.CustomPropertySettings.Name).ToList());
			CollectionAssert.AreEqual(new[] { "Value 1", "Value 3" }, partialCollection.GetValues(PropertySettingCollectionExposers.CustomPropertySettings.Value).ToList());
		}

		[TestMethod]
		public void CreatePartialCollection_NoCustomSettings_ReturnsNoCustomValues()
		{
			var domPartialObject = CreateDomPartialObject(
				Guid.NewGuid(),
				Fields(PropertyValue.PropertyID, Guid.NewGuid(), Guid.NewGuid()),
				Fields(PropertyValue.Value, "Value 1", "Value 2"));

			var partialCollection = CreatePartialCollection(domPartialObject, PropertySettingCollectionExposers.CustomPropertySettings.Name, PropertySettingCollectionExposers.CustomPropertySettings.Value);

			Assert.AreEqual(0, partialCollection.GetValues(PropertySettingCollectionExposers.CustomPropertySettings.Name).Count());
			Assert.AreEqual(0, partialCollection.GetValues(PropertySettingCollectionExposers.CustomPropertySettings.Value).Count());
		}

		[TestMethod]
		public void PropertySettings_AreCombinedPerSectionAndGroupedPerTypeLikeAFullRead()
		{
			var stringId1 = Guid.NewGuid();
			var stringId2 = Guid.NewGuid();
			var booleanId = Guid.NewGuid();
			var discreteId = Guid.NewGuid();
			var fileId = Guid.NewGuid();
			var deletedId = Guid.NewGuid();

			var context = CreateContext(
				CreateDomPartialObject(
					Guid.NewGuid(),
					Fields(PropertyValue.PropertyID, fileId, discreteId, stringId1, deletedId, booleanId, stringId2),
					Fields(PropertyValue.Value, "file.txt", "Option", "First", "Deleted", "True", "Second")),
				new StringProperty(stringId1),
				new StringProperty(stringId2),
				new BooleanProperty(booleanId),
				new DiscreteProperty(discreteId),
				new FileProperty(fileId));

			Assert.IsTrue(context.CanCombinePropertyValues());
			CollectionAssert.AreEqual(new[] { stringId1, stringId2, booleanId, discreteId, fileId }, context.GetPropertyIds().ToList(), "A setting of which the property doesn't exist is ignored.");
			CollectionAssert.AreEqual(new[] { "First", "Second" }, context.GetStringValues().ToList());
			CollectionAssert.AreEqual(new[] { true }, context.GetBooleanValues().ToList());
			CollectionAssert.AreEqual(new[] { "Option" }, context.GetDiscreteValues().ToList());
		}

		[TestMethod]
		public void PropertySettings_WithoutValues_AreCombinedLikeAFullRead()
		{
			var booleanId = Guid.NewGuid();
			var discreteId = Guid.NewGuid();

			var context = CreateContext(
				CreateDomPartialObject(Guid.NewGuid(), Fields(PropertyValue.PropertyID, discreteId, booleanId)),
				new BooleanProperty(booleanId),
				new DiscreteProperty(discreteId));

			Assert.IsTrue(context.CanCombinePropertyValues());
			CollectionAssert.AreEqual(new[] { false }, context.GetBooleanValues().ToList(), "A boolean setting without value is false.");
			Assert.AreEqual(0, context.GetDiscreteValues().Count());
		}

		[TestMethod]
		public void PropertySettings_ThatCantBeCombined_ReadTheFullInstance()
		{
			var stringId = Guid.NewGuid();

			// A custom setting mixed with a property setting: the values can't be matched to their setting by position.
			var mixed = CreateDomPartialObject(
				Guid.NewGuid(),
				Fields(PropertyValue.PropertyID, stringId),
				Fields(PropertyValue.PropertyName, "Custom"),
				Fields(PropertyValue.Value, "Custom value", "String value"));

			// One of the two property settings has no value.
			var missingValue = CreateDomPartialObject(
				Guid.NewGuid(),
				Fields(PropertyValue.PropertyID, stringId, Guid.NewGuid()),
				Fields(PropertyValue.Value, "String value"));

			Assert.IsFalse(new PropertySettingCollectionPartialReadContext(mixed).CanCombinePropertyValues());
			Assert.IsFalse(new PropertySettingCollectionPartialReadContext(mixed).CanCombineCustomValues());
			Assert.IsFalse(new PropertySettingCollectionPartialReadContext(mixed).CanCombineCustomNames());
			Assert.IsFalse(new PropertySettingCollectionPartialReadContext(missingValue).CanCombinePropertyValues());

			var exception = Assert.ThrowsException<InvalidOperationException>(() => CreatePartialCollection(mixed, PropertySettingCollectionExposers.CustomPropertySettings.Value));
			StringAssert.Contains(exception.Message, "isn't part of the selected property setting collection DOM fields");
		}

		[TestMethod]
		public void PropertySettings_FromFullInstance_AreParsedLikeAFullRead()
		{
			var stringId = Guid.NewGuid();
			var booleanId = Guid.NewGuid();

			var instance = new Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties.PropertyValuesInstance(Guid.NewGuid());
			instance.PropertyValue.Add(new Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties.PropertyValueSection { PropertyName = "Custom", Value = "Custom value" });
			instance.PropertyValue.Add(new Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties.PropertyValueSection { PropertyID = booleanId, Value = "True" });
			instance.PropertyValue.Add(new Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties.PropertyValueSection { PropertyID = stringId });

			var context = CreateContext(CreateDomPartialObject(instance.ID.Id), new StringProperty(stringId), new BooleanProperty(booleanId));
			context.FullInstance = instance;

			CollectionAssert.AreEqual(new[] { "Custom" }, context.GetCustomNames().ToList());
			CollectionAssert.AreEqual(new[] { "Custom value" }, context.GetCustomValues().ToList());
			CollectionAssert.AreEqual(new[] { stringId, booleanId }, context.GetPropertyIds().ToList());
			Assert.AreEqual(0, context.GetStringValues().Count());
			CollectionAssert.AreEqual(new[] { true }, context.GetBooleanValues().ToList());
		}

		private static Skyline.DataMiner.SDM.PartialObject<PropertySettingCollection> CreatePartialCollection(PartialObject<DomInstance, DomInstanceId> domPartialObject, params FieldExposer[] fields)
		{
			return PropertySettingCollectionPartialFieldMapper.CreatePartialCollections(null, new[] { domPartialObject }, fields).Single();
		}

		private static PropertySettingCollectionPartialReadContext CreateContext(PartialObject<DomInstance, DomInstanceId> domPartialObject, params Property[] properties)
		{
			return new PropertySettingCollectionPartialReadContext(domPartialObject)
			{
				PropertiesById = properties.ToDictionary(x => x.Id),
			};
		}

		private static string FieldName(FieldDescriptorID descriptor)
		{
			return $"FieldValues.{descriptor.Id}";
		}

		// The DOM stores a selected field value as a list under the name of its field exposer, to support multiple sections.
		private static (string Key, IPartialObjectValue Value) Fields<T>(FieldDescriptorID descriptor, params T[] values)
		{
			return (FieldName(descriptor), new PartialObjectValue<List<T>> { Value = values.ToList() });
		}

		private static PartialObject<DomInstance, DomInstanceId> CreateDomPartialObject(Guid id, params (string Key, IPartialObjectValue Value)[] values)
		{
			return new PartialObject<DomInstance, DomInstanceId>(new DomInstanceId(id), values.ToDictionary(x => x.Key, x => x.Value));
		}
	}
}
