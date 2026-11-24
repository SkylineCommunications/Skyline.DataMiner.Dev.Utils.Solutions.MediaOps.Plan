namespace RT_MediaOps.Plan.RST.ResourceProperties
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.ManagerStore.Select;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using PropertyInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.PropertyInfo;

	[TestClass]
	public sealed class ResourcePropertyPartialFieldMapperTests
	{
		[TestMethod]
		public void CreateSelectedFields_SelectsEachDomFieldOnce()
		{
			var selectedFields = ResourcePropertyPartialFieldMapper.CreateSelectedFields(new FieldExposer[] { ResourcePropertyExposers.Name, ResourcePropertyExposers.Id, ResourcePropertyExposers.Name });

			CollectionAssert.AreEqual(new[] { FieldName(PropertyInfo.PropertyName) }, selectedFields.Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_OnlyId_SelectsStatus()
		{
			CollectionAssert.AreEqual(new[] { "StatusId" }, ResourcePropertyPartialFieldMapper.CreateSelectedFields(new[] { ResourcePropertyExposers.Id }).Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_UnsupportedField_ThrowsNotSupportedException()
		{
			var unknownField = new Exposer<ResourceProperty, string>(x => x.Name, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => ResourcePropertyPartialFieldMapper.CreateSelectedFields(new[] { unknownField }));
			Assert.ThrowsException<NotSupportedException>(() => ResourcePropertyPartialFieldMapper.CreateSelectedFields(new[] { ResourceExposers.Name }));
		}

		[TestMethod]
		public void CreatePartialResourceProperty_ConvertsValuesLikeAFullRead()
		{
			var id = Guid.NewGuid();

			var partialProperty = CreatePartialResourceProperty(
				CreateDomPartialObject(id, Field(PropertyInfo.PropertyName, "Color")),
				ResourcePropertyExposers.Id,
				ResourcePropertyExposers.Name);

			Assert.AreEqual(id, partialProperty.Id);
			Assert.AreEqual(id, partialProperty.GetValue(ResourcePropertyExposers.Id));
			Assert.AreEqual("Color", partialProperty.GetValue(ResourcePropertyExposers.Name));
		}

		[TestMethod]
		public void CreatePartialResourceProperty_MissingName_IsNullLikeAFullRead()
		{
			var partialProperty = CreatePartialResourceProperty(CreateDomPartialObject(Guid.NewGuid()), ResourcePropertyExposers.Name);

			Assert.IsNull(partialProperty.GetValue(ResourcePropertyExposers.Name));
		}

		private static Skyline.DataMiner.SDM.PartialObject<ResourceProperty> CreatePartialResourceProperty(PartialObject<DomInstance, DomInstanceId> domPartialObject, params FieldExposer[] fields)
		{
			return ResourcePropertyPartialFieldMapper.CreatePartialResourceProperties(new[] { domPartialObject }, fields).Single();
		}

		private static string FieldName(FieldDescriptorID descriptor)
		{
			return $"FieldValues.{descriptor.Id}";
		}

		// The DOM stores a selected field value as a list under the name of its field exposer, to support multiple sections.
		private static (string Key, IPartialObjectValue Value) Field(FieldDescriptorID descriptor, string value)
		{
			return (FieldName(descriptor), new PartialObjectValue<List<string>> { Value = new List<string> { value } });
		}

		private static PartialObject<DomInstance, DomInstanceId> CreateDomPartialObject(Guid id, params (string Key, IPartialObjectValue Value)[] values)
		{
			return new PartialObject<DomInstance, DomInstanceId>(new DomInstanceId(id), values.ToDictionary(x => x.Key, x => x.Value));
		}
	}
}
