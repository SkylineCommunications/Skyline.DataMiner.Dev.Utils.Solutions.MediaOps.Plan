namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    internal class ApiRepositoryQuery<T> : IOrderedQueryable<T> where T : ApiObject
    {
        public ApiRepositoryQuery(ApiRepositoryQueryProvider<T> provider)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = Expression.Constant(this);
        }

        public ApiRepositoryQuery(ApiRepositoryQueryProvider<T> provider, Expression expression)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        public Type ElementType => typeof(T);

        public Expression Expression { get; }

        public ApiRepositoryQueryProvider<T> Provider { get; }

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
