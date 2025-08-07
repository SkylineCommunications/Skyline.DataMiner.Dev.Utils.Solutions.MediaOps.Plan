namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    internal static class ExpressionTools
    {
        public static bool TryGetMember(Expression expression, out MemberInfo memberInfo)
        {
            expression = StripQuotes(expression);

            if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
            {
                expression = unary.Operand;
            }

            if (expression is LambdaExpression lambda)
            {
                expression = lambda.Body;
            }

            if (expression is MemberExpression memberExpression)
            {
                memberInfo = memberExpression.Member;
                return true;
            }

            memberInfo = null;
            return false;
        }

        public static bool TryGetValue(Expression expression, out object value)
        {
            switch (expression)
            {
                case ConstantExpression constant:
                    value = constant.Value;
                    return true;

                case MemberExpression member when member.Member is FieldInfo fieldInfo && fieldInfo.IsStatic:
                    value = fieldInfo.GetValue(null);
                    return true;

                case MemberExpression member when member.Member is PropertyInfo propertyInfo && propertyInfo.GetGetMethod().IsStatic:
                    value = propertyInfo.GetValue(null);
                    return true;

                case UnaryExpression unary when unary.NodeType == ExpressionType.Convert:
                    return TryGetValue(unary.Operand, out value);
            }

            try
            {
                var lambda = Expression.Lambda(expression).Compile();
                value = lambda.DynamicInvoke();
                return true;
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }

        public static Expression StripQuotes(Expression expression)
        {
            while (expression.NodeType == ExpressionType.Quote)
            {
                expression = ((UnaryExpression)expression).Operand;
            }

            return expression;
        }
    }
}
