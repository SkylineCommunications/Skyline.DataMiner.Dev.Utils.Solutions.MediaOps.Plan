namespace Skyline.DataMiner.SDM
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	/// <summary>
	/// Wraps a query and keeps the selected fields when the query is modified.
	/// </summary>
	internal sealed class PartialQuery<T> : IPartialQuery<T>
	{
		private readonly IQuery<T> query;

		public PartialQuery(IQuery<T> query, IReadOnlyCollection<FieldExposer> fields)
		{
			this.query = query;
			Fields = fields;
		}

		public IReadOnlyCollection<FieldExposer> Fields { get; }

		public FilterElement<T> Filter => query.Filter;

		public Type DataType => query.DataType;

		public IFilterElement FilterElement => query.FilterElement;

		public IOrderBy Order => query.Order;

		public ILimitBy Limit => query.Limit;

		public IQueryExecutionOptions ExecutionOptions => query.ExecutionOptions;

		public IEnumerable<T> ExecuteInMemory(IEnumerable<T> data)
		{
			return query.ExecuteInMemory(data);
		}

		public IQuery<T> WithFilter(FilterElement<T> filter)
		{
			return Wrap(query.WithFilter(filter));
		}

		public IQuery<T> WithFilter(Func<FilterElement<T>, FilterElement<T>> filterModifier)
		{
			return Wrap(query.WithFilter(filterModifier));
		}

		IQuery<TTarget> IQuery<T>.WithFilter<TTarget>(FilterElement<TTarget> filter)
		{
			// The selected fields belong to T, so they can't be carried over to a query on another type.
			return query.WithFilter(filter);
		}

		public IQuery<T> WithOrder(IOrderBy order)
		{
			return Wrap(query.WithOrder(order));
		}

		public IQuery<T> WithLimit(ILimitBy limit)
		{
			return Wrap(query.WithLimit(limit));
		}

		public IQuery<T> WithExecutionOptions(IQueryExecutionOptions executionOptions)
		{
			return Wrap(query.WithExecutionOptions(executionOptions));
		}

		public IQuery<T> WithExecutionOptions(Func<IQueryExecutionOptions, IQueryExecutionOptions> executionOptionsModifier)
		{
			return Wrap(query.WithExecutionOptions(executionOptionsModifier));
		}

		public override string ToString()
		{
			return $"{query} SELECT {string.Join(", ", Fields)}";
		}

		private PartialQuery<T> Wrap(IQuery<T> modified)
		{
			return new PartialQuery<T>(modified, Fields);
		}
	}
}
