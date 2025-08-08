namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Linq.Expressions;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal class ExpressionToFilterConverter<T>
        where T : ApiObject
    {
        private readonly Repository<T> _repository;

        private ExpressionToFilterConverter(Repository<T> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public static FilterElement<DomInstance> Convert(Expression expression, Repository<T> repository)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (repository is null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            var converter = new ExpressionToFilterConverter<T>(repository);

            return converter.ConvertInternal(expression);
        }

        private FilterElement<DomInstance> ConvertInternal(Expression expr)
        {
            return expr switch
            {
                BinaryExpression binary => ConvertBinary(binary),
                UnaryExpression unary => ConvertUnary(unary),
                ConstantExpression constant => ConvertConstant(constant),
                MethodCallExpression methodCall => ConvertMethodCall(methodCall),
                LambdaExpression lambda => ConvertInternal(lambda.Body),
                TypeBinaryExpression typeBinary => ConvertTypeBinary(typeBinary),
                _ => throw new NotSupportedException($"Unsupported expression: {expr.NodeType} ({expr})"),
            };
        }

        private FilterElement<DomInstance> ConvertBinary(BinaryExpression node)
        {
            // Handle logical combinations
            if (node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.OrElse)
            {
                var left = ConvertInternal(node.Left);
                var right = ConvertInternal(node.Right);

                return node.NodeType switch
                {
                    ExpressionType.AndAlso => new ANDFilterElement<DomInstance>(left, right),
                    ExpressionType.OrElse => new ORFilterElement<DomInstance>(left, right),
                    _ => throw new InvalidOperationException()
                };
            }

            // Handle comparisons
            if ((ExpressionTools.TryGetMember(node.Left, out var memberInfo) && ExpressionTools.TryGetValue(node.Right, out var value)) ||
                (ExpressionTools.TryGetMember(node.Right, out memberInfo) && ExpressionTools.TryGetValue(node.Left, out value)))
            {
                var comparer = ExpressionTypeToComparer(node.NodeType);
                return _repository.CreateFilter(memberInfo.Name, comparer, value);
            }

            throw new NotSupportedException($"Unsupported comparison expression: {node}");
        }

        private FilterElement<DomInstance> ConvertUnary(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Not:
                    var operand = ConvertInternal(node.Operand);
                    return new NOTFilterElement<DomInstance>(operand);

                case ExpressionType.Quote:
                    return ConvertInternal(node.Operand);

                default:
                    throw new NotSupportedException($"Unsupported unary expression: {node.NodeType}");
            }
        }

        private FilterElement<DomInstance> ConvertConstant(ConstantExpression node)
        {
            return node.Value switch
            {
                true => new TRUEFilterElement<DomInstance>(),
                false => new FALSEFilterElement<DomInstance>(),
                _ => throw new NotSupportedException($"Unsupported constant: {node.Value}")
            };
        }

        private FilterElement<DomInstance> ConvertMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(String) &&
                node.Method.Name == nameof(string.Contains) &&
                ExpressionTools.TryGetMember(node.Object, out var memberInfo) &&
                ExpressionTools.TryGetValue(node.Arguments[0], out var value))
            {
                return _repository.CreateFilter(memberInfo.Name, Comparer.Contains, value);
            }

            // Handle .Where(x => x.Levels.Any(l => l.Endpoint == videoSource1))
            if (node.Method.DeclaringType == typeof(System.Linq.Enumerable) &&
                node.Method.Name == nameof(System.Linq.Enumerable.Any) &&
                node.Arguments.Count == 2 &&
                ExpressionTools.TryGetMember(node.Arguments[0], out var collectionMemberInfo))
            {
                return ConvertInternal(node.Arguments[1]);
            }

            throw new NotSupportedException($"Unsupported method call: {node.Method}");
        }

        private FilterElement<DomInstance> ConvertTypeBinary(TypeBinaryExpression node)
        {
            return _repository.CreateFilter(node.TypeOperand, ExpressionTypeToComparer(node.NodeType));
        }

        private static Comparer ExpressionTypeToComparer(ExpressionType type)
        {
            return type switch
            {
                ExpressionType.Equal => Comparer.Equals,
                ExpressionType.NotEqual => Comparer.NotEquals,
                ExpressionType.LessThan => Comparer.LT,
                ExpressionType.LessThanOrEqual => Comparer.LTE,
                ExpressionType.GreaterThan => Comparer.GT,
                ExpressionType.GreaterThanOrEqual => Comparer.GTE,
                ExpressionType.TypeIs => Comparer.Equals,
                ExpressionType.TypeEqual => Comparer.Equals,
                _ => throw new NotSupportedException($"Unknown comparison: {type}")
            };
        }
    }
}
