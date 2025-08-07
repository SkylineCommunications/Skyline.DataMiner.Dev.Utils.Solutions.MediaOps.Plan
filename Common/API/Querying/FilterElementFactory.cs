namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    public static class FilterElementFactory
    {
        public static FilterElement<DomInstance> Create<T>(DynamicListExposer<DomInstance, T> exposer, Comparer comparer, object value)
        {
            if (exposer == null)
            {
                throw new ArgumentNullException(nameof(exposer));
            }

            var filterValue = ConvertFilterValue<T>(value);

            return CreateFilter(exposer, comparer, filterValue);
        }

        public static FilterElement<DomInstance> Create<T>(DynamicListExposer<DomInstance, object> exposer, Comparer comparer, object value)
        {
            if (exposer == null)
            {
                throw new ArgumentNullException(nameof(exposer));
            }

            var filterValue = ConvertFilterValue<T>(value);

            return CreateFilter(exposer, comparer, filterValue);
        }

        public static FilterElement<DomInstance> Create<T>(Exposer<DomInstance, T> exposer, Comparer comparer, object value)
        {
            if (exposer == null)
            {
                throw new ArgumentNullException(nameof(exposer));
            }

            var filterValue = ConvertFilterValue<T>(value);

            return CreateFilter(exposer, comparer, filterValue);
        }

        public static FilterElement<DomInstance> Create<T>(Exposer<DomInstance, object> exposer, Comparer comparer, object value)
        {
            if (exposer == null)
            {
                throw new ArgumentNullException(nameof(exposer));
            }

            var filterValue = ConvertFilterValue<T>(value);

            return CreateFilter(exposer, comparer, filterValue);
        }

        private static FilterElement<DomInstance> CreateFilter<T>(DynamicListExposer<DomInstance, T> exposer, Comparer comparer, T filterValue)
        {
            switch (comparer)
            {
                case Comparer.Equals:
                    return exposer.Equal(filterValue);
                case Comparer.NotEquals:
                    return exposer.NotEqual(filterValue);
                case Comparer.GT:
                    return exposer.GreaterThan(filterValue);
                case Comparer.GTE:
                    return exposer.GreaterThanOrEqual(filterValue);
                case Comparer.LT:
                    return exposer.LessThan(filterValue);
                case Comparer.LTE:
                    return exposer.LessThanOrEqual(filterValue);
                case Comparer.Contains:
                    return exposer.Contains(filterValue);
                case Comparer.NotContains:
                    return exposer.NotContains(filterValue);
                default:
                    throw new NotSupportedException("This comparer option is not supported");
            }
        }

        private static FilterElement<DomInstance> CreateFilter<T>(Exposer<DomInstance, T> exposer, Comparer comparer, T filterValue)
        {
            switch (comparer)
            {
                case Comparer.Equals:
                    return exposer.UncheckedEqual(filterValue);
                case Comparer.NotEquals:
                    return exposer.UncheckedNotEqual(filterValue);
                case Comparer.GT:
                    return exposer.UncheckedGreaterThan(filterValue);
                case Comparer.GTE:
                    return exposer.UncheckedGreaterThanOrEqual(filterValue);
                case Comparer.LT:
                    return exposer.UncheckedLessThan(filterValue);
                case Comparer.LTE:
                    return exposer.UncheckedLessThanOrEqual(filterValue);
                default:
                    throw new NotSupportedException("This comparer option is not supported");
            }
        }

        private static T ConvertFilterValue<T>(object value)
        {
            if (value == null)
            {
                return default;
            }

            // Already the correct type
            if (value is T typedValue)
            {
                return typedValue;
            }

            if (typeof(T) == typeof(string) && value is DmsElementId id)
            {
                return (T)(object)id.Value;
            }

            if (typeof(T) == typeof(Guid))
            {
                if (value is ApiObject apiRef)
                {
                    return (T)(object)apiRef.Id;
                }
                else if (value is string str && Guid.TryParse(str, out var guid))
                {
                    return (T)(object)guid;
                }
            }

            if (value is IConvertible)
            {
                var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                return (T)Convert.ChangeType(value, targetType);
            }

            throw new InvalidCastException($"Cannot convert value of type {value.GetType()} to {typeof(T)}.");
        }
    }
}
