namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.SRM.Capacities;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

	using CoreFunctionResource = Net.ResourceManager.Objects.FunctionResource;
	using CoreResource = Net.Messages.Resource;
	using DomResource = Storage.DOM.SlcResource_Studio.ResourceInstance;
	using DomResourcePool = Storage.DOM.SlcResource_Studio.ResourcepoolInstance;

	internal class CoreResourceHandler
	{
		private readonly MediaOpsPlanApi planApi;

		private readonly List<DomResource> successfulItems = new List<DomResource>();
		private readonly List<Guid> unsuccessfulIds = new List<Guid>();
		private readonly Dictionary<Guid, MediaOpsTraceData> traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();
		private readonly Dictionary<Guid, Action<CoreResource>> enableDveActionByCoreId = new Dictionary<Guid, Action<CoreResource>>();

		private readonly IReadOnlyDictionary<Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type, Action<DomResource, CoreResource, ICollection<SynchronizationDifference>>> typeSyncers;

		private readonly Lazy<Dictionary<Guid, Net.Profiles.Parameter>> lazyCoreCapabilitiesById;
		private readonly Lazy<Dictionary<Guid, Net.Profiles.Parameter>> lazyCoreTimeDependentCapabilitiesById;
		private readonly Lazy<Dictionary<Guid, Net.Profiles.Parameter>> lazyCoreCapacitiesById;
		private readonly Lazy<DomCapabilitiesHandler> lazyCapabilitiesHandler;

		private CoreResourceHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));

			lazyCoreCapabilitiesById = new Lazy<Dictionary<Guid, Net.Profiles.Parameter>>(() => planApi.CoreHelpers.ProfileProvider.GetCapabilities(new TRUEFilterElement<Net.Profiles.Parameter>()).ToDictionary(x => x.ID));
			lazyCoreTimeDependentCapabilitiesById = new Lazy<Dictionary<Guid, Net.Profiles.Parameter>>(() => planApi.CoreHelpers.ProfileProvider.GetTimeDependentCapabilities(new TRUEFilterElement<Net.Profiles.Parameter>()).ToDictionary(x => x.ID));
			lazyCoreCapacitiesById = new Lazy<Dictionary<Guid, Net.Profiles.Parameter>>(() => planApi.CoreHelpers.ProfileProvider.GetCapacities(new TRUEFilterElement<Net.Profiles.Parameter>()).ToDictionary(x => x.ID));
			lazyCapabilitiesHandler = new Lazy<DomCapabilitiesHandler>(() => new DomCapabilitiesHandler(planApi));

			typeSyncers = new Dictionary<Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type, Action<DomResource, CoreResource, ICollection<SynchronizationDifference>>>
			{
				[Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.Unmanaged] = ApplyUnmanagedResourceConfig,
				[Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.Element] = ApplyElementResourceConfig,
				[Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.Service] = ApplyServiceResourceConfig,
				[Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.VirtualFunction] = ApplyVirtualFunctionResourceConfig,
			};
		}

		private Dictionary<Guid, Net.Profiles.Parameter> CoreCapabilitiesById => lazyCoreCapabilitiesById.Value;

		private Dictionary<Guid, Net.Profiles.Parameter> CoreTimeDependentCapabilitiesById => lazyCoreTimeDependentCapabilitiesById.Value;

		private Dictionary<Guid, Net.Profiles.Parameter> CoreCapacitiesById => lazyCoreCapacitiesById.Value;

		private DomCapabilitiesHandler CapabilitiesHandler => lazyCapabilitiesHandler.Value;

		public static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<DomResource> domResources, out DomInstanceBulkOperationResult<DomResource> result)
		{
			var handler = new CoreResourceHandler(planApi);
			ActivityHelper.Track(nameof(CoreResourceHandler), nameof(TryCreateOrUpdate), act => handler.CreateOrUpdate(domResources));

			result = new DomInstanceBulkOperationResult<DomResource>(handler.successfulItems, handler.unsuccessfulIds, handler.traceDataPerItem);

			return !result.HasFailures;
		}

		public static bool TryGetDifferences(MediaOpsPlanApi planApi, ICollection<DomResource> domResources, out SynchronizationDetectionResult result)
		{
			var handler = new CoreResourceHandler(planApi);
			var detection = new SynchronizationDetectionResult();
			ActivityHelper.Track(nameof(CoreResourceHandler), nameof(TryGetDifferences), act => handler.DetectDifferences(domResources, detection));

			result = detection;

			return result.IsSynchronized;
		}

		public static bool TryDelete(MediaOpsPlanApi planApi, ICollection<DomResource> domResources, out DomInstanceBulkOperationResult<DomResource> result)
		{
			var handler = new CoreResourceHandler(planApi);
			ActivityHelper.Track(nameof(CoreResourceHandler), nameof(TryDelete), act => handler.Delete(domResources));

			result = new DomInstanceBulkOperationResult<DomResource>(handler.successfulItems, handler.unsuccessfulIds, handler.traceDataPerItem);

			return !result.HasFailures;
		}

		public static bool TryDeprecate(MediaOpsPlanApi planApi, ICollection<DomResource> domResources, out DomInstanceBulkOperationResult<DomResource> result)
		{
			var handler = new CoreResourceHandler(planApi);
			handler.Deprecate(domResources);

			result = new DomInstanceBulkOperationResult<DomResource>(handler.successfulItems, handler.unsuccessfulIds, handler.traceDataPerItem);

			return !result.HasFailures;
		}

		public static bool TryRestore(MediaOpsPlanApi planApi, ICollection<DomResource> domResources, out DomInstanceBulkOperationResult<DomResource> result)
		{
			var handler = new CoreResourceHandler(planApi);
			handler.Restore(domResources);

			result = new DomInstanceBulkOperationResult<DomResource>(handler.successfulItems, handler.unsuccessfulIds, handler.traceDataPerItem);

			return !result.HasFailures;
		}

		public static bool TryValidateVirtualFunctionConfiguration(MediaOpsPlanApi planApi, ResourceVirtualFunctionLinkSetting setting, out ResourceError error)
		{
			error = null;

			var handler = new CoreResourceHandler(planApi);
			if (setting == null)
			{
				throw new ArgumentNullException(nameof(setting));
			}

			var elementId = new DmsElementId(setting.AgentId, setting.ElementId);
			if (!handler.TryValidateElementLink(elementId, out string invalidElementInfoReason))
			{
				error = new ResourceInvalidElementLinkError
				{
					ErrorMessage = invalidElementInfoReason,
					AgentId = setting.AgentId,
					ElementId = setting.ElementId,
				};

				return false;
			}

			if (!handler.TryValidateVirtualFunctionResourceFunctionDefinition(setting.FunctionId, out string invalidFunctionDefinitionReason))
			{
				error = new ResourceInvalidFunctionLinkError
				{
					ErrorMessage = invalidFunctionDefinitionReason,
					FunctionId = setting.FunctionId,
				};

				return false;
			}

			if (!handler.TryValidateVirtualFunctionResourceTableIndex(setting.FunctionId, elementId, setting.FunctionTableIndex, out string invalidTableIndexReason))
			{
				error = new ResourceInvalidTableIndexLinkError
				{
					ErrorMessage = invalidTableIndexReason,
					AgentId = setting.AgentId,
					ElementId = setting.ElementId,
					FunctionId = setting.FunctionId,
					FunctionTableIndex = setting.FunctionTableIndex,
				};

				return false;
			}

			return true;
		}

		public static bool TryValidateServiceConfiguration(MediaOpsPlanApi planApi, ResourceServiceLinkSetting setting, out ResourceError error)
		{
			error = null;

			var handler = new CoreResourceHandler(planApi);
			if (setting == null)
			{
				throw new ArgumentNullException(nameof(setting));
			}

			var serviceId = new DmsServiceId(setting.AgentId, setting.ServiceId);
			if (!handler.TryValidateServiceResourceServiceLink(serviceId, out var reason))
			{
				error = new ResourceInvalidServiceLinkError
				{
					ErrorMessage = reason,
					AgentId = setting.AgentId,
					ServiceId = setting.ServiceId,
				};

				return false;
			}

			return true;
		}

		public static bool TryValidateElementConfiguration(MediaOpsPlanApi planApi, ResourceElementLinkSetting setting, out ResourceError error)
		{
			error = null;

			var handler = new CoreResourceHandler(planApi);
			if (setting == null)
			{
				throw new ArgumentNullException(nameof(setting));
			}

			var elementId = new DmsElementId(setting.AgentId, setting.ElementId);
			if (!handler.TryValidateElementLink(elementId, out var reason))
			{
				error = new ResourceInvalidElementLinkError
				{
					ErrorMessage = reason,
					AgentId = setting.AgentId,
					ElementId = setting.ElementId,
				};

				return false;
			}

			return true;
		}

		private void Deprecate(ICollection<DomResource> domResources)
		{
			if (domResources == null)
			{
				throw new ArgumentNullException(nameof(domResources));
			}

			if (domResources.Count == 0)
			{
				return;
			}

			Deprecate(ResourceMapping.GetMappings(planApi, domResources).ToList());
		}

		private void Deprecate(ICollection<ResourceMapping> resourceMappings)
		{
			if (resourceMappings == null)
			{
				throw new ArgumentNullException(nameof(resourceMappings));
			}

			if (resourceMappings.Count == 0)
			{
				return;
			}

			var domInstanceByCoreId = new Dictionary<Guid, DomInstance>();
			var resourcesToDeprecate = new List<CoreResource>();

			foreach (var mapping in resourceMappings)
			{
				if (mapping.State != CoreResourceState.Existing)
				{
					AddCoreResourceNotFoundError(mapping);

					continue;
				}

				mapping.CoreResource.Mode = Net.Messages.ResourceMode.Unavailable;

				resourcesToDeprecate.Add(mapping.CoreResource);
				domInstanceByCoreId.Add(mapping.CoreResource.ID, mapping.DomResource);
			}

			planApi.CoreHelpers.ResourceManagerHelper.TryCreateOrUpdateResourcesInBatches(resourcesToDeprecate, out var result);

			foreach (var id in result.UnsuccessfulIds)
			{
				if (!domInstanceByCoreId.TryGetValue(id, out var domInstance))
				{
					planApi.Logger.Error(this, $"Failed to find DOM ID for CORE resource ID {id}.");
					continue;
				}

				unsuccessfulIds.Add(domInstance.ID.Id);

				if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					traceDataPerItem.Add(domInstance.ID.Id, traceData);
				}
			}

			foreach (var id in result.SuccessfulIds)
			{
				if (!domInstanceByCoreId.TryGetValue(id, out var domInstance))
				{
					planApi.Logger.Error(this, $"Failed to find DOM ID for CORE resource ID", [id]);
					continue;
				}

				successfulItems.Add(new DomResource(domInstance));
			}
		}

		private void Restore(ICollection<DomResource> domResources)
		{
			if (domResources == null)
			{
				throw new ArgumentNullException(nameof(domResources));
			}

			if (domResources.Count == 0)
			{
				return;
			}

			Restore(ResourceMapping.GetMappings(planApi, domResources).ToList());
		}

		private void Restore(ICollection<ResourceMapping> resourceMappings)
		{
			if (resourceMappings == null)
			{
				throw new ArgumentNullException(nameof(resourceMappings));
			}

			if (resourceMappings.Count == 0)
			{
				return;
			}

			var domResourceByCoreId = new Dictionary<Guid, DomResource>();
			var resourcesToRestore = new List<CoreResource>();

			foreach (var mapping in resourceMappings)
			{
				if (mapping.State != CoreResourceState.Existing)
				{
					AddCoreResourceNotFoundError(mapping);

					continue;
				}

				mapping.CoreResource.Mode = Net.Messages.ResourceMode.Available;

				resourcesToRestore.Add(mapping.CoreResource);
				domResourceByCoreId.Add(mapping.CoreResource.ID, mapping.DomResource);
			}

			planApi.CoreHelpers.ResourceManagerHelper.TryCreateOrUpdateResourcesInBatches(resourcesToRestore, out var result);

			foreach (var id in result.UnsuccessfulIds)
			{
				if (!domResourceByCoreId.TryGetValue(id, out var domResource))
				{
					planApi.Logger.Error(this, $"Failed to find DOM ID for CORE resource ID {id}.");
					continue;
				}

				unsuccessfulIds.Add(domResource.ID.Id);

				if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					traceDataPerItem.Add(domResource.ID.Id, traceData);
				}
			}

			foreach (var id in result.SuccessfulIds)
			{
				if (!domResourceByCoreId.TryGetValue(id, out var domResource))
				{
					planApi.Logger.Error(this, $"Failed to find DOM ID for CORE resource ID", [id]);
					continue;
				}

				successfulItems.Add(domResource);
			}
		}

		private void CreateOrUpdate(ICollection<DomResource> domResources)
		{
			if (domResources == null)
			{
				throw new ArgumentNullException(nameof(domResources));
			}

			if (domResources.Count == 0)
			{
				return;
			}

			var resourceMappingByDomId = ResourceMapping.GetMappings(planApi, domResources).ToDictionary(x => x.DomResource.ID.Id);

			ValidateMappings(resourceMappingByDomId, validateCompletedVirtualFunctionResources: false);

			CreateOrUpdate(resourceMappingByDomId.Where(x => !traceDataPerItem.Keys.Contains(x.Key)).Select(x => x.Value).ToList());
		}

		private void DetectDifferences(ICollection<DomResource> domResources, SynchronizationDetectionResult detection)
		{
			if (domResources == null)
			{
				throw new ArgumentNullException(nameof(domResources));
			}

			if (domResources.Count == 0)
			{
				return;
			}

			var resourceMappingByDomId = ResourceMapping.GetMappings(planApi, domResources).ToDictionary(x => x.DomResource.ID.Id);

			ValidateMappings(resourceMappingByDomId, validateCompletedVirtualFunctionResources: true);

			foreach (var entry in resourceMappingByDomId)
			{
				bool isBlocked = traceDataPerItem.TryGetValue(entry.Key, out var traceData);
				if (isBlocked)
				{
					detection.BlockersPerItem.Add(entry.Key, traceData);
				}

				if (entry.Value.State != CoreResourceState.Existing)
				{
					detection.DifferencesPerItem.Add(entry.Key, [new MissingCoreObjectDifference()]);
					continue;
				}

				if (isBlocked)
				{
					// A blocked resource cannot be synchronized, so comparing the remaining configuration adds no value.
					continue;
				}

				// The sync methods patch while they compare, so detection has to run against a copy.
				if (entry.Value.CoreResource.Clone() is not CoreResource coreResourceCopy)
				{
					throw new InvalidOperationException($"Failed to clone CORE resource {entry.Value.CoreResource.ID} for comparison.");
				}

				var differences = SyncDomResourceWithCoreResource(entry.Value.DomResource, coreResourceCopy);
				if (differences.Count > 0)
				{
					detection.DifferencesPerItem.Add(entry.Key, differences);
				}
			}
		}

		private void ValidateMappings(IReadOnlyDictionary<Guid, ResourceMapping> resourceMappingByDomId, bool validateCompletedVirtualFunctionResources)
		{
			var elementResourcesToValidate = new List<DomResource>();
			var serviceResourcesToValidate = new List<DomResource>();
			var virtualFunctionResourcesToValidate = new List<DomResource>();
			foreach (var resourceMapping in resourceMappingByDomId.Values)
			{
				if (resourceMapping.State == CoreResourceState.Missing)
				{
					AddCoreResourceNotFoundError(resourceMapping);
					continue;
				}

				if (resourceMapping.DomResource.ResourceInfo.Type == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.Element)
				{
					elementResourcesToValidate.Add(resourceMapping.DomResource);
				}
				else if (resourceMapping.DomResource.ResourceInfo.Type == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.Service)
				{
					serviceResourcesToValidate.Add(resourceMapping.DomResource);
				}
				else if (resourceMapping.DomResource.ResourceInfo.Type == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.VirtualFunction
					&& (validateCompletedVirtualFunctionResources || resourceMapping.DomResource.Status == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum.Draft))
				{
					virtualFunctionResourcesToValidate.Add(resourceMapping.DomResource);
				}
			}

			ValidateElementResources(elementResourcesToValidate);
			ValidateServiceResources(serviceResourcesToValidate);
			ValidateVirtualFunctionResources(virtualFunctionResourcesToValidate);
			ValidateNames(resourceMappingByDomId.Where(x => !traceDataPerItem.Keys.Contains(x.Key) && x.Value.NeedsNameValidation).Select(x => x.Value.DomResource).ToList());
		}

		private void CreateOrUpdate(ICollection<ResourceMapping> resourceMappings)
		{
			if (resourceMappings == null)
			{
				throw new ArgumentNullException(nameof(resourceMappings));
			}

			if (resourceMappings.Count == 0)
			{
				return;
			}

			var domResourceByCoreId = new Dictionary<Guid, DomResource>();

			var resourcesToCreateOrUpdate = new List<CoreResource>();
			foreach (var mapping in resourceMappings)
			{
				var dom = mapping.DomResource;
				var core = mapping.CoreResource;

				if (SyncDomResourceWithCoreResource(dom, core).Count == 0)
				{
					planApi.Logger.Information(this, $"No CORE changes for DOM resource {mapping.DomResource.ID}");
					continue;
				}

				resourcesToCreateOrUpdate.Add(core);

				domResourceByCoreId.Add(core.ID, dom);
			}

			planApi.CoreHelpers.ResourceManagerHelper.TryCreateOrUpdateResourcesInBatches(resourcesToCreateOrUpdate, out var result);

			foreach (var id in result.UnsuccessfulIds)
			{
				if (!domResourceByCoreId.TryGetValue(id, out var domResource))
				{
					planApi.Logger.Error(this, $"Failed to find DOM ID for CORE resource ID {id}.");
					continue;
				}

				unsuccessfulIds.Add(domResource.ID.Id);

				if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					traceDataPerItem.Add(domResource.ID.Id, traceData);
				}
			}

			var createdOrUpdatedResourcesByCoreId = result.SuccessfulItems?.Cast<CoreResource>()?.ToDictionary(x => x.ID) ?? new Dictionary<Guid, CoreResource>();
			foreach (var id in result.SuccessfulIds)
			{
				if (!domResourceByCoreId.TryGetValue(id, out var domResource))
				{
					planApi.Logger.Error(this, $"Failed to find DOM ID for CORE resource pool ID {id}.");
					continue;
				}

				domResource.ResourceInternalProperties.Resource_Id = id;

				if (enableDveActionByCoreId.TryGetValue(id, out var enableDveAction))
				{
					enableDveAction?.Invoke(createdOrUpdatedResourcesByCoreId[id]);
				}

				successfulItems.Add(domResource);
			}
		}

		private List<SynchronizationDifference> SyncDomResourceWithCoreResource(DomResource dom, CoreResource core)
		{
			var differences = new List<SynchronizationDifference>();

			SyncName(dom, core, differences);
			SyncType(dom, core, differences);
			SyncCapacities(dom, core, differences);
			SyncCapabilities(dom, core, differences);
			SyncConcurrency(dom, core, differences);
			SyncPools(dom, core, differences);

			return differences;
		}

		private void Delete(ICollection<DomResource> domResources)
		{
			if (domResources == null)
			{
				throw new ArgumentNullException(nameof(domResources));
			}

			if (domResources.Count == 0)
			{
				return;
			}

			var domResourcesById = new Dictionary<Guid, DomResource>();
			var domResourcesByCoreId = new Dictionary<Guid, DomResource>();

			foreach (var domResource in domResources)
			{
				domResourcesById.Add(domResource.ID.Id, domResource);

				if (domResource.ResourceInternalProperties.Resource_Id.GetValueOrDefault() == Guid.Empty)
				{
					// No CORE resource linked to the DOM resource
					continue;
				}

				domResourcesByCoreId.Add(domResource.ResourceInternalProperties.Resource_Id.Value, domResource);
			}

			FilterElement<CoreResource> Filter(Guid resourceId) => Skyline.DataMiner.Net.Messages.ResourceExposers.ID.Equal(resourceId);
			var coreResourceById = planApi.CoreHelpers.ResourceManagerHelper.GetResources(domResourcesByCoreId.Keys, Filter).ToDictionary(x => x.ID);

			// DOM resources without a CORE can be removed.
			successfulItems.AddRange(domResources
				.Where(x => !x.ResourceInternalProperties.Resource_Id.HasValue
				|| x.ResourceInternalProperties.Resource_Id.Value == Guid.Empty
				|| !coreResourceById.ContainsKey(x.ResourceInternalProperties.Resource_Id.Value)));

			var options = new Net.Messages.ResourceDeleteOptions
			{
				IgnoreCanceledReservations = true,
				IgnorePastReservation = true,
			};

			planApi.CoreHelpers.ResourceManagerHelper.TryDeleteResourcesInBatches(coreResourceById.Values.ToArray(), options, out var result);

			foreach (var id in result.UnsuccessfulIds)
			{
				if (!domResourcesByCoreId.TryGetValue(id, out var domResource))
				{
					planApi.Logger.Error(this, $"Failed to find DOM ID for CORE resource ID {id}.");
					continue;
				}

				unsuccessfulIds.Add(domResource.ID.Id);

				if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					traceDataPerItem.Add(domResource.ID.Id, traceData);
				}
			}

			foreach (var id in result.SuccessfulIds)
			{
				if (!domResourcesByCoreId.TryGetValue(id, out var domResource))
				{
					planApi.Logger.Error(this, $"Failed to find DOM ID for CORE resource ID {id}.");
					continue;
				}

				successfulItems.Add(domResource);
			}
		}

		private void ApplyUnmanagedResourceConfig(DomResource domResource, CoreResource coreResource, ICollection<SynchronizationDifference> differences)
		{
			SetResourceType(coreResource, "Unlinked Resource", differences);
		}

		private void ApplyElementResourceConfig(DomResource domResource, CoreResource coreResource, ICollection<SynchronizationDifference> differences)
		{
			var elementInfo = new DmsElementId(domResource.ResourceInternalProperties.Metadata.LinkedElementInfo);

			if (coreResource.DmaID != elementInfo.AgentId || coreResource.ElementID != elementInfo.ElementId)
			{
				differences.Add(new ElementLinkDifference(elementInfo.AgentId, elementInfo.ElementId, coreResource.DmaID, coreResource.ElementID));

				coreResource.DmaID = elementInfo.AgentId;
				coreResource.ElementID = elementInfo.ElementId;
			}

			SetResourceType(coreResource, "Element", differences);
		}

		private void ApplyServiceResourceConfig(DomResource domResource, CoreResource coreResource, ICollection<SynchronizationDifference> differences)
		{
			var linkedServiceInfo = domResource.ResourceInternalProperties.Metadata.LinkedServiceInfo;
			var serviceLinkProperty = coreResource.Properties.FirstOrDefault(x => String.Equals(x.Name, "Service Link"));
			if (serviceLinkProperty == null)
			{
				coreResource.Properties.Add(new Net.Messages.ResourceManagerProperty("Service Link", linkedServiceInfo));
				differences.Add(new ServiceLinkDifference(SynchronizationDifferenceKind.Missing, linkedServiceInfo, null));
			}
			else if (!String.Equals(serviceLinkProperty.Value, linkedServiceInfo))
			{
				differences.Add(new ServiceLinkDifference(SynchronizationDifferenceKind.ValueMismatch, linkedServiceInfo, serviceLinkProperty.Value));
				serviceLinkProperty.Value = linkedServiceInfo;
			}
			else
			{
				// no property update required
			}

			SetResourceType(coreResource, "Service", differences);
		}

		private void ApplyVirtualFunctionResourceConfig(DomResource domResource, CoreResource coreResource, ICollection<SynchronizationDifference> differences)
		{
			if (coreResource is not CoreFunctionResource functionResource)
			{
				throw new InvalidOperationException($"Core Resource {coreResource.Name} ({coreResource.ID}) is not a FunctionResource.");
			}

			var functionDefinition = planApi.CoreHelpers.ProtocolFunctionHelperCache.GetFunctionDefinition(domResource.ResourceInternalProperties.Metadata.LinkedFunctionId);
			var elementInfo = new DmsElementId(domResource.ResourceInternalProperties.Metadata.LinkedElementInfo);
			string tableIndex = domResource.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex;

			var difference = new VirtualFunctionLinkDifference
			{
				DomFunctionId = functionDefinition.GUID,
				CoreFunctionId = functionResource.FunctionGUID,
				DomAgentId = elementInfo.AgentId,
				DomElementId = elementInfo.ElementId,
				CoreAgentId = functionResource.MainDVEDmaID,
				CoreElementId = functionResource.MainDVEElementID,
				DomFunctionTableIndex = tableIndex,
				CoreFunctionTableIndex = functionResource.LinkerTableEntries.FirstOrDefault()?.Item2,
			};

			bool updateRequired = functionResource.FunctionGUID != functionDefinition.GUID
				|| functionResource.MainDVEDmaID != elementInfo.AgentId
				|| functionResource.MainDVEElementID != elementInfo.ElementId;

			functionResource.FunctionGUID = functionDefinition.GUID;
			functionResource.MainDVEDmaID = elementInfo.AgentId;
			functionResource.MainDVEElementID = elementInfo.ElementId;

			if (functionDefinition.EntryPoints.Any())
			{
				int parameterId = functionDefinition.EntryPoints.First().ParameterId;
				var existingEntry = functionResource.LinkerTableEntries.FirstOrDefault();

				if (existingEntry == null || existingEntry.Item1 != parameterId || !String.Equals(existingEntry.Item2, tableIndex))
				{
					functionResource.LinkerTableEntries = [new Tuple<int, string>(parameterId, tableIndex)];
					updateRequired = true;
				}
			}

			if (updateRequired)
			{
				differences.Add(difference);
			}

			SetResourceType(coreResource, "Virtual Function", differences);

			Action<CoreResource> enableDveAction = (createdResource) =>
			{
				if (createdResource is not Net.ResourceManager.Objects.FunctionResource fResource)
				{
					return;
				}

				var element = planApi.CoreHelpers.DmsCache.GetElement(elementInfo);
				var genericDveTable = element.GetTable(65132);
				var dveStateColumn = genericDveTable.GetColumn<int?>(65136);
				dveStateColumn.SetValue(fResource.PK, 1);
			};

			enableDveActionByCoreId[coreResource.ID] = enableDveAction;
		}

		private void SetResourceType(CoreResource coreResource, string resourceTypeValue, ICollection<SynchronizationDifference> differences)
		{
			var resourceTypeCapability = coreResource.Capabilities.FirstOrDefault(x => x.CapabilityProfileID == CoreCapabilities.ResourceType.Id);
			var capabilityValue = new Net.Profiles.CapabilityParameterValue(new List<string> { resourceTypeValue });
			if (resourceTypeCapability == null)
			{
				coreResource.Capabilities.Add(new Net.SRM.Capabilities.ResourceCapability(CoreCapabilities.ResourceType.Id)
				{
					Value = capabilityValue,
				});

				differences.Add(new ResourceTypeDifference(SynchronizationDifferenceKind.Missing, resourceTypeValue, null));
			}
			else if (!resourceTypeCapability.Value.Equals(capabilityValue))
			{
				differences.Add(new ResourceTypeDifference(SynchronizationDifferenceKind.ValueMismatch, resourceTypeValue, resourceTypeCapability.Value?.Discreets?.FirstOrDefault()));

				resourceTypeCapability.Value = capabilityValue;
			}
			else
			{
				// no resource type update required
			}
		}

		private void ValidateNames(ICollection<DomResource> domResources)
		{
			if (domResources == null)
			{
				throw new ArgumentNullException(nameof(domResources));
			}

			if (domResources.Count == 0)
			{
				return;
			}

			var resourcesRequiringValidation = domResources.ToList();
			var resourcesWithDuplicateNames = resourcesRequiringValidation
				.GroupBy(resource => resource.ResourceInfo.Name)
				.Where(g => g.Count() > 1)
				.SelectMany(x => x)
				.ToList();

			foreach (var resource in resourcesWithDuplicateNames)
			{
				var error = new ResourceDuplicateNameError
				{
					ErrorMessage = $"Resource '{resource.ResourceInfo.Name}' has a duplicate name.",
					Id = resource.ID.Id,
					Name = resource.ResourceInfo.Name,
				};
				AddError(resource.ID.Id, error);

				resourcesRequiringValidation.Remove(resource);
			}

			var coreResourceNames = resourcesRequiringValidation.Select(x => x.ResourceInfo.Name);
			FilterElement<CoreResource> Filter(string name) => Net.Messages.ResourceExposers.Name.Equal(name);
			var coreResourcesByName = planApi.CoreHelpers.ResourceManagerHelper.GetResources(coreResourceNames, Filter)
				.GroupBy(x => x.Name)
				.ToDictionary(x => x.Key, x => (IReadOnlyCollection<CoreResource>)x.ToList());

			foreach (var resource in resourcesRequiringValidation)
			{
				if (!coreResourcesByName.TryGetValue(resource.ResourceInfo.Name, out var coreResources))
				{
					continue;
				}

				var existingResources = coreResources.Where(x => x.ID != resource.ResourceInternalProperties.Resource_Id.GetValueOrDefault()).ToList();
				if (existingResources.Count == 0)
				{
					continue;
				}

				planApi.Logger.Information(this, $"Name '{resource.ResourceInfo.Name}' is already in use by CORE resource(s) with ID(s): {string.Join(" ,", existingResources.Select(x => x.ID))}");

				var error = new ResourceNameExistsError
				{
					ErrorMessage = "Name is already in use.",
					Id = resource.ID.Id,
					Name = resource.ResourceInfo.Name,
				};

				AddError(resource.ID.Id, error);
			}
		}

		private void ValidateElementResources(ICollection<DomResource> domResources)
		{
			if (domResources == null)
			{
				throw new ArgumentNullException(nameof(domResources));
			}

			foreach (var domResource in domResources)
			{
				var elementId = new DmsElementId(domResource.ResourceInternalProperties.Metadata.LinkedElementInfo);
				if (!TryValidateElementLink(elementId, out string reason))
				{
					var error = new ResourceInvalidElementLinkError
					{
						ErrorMessage = reason,
						Id = domResource.ID.Id,
						AgentId = elementId.AgentId,
						ElementId = elementId.ElementId,
					};

					AddError(domResource.ID.Id, error);
				}
			}
		}

		public bool TryValidateElementLink(DmsElementId elementId, out string reason)
		{
			reason = String.Empty;
			var element = planApi.CoreHelpers.DmsCache.GetElement(elementId);

			if (element == null)
			{
				reason = $"No element found with ID '{elementId}'.";
				return false;
			}

			if (element.FunctionSettings.IsFunctionElement)
			{
				reason = $"Element '{element.Name}' is a function element and cannot be linked to a resource.";
				return false;
			}

			return true;
		}

		private void ValidateServiceResources(ICollection<DomResource> domResources)
		{
			if (domResources == null)
			{
				throw new ArgumentNullException(nameof(domResources));
			}

			foreach (var domResource in domResources)
			{
				var serviceId = new DmsServiceId(domResource.ResourceInternalProperties.Metadata.LinkedServiceInfo);
				if (!TryValidateServiceResourceServiceLink(serviceId, out string reason))
				{
					var error = new ResourceInvalidServiceLinkError
					{
						ErrorMessage = reason,
						Id = domResource.ID.Id,
						AgentId = serviceId.AgentId,
						ServiceId = serviceId.ServiceId,
					};

					AddError(domResource.ID.Id, error);
				}
			}
		}

		private bool TryValidateServiceResourceServiceLink(DmsServiceId serviceId, out string reason)
		{
			reason = String.Empty;

			var service = planApi.CoreHelpers.DmsCache.GetService(serviceId);

			if (service == null)
			{
				reason = $"No service found with ID '{serviceId}'.";
				return false;
			}

			return true;
		}

		private void ValidateVirtualFunctionResources(ICollection<DomResource> domResources)
		{
			if (domResources == null)
			{
				throw new ArgumentNullException(nameof(domResources));
			}

			if (domResources.Count == 0)
			{
				return;
			}

			// Validate elements
			var domResourcesToValidate = new List<DomResource>(domResources);
			var invalidDomResources = new List<DomResource>();
			foreach (var domResource in domResourcesToValidate)
			{
				var elementId = new DmsElementId(domResource.ResourceInternalProperties.Metadata.LinkedElementInfo);
				if (!TryValidateElementLink(elementId, out string invalidElementInfoReason))
				{
					var error = new ResourceInvalidElementLinkError
					{
						ErrorMessage = invalidElementInfoReason,
						Id = domResource.ID.Id,
						AgentId = elementId.AgentId,
						ElementId = elementId.ElementId,
					};

					AddError(domResource.ID.Id, error);

					invalidDomResources.Add(domResource);
				}
			}

			domResourcesToValidate.RemoveAll(x => invalidDomResources.Contains(x));

			// Validate functions
			var domResourcesByFunctionId = domResourcesToValidate
				.GroupBy(x => x.ResourceInternalProperties.Metadata.LinkedFunctionId)
				.ToDictionary(x => x.Key, x => x.ToList());
			var functionDefinitionsById = planApi.CoreHelpers.ProtocolFunctionHelperCache.GetFunctionDefinitions(domResourcesByFunctionId.Keys);

			foreach (var kvp in domResourcesByFunctionId.Where(x => !functionDefinitionsById.ContainsKey(x.Key)).ToList())
			{
				var error = new ResourceInvalidFunctionLinkError
				{
					ErrorMessage = $"No function found with ID '{kvp.Key}'.",
					FunctionId = kvp.Key,
				};

				AddError(kvp.Value, error);

				domResourcesByFunctionId.Remove(kvp.Key);
			}

			// validate table indexes
			var domResourcesByElementFunction = domResourcesByFunctionId.Values.SelectMany(x => x)
				.GroupBy(x => new ElementFunctionMapping
				{
					FunctionDefinitionId = x.ResourceInternalProperties.Metadata.LinkedFunctionId,
					ElementInfo = new DmsElementId(x.ResourceInternalProperties.Metadata.LinkedElementInfo),
				})
				.ToDictionary(x => x.Key, x => x.ToList());

			foreach (var kvp in domResourcesByElementFunction)
			{
				if (!functionDefinitionsById.TryGetValue(kvp.Key.FunctionDefinitionId, out var functionDefinition))
				{
					// This should not happen as we have already filtered invalid function IDs
					continue;
				}

				if (functionDefinition.EntryPoints == null || !functionDefinition.EntryPoints.Any())
				{
					continue;
				}

				var resourcesRequiringValidation = kvp.Value.ToList();

				// Check for duplicate table indexes
				var resourcesWithSameTableIndex = resourcesRequiringValidation
					.Where(x => !string.IsNullOrEmpty(x.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex))
					.GroupBy(x => x.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex)
					.Where(g => g.Count() > 1)
					.SelectMany(x => x)
					.ToList();

				foreach (var resource in resourcesWithSameTableIndex)
				{
					var elementId = new DmsElementId(resource.ResourceInternalProperties.Metadata.LinkedElementInfo);

					var error = new ResourceDuplicateTableIndexLinkError
					{
						ErrorMessage = $"Resource '{resource.ResourceInfo.Name}' has a duplicate table index '{resource.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex}'.",
						Id = resource.ID.Id,
						AgentId = elementId.AgentId,
						ElementId = elementId.ElementId,
						FunctionId = resource.ResourceInternalProperties.Metadata.LinkedFunctionId,
						FunctionTableIndex = resource.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex,
					};

					AddError(resource.ID.Id, error);

					resourcesRequiringValidation.Remove(resource);
				}

				var entryPoints = planApi.CoreHelpers.ProtocolFunctionHelperCache.GetElementFunctionEntryPoints(kvp.Key.FunctionDefinitionId, kvp.Key.ElementInfo, forceGet: true, returnAvailableOnly: true);
				foreach (var resource in resourcesRequiringValidation.Where(x => !entryPoints.Any(y => y.IndexValue == x.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex)))
				{
					var elementId = new DmsElementId(resource.ResourceInternalProperties.Metadata.LinkedElementInfo);

					var error = new ResourceInvalidTableIndexLinkError
					{
						ErrorMessage = $"Resource '{resource.ResourceInfo.Name}' has an invalid table index '{resource.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex}'.",
						Id = resource.ID.Id,
						AgentId = elementId.AgentId,
						ElementId = elementId.ElementId,
						FunctionId = resource.ResourceInternalProperties.Metadata.LinkedFunctionId,
						FunctionTableIndex = resource.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex,
					};

					AddError(resource.ID.Id, error);
				}
			}
		}

		private bool TryValidateVirtualFunctionResourceFunctionDefinition(Guid functionDefinitionId, out string reason)
		{
			reason = String.Empty;

			var functionDefinition = planApi.CoreHelpers.ProtocolFunctionHelperCache.GetFunctionDefinition(functionDefinitionId);

			if (functionDefinition == null)
			{
				reason = $"No function found with ID '{functionDefinitionId}'.";
				return false;
			}

			return true;
		}

		private bool TryValidateVirtualFunctionResourceTableIndex(Guid functionDefinitionId, DmsElementId functionElementId, string tableIndex, out string reason)
		{
			reason = String.Empty;

			var entryPoints = planApi.CoreHelpers.ProtocolFunctionHelperCache.GetElementFunctionEntryPoints(functionDefinitionId, functionElementId, returnAvailableOnly: true);
			if (!entryPoints.Any(x => x.IndexValue == tableIndex))
			{
				reason = $"Invalid table index '{tableIndex}'.";
				return false;
			}

			return true;
		}

		private void AddError(ICollection<DomResource> domResources, MediaOpsErrorData error)
		{
			if (domResources == null)
			{
				throw new ArgumentNullException(nameof(domResources));
			}

			if (domResources.Count == 0)
			{
				return;
			}

			foreach (var domResource in domResources)
			{
				AddError(domResource.ID.Id, error);
			}
		}

		private void AddCoreResourceNotFoundError(ResourceMapping mapping)
		{
			var domResource = mapping.DomResource;
			var coreResourceId = domResource.ResourceInternalProperties.Resource_Id.GetValueOrDefault();

			var errorMessage = coreResourceId != Guid.Empty
				? $"The linked CORE resource with ID '{coreResourceId}' no longer exists."
				: "The resource is not linked to a CORE resource.";

			AddError(
				domResource.ID.Id,
				new ResourceNotFoundError
				{
					ErrorMessage = errorMessage,
					Id = domResource.ID.Id,
				});
		}

		private void AddError(Guid id, MediaOpsErrorData error)
		{
			if (id == Guid.Empty)
			{
				throw new ArgumentException("Id cannot be empty.", nameof(id));
			}

			if (error == null)
			{
				throw new ArgumentNullException(nameof(error));
			}

			if (!traceDataPerItem.TryGetValue(id, out var mediaOpsTraceData))
			{
				mediaOpsTraceData = new MediaOpsTraceData();
				traceDataPerItem.Add(id, mediaOpsTraceData);

				unsuccessfulIds.Add(id);
			}

			mediaOpsTraceData.Add(error);
		}

		private void SyncName(DomResource domResource, CoreResource coreResource, ICollection<SynchronizationDifference> differences)
		{
			if (String.Equals(domResource.ResourceInfo.Name, coreResource.Name))
			{
				return;
			}

			differences.Add(new NameDifference(domResource.ResourceInfo.Name, coreResource.Name));

			coreResource.Name = domResource.ResourceInfo.Name;
		}

		private void SyncType(DomResource domResource, CoreResource coreResource, ICollection<SynchronizationDifference> differences)
		{
			typeSyncers[domResource.ResourceInfo.Type.Value].Invoke(domResource, coreResource, differences);
		}

		private void SyncCapacities(DomResource domResource, CoreResource coreResource, ICollection<SynchronizationDifference> differences)
		{
			var required = GetRequiredResourceCapacities(domResource);
			var removed = coreResource.Capacities.Where(x => !required.Select(y => y.CapacityProfileID).Contains(x.CapacityProfileID)).ToList();

			foreach (var resourceCapacity in removed)
			{
				differences.Add(CreateDifference(SynchronizationDifferenceKind.Obsolete, resourceCapacity.CapacityProfileID, null, resourceCapacity.Value));

				coreResource.Capacities.Remove(resourceCapacity);
			}

			foreach (var resourceCapacity in required)
			{
				var capacity = coreResource.Capacities.SingleOrDefault(x => x.CapacityProfileID == resourceCapacity.CapacityProfileID);
				if (capacity == null)
				{
					differences.Add(CreateDifference(SynchronizationDifferenceKind.Missing, resourceCapacity.CapacityProfileID, resourceCapacity.Value, null));

					coreResource.Capacities.Add(resourceCapacity);
					continue;
				}

				var currentMin = capacity.Value.MinDecimalQuantity;
				var currentMax = capacity.Value.MaxDecimalQuantity;

				if (!HasChangedValue(capacity, resourceCapacity))
				{
					continue;
				}

				var difference = CreateDifference(SynchronizationDifferenceKind.ValueMismatch, resourceCapacity.CapacityProfileID, resourceCapacity.Value, null);
				difference.CoreMinValue = difference.IsRange ? currentMin : null;
				difference.CoreMaxValue = currentMax;

				differences.Add(difference);
			}

			CapacityDifference CreateDifference(SynchronizationDifferenceKind kind, Guid capacityId, Net.Profiles.CapacityParameterValue domValue, Net.Profiles.CapacityParameterValue coreValue)
			{
				bool isRange = CoreCapacitiesById.TryGetValue(capacityId, out var coreCapacity) && coreCapacity.IsRange();

				return new CapacityDifference(kind, capacityId)
				{
					IsRange = isRange,
					DomMinValue = isRange ? domValue?.MinDecimalQuantity : null,
					DomMaxValue = domValue?.MaxDecimalQuantity,
					CoreMinValue = isRange ? coreValue?.MinDecimalQuantity : null,
					CoreMaxValue = coreValue?.MaxDecimalQuantity,
				};
			}

			bool HasChangedValue(MultiResourceCapacity current, MultiResourceCapacity expected)
			{
				var hasChangedValue = false;

				if (!CoreCapacitiesById.TryGetValue(current.CapacityProfileID, out var coreCapacity))
				{
					return hasChangedValue;
				}

				if (coreCapacity.IsRange())
				{
					if (!current.Value.MinDecimalQuantity.Equals(expected.Value.MinDecimalQuantity))
					{
						current.Value.MinDecimalQuantity = expected.Value.MinDecimalQuantity;
						hasChangedValue = true;
					}

					if (!current.Value.MaxDecimalQuantity.Equals(expected.Value.MaxDecimalQuantity))
					{
						current.Value.MaxDecimalQuantity = expected.Value.MaxDecimalQuantity;
						hasChangedValue = true;
					}
				}
				else if (!current.Value.MaxDecimalQuantity.Equals(expected.Value.MaxDecimalQuantity))
				{
					current.Value.MaxDecimalQuantity = expected.Value.MaxDecimalQuantity;
					hasChangedValue = true;
				}

				return hasChangedValue;
			}
		}

		private List<MultiResourceCapacity> GetRequiredResourceCapacities(DomResource domResource)
		{
			var capacities = new List<MultiResourceCapacity>();
			foreach (var resourceCapacity in domResource.ResourceCapacities)
			{
				if (!Guid.TryParse(resourceCapacity.ProfileParameterID, out Guid profileParameterId))
				{
					planApi.Logger.Warning(this, $"Invalid ProfileParameterID '{resourceCapacity.ProfileParameterID}' for resource '{domResource.ResourceInfo.Name}'. Skipping capacity sync.");
					continue;
				}

				if (!CoreCapacitiesById.TryGetValue(profileParameterId, out var coreCapacity))
				{
					continue;
				}

				var capacity = new MultiResourceCapacity
				{
					CapacityProfileID = coreCapacity.ID,
				};

				if (coreCapacity.IsRange())
				{
					capacity.Value = new Net.Profiles.CapacityParameterValue
					{
						MinDecimalQuantity = (decimal)resourceCapacity.DoubleMinValue,
						MaxDecimalQuantity = (decimal)resourceCapacity.DoubleMaxValue,
					};
				}
				else
				{
					capacity.Value = new Net.Profiles.CapacityParameterValue
					{
						MaxDecimalQuantity = (decimal)resourceCapacity.DoubleMaxValue,
					};
				}

				capacities.Add(capacity);
			}

			return capacities;
		}

		private void SyncCapabilities(DomResource domResource, CoreResource coreResource, ICollection<SynchronizationDifference> differences)
		{
			var required = GetRequiredResourceCapabilities(domResource);
			var removed = coreResource.Capabilities
				.Where(x =>
					x.CapabilityProfileID != CoreCapabilities.ResourceType.Id
					&& !required.Select(y => y.CapabilityProfileID).Contains(x.CapabilityProfileID))
				.ToList();

			foreach (var resourceCapability in removed)
			{
				differences.Add(new CapabilityDifference(SynchronizationDifferenceKind.Obsolete, resourceCapability.CapabilityProfileID, null, resourceCapability.Value?.Discreets)
				{
					IsTimeDependent = resourceCapability.IsTimeDynamic,
				});

				coreResource.Capabilities.Remove(resourceCapability);
			}

			foreach (var resourceCapability in required)
			{
				var capability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == resourceCapability.CapabilityProfileID);
				if (capability == null)
				{
					differences.Add(new CapabilityDifference(SynchronizationDifferenceKind.Missing, resourceCapability.CapabilityProfileID, resourceCapability.Value?.Discreets, null)
					{
						IsTimeDependent = resourceCapability.IsTimeDynamic,
					});

					coreResource.Capabilities.Add(resourceCapability);
					continue;
				}

				if (capability.IsTimeDynamic || capability.Value.Discreets.ScrambledEquals(resourceCapability.Value.Discreets))
				{
					continue;
				}

				differences.Add(new CapabilityDifference(SynchronizationDifferenceKind.ValueMismatch, resourceCapability.CapabilityProfileID, resourceCapability.Value?.Discreets, capability.Value.Discreets)
				{
					IsTimeDependent = capability.IsTimeDynamic,
				});

				capability.Value.Discreets = resourceCapability.Value.Discreets;
			}
		}

		private List<Net.SRM.Capabilities.ResourceCapability> GetRequiredResourceCapabilities(DomResource domResource)
		{
			var domCapabilities = CapabilitiesHandler.GetExpectedCoreResourceCapabilities(domResource);

			var coreCapabilities = new List<Net.SRM.Capabilities.ResourceCapability>();
			foreach (var configuredCapability in domCapabilities)
			{
				if (!CoreCapabilitiesById.TryGetValue(configuredCapability.ProfileParameterId, out var coreCapability))
				{
					continue;
				}

				if (coreCapability.IsTimeDependent(out var timeDependentCapabilityLink))
				{
					if (!CoreTimeDependentCapabilitiesById.TryGetValue(timeDependentCapabilityLink.LinkedParameterId, out var linkedCoreCapability))
					{
						continue;
					}

					var timeDependentCapability = new Net.SRM.Capabilities.ResourceCapability(linkedCoreCapability.ID)
					{
						Value = new Net.Profiles.CapabilityParameterValue(),
						IsTimeDynamic = true,
					};

					coreCapabilities.Add(timeDependentCapability);
				}

				var capability = new Net.SRM.Capabilities.ResourceCapability(coreCapability.ID)
				{
					Value = new Net.Profiles.CapabilityParameterValue(GetDiscretes(configuredCapability)),
				};

				coreCapabilities.Add(capability);
			}

			return coreCapabilities;
		}

		private List<string> GetDiscretes(IConfiguredCapability configuredCapability)
		{
			if (string.IsNullOrEmpty(configuredCapability.StringValue))
			{
				return new List<string>();
			}

			return configuredCapability.StringValue.Split(';').ToList();
		}

		private void SyncConcurrency(DomResource domResource, CoreResource coreResource, ICollection<SynchronizationDifference> differences)
		{
			var configuredConcurrency = (int)domResource.ResourceInfo.Concurrency;
			if (configuredConcurrency < 1)
			{
				configuredConcurrency = 1;
			}

			if (coreResource.MaxConcurrency == configuredConcurrency)
			{
				return;
			}

			differences.Add(new MaxConcurrencyDifference(configuredConcurrency, coreResource.MaxConcurrency));

			coreResource.MaxConcurrency = configuredConcurrency;
		}

		private void SyncPools(DomResource domResource, CoreResource coreResource, ICollection<SynchronizationDifference> differences)
		{
			var poolIds = domResource.ResourceInternalProperties?.PoolIds ?? Enumerable.Empty<Guid>();
			var cachedDomPoolsById = domResource.DomInstanceCache.GetFromCache<DomResourcePool>().ToDictionary(x => x.ID.Id);

			var missingPoolIds = poolIds.Where(x => !cachedDomPoolsById.ContainsKey(x));
			var domPools = planApi.ResourcePools.Read(missingPoolIds).Select(x => x.OriginalInstance).ToList();
			domPools.AddRange(cachedDomPoolsById.Values);

			var corePoolIds = domPools.Select(x => x.ResourcePoolInternalProperties.ResourcePoolId).Where(x => x != Guid.Empty).ToList();

			if (coreResource.PoolGUIDs.ScrambledEquals(corePoolIds))
			{
				return;
			}

			foreach (var corePoolId in corePoolIds.Except(coreResource.PoolGUIDs))
			{
				differences.Add(new ResourcePoolMembershipDifference(SynchronizationDifferenceKind.Missing, corePoolId));
			}

			foreach (var corePoolId in coreResource.PoolGUIDs.Except(corePoolIds))
			{
				differences.Add(new ResourcePoolMembershipDifference(SynchronizationDifferenceKind.Obsolete, corePoolId));
			}

			coreResource.PoolGUIDs.Clear();
			coreResource.PoolGUIDs.AddRange(corePoolIds);
		}

		private sealed class ResourceMapping
		{
			private ResourceMapping(DomResource domResource, CoreResourceState state)
				: this(domResource, BuildCoreResource(domResource.ResourceInfo.Type.Value), state)
			{
			}

			private ResourceMapping(DomResource domResource, CoreResource coreResource, CoreResourceState state)
			{
				DomResource = domResource ?? throw new ArgumentNullException(nameof(domResource));
				CoreResource = coreResource ?? throw new ArgumentNullException(nameof(coreResource));
				State = state;
			}

			public DomResource DomResource { get; }

			public CoreResource CoreResource { get; }

			/// <summary>
			/// Indicates whether the CORE resource already exists, still needs to be created, or went missing.
			/// </summary>
			public CoreResourceState State { get; }

			public bool NeedsNameValidation => State == CoreResourceState.New || DomResource.ResourceInfo.Name != CoreResource.Name;

			public static IEnumerable<ResourceMapping> GetMappings(MediaOpsPlanApi planApi, ICollection<DomResource> domResources)
			{
				if (planApi == null)
				{
					throw new ArgumentNullException(nameof(planApi));
				}

				if (domResources == null)
				{
					throw new ArgumentNullException(nameof(domResources));
				}

				if (domResources.Count == 0)
				{
					return [];
				}

				return GetMappingsIterator(planApi, domResources);
			}

			private static IEnumerable<ResourceMapping> GetMappingsIterator(MediaOpsPlanApi planApi, ICollection<DomResource> domResources)
			{
				var coreResourceIds = domResources
					.Where(x => x.ResourceInternalProperties.Resource_Id.HasValue && x.ResourceInternalProperties.Resource_Id.Value != Guid.Empty)
					.Select(x => x.ResourceInternalProperties.Resource_Id.Value)
					.Distinct();
				FilterElement<CoreResource> Filter(Guid id) => Net.Messages.ResourceExposers.ID.Equal(id);
				var coreResourcesById = planApi.CoreHelpers.ResourceManagerHelper.GetResources(coreResourceIds, Filter).ToDictionary(x => x.ID);

				foreach (var domResource in domResources)
				{
					var storedCoreResourceId = domResource.ResourceInternalProperties.Resource_Id.GetValueOrDefault();
					if (storedCoreResourceId == Guid.Empty)
					{
						// The DOM resource was never synced to CORE, so a new CORE resource needs to be created.
						yield return new ResourceMapping(domResource, CoreResourceState.New);
						continue;
					}

					if (coreResourcesById.TryGetValue(storedCoreResourceId, out var coreResource))
					{
						yield return new ResourceMapping(domResource, coreResource, CoreResourceState.Existing);
						continue;
					}

					// The DOM resource refers to a CORE resource that existed in the past but can no longer be found.
					yield return new ResourceMapping(domResource, CoreResourceState.Missing);
				}
			}

			private static CoreResource BuildCoreResource(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type resourceType)
			{
				if (resourceType == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Type.VirtualFunction)
				{
					return new CoreFunctionResource()
					{
						ID = Guid.NewGuid(),
					};
				}

				return new CoreResource(Guid.NewGuid());
			}
		}

		private enum CoreResourceState
		{
			/// <summary>
			/// The DOM resource was never synced to CORE, so the CORE resource still needs to be created.
			/// </summary>
			New,

			/// <summary>
			/// The CORE resource exists and may need to be updated.
			/// </summary>
			Existing,

			/// <summary>
			/// The DOM resource refers to a CORE resource that no longer exists. This is an invalid situation, as the CORE resource existed in the past.
			/// </summary>
			Missing,
		}

		private struct ElementFunctionMapping
		{
			public Guid FunctionDefinitionId { get; set; }

			public DmsElementId ElementInfo { get; set; }
		}
	}
}
