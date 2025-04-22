namespace Skyline.DataMiner.MediaOps.API.Common.API.Generic.DomQuerying
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal class ExpressionToFilterVisitor<T> : ExpressionVisitor
        where T : IApiObject
    {
        private readonly DomDefinitionBase<T> _domDefinition;
        private readonly List<FilterElement<DomInstance>> _filters = new List<FilterElement<DomInstance>>();

        private ExpressionToFilterVisitor(DomDefinitionBase<T> domDefinition)
        {
            _domDefinition = domDefinition ?? throw new ArgumentNullException(nameof(domDefinition));
        }

        public static FilterElement<DomInstance> GetFilter(Expression expression, DomDefinitionBase<T> domDefinition)
        {
            var visitor = new ExpressionToFilterVisitor<T>(domDefinition);

            try
            {
                visitor.Visit(expression);
            }
            catch (Exception ex)
            {
                throw new NotSupportedException($"Unsupported expression: {expression}", ex);
            }

            return CombineFilters(visitor._filters);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var left = Visit(node.Left);
            var right = Visit(node.Right);

            switch (node.NodeType)
            {
                case ExpressionType.OrElse:
                    {
                        var filter = new ORFilterElement<DomInstance>(_filters.ToArray());
                        _filters.Clear();
                        _filters.Add(filter);
                    }

                    break;

                case ExpressionType.AndAlso:
                    {
                        var filter = new ANDFilterElement<DomInstance>(_filters.ToArray());
                        _filters.Clear();
                        _filters.Add(filter);
                    }

                    break;

                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                    {
                        if (ExpressionTools.TryGetMember(left, out var memberInfo) && ExpressionTools.TryGetValue(right, out var value) ||
                            ExpressionTools.TryGetMember(right, out memberInfo) && ExpressionTools.TryGetValue(left, out value))
                        {
                            var comparer = ExpressionTypeToComparer(node.NodeType);
                            var filter = _domDefinition.CreateFilter(memberInfo.Name, comparer, value);
                            _filters.Add(filter);
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported expression: {node}");
                        }
                    }

                    break;
            }

            return node.Update(left, node.Conversion, right);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            var operand = Visit(node.Operand);

            switch (node.NodeType)
            {
                case ExpressionType.Not:
                    var filter = new NOTFilterElement<DomInstance>(CombineFilters(_filters));
                    _filters.Clear();
                    _filters.Add(filter);
                    break;
            }

            return node.Update(operand);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            switch (node.Value)
            {
                case true:
                    _filters.Add(new TRUEFilterElement<DomInstance>());
                    return node;

                case false:
                    _filters.Add(new FALSEFilterElement<DomInstance>());
                    return node;
            }

            return base.VisitConstant(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var obj = Visit(node.Object);
            var arguments = Visit(node.Arguments);

            if (node.Method.DeclaringType == typeof(string) &&
                node.Method.Name == nameof(String.Contains) &&
                ExpressionTools.TryGetMember(obj, out var memberInfo) &&
                ExpressionTools.TryGetValue(arguments[0], out var value))
            {
                var fieldName = memberInfo.Name;
                var filter = _domDefinition.CreateFilter(fieldName, Comparer.Contains, value);
                _filters.Add(filter);
            }

            return node.Update(obj, arguments);
        }

        private static FilterElement<DomInstance> CombineFilters(ICollection<FilterElement<DomInstance>> filters)
        {
            if (filters.Count == 1)
            {
                return filters.First();
            }

            return new ANDFilterElement<DomInstance>(filters.ToArray());
        }

        private static Comparer ExpressionTypeToComparer(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Equal:
                    return Comparer.Equals;

                case ExpressionType.NotEqual:
                    return Comparer.NotEquals;

                case ExpressionType.LessThan:
                    return Comparer.LT;

                case ExpressionType.LessThanOrEqual:
                    return Comparer.LTE;

                case ExpressionType.GreaterThan:
                    return Comparer.GT;

                case ExpressionType.GreaterThanOrEqual:
                    return Comparer.GTE;
            }

            throw new InvalidOperationException($"Unknown expression type: {type}");
        }
    }
}