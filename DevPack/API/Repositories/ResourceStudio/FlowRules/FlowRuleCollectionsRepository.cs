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
	internal class FlowRuleCollectionsRepository : Repository, IFlowRuleCollectionsRepository
	{
		private readonly FlowRulesFilterTranslator filterTranslator = new FlowRulesFilterTranslator();

		/// <summary>
		/// Initializes a new instance of the <see cref="FlowRuleCollectionsRepository"/> class.
		/// </summary>
		/// <param name="planApi">The MediaOps Plan API instance.</param>
		public FlowRuleCollectionsRepository(MediaOpsPlanApi planApi) : base(planApi)
		{
		}

		/// <inheritdoc/>
		public long Count()
		{
			return Count(new TRUEFilterElement<FlowRuleCollection>());
		}

		/// <inheritdoc/>
		public long Count(FilterElement<FlowRuleCollection> filter)
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
		public long Count(IQuery<FlowRuleCollection> query)
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
		public FlowRuleCollection Read(Guid id)
		{
			if (id == Guid.Empty)
			{
				throw new ArgumentException("Flow rules ID cannot be empty.", nameof(id));
			}

			return Read(FlowRuleCollectionExposers.Id.Equal(id)).FirstOrDefault();
		}

		/// <inheritdoc/>
		public IEnumerable<FlowRuleCollection> Read(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Array.Empty<FlowRuleCollection>();
			}

			return Read(new ORFilterElement<FlowRuleCollection>(ids.Select(x => FlowRuleCollectionExposers.Id.Equal(x)).ToArray()));
		}

		/// <inheritdoc/>
		public IEnumerable<FlowRuleCollection> Read()
		{
			return Read(new TRUEFilterElement<FlowRuleCollection>());
		}

		/// <inheritdoc/>
		public IEnumerable<FlowRuleCollection> Read(FilterElement<FlowRuleCollection> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (filter.isEmpty())
			{
				return Enumerable.Empty<FlowRuleCollection>();
			}

			return PlanApi.DomHelpers.SlcResourceStudioHelper
				.GetFlowRules(filterTranslator.TranslateFilter(filter))
				.Select(x => new FlowRuleCollection(x));
		}

		/// <inheritdoc/>
		public IEnumerable<FlowRuleCollection> Read(IQuery<FlowRuleCollection> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (query.Filter.isEmpty())
			{
				return Enumerable.Empty<FlowRuleCollection>();
			}

			return PlanApi.DomHelpers.SlcResourceStudioHelper
				.GetFlowRules(TranslateToDomQuery(query))
				.Select(x => new FlowRuleCollection(x));
		}

		/// <inheritdoc/>
		public IEnumerable<IPagedResult<FlowRuleCollection>> ReadPaged()
		{
			return ReadPaged(new TRUEFilterElement<FlowRuleCollection>());
		}

		/// <inheritdoc/>
		public IEnumerable<IPagedResult<FlowRuleCollection>> ReadPaged(int pageSize)
		{
			return ReadPaged(new TRUEFilterElement<FlowRuleCollection>(), pageSize);
		}

		/// <inheritdoc/>
		public IEnumerable<IPagedResult<FlowRuleCollection>> ReadPaged(FilterElement<FlowRuleCollection> filter)
		{
			return ReadPaged(filter, MediaOpsPlanApi.DefaultPageSize);
		}

		/// <inheritdoc/>
		public IEnumerable<IPagedResult<FlowRuleCollection>> ReadPaged(IQuery<FlowRuleCollection> query)
		{
			return ReadPaged(query, MediaOpsPlanApi.DefaultPageSize);
		}

		/// <inheritdoc/>
		public IEnumerable<IPagedResult<FlowRuleCollection>> ReadPaged(FilterElement<FlowRuleCollection> filter, int pageSize)
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
				return Enumerable.Empty<IPagedResult<FlowRuleCollection>>();
			}

			return ReadPagedIterator(PlanApi.DomHelpers.SlcResourceStudioHelper.GetFlowRulesPaged(filterTranslator.TranslateFilter(filter), pageSize), pageSize);
		}

		/// <inheritdoc/>
		public IEnumerable<IPagedResult<FlowRuleCollection>> ReadPaged(IQuery<FlowRuleCollection> query, int pageSize)
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
				return Enumerable.Empty<IPagedResult<FlowRuleCollection>>();
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
			return FlowRuleEvaluationHandler.Evaluate(PlanApi, connections);
		}

		/// <inheritdoc/>
		public IReadOnlyDictionary<Guid, FlowRuleEvaluation> EvaluateCandidates(FlowRuleObjectType type, Guid id, FlowRuleDirection direction, FlowRuleObjectType candidateType, IEnumerable<Guid> candidateIds)
		{
			return FlowRuleEvaluationHandler.EvaluateCandidates(PlanApi, type, id, direction, candidateType, candidateIds);
		}

		/// <inheritdoc/>
		public IReadOnlyCollection<FlowRuleConnectionEvaluation> Evaluate<TNode>(NodeGraph<TNode> nodeGraph) where TNode : NodeBase
		{
			return FlowRuleEvaluationHandler.Evaluate(PlanApi, nodeGraph);
		}

		private static IEnumerable<IPagedResult<FlowRuleCollection>> ReadPagedIterator(IEnumerable<IEnumerable<StorageResourceStudio.FlowrulesInstance>> pages, int pageSize)
		{
			var pageNumber = 0;
			var enumerator = pages.GetEnumerator();
			var hasNext = enumerator.MoveNext();

			while (hasNext)
			{
				var page = enumerator.Current;
				hasNext = enumerator.MoveNext();
				yield return new PagedResult<FlowRuleCollection>(page.Select(x => new FlowRuleCollection(x)), pageNumber++, pageSize, hasNext);
			}
		}

		private IQuery<DomInstance> TranslateToDomQuery(IQuery<FlowRuleCollection> query)
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
