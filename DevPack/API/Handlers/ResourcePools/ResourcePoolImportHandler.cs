namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using CoreResource = Net.Messages.Resource;
	using CoreResourcePool = Net.Messages.ResourcePool;
	using DomResource = Storage.DOM.SlcResource_Studio.ResourceInstance;
	using DomResourcePool = Storage.DOM.SlcResource_Studio.ResourcepoolInstance;

	internal class ResourcePoolImportHandler
	{
		private readonly MediaOpsPlanApi planApi;

		private Dictionary<Guid, Net.Messages.FunctionDefinition> functionDefinitionsById;

		private ResourcePoolImportHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		public static void Import(MediaOpsPlanApi planApi, ICollection<CoreResourcePool> coreResourcePools)
		{
			var handler = new ResourcePoolImportHandler(planApi);
			handler.Import(coreResourcePools);
		}

		private void Import(ICollection<CoreResourcePool> coreResourcePools)
		{
			if (coreResourcePools == null)
			{
				throw new ArgumentNullException(nameof(coreResourcePools));
			}

			if (coreResourcePools.Count == 0)
			{
				return;
			}

			var validCoreResourcePoolIds = coreResourcePools.Where(x => x.ID != Guid.Empty).Select(x => x.ID).Distinct().ToList();
			var invalidCoreResourcePoolIds = coreResourcePools.Select(x => x.ID).Except(validCoreResourcePoolIds).Where(x => x != Guid.Empty).ToList();
			if (invalidCoreResourcePoolIds.Count > 0)
			{
				planApi.Logger.Error(this, $"Failed to find CORE resource pools with IDs: {string.Join("|", invalidCoreResourcePoolIds)}.");
			}

			var coreResourcePoolsById = planApi.CoreHelpers.ResourceManagerHelper.GetResourcePoolsInBatches(validCoreResourcePoolIds);
			var resourcePoolMappings = GetOrCreateDomResourcePools(coreResourcePoolsById.Values.ToList()).ToList();

			var coreResources = planApi.CoreHelpers.ResourceManagerHelper.GetResources(new ORFilterElement<CoreResource>(coreResourcePoolsById.Keys.Select(x => Net.Messages.ResourceExposers.PoolGUIDs.Contains(x)).ToArray())).ToList();
			var resourceMappings = BuildDomResources(coreResources).ToList();

			SetPoolLinks(resourceMappings, resourcePoolMappings);

			planApi.CoreHelpers.ResourceManagerHelper.TryCreateOrUpdateResourcesInBatches(resourceMappings.Select(x => x.Core), out var coreResult);

			var domResourcesToCreate = resourceMappings.Where(x => coreResult.SuccessfulIds.Contains(x.Core.ID)).Select(x => x.Dom).ToList();
			planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(domResourcesToCreate.Select(x => x.ToInstance()), out var domResult);
			foreach (var domInstanceId in domResult.SuccessfulIds)
			{
				planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.DoStatusTransition(domInstanceId, SlcResource_StudioIds.Behaviors.Resource_Behavior.Transitions.Draft_To_Complete);
			}
		}

		private IEnumerable<ResourcePoolMapping> GetOrCreateDomResourcePools(ICollection<CoreResourcePool> coreResourcePools)
		{
			var corePoolsById = coreResourcePools.ToDictionary(x => x.ID);

			FilterElement<DomInstance> filter(Guid coreResourcePoolId) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resourcepool.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Resource_Pool_Id).Equal(Convert.ToString(coreResourcePoolId)));

			var domResourcePoolsByCoreId = planApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(corePoolsById.Keys, filter).ToDictionary(x => x.ResourcePoolInternalProperties.ResourcePoolId);

			// Verify duplicate CORE names
			var toCreateCoreNameValidation = coreResourcePools.Where(x => !domResourcePoolsByCoreId.ContainsKey(x.ID)).ToList();

			var poolsWithDuplicateCoreNames = toCreateCoreNameValidation
				.GroupBy(pool => pool.Name)
				.Where(g => g.Count() > 1)
				.SelectMany(x => x)
				.ToList();

			foreach (var pool in poolsWithDuplicateCoreNames)
			{
				planApi.Logger.Information(this, $"CORE resource pool '{pool.Name}' has a duplicate name.");
				corePoolsById.Remove(pool.ID);
			}

			// Verify duplicate DOM names
			var toCreateDomNameValidation = toCreateCoreNameValidation.Where(x => corePoolsById.ContainsKey(x.ID)).ToList();

			FilterElement<DomInstance> domNameFilter(string name) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resourcepool.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourcePoolInfo.Name).Equal(name));

			var domPoolsbyName = planApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(toCreateDomNameValidation.Select(x => x.Name), domNameFilter)
				.GroupBy(x => x.Name)
				.ToDictionary(x => x.Key, x => (IReadOnlyCollection<DomResourcePool>)x.ToList());

			foreach (var pool in toCreateDomNameValidation)
			{
				if (!domPoolsbyName.TryGetValue(pool.Name, out var domPools))
				{
					continue;
				}

				planApi.Logger.Information(this, $"Name '{pool.Name}' is already in use by DOM resource pool(s) with ID(s)", [domPools.Select(x => x.ID.Id).ToArray()]);
				corePoolsById.Remove(pool.ID);
			}

			var toCreate = toCreateDomNameValidation
				.Where(x => corePoolsById.ContainsKey(x.ID))
				.Select(x =>
					{
						var domPool = new DomResourcePool();
						domPool.ResourcePoolInfo.Name = x.Name;
						domPool.ResourcePoolInternalProperties.ResourcePoolId = x.ID;

						return domPool;
					})
				.ToList();

			planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(toCreate.Select(x => x.ToInstance()), out var domResult);
			foreach (var domInstanceId in domResult.SuccessfulIds)
			{
				planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.DoStatusTransition(domInstanceId, SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Transitions.Draft_To_Complete);
			}

			foreach (var domPool in domResourcePoolsByCoreId.Values.Concat(domResult.SuccessfulItems.Select(x => new DomResourcePool(x))))
			{
				if (!corePoolsById.TryGetValue(domPool.ResourcePoolInternalProperties.ResourcePoolId, out var corePool))
				{
					continue;
				}

				yield return new ResourcePoolMapping
				{
					Core = corePool,
					Dom = domPool,
				};
			}
		}

		private IEnumerable<ResourceMapping> BuildDomResources(ICollection<CoreResource> coreResources)
		{
			var coreResourcesById = coreResources.ToDictionary(x => x.ID);

			FilterElement<DomInstance> filter(Guid coreResourceId) =>
			   DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id)
			   .AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.Resource_Id).Equal(coreResourceId));

			var domResourcesByCoreId = planApi.DomHelpers.SlcResourceStudioHelper.GetResources(coreResourcesById.Keys, filter).ToDictionary(x => x.ResourceInternalProperties.Resource_Id);

			// Verify duplicate CORE names
			var toCreateCoreNameValidation = coreResources.Where(x => !domResourcesByCoreId.ContainsKey(x.ID)).ToList();

			var resourcesWithDuplicateCoreNames = toCreateCoreNameValidation
				.GroupBy(pool => pool.Name)
				.Where(g => g.Count() > 1)
				.SelectMany(x => x)
				.ToList();

			foreach (var resource in resourcesWithDuplicateCoreNames)
			{
				planApi.Logger.Information(this, $"CORE resource '{resource.Name}' has a duplicate name.");
				coreResourcesById.Remove(resource.ID);
			}

			// Verify duplicate DOM names
			var toCreateDomNameValidation = toCreateCoreNameValidation.Where(x => coreResourcesById.ContainsKey(x.ID)).ToList();

			FilterElement<DomInstance> domNameFilter(string name) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Name).Equal(name));

			var domResourcesbyName = planApi.DomHelpers.SlcResourceStudioHelper.GetResources(toCreateDomNameValidation.Select(x => x.Name), domNameFilter)
				.GroupBy(x => x.Name)
				.ToDictionary(x => x.Key, x => (IReadOnlyCollection<DomResource>)x.ToList());

			foreach (var resource in toCreateDomNameValidation)
			{
				if (!domResourcesbyName.TryGetValue(resource.Name, out var domResources))
				{
					continue;
				}

				planApi.Logger.Information(this, $"Name '{resource.Name}' is already in use by DOM resource(s) with ID(s)", [domResources.Select(x => x.ID.Id).ToArray()]);
			}

			var functionDefinitionIds = toCreateDomNameValidation.OfType<Net.ResourceManager.Objects.FunctionResource>().Select(x => x.FunctionGUID).Distinct().ToList();
			functionDefinitionsById = planApi.CoreHelpers.ProtocolFunctionHelper.GetFunctionDefinitions(functionDefinitionIds.Select(x => new Net.FunctionDefinitionID(x)).ToList(), false).OfType<Net.Messages.FunctionDefinition>().ToDictionary(x => x.GUID);

			foreach (var coreResource in toCreateDomNameValidation.Where(x => coreResourcesById.ContainsKey(x.ID)))
			{
				var domResource = BuildDomResource(coreResource);
				if (domResource != null)
				{
					yield return new ResourceMapping
					{
						Core = coreResource,
						Dom = domResource,
					};
				}
			}
		}

		private DomResource BuildDomResource(CoreResource coreResource)
		{
			var domResource = new DomResource();
			domResource.ResourceInfo.Name = coreResource.Name;
			domResource.ResourceInfo.Concurrency = coreResource.MaxConcurrency;
			domResource.ResourceInternalProperties.Resource_Id = coreResource.ID;

			foreach (var resourceCapacity in coreResource.Capacities)
			{
				domResource.ResourceCapacities.Add(new ResourceCapacitiesSection
				{
					ProfileParameterId = resourceCapacity.CapacityProfileID,
					DoubleMaxValue = (double)resourceCapacity.Value.MaxDecimalQuantity,
					DoubleMinValue = resourceCapacity.Value.MinDecimalQuantity.HasValue ? (double)resourceCapacity.Value.MinDecimalQuantity : null
				});
			}

			foreach (var resourceCapability in coreResource.Capabilities)
			{
				domResource.ResourceCapabilities.Add(new ResourceCapabilitiesSection
				{
					ProfileParameterId = resourceCapability.CapabilityProfileID,
					StringValue = string.Join(";", resourceCapability.Value.Discreets),
				});
			}

			if (!TryApplyResourceTypeSpecifics(domResource, coreResource))
			{
				return null;
			}

			return domResource;
		}

		private bool TryApplyResourceTypeSpecifics(DomResource domResource, CoreResource coreResource)
		{
			if (coreResource is Net.ResourceManager.Objects.FunctionResource functionResource)
			{
				domResource.ResourceInfo.Type = SlcResource_StudioIds.Enums.Type.VirtualFunction;
				domResource.ResourceInternalProperties.Metadata.LinkedElementInfo = new DmsElementId(functionResource.MainDVEDmaID, functionResource.MainDVEElementID).Value;
				domResource.ResourceInternalProperties.Metadata.LinkedFunctionId = functionResource.FunctionGUID;

				if (!functionDefinitionsById.TryGetValue(functionResource.FunctionGUID, out var functionDefinition))
				{
					planApi.Logger.Error($"No function definition found with ID '{functionResource.FunctionGUID}'. Skip import resource {coreResource.Name} ({coreResource.ID}).");
					return false;
				}

				if (functionDefinition.EntryPoints.Any())
				{
					if (functionResource.HasValidLinks() && functionResource.LinkerTableEntries.Any())
					{
						domResource.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex = functionResource.LinkerTableEntries.First().Item2;
					}
					else
					{
						planApi.Logger.Error($"No valid table link found. Skip import resource {coreResource.Name} ({coreResource.ID}).");
						return false;
					}
				}

				SetResourceType(coreResource, "Virtual Function");
			}
			else if (coreResource.DmaID > 0 && coreResource.ElementID > 0)
			{
				domResource.ResourceInfo.Type = SlcResource_StudioIds.Enums.Type.Element;
				domResource.ResourceInternalProperties.Metadata.LinkedElementInfo = new DmsElementId(coreResource.DmaID, coreResource.ElementID).Value;

				SetResourceType(coreResource, "Element");
			}
			else
			{
				domResource.ResourceInfo.Type = SlcResource_StudioIds.Enums.Type.Unmanaged;

				SetResourceType(coreResource, "Unlinked Resource");
			}

			return true;
		}

		private void SetResourceType(CoreResource coreResource, string resourceTypeValue)
		{
			var resourceTypeCapability = coreResource.Capabilities.FirstOrDefault(x => x.CapabilityProfileID == planApi.Capabilities.SystemCapabilities.ResourceType.ID);
			var capabilityValue = new Net.Profiles.CapabilityParameterValue(new List<string> { resourceTypeValue });
			if (resourceTypeCapability == null)
			{
				coreResource.Capabilities.Add(new Net.SRM.Capabilities.ResourceCapability(planApi.Capabilities.SystemCapabilities.ResourceType.ID)
				{
					Value = capabilityValue,
				});
			}
			else if (!resourceTypeCapability.Value.Equals(capabilityValue))
			{
				resourceTypeCapability.Value = capabilityValue;
			}
		}

		private void SetPoolLinks(List<ResourceMapping> resourceMappings, List<ResourcePoolMapping> resourcePoolMappings)
		{
			var poolMappingsByCoreId = resourcePoolMappings.ToDictionary(x => x.Core.ID);

			foreach (var resourceMapping in resourceMappings)
			{
				var linkedPoolIds = new HashSet<Guid>();
				foreach (var coreResourcePoolId in resourceMapping.Core.PoolGUIDs)
				{
					if (!poolMappingsByCoreId.TryGetValue(coreResourcePoolId, out var poolMapping))
					{
						continue;
					}

					linkedPoolIds.Add(poolMapping.Dom.ID.Id);
				}

				resourceMapping.Dom.ResourceInternalProperties.PoolIds = linkedPoolIds.ToList();
			}
		}

		private sealed class ResourcePoolMapping
		{
			public DomResourcePool Dom { get; set; }

			public CoreResourcePool Core { get; set; }
		}

		private sealed class ResourceMapping
		{
			public DomResource Dom { get; set; }

			public CoreResource Core { get; set; }
		}
	}
}
