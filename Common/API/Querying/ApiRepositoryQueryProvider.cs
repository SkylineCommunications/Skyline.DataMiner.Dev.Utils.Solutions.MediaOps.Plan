namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    internal class ApiRepositoryQueryProvider<T> : IQueryProvider where T : ApiObject
    {
        public ApiRepositoryQueryProvider(Repository<T> repository)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public Repository<T> Repository { get; }

        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = expression.Type.GetGenericArguments()[0];
            var queryType = typeof(ApiRepositoryQuery<>).MakeGenericType(elementType);
            return (IQueryable)Activator.CreateInstance(queryType, this, expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            if (typeof(TElement) != typeof(T))
            {
                throw new InvalidOperationException($"Invalid element type: expected {typeof(T)}, but got {typeof(TElement)}.");
            }

            return (IQueryable<TElement>)new ApiRepositoryQuery<T>(this, expression);
        }

        public object Execute(Expression expression)
        {
            return Execute<T>(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return ApiRepositoryQueryExecutor<T, TResult>.Execute(this, expression);
        }
    }
}
