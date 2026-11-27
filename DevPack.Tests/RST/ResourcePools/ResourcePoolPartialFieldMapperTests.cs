namespace RT_MediaOps.Plan.RST.ResourcePools
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

	using Capabilities = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCapabilities;
	using Links = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolLinks;
	using ResourcePoolInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInfo;
	using ResourcePoolOther = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolOther;
	using Resourceselectiontype = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Resourceselectiontype;

	[TestClass]
	public sealed class ResourcePoolPartialFieldMapperTests
	{
		[TestMethod]
		public void CreateSelectedFields_SelectsEachDomFieldOnce()
		{
			var selectedFields = ResourcePoolPartialFieldMapper.CreateSelectedFields(new FieldExposer[] { ResourcePoolExposers.IconImage, ResourcePoolExposers.HasIconImage, ResourcePoolExposers.Name });

			CollectionAssert.AreEquivalent(
				new[] { FieldName(ResourcePoolOther.IconImage), FieldName(ResourcePoolInfo.Name) },
				selectedFields.Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_StateOrOnlyId_SelectsStatus()
		{
			CollectionAssert.AreEqual(new[] { "StatusId" }, ResourcePoolPartialFieldMapper.CreateSelectedFields(new[] { ResourcePoolExposers.State }).Fields.Select(x => x.FieldName).ToList());
			CollectionAssert.AreEqual(new[] { "StatusId" }, ResourcePoolPartialFieldMapper.CreateSelectedFields(new[] { ResourcePoolExposers.Id }).Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreateSelectedFields_UnsupportedField_ThrowsNotSupportedException()
		{
			var unknownField = new Exposer<ResourcePool, string>(x => x.Name, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => ResourcePoolPartialFieldMapper.CreateSelectedFields(new[] { unknownField }));
			Assert.ThrowsException<NotSupportedException>(() => ResourcePoolPartialFieldMapper.CreateSelectedFields(new[] { ResourceExposers.Name }));
		}

		[TestMethod]
		public void CreateSelectedFields_WrongExposerType_ThrowsNotSupportedException()
		{
			var nameWithWrongType = new Exposer<ResourcePool, int>(x => x.Name.Length, ResourcePoolExposers.Name.fieldName);

			Assert.ThrowsException<NotSupportedException>(() => ResourcePoolPartialFieldMapper.CreateSelectedFields(new[] { nameWithWrongType }));
		}

		[TestMethod]
		public void CreateSelectedFields_MultiSectionFields_SelectTheFieldsTheyAreCombinedFrom()
		{
			var selectedFields = ResourcePoolPartialFieldMapper.CreateSelectedFields(new FieldExposer[] { ResourcePoolExposers.LinkedResourcePools.SelectionType, ResourcePoolExposers.Capabilities.CapabilityId });

			CollectionAssert.AreEquivalent(
				new[]
				{
					FieldName(Links.LinkedResourcePool),
					FieldName(Links.ResourceSelectionType),
					FieldName(Capabilities.ProfileParameterID),
					FieldName(Capabilities.StringValue),
				},
				selectedFields.Fields.Select(x => x.FieldName).ToList());
		}

		[TestMethod]
		public void CreatePartialResourcePool_ConvertsValuesLikeAFullRead()
		{
			var id = Guid.NewGuid();

			var domPartialObject = CreateDomPartialObject(
				id,
				Field<string>(ResourcePoolInfo.Name, "Pool"),
				Field<string>(ResourcePoolInfo.Category, "Category"),
				Field<string>(ResourcePoolOther.IconImage, "icon.png"),
				("StatusId", new PartialObjectValue<string> { Value = SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Statuses.Complete }));

			var partialPool = CreatePartialResourcePool(
				domPartialObject,
				ResourcePoolExposers.Id,
				ResourcePoolExposers.Name,
				ResourcePoolExposers.State,
				ResourcePoolExposers.IconImage,
				ResourcePoolExposers.HasIconImage,
				ResourcePoolExposers.CategoryId,
				ResourcePoolExposers.HasCategory);

			Assert.AreEqual(id, partialPool.GetValue(ResourcePoolExposers.Id));
			Assert.AreEqual("Pool", partialPool.GetValue(ResourcePoolExposers.Name));
			Assert.AreEqual(ResourcePoolState.Complete, partialPool.GetValue(ResourcePoolExposers.State));
			Assert.AreEqual("icon.png", partialPool.GetValue(ResourcePoolExposers.IconImage));
			Assert.IsTrue(partialPool.GetValue(ResourcePoolExposers.HasIconImage));
			Assert.AreEqual("Category", partialPool.GetValue(ResourcePoolExposers.CategoryId));
			Assert.IsTrue(partialPool.GetValue(ResourcePoolExposers.HasCategory));
		}

		[TestMethod]
		public void CreatePartialResourcePool_MissingValues_UseTheDefaultsOfAFullRead()
		{
			var partialPool = CreatePartialResourcePool(
				CreateDomPartialObject(Guid.NewGuid()),
				ResourcePoolExposers.Name,
				ResourcePoolExposers.Url,
				ResourcePoolExposers.HasUrl,
				ResourcePoolExposers.HasIconImage,
				ResourcePoolExposers.HasCategory,
				ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId,
				ResourcePoolExposers.LinkedResourcePools.SelectionType,
				ResourcePoolExposers.Capabilities.CapabilityId,
				ResourcePoolExposers.Capabilities.Discretes);

			Assert.IsNull(partialPool.GetValue(ResourcePoolExposers.Name));
			Assert.IsNull(partialPool.GetValue(ResourcePoolExposers.Url));
			Assert.IsFalse(partialPool.GetValue(ResourcePoolExposers.HasUrl));
			Assert.IsFalse(partialPool.GetValue(ResourcePoolExposers.HasIconImage));
			Assert.IsFalse(partialPool.GetValue(ResourcePoolExposers.HasCategory));
			Assert.AreEqual(0, partialPool.GetValues(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId).Count());
			Assert.AreEqual(0, partialPool.GetValues(ResourcePoolExposers.LinkedResourcePools.SelectionType).Count());
			Assert.AreEqual(0, partialPool.GetValues(ResourcePoolExposers.Capabilities.CapabilityId).Count());
			Assert.AreEqual(0, partialPool.GetValues(ResourcePoolExposers.Capabilities.Discretes).Count());
		}

		[TestMethod]
		public void CreatePartialResourcePool_LinkSections_AreCombinedPerSection()
		{
			var poolA = Guid.NewGuid();
			var poolB = Guid.NewGuid();

			// The empty link is ignored, the same way a full read ignores links without a resource pool.
			var domPartialObject = CreateDomPartialObject(
				Guid.NewGuid(),
				Fields(Links.LinkedResourcePool, poolA, Guid.Empty, poolB),
				Fields(Links.ResourceSelectionType, (int)Resourceselectiontype.Automatic, (int)Resourceselectiontype.Automatic, (int)Resourceselectiontype.Manual));

			var partialPool = CreatePartialResourcePool(domPartialObject, ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId, ResourcePoolExposers.LinkedResourcePools.SelectionType);

			CollectionAssert.AreEqual(new[] { poolA, poolB }, partialPool.GetValues(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId).ToList());
			CollectionAssert.AreEqual(new[] { ResourceSelectionType.Automatic, ResourceSelectionType.Manual }, partialPool.GetValues(ResourcePoolExposers.LinkedResourcePools.SelectionType).ToList());
		}

		[TestMethod]
		public void CreatePartialResourcePool_CapabilitySections_AreParsedLikeAFullRead()
		{
			var capabilityA = Guid.NewGuid();
			var capabilityB = Guid.NewGuid();

			var domPartialObject = CreateDomPartialObject(
				Guid.NewGuid(),
				Fields(Capabilities.ProfileParameterID, capabilityA.ToString(), "invalid", capabilityB.ToString()),
				Fields(Capabilities.StringValue, "Low;Medium;Low", "4K"));

			var partialPool = CreatePartialResourcePool(domPartialObject, ResourcePoolExposers.Capabilities.CapabilityId, ResourcePoolExposers.Capabilities.Discretes);

			CollectionAssert.AreEqual(new[] { capabilityA, Guid.Empty, capabilityB }, partialPool.GetValues(ResourcePoolExposers.Capabilities.CapabilityId).ToList());
			CollectionAssert.AreEqual(new[] { "Low", "Medium", "4K" }, partialPool.GetValues(ResourcePoolExposers.Capabilities.Discretes).ToList());
		}

		[TestMethod]
		public void CreatePartialResourcePool_SectionsThatCantBeCombined_ReadTheFullInstance()
		{
			// One of the two links has no selection type, so the types can't be matched to the links by position.
			var linkObject = CreateDomPartialObject(
				Guid.NewGuid(),
				Fields(Links.LinkedResourcePool, Guid.NewGuid(), Guid.NewGuid()),
				Fields(Links.ResourceSelectionType, (int)Resourceselectiontype.Automatic));

			// One of the two capabilities has no profile parameter, so the IDs can't be matched to the capabilities by position.
			var capabilityObject = CreateDomPartialObject(
				Guid.NewGuid(),
				Fields(Capabilities.ProfileParameterID, Guid.NewGuid().ToString()),
				Fields(Capabilities.StringValue, "A", "B"));

			var linkException = Assert.ThrowsException<InvalidOperationException>(() => CreatePartialResourcePool(linkObject, ResourcePoolExposers.LinkedResourcePools.SelectionType));
			StringAssert.Contains(linkException.Message, "full resource pool DOM instance");

			var capabilityException = Assert.ThrowsException<InvalidOperationException>(() => CreatePartialResourcePool(capabilityObject, ResourcePoolExposers.Capabilities.CapabilityId));
			StringAssert.Contains(capabilityException.Message, "full resource pool DOM instance");

			// Fields that don't need to be matched per section are still read from the partial instance.
			var partialPool = CreatePartialResourcePool(linkObject, ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId);
			Assert.AreEqual(2, partialPool.GetValues(ResourcePoolExposers.LinkedResourcePools.LinkedResourcePoolId).Count());
		}

		private static Skyline.DataMiner.SDM.PartialObject<ResourcePool> CreatePartialResourcePool(PartialObject<DomInstance, DomInstanceId> domPartialObject, params FieldExposer[] fields)
		{
			return ResourcePoolPartialFieldMapper.CreatePartialResourcePools(null, new[] { domPartialObject }, fields).Single();
		}

		private static string FieldName(FieldDescriptorID descriptor)
		{
			return $"FieldValues.{descriptor.Id}";
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

		private static PartialObject<DomInstance, DomInstanceId> CreateDomPartialObject(Guid id, params (string Key, IPartialObjectValue Value)[] values)
		{
			return new PartialObject<DomInstance, DomInstanceId>(new DomInstanceId(id), values.ToDictionary(x => x.Key, x => x.Value));
		}
	}
}
