namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;

	using SLDataGateway.API.Types.Querying;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	/// <summary>
	/// Provides read access to flow rules and evaluates connections against them.
	/// </summary>
	internal class FlowRulesRepository : Repository, IFlowRulesRepository
	{
		private readonly FlowRulesFilterTranslator filterTranslator = new FlowRulesFilterTranslator();

		/// <summary>
		/// Initializes a new instance of the <see cref="FlowRulesRepository"/> class.
		/// </summary>
		/// <param name="planApi">The MediaOps Plan API instance.</param>
		public FlowRulesRepository(MediaOpsPlanApi planApi) : base(planApi)
		{
		}

		/// <inheritdoc/>
		public long Count()
		{
			return Count(new TRUEFilterElement<FlowRules>());
		}

		/// <inheritdoc/>
		public long Count(FilterElement<FlowRules> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (filter.isEmpty())
			{
				return 0;
			}

			return PlanApi.DomHelpers.SlcResourceStudioHelper.CountResourceStudioInstances(filterTranslator.TranslateFilter(filter));
		}

		/// <inheritdoc/>
		public long Count(IQuery<FlowRules> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (query.Filter.isEmpty())
			{
				return 0;
			}

			return PlanApi.DomHelpers.SlcResourceStudioHelper.CountResourceStudioInstances(TranslateToDomQuery(query));
		}

		/// <inheritdoc/>
		public FlowRules Read(Guid id)
		{
			if (id == Guid.Empty)
			{
				throw new ArgumentException("Flow rules ID cannot be empty.", nameof(id));
			}

			return Read(FlowRulesExposers.Id.Equal(id)).FirstOrDefault();
		}

		/// <inheritdoc/>
		public IEnumerable<FlowRules> Read(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Array.Empty<FlowRules>();
			}

			return Read(new ORFilterElement<FlowRules>(ids.Select(x => FlowRulesExposers.Id.Equal(x)).ToArray()));
		}

		/// <inheritdoc/>
		public IEnumerable<FlowRules> Read()
		{
			return Read(new TRUEFilterElement<FlowRules>());
		}

		/// <inheritdoc/>
		public IEnumerable<FlowRules> Read(FilterElement<FlowRules> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (filter.isEmpty())
			{
				return Enumerable.Empty<FlowRules>();
			}

			return PlanApi.DomHelpers.SlcResourceStudioHelper
				.GetFlowRules(filterTranslator.TranslateFilter(filter))
				.Select(x => new FlowRules(x));
		}

		/// <inheritdoc/>
		public IEnumerable<FlowRules> Read(IQuery<FlowRules> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (query.Filter.isEmpty())
			{
				return Enumerable.Empty<FlowRules>();
			}

			return PlanApi.DomHelpers.SlcResourceStudioHelper
				.GetFlowRules(TranslateToDomQuery(query))
				.Select(x => new FlowRules(x));
		}

		/// <inheritdoc/>
		public IEnumerable<IPagedResult<FlowRules>> ReadPaged()
		{
			return ReadPaged(new TRUEFilterElement<FlowRules>());
		}

		/// <inheritdoc/>
		public IEnumerable<IPagedResult<FlowRules>> ReadPaged(int pageSize)
		{
			return ReadPaged(new TRUEFilterElement<FlowRules>(), pageSize);
		}

		/// <inheritdoc/>
		public IEnumerable<IPagedResult<FlowRules>> ReadPaged(FilterElement<FlowRules> filter)
		{
			return ReadPaged(filter, MediaOpsPlanApi.DefaultPageSize);
		}

		/// <inheritdoc/>
		public IEnumerable<IPagedResult<FlowRules>> ReadPaged(IQuery<FlowRules> query)
		{
			return ReadPaged(query, MediaOpsPlanApi.DefaultPageSize);
		}

		/// <inheritdoc/>
		public IEnumerable<IPagedResult<FlowRules>> ReadPaged(FilterElement<FlowRules> filter, int pageSize)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (pageSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
			}

			if (filter.isEmpty())
			{
				return Enumerable.Empty<IPagedResult<FlowRules>>();
			}

			return ReadPagedIterator(PlanApi.DomHelpers.SlcResourceStudioHelper.GetFlowRulesPaged(filterTranslator.TranslateFilter(filter), pageSize), pageSize);
		}

		/// <inheritdoc/>
		public IEnumerable<IPagedResult<FlowRules>> ReadPaged(IQuery<FlowRules> query, int pageSize)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (pageSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
			}

			if (query.Filter.isEmpty())
			{
				return Enumerable.Empty<IPagedResult<FlowRules>>();
			}

			return ReadPagedIterator(PlanApi.DomHelpers.SlcResourceStudioHelper.GetFlowRulesPaged(TranslateToDomQuery(query), pageSize), pageSize);
		}

		/// <inheritdoc/>
		public FlowRuleEvaluation Evaluate(FlowRuleObjectType fromType, Guid fromId, FlowRuleObjectType toType, Guid toId)
		{
			return Evaluate([(fromType, fromId, toType, toId)]).Single();
		}

		/// <inheritdoc/>
		public IReadOnlyCollection<FlowRuleEvaluation> Evaluate(IEnumerable<(FlowRuleObjectType FromType, Guid FromId, FlowRuleObjectType ToType, Guid ToId)> connections)
		{
			if (connections == null)
			{
				throw new ArgumentNullException(nameof(connections));
			}

			var list = connections
				.Select(x => (From: ToObject(x.FromType, x.FromId, nameof(connections)), To: ToObject(x.ToType, x.ToId, nameof(connections))))
				.ToList();

			var flowRulesByOwner = Read(list.SelectMany(x => new[] { x.From, x.To }));

			return list
				.Select(x => FlowRuleEvaluator.Evaluate(x.From, x.To, owner => GetOrNull(flowRulesByOwner, owner)))
				.ToList();
		}

		/// <inheritdoc/>
		public IReadOnlyDictionary<Guid, FlowRuleEvaluation> EvaluateCandidates(FlowRuleObjectType type, Guid id, FlowRuleDirection direction, FlowRuleObjectType candidateType, IEnumerable<Guid> candidateIds)
		{
			var existing = ToObject(type, id, nameof(id));
			FlowRuleObject.ValidateType(candidateType, nameof(candidateType));

			if (candidateIds == null)
			{
				throw new ArgumentNullException(nameof(candidateIds));
			}

			if (!Enum.IsDefined(typeof(FlowRuleDirection), direction))
			{
				throw new ArgumentException($"Value '{direction}' is not a valid flow rule direction.", nameof(direction));
			}

			var candidates = candidateIds
				.Where(x => x != Guid.Empty)
				.Distinct()
				.Select(x => new FlowRuleObject(candidateType, x))
				.ToList();
			var flowRulesByOwner = Read(candidates.Concat([existing]));

			FlowRules GetFlowRules(FlowRuleObject owner) => GetOrNull(flowRulesByOwner, owner);

			return candidates.ToDictionary(
				candidate => candidate.Id,
				candidate => direction == FlowRuleDirection.After
					? FlowRuleEvaluator.Evaluate(existing, candidate, GetFlowRules)
					: FlowRuleEvaluator.Evaluate(candidate, existing, GetFlowRules));
		}

		/// <inheritdoc/>
		public IReadOnlyCollection<FlowRuleConnectionEvaluation> Evaluate<TNode>(NodeGraph<TNode> nodeGraph) where TNode : NodeBase
		{
			if (nodeGraph == null)
			{
				throw new ArgumentNullException(nameof(nodeGraph));
			}

			var flowRulesByOwner = Read(FlowRuleEvaluator.GetConnectedObjects(nodeGraph));
			return FlowRuleEvaluator.Evaluate(nodeGraph, owner => GetOrNull(flowRulesByOwner, owner));
		}

		// Looks the rule sets up by their stored owner, in batches, so it works the same for every owner type.
		internal IReadOnlyDictionary<FlowRuleObject, FlowRules> Read(IEnumerable<FlowRuleObject> owners)
		{
			var result = new Dictionary<FlowRuleObject, FlowRules>();

			foreach (var group in owners.Where(x => !x.IsEmpty).Distinct().GroupBy(x => x.Type))
			{
				var requested = new HashSet<FlowRuleObject>(group);

				foreach (var instance in PlanApi.DomHelpers.SlcResourceStudioHelper.GetFlowRulesByOwner(group.Select(x => x.Id)))
				{
					var flowRules = new FlowRules(instance);
					if (!requested.Contains(flowRules.Owner))
					{
						continue;
					}

					if (result.TryGetValue(flowRules.Owner, out var existing))
					{
						PlanApi.Logger.Warning(this, "Owner {0} has multiple flow rules: {1} and {2}. Only {1} is used.", [flowRules.Owner, existing.Id, flowRules.Id]);
						continue;
					}

					result[flowRules.Owner] = flowRules;
				}
			}

			return result;
		}

		private static FlowRuleObject ToObject(FlowRuleObjectType type, Guid id, string paramName)
		{
			FlowRuleObject.ValidateType(type, paramName);

			if (id == Guid.Empty)
			{
				throw new ArgumentException("Object ID cannot be empty.", paramName);
			}

			return new FlowRuleObject(type, id);
		}

		private static FlowRules GetOrNull(IReadOnlyDictionary<FlowRuleObject, FlowRules> flowRulesByOwner, FlowRuleObject owner)
		{
			return flowRulesByOwner.TryGetValue(owner, out var flowRules) ? flowRules : null;
		}

		private static IEnumerable<IPagedResult<FlowRules>> ReadPagedIterator(IEnumerable<IEnumerable<StorageResourceStudio.FlowrulesInstance>> pages, int pageSize)
		{
			var pageNumber = 0;
			var enumerator = pages.GetEnumerator();
			var hasNext = enumerator.MoveNext();

			while (hasNext)
			{
				var page = enumerator.Current;
				hasNext = enumerator.MoveNext();
				yield return new PagedResult<FlowRules>(page.Select(x => new FlowRules(x)), pageNumber++, pageSize, hasNext);
			}
		}

		private IQuery<DomInstance> TranslateToDomQuery(IQuery<FlowRules> query)
		{
			var domFilter = filterTranslator.TranslateFilter(query.Filter);
			var domOrderBy = filterTranslator.TranslateFullOrderBy(query.Order);

			return query
				.WithFilter(domFilter)
				.WithOrder(domOrderBy)
				.WithLimit(query.Limit);
		}
	}
}
