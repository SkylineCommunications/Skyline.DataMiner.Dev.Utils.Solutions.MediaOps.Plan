namespace RT_MediaOps.Plan.RST.Resources
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.ManagerStore.Select;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

	using Capabilities = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapabilities;
	using Capacities = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities;
	using Properties = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceProperties;
	using ResourceInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceInfo;
	using ResourceInternalProperties = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceInternalProperties;
	using ResourceType = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type;

	[TestClass]
	public sealed class ResourcePartialFieldMapperTests
	{
		[TestMethod]
		public void CreateSelectedFields_AlwaysSelectsTheTypeAndEachDomFieldOnce()
		{
			var selectedFields = ResourcePartialFieldMapper.CreateSelectedFields(new FieldExposer[] { ResourceExposers.Name, ResourceExposers.Type, ElementResourceExposers.AgentId, ElementResourceExposers.ElementId });

			CollectionAssert.AreEquivalent(
				new[] { FieldName(ResourceInfo.Type), FieldName(ResourceInfo.Name), FieldName(ResourceInternalProperties.ResourceMetadata), FieldName(ResourceInfo.Element) },
				selectedFields.Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_State_SelectsStatus()
		{
			CollectionAssert.AreEquivalent(
				new[] { FieldName(ResourceInfo.Type), "StatusId" },
				ResourcePartialFieldMapper.CreateSelectedFields(new[] { ResourceExposers.State }).Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_MultiSectionFields_SelectTheFieldsTheyAreCombinedWith()
		{
			var selectedFields = ResourcePartialFieldMapper.CreateSelectedFields(new FieldExposer[] { ResourceExposers.Capabilities.CapabilityId, ResourceExposers.Capacities.CapacityId, ResourceExposers.Properties.PropertyId });

			CollectionAssert.AreEquivalent(
				new[]
				{
					FieldName(ResourceInfo.Type),
					FieldName(Capabilities.ProfileParameterID),
					FieldName(Capabilities.StringValue),
					FieldName(Capacities.ProfileParameterID),
					FieldName(Capacities.DoubleMaxValue),
					FieldName(Capacities.DoubleMinValue),
					FieldName(Properties.Property),
					FieldName(Properties.PropertyValue),
				},
				selectedFields.Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_UnsupportedField_ThrowsNotSupportedException()
		{
			var unknownField = new Exposer<Resource, string>(x => x.Name, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => ResourcePartialFieldMapper.CreateSelectedFields(new[] { unknownField }));
			Assert.ThrowsException<NotSupportedException>(() => ResourcePartialFieldMapper.CreateSelectedFields(new[] { JobExposers.Name }));
		}

		[TestMethod]
		public void CreatePartialResource_ConvertsValuesLikeAFullRead()
		{
			var id = Guid.NewGuid();
			var poolId = Guid.NewGuid();
			var otherPoolId = Guid.NewGuid();

			var domPartialObject = CreateDomPartialObject(
				id,
				ResourceType.Unmanaged,
				Field<string>(ResourceInfo.Name, "Resource"),
				Field<bool>(ResourceInfo.Favorite, true),
				Field<long>(ResourceInfo.Concurrency, 5),
				Field<string>(ResourceInternalProperties.Pool_Ids, $"{poolId};{otherPoolId};{poolId}"),
				("StatusId", new PartialObjectValue<string> { Value = SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Complete }));

			var partialResource = CreatePartialResource(domPartialObject, ResourceExposers.Id, ResourceExposers.Name, ResourceExposers.IsFavorite, ResourceExposers.Concurrency, ResourceExposers.State, ResourceExposers.ResourcePoolIds, ResourceExposers.Type);

			Assert.AreEqual(id, partialResource.Id);
			Assert.AreEqual(id, partialResource.GetValue(ResourceExposers.Id));
			Assert.AreEqual("Resource", partialResource.GetValue(ResourceExposers.Name));
			Assert.IsTrue(partialResource.GetValue(ResourceExposers.IsFavorite));
			Assert.AreEqual(5, partialResource.GetValue(ResourceExposers.Concurrency));
			Assert.AreEqual(ResourceState.Complete, partialResource.GetValue(ResourceExposers.State));
			CollectionAssert.AreEqual(new[] { poolId, otherPoolId }, partialResource.GetValues(ResourceExposers.ResourcePoolIds).ToList(), "Pool IDs are unique, like a full read.");
			Assert.AreEqual(typeof(UnmanagedResource), partialResource.GetValue(ResourceExposers.Type));
		}

		[TestMethod]
		public void CreatePartialResource_MissingValues_UseTheDefaultsOfAFullRead()
		{
			var partialResource = CreatePartialResource(
				CreateDomPartialObject(Guid.NewGuid(), ResourceType.Unmanaged),
				ResourceExposers.Name,
				ResourceExposers.IsFavorite,
				ResourceExposers.Concurrency,
				ResourceExposers.ResourcePoolIds,
				ResourceExposers.Capabilities.CapabilityId,
				ResourceExposers.Capabilities.Discretes,
				ResourceExposers.Capacities.CapacityId,
				ResourceExposers.Properties.PropertyId,
				ResourceExposers.Properties.Value);

			Assert.IsNull(partialResource.GetValue(ResourceExposers.Name));
			Assert.IsFalse(partialResource.GetValue(ResourceExposers.IsFavorite));
			Assert.AreEqual(1, partialResource.GetValue(ResourceExposers.Concurrency));
			Assert.AreEqual(0, partialResource.GetValues(ResourceExposers.ResourcePoolIds).Count());
			Assert.AreEqual(0, partialResource.GetValues(ResourceExposers.Capabilities.CapabilityId).Count());
			Assert.AreEqual(0, partialResource.GetValues(ResourceExposers.Capabilities.Discretes).Count());
			Assert.AreEqual(0, partialResource.GetValues(ResourceExposers.Capacities.CapacityId).Count());
			Assert.AreEqual(0, partialResource.GetValues(ResourceExposers.Properties.PropertyId).Count());
			Assert.AreEqual(0, partialResource.GetValues(ResourceExposers.Properties.Value).Count());
		}

		[TestMethod]
		public void CreatePartialResources_ResourcesWithoutKnownType_AreSkippedLikeAFullRead()
		{
			var resourceId = Guid.NewGuid();

			var partialResources = ResourcePartialFieldMapper.CreatePartialResources(
				null,
				new[]
				{
					CreateDomPartialObject(Guid.NewGuid(), null),
					CreateDomPartialObject(Guid.NewGuid(), (ResourceType)99),
					CreateDomPartialObject(resourceId, ResourceType.Service),
				},
				new[] { ResourceExposers.Type });

			Assert.AreEqual(resourceId, partialResources.Single().Id);
			Assert.AreEqual(typeof(ServiceResource), partialResources.Single().GetValue(ResourceExposers.Type));
		}

		[TestMethod]
		public void CreatePartialResource_TypeSpecificFields_AreReadFromTheMetadataOfThatType()
		{
			var functionId = Guid.NewGuid();
			var fields = TypeSpecificFields;

			var element = CreatePartialResource(CreateDomPartialObject(Guid.NewGuid(), ResourceType.Element, Metadata("{\"LinkedElementInfo\":\"100/200\"}")), fields);
			var legacyElement = CreatePartialResource(CreateDomPartialObject(Guid.NewGuid(), ResourceType.Element, Field<string>(ResourceInfo.Element, "101/201")), fields);
			var service = CreatePartialResource(CreateDomPartialObject(Guid.NewGuid(), ResourceType.Service, Metadata("{\"LinkedServiceInfo\":\"100/20\"}")), fields);
			var virtualFunction = CreatePartialResource(
				CreateDomPartialObject(Guid.NewGuid(), ResourceType.VirtualFunction, Metadata($"{{\"LinkedElementInfo\":\"100/200\",\"LinkedFunctionId\":\"{functionId}\",\"LinkedFunctionTableIndex\":\"1\"}}")),
				fields);
			var unmanaged = CreatePartialResource(CreateDomPartialObject(Guid.NewGuid(), ResourceType.Unmanaged, Metadata("{\"LinkedElementInfo\":\"100/200\"}")), fields);

			AssertTypeSpecificFields(element, elementAgent: 100, element: 200);
			AssertTypeSpecificFields(legacyElement, elementAgent: 101, element: 201);
			AssertTypeSpecificFields(service, serviceAgent: 100, service: 20);
			AssertTypeSpecificFields(virtualFunction, functionAgent: 100, functionElement: 200, functionId: functionId, functionTableIndex: "1");
			AssertTypeSpecificFields(unmanaged);
		}

		[TestMethod]
		public void CreatePartialResource_TypeSpecificFieldsWithoutMetadata_UseTheDefaultsOfAFullRead()
		{
			var element = CreatePartialResource(CreateDomPartialObject(Guid.NewGuid(), ResourceType.Element, Metadata("invalid")), TypeSpecificFields);
			var virtualFunction = CreatePartialResource(CreateDomPartialObject(Guid.NewGuid(), ResourceType.VirtualFunction), TypeSpecificFields);

			AssertTypeSpecificFields(element, elementAgent: 0, element: 0);
			AssertTypeSpecificFields(virtualFunction, functionAgent: 0, functionElement: 0, functionId: Guid.Empty, functionTableIndex: null);
		}

		[TestMethod]
		public void CreatePartialResource_CapabilitySections_AreParsedLikeAFullRead()
		{
			var location = Guid.NewGuid();
			var resolution = Guid.NewGuid();

			// Discretes are unique per section, and a section without discretes stores an empty string.
			var domPartialObject = CreateDomPartialObject(
				Guid.NewGuid(),
				ResourceType.Unmanaged,
				Fields(Capabilities.ProfileParameterID, location.ToString(), resolution.ToString(), "invalid"),
				Fields(Capabilities.StringValue, "USA;Belgium;USA", "4K;;USA", String.Empty));

			var partialResource = CreatePartialResource(domPartialObject, ResourceExposers.Capabilities.CapabilityId, ResourceExposers.Capabilities.Discretes);

			CollectionAssert.AreEqual(new[] { location, resolution, Guid.Empty }, partialResource.GetValues(ResourceExposers.Capabilities.CapabilityId).ToList());
			CollectionAssert.AreEqual(new[] { "USA", "Belgium", "4K", "USA" }, partialResource.GetValues(ResourceExposers.Capabilities.Discretes).ToList());
		}

		[TestMethod]
		public void CreatePartialResource_PropertySections_AreParsedLikeAFullRead()
		{
			var channel = Guid.NewGuid();
			var format = Guid.NewGuid();

			// The second property has no value, which a full read skips for the values but not for the IDs.
			var domPartialObject = CreateDomPartialObject(
				Guid.NewGuid(),
				ResourceType.Unmanaged,
				Fields(Properties.Property, channel, format),
				Fields(Properties.PropertyValue, "VRT"));

			var partialResource = CreatePartialResource(domPartialObject, ResourceExposers.Properties.PropertyId, ResourceExposers.Properties.Value);

			CollectionAssert.AreEqual(new[] { channel, format }, partialResource.GetValues(ResourceExposers.Properties.PropertyId).ToList());
			CollectionAssert.AreEqual(new[] { "VRT" }, partialResource.GetValues(ResourceExposers.Properties.Value).ToList());
		}

		[TestMethod]
		public void CreatePartialResource_CapacitySections_ListNumberCapacitiesBeforeRangeCapacities()
		{
			var range = Guid.NewGuid();
			var number = Guid.NewGuid();
			var otherNumber = Guid.NewGuid();
			IReadOnlyCollection<Guid>? requestedIds = null;

			var domPartialObject = CreateDomPartialObject(
				Guid.NewGuid(),
				ResourceType.Unmanaged,
				Fields(Capacities.ProfileParameterID, range.ToString(), number.ToString(), otherNumber.ToString()),
				Fields(Capacities.DoubleMaxValue, 10d, 20d, 30d),
				Fields(Capacities.DoubleMinValue, 5d));

			var partialResource = ResourcePartialFieldMapper.CreatePartialResources(
				null,
				new[] { domPartialObject },
				new[] { ResourceExposers.Capacities.CapacityId },
				ids =>
				{
					requestedIds = ids;
					return new[] { range };
				}).Single();

			CollectionAssert.AreEquivalent(new[] { range, number, otherNumber }, requestedIds!.ToList());
			CollectionAssert.AreEqual(new[] { number, otherNumber, range }, partialResource.GetValues(ResourceExposers.Capacities.CapacityId).ToList());
		}

		[TestMethod]
		public void CreatePartialResource_SectionsThatCantBeCombined_ReadTheFullInstance()
		{
			// One of the two capability sections has no profile parameter, so the IDs can't be matched to the sections by position.
			var capabilities = CreateDomPartialObject(
				Guid.NewGuid(),
				ResourceType.Unmanaged,
				Fields(Capabilities.ProfileParameterID, Guid.NewGuid().ToString()),
				Fields(Capabilities.StringValue, "A", "B"));

			// The property ID is missing on the section that has a value.
			var properties = CreateDomPartialObject(
				Guid.NewGuid(),
				ResourceType.Unmanaged,
				Fields(Properties.PropertyValue, "VRT"));

			var capabilitiesException = Assert.ThrowsException<InvalidOperationException>(() => CreatePartialResource(capabilities, ResourceExposers.Capabilities.CapabilityId));
			var propertiesException = Assert.ThrowsException<InvalidOperationException>(() => CreatePartialResource(properties, ResourceExposers.Properties.PropertyId));

			StringAssert.Contains(capabilitiesException.Message, "stored outside the resource DOM instance");
			StringAssert.Contains(propertiesException.Message, "stored outside the resource DOM instance");
		}

		private static FieldExposer[] TypeSpecificFields => new FieldExposer[]
		{
			ElementResourceExposers.AgentId,
			ElementResourceExposers.ElementId,
			ServiceResourceExposers.AgentId,
			ServiceResourceExposers.ServiceId,
			VirtualFunctionResourceExposers.AgentId,
			VirtualFunctionResourceExposers.ElementId,
			VirtualFunctionResourceExposers.FunctionId,
			VirtualFunctionResourceExposers.FunctionTableIndex,
		};

		private static void AssertTypeSpecificFields(
			Skyline.DataMiner.SDM.PartialObject<Resource> partialResource,
			int elementAgent = -1,
			int element = -1,
			int serviceAgent = -1,
			int service = -1,
			int functionAgent = -1,
			int functionElement = -1,
			Guid functionId = default,
			string? functionTableIndex = null)
		{
			Assert.AreEqual(elementAgent, partialResource.GetValue(ElementResourceExposers.AgentId));
			Assert.AreEqual(element, partialResource.GetValue(ElementResourceExposers.ElementId));
			Assert.AreEqual(serviceAgent, partialResource.GetValue(ServiceResourceExposers.AgentId));
			Assert.AreEqual(service, partialResource.GetValue(ServiceResourceExposers.ServiceId));
			Assert.AreEqual(functionAgent, partialResource.GetValue(VirtualFunctionResourceExposers.AgentId));
			Assert.AreEqual(functionElement, partialResource.GetValue(VirtualFunctionResourceExposers.ElementId));
			Assert.AreEqual(functionId, partialResource.GetValue(VirtualFunctionResourceExposers.FunctionId));
			Assert.AreEqual(functionTableIndex, partialResource.GetValue(VirtualFunctionResourceExposers.FunctionTableIndex));
		}

		private static Skyline.DataMiner.SDM.PartialObject<Resource> CreatePartialResource(PartialObject<DomInstance, DomInstanceId> domPartialObject, params FieldExposer[] fields)
		{
			return ResourcePartialFieldMapper.CreatePartialResources(null, new[] { domPartialObject }, fields).Single();
		}

		private static string FieldName(FieldDescriptorID descriptor)
		{
			return $"FieldValues.{descriptor.Id}";
		}

		private static (string Key, IPartialObjectValue Value) Metadata(string json)
		{
			return Field<string>(ResourceInternalProperties.ResourceMetadata, json);
		}

		// The DOM stores a selected field value as a list under the name of its field exposer, to support multiple sections.
		private static (string Key, IPartialObjectValue Value) Field<T>(FieldDescriptorID descriptor, T value)
		{
			return Fields(descriptor, value);
		}

		private static (string Key, IPartialObjectValue Value) Fields<T>(FieldDescriptorID descriptor, params T[] values)
		{
			return (FieldName(descriptor), new PartialObjectValue<List<T>> { Value = values.ToList() });
		}

		private static PartialObject<DomInstance, DomInstanceId> CreateDomPartialObject(Guid id, ResourceType? type, params (string Key, IPartialObjectValue Value)[] values)
		{
			var allValues = values.ToList();
			if (type.HasValue)
			{
				allValues.Add(Field<int>(ResourceInfo.Type, (int)type.Value));
			}

			return new PartialObject<DomInstance, DomInstanceId>(new DomInstanceId(id), allValues.ToDictionary(x => x.Key, x => x.Value));
		}
	}
}
