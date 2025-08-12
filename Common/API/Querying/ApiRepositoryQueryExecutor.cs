namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using SLDataGateway.API.Querying;
    using SLDataGateway.API.Types.Querying;

    internal class ApiRepositoryQueryExecutor<T, TResult> : ExpressionVisitor
        where T : ApiObject
    {
        private readonly ApiRepositoryQueryProvider<T> provider;

        private readonly List<FilterElement<DomInstance>> filters = new List<FilterElement<DomInstance>>();
        private readonly List<IOrderByElement> orderBy = new List<IOrderByElement>();
        private int? limit;
        private bool canExtendQuery = true;

        private ApiRepositoryQueryExecutor(ApiRepositoryQueryProvider<T> provider)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        internal static TResult Execute(ApiRepositoryQueryProvider<T> provider, Expression expression)
        {
            var visitor = new ApiRepositoryQueryExecutor<T, TResult>(provider);
            expression = visitor.Visit(expression);

            var lambda = Expression.Lambda<Func<TResult>>(expression).Compile();
            return lambda();
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if ((node.Method.DeclaringType == typeof(Queryable) || node.Method.DeclaringType == typeof(Enumerable))
                && canExtendQuery)
            {
                switch (node.Method.Name)
                {
                    case nameof(Queryable.First):
                    case nameof(Queryable.FirstOrDefault):
                        return HandleFirstAndSingle(node, limit: 1);

                    case nameof(Queryable.Single):
                    case nameof(Queryable.SingleOrDefault):
                        return HandleFirstAndSingle(node, limit: 2);

                    case nameof(Queryable.Any):
                        return HandleAny(node);

                    case nameof(Queryable.All):
                        return HandleAll(node);

                    case nameof(Queryable.Where):
                        return HandleWhere(node);

                    case nameof(Queryable.Count):
                    case nameof(Queryable.LongCount):
                        return HandleCount(node);

                    case nameof(Queryable.Take):
                        return HandleTake(node);

                    case nameof(Queryable.OrderBy):
                    case nameof(Queryable.ThenBy):
                        return HandleOrderBy(node, SortOrder.Ascending);

                    case nameof(Queryable.OrderByDescending):
                    case nameof(Queryable.ThenByDescending):
                        return HandleOrderBy(node, SortOrder.Descending);
                }
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is ApiRepositoryQuery<T>)
            {
                // replace with an expression that executes the query
                return BuildExecuteQueryExpression();
            }

            return base.VisitConstant(node);
        }

        private Expression HandleWhere(MethodCallExpression node)
        {
            Visit(node.Arguments[0]);

            AddFilter(node.Arguments[1]);
            return BuildExecuteQueryExpression();
        }

        private Expression HandleOrderBy(MethodCallExpression node, SortOrder sortOrder)
        {
            var arguments = Visit(node.Arguments);

            if (!node.Method.Name.StartsWith("ThenBy"))
            {
                orderBy.Clear();
            }

            if (ExpressionTools.TryGetMember(arguments[1], out var member))
            {
                var orderBy = provider.Repository.CreateOrderBy(member.Name, sortOrder);
                if (orderBy != null)
                {
                    this.orderBy.Add(orderBy);
                }

                return BuildExecuteQueryExpression();
            }

            throw new NotSupportedException($"Unsupported expression: {node}");
        }

        private Expression HandleCount(MethodCallExpression node)
        {
            Visit(node.Arguments[0]);

            if (node.Arguments.Count == 2)
            {
                AddFilter(node.Arguments[1]);
            }

            canExtendQuery = false;
            return Expression.Convert(BuildExecuteCountExpression(), node.Type);
        }

        private Expression HandleFirstAndSingle(MethodCallExpression node, int limit)
        {
            Visit(node.Arguments[0]);

            if (node.Arguments.Count == 2)
            {
                AddFilter(node.Arguments[1]);
            }

            this.limit = limit;
            canExtendQuery = false;

            // Queryable.First(x, ...) => Queryable.First(BuildExecuteQueryExpression(), ...)
            var newArguments =
                new[] { BuildExecuteQueryExpression() }
                .Concat(node.Arguments.Skip(1).Select(Visit));

            return node.Update(Visit(node.Object), newArguments);
        }

        private Expression HandleAny(MethodCallExpression node)
        {
            Visit(node.Arguments[0]);

            if (node.Arguments.Count == 2)
            {
                AddFilter(node.Arguments[1]);
            }

            canExtendQuery = false;

            // Queryable.Any(x, ...) => BuildExecuteCountExpression() > 0
            return Expression.GreaterThan(BuildExecuteCountExpression(), Expression.Constant(0L));
        }

        private Expression HandleAll(MethodCallExpression node)
        {
            Visit(node.Arguments[0]);

            canExtendQuery = false;

            // Queryable.All(x, ...) => BuildExecuteCountExpression(!filter) == 0
            AddFilter(node.Arguments[1], invertFilter: true);
            return Expression.Equal(BuildExecuteCountExpression(), Expression.Constant(0L));
        }

        private Expression HandleTake(MethodCallExpression node)
        {
            Visit(node.Arguments[0]);

            if (ExpressionTools.TryGetValue(node.Arguments[1], out var value) &&
                value is int number)
            {
                limit = number;

                canExtendQuery = false;
                return BuildExecuteQueryExpression();
            }

            throw new NotSupportedException($"Unsupported expression: {node}");
        }

        private void AddFilter(Expression expression, bool invertFilter = false)
        {
            var filter = ExpressionToFilterConverter<T>.Convert(expression, provider.Repository);

            if (filter == null)
            {
                throw new NotSupportedException($"Unsupported expression: {expression}");
            }

            if (invertFilter)
            {
                filter = new NOTFilterElement<DomInstance>(filter);
            }

            filters.Add(filter);
        }

        private FilterElement<DomInstance> BuildFilter()
        {
            FilterElement<DomInstance> filter;

            if (filters.Count == 1)
            {
                filter = filters[0];
            }
            else if (filters.Count > 1)
            {
                filter = new ANDFilterElement<DomInstance>(filters.ToArray());
            }
            else
            {
                filter = new TRUEFilterElement<DomInstance>();
            }

            return filter;
        }

        private IQuery<DomInstance> BuildQuery()
        {
            var query = BuildFilter().ToQuery();

            if (orderBy.Count > 0)
            {
                query = query.WithOrder(new OrderBy(orderBy));
            }

            if (limit.HasValue)
            {
                query = query.Limit(limit.Value);
            }

            return query;
        }

        private Expression BuildExecuteQueryExpression()
        {
            var query = BuildQuery();

            Expression<Func<IQueryable<T>>> func = () => provider.Repository.Read(query).AsQueryable();
            return func.Body;
        }

        private Expression BuildExecuteCountExpression()
        {
            var filter = BuildFilter();

            Expression<Func<long>> func = () => provider.Repository.Count(filter);
            return func.Body;
        }
    }
}
