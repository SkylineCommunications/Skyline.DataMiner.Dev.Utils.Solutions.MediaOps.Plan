namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    internal class ApiRepositoryQueryProvider<T, TFilterElement> : IQueryProvider
        where T : ApiObject
        where TFilterElement : Net.Messages.SLDataGateway.DataType
    {
        public ApiRepositoryQueryProvider(Repository<T, TFilterElement> repository)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public Repository<T, TFilterElement> Repository { get; }

        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = expression.Type.GetGenericArguments()[0];
            var queryType = typeof(ApiRepositoryQuery<T, TFilterElement>).MakeGenericType(elementType);
            return (IQueryable)Activator.CreateInstance(queryType, this, expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            if (typeof(TElement) != typeof(T))
            {
                throw new InvalidOperationException($"Invalid element type: expected {typeof(T)}, but got {typeof(TElement)}.");
            }

            return (IQueryable<TElement>)new ApiRepositoryQuery<T, TFilterElement>(this, expression);
        }

        public object Execute(Expression expression)
        {
            return Execute<T>(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return ApiRepositoryQueryExecutor<T, TResult, TFilterElement>.Execute(this, expression);
        }
    }
}
