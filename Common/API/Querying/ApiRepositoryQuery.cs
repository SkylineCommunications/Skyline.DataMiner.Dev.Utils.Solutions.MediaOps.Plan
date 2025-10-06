namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    internal class ApiRepositoryQuery<T, TFilterElement> : IOrderedQueryable<T>
        where T : ApiObject
        where TFilterElement : Net.Messages.SLDataGateway.DataType
    {
        public ApiRepositoryQuery(ApiRepositoryQueryProvider<T, TFilterElement> provider)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = Expression.Constant(this);
        }

        public ApiRepositoryQuery(ApiRepositoryQueryProvider<T, TFilterElement> provider, Expression expression)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        public Type ElementType => typeof(T);

        public Expression Expression { get; }

        public ApiRepositoryQueryProvider<T, TFilterElement> Provider { get; }

        IQueryProvider IQueryable.Provider => Provider;

        public IEnumerator<T> GetEnumerator()
        {
            return Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
