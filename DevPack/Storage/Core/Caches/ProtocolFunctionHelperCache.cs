namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Messages;

	internal class ProtocolFunctionHelperCache
	{
		private readonly ProtocolFunctionHelper helper;

		private readonly Dictionary<Guid, FunctionDefinition> functionDefinitionsById = [];

		private readonly Dictionary<ElementFunctionMapping, List<EntryPointData>> entryPointDataByElementFunction = [];

		public ProtocolFunctionHelperCache(ProtocolFunctionHelper helper)
		{
			this.helper = helper ?? throw new ArgumentNullException(nameof(helper));
		}

		public FunctionDefinition GetFunctionDefinition(Guid id, bool forceGet = false)
		{
			if (id == Guid.Empty)
			{
				throw new ArgumentException("Function definition ID cannot be empty.", nameof(id));
			}

			var result = GetFunctionDefinitions([id], forceGet);
			if (result.TryGetValue(id, out var functionDefinition))
			{
				return functionDefinition;
			}

			return null;
		}

		public IReadOnlyDictionary<Guid, FunctionDefinition> GetFunctionDefinitions(IEnumerable<Guid> ids, bool forceGet = false)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			var result = new Dictionary<Guid, FunctionDefinition>();
			var idsToRetrieve = new List<Guid>();

			if (forceGet)
			{
				idsToRetrieve.AddRange(ids.Where(x => x != Guid.Empty).Distinct());
			}
			else
			{
				foreach (var id in ids.Where(x => x != Guid.Empty).Distinct())
				{
					if (functionDefinitionsById.TryGetValue(id, out var functionDefinition))
					{
						result[id] = functionDefinition;
					}
					else
					{
						idsToRetrieve.Add(id);
					}
				}
			}

			if (idsToRetrieve.Count > 0)
			{
				var functionDefinitions = ActivityHelper.ActivityHelper.Track(nameof(ProtocolFunctionHelper), nameof(ProtocolFunctionHelper.GetFunctionDefinitions), act => helper.GetFunctionDefinitions(idsToRetrieve.Select(x => new Net.FunctionDefinitionID(x)).ToList()));
				foreach (var systemFunctionDefinition in functionDefinitions)
				{
					if (systemFunctionDefinition is not FunctionDefinition functionDefinition)
					{
						continue;
					}

					result[functionDefinition.FunctionDefinitionID.Id] = functionDefinition;
					functionDefinitionsById[functionDefinition.FunctionDefinitionID.Id] = functionDefinition;
				}
			}

			return result;
		}

		public IEnumerable<EntryPointData> GetElementFunctionEntryPoints(Guid functionDefinitionId, DmsElementId elementInfo, bool forceGet = false, bool returnAvailableOnly = false)
		{
			if (elementInfo == default)
			{
				throw new ArgumentNullException(nameof(elementInfo));
			}

			if (functionDefinitionId == Guid.Empty)
			{
				throw new ArgumentException("Function definition ID cannot be empty.", nameof(functionDefinitionId));
			}

			var key = new ElementFunctionMapping
			{
				FunctionDefinitionId = functionDefinitionId,
				ElementInfo = elementInfo,
			};

			if (!forceGet && entryPointDataByElementFunction.TryGetValue(key, out var entryPointData))
			{
				return returnAvailableOnly
					? entryPointData.Where(x => x.Element == null)
					: entryPointData;
			}

			var functionEntryPoint = ActivityHelper.ActivityHelper.Track(nameof(ProtocolFunctionHelper), nameof(ProtocolFunctionHelper.GetFunctionEntryPoints), act => helper.GetFunctionEntryPoints(functionDefinitionId, elementInfo.AgentId, elementInfo.ElementId).FirstOrDefault());
			if (functionEntryPoint == null)
			{
				entryPointDataByElementFunction.Add(key, new List<EntryPointData>());
				return Enumerable.Empty<EntryPointData>();
			}

			entryPointDataByElementFunction.Add(key, functionEntryPoint.Data.ToList());

			return returnAvailableOnly
				? functionEntryPoint.Data.Where(x => x.Element == null)
				: functionEntryPoint.Data;
		}

		private struct ElementFunctionMapping
		{
			public Guid FunctionDefinitionId { get; set; }

			public DmsElementId ElementInfo { get; set; }
		}
	}
}
