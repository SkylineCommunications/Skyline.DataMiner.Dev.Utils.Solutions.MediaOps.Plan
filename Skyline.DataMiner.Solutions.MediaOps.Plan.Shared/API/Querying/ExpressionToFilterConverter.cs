namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Linq.Expressions;
    using Skyline.DataMiner.Net.Apps.UserDefinableApis.Messages;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal class ExpressionToFilterConverter<T, TFilterElement>
        where T : ApiObject
        where TFilterElement : DataType
    {
        private readonly Repository<T, TFilterElement> _repository;

        private ExpressionToFilterConverter(Repository<T, TFilterElement> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public static FilterElement<TFilterElement> Convert(Expression expression, Repository<T, TFilterElement> repository)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (repository is null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            var converter = new ExpressionToFilterConverter<T, TFilterElement>(repository);

            return converter.ConvertInternal(expression);
        }

        private FilterElement<TFilterElement> ConvertInternal(Expression expr)
        {
            return expr switch
            {
                BinaryExpression binary => ConvertBinary(binary),
                UnaryExpression unary => ConvertUnary(unary),
                ConstantExpression constant => ConvertConstant(constant),
                MethodCallExpression methodCall => ConvertMethodCall(methodCall),
                LambdaExpression lambda => ConvertInternal(lambda.Body),
                TypeBinaryExpression typeBinary => ConvertTypeBinary(typeBinary),
                MemberExpression member => ConvertMember(member),
                _ => throw new NotSupportedException($"Unsupported expression: {expr.NodeType} ({expr})"),
            };
        }

        private FilterElement<TFilterElement> ConvertBinary(BinaryExpression node)
        {
            // Handle logical combinations
            if (node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.OrElse)
            {
                var left = ConvertInternal(node.Left);
                var right = ConvertInternal(node.Right);

                return node.NodeType switch
                {
                    ExpressionType.AndAlso => new ANDFilterElement<TFilterElement>(left, right),
                    ExpressionType.OrElse => new ORFilterElement<TFilterElement>(left, right),
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

        private FilterElement<TFilterElement> ConvertUnary(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Not:
                    var operand = ConvertInternal(node.Operand);
                    return new NOTFilterElement<TFilterElement>(operand);

                case ExpressionType.Quote:
                    return ConvertInternal(node.Operand);

                default:
                    throw new NotSupportedException($"Unsupported unary expression: {node.NodeType}");
            }
        }

        private FilterElement<TFilterElement> ConvertConstant(ConstantExpression node)
        {
            return node.Value switch
            {
                true => new TRUEFilterElement<TFilterElement>(),
                false => new FALSEFilterElement<TFilterElement>(),
                _ => throw new NotSupportedException($"Unsupported constant: {node.Value}")
            };
        }

        private FilterElement<TFilterElement> ConvertMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(String) && TryConvertStringMethodCall(node, out var filter))
            {
                return filter;
            }

            // Handle .Where(x => x.Discreets.Contains("option1")) 
            if (node.Method.DeclaringType == typeof(System.Linq.Enumerable) &&
                node.Method.Name == nameof(System.Linq.Enumerable.Contains) &&
                ExpressionTools.TryGetMember(node.Arguments[0], out var listStringMemberInfo) &&
                ExpressionTools.TryGetValue(node.Arguments[1], out var listStringValue))
            {
                return _repository.CreateFilter(listStringMemberInfo.Name, Comparer.Contains, listStringValue);
            }

            // Handle .Where(x => x.IsActive.Equals(true))
            // Handle .Where(x => x.Name.Equals("foo"))
            if (node.Method.Name == nameof(object.Equals) &&
                ExpressionTools.TryGetMember(node.Object, out var equalsMemberInfo) &&
                ExpressionTools.TryGetValue(node.Arguments[0], out var equalsValue))
            {
                return _repository.CreateFilter(equalsMemberInfo.Name, Comparer.Equals, equalsValue);
            }

            // Handle .Where(x => x.Levels.Any(l => l.Endpoint == videoSource1))
            if (node.Method.DeclaringType == typeof(System.Linq.Enumerable) &&
                node.Method.Name == nameof(System.Linq.Enumerable.Any) &&
                node.Arguments.Count == 2 &&
                ExpressionTools.TryGetMember(node.Arguments[0], out _))
            {
                return ConvertInternal(node.Arguments[1]);
            }

            throw new NotSupportedException($"Unsupported method call: {node.Method}");
        }

        private bool TryConvertStringMethodCall(MethodCallExpression node, out FilterElement<TFilterElement> filter)
        {
            // Handle .Where(x => x.Name.Contains("foo"))
            if (node.Method.Name == nameof(string.Contains) &&
                ExpressionTools.TryGetMember(node.Object, out var containsMemberInfo) &&
                ExpressionTools.TryGetValue(node.Arguments[0], out var containsValue))
            {
                filter = _repository.CreateFilter(containsMemberInfo.Name, Comparer.Contains, containsValue);
                return true;
            }

            // Handle .Where(x => x.Name.StartsWith("foo"))
            if (node.Method.Name == nameof(string.StartsWith) &&
                ExpressionTools.TryGetMember(node.Object, out var startsWithMemberInfo) &&
                ExpressionTools.TryGetValue(node.Arguments[0], out var startsWithValue))
            {
                filter = _repository.CreateFilter(startsWithMemberInfo.Name, Comparer.Regex, $"^{startsWithValue}");
                return true;
            }

            // Handle .Where(x => x.Name.EndsWith("foo"))
            if (node.Method.Name == nameof(string.EndsWith) &&
                ExpressionTools.TryGetMember(node.Object, out var endsWithMemberInfo) &&
                ExpressionTools.TryGetValue(node.Arguments[0], out var endsWithValue))
            {
                filter = _repository.CreateFilter(endsWithMemberInfo.Name, Comparer.Regex, $"{endsWithValue}$");
                return true;
            }

            // Handle .Where(x => String.Equals(x.Name, "foo"))
            if (node.Method.Name == nameof(string.Equals) &&
                node.Arguments.Count == 2 &&
                ExpressionTools.TryGetMember(node.Arguments[0], out var staticStringEqualsMember1) &&
                ExpressionTools.TryGetValue(node.Arguments[1], out var staticStringEqualsValue1))
            {
                filter = _repository.CreateFilter(staticStringEqualsMember1.Name, Comparer.Equals, staticStringEqualsValue1);
                return true;
            }

            // Handle .Where(x => String.Equals("foo", x.Name))
            if (node.Method.Name == nameof(string.Equals) &&
                node.Arguments.Count == 2 &&
                ExpressionTools.TryGetValue(node.Arguments[0], out var staticStringEqualsValue2) &&
                ExpressionTools.TryGetMember(node.Arguments[1], out var staticStringEqualsMember2))
            {
                filter = _repository.CreateFilter(staticStringEqualsMember2.Name, Comparer.Equals, staticStringEqualsValue2);
                return true;
            }

            filter = null;
            return false;
        }

        private FilterElement<TFilterElement> ConvertTypeBinary(TypeBinaryExpression node)
        {
            return _repository.CreateFilter(node.TypeOperand, ExpressionTypeToComparer(node.NodeType));
        }

        private FilterElement<TFilterElement> ConvertMember(MemberExpression node)
        {
            // Handle .Where(x => x.IsActive)
            if (node.Type == typeof(Boolean))
            {
                return _repository.CreateFilter(node.Member.Name, Comparer.Equals, true);
            }

            throw new NotSupportedException($"Unsupported property expression: {node}");
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
