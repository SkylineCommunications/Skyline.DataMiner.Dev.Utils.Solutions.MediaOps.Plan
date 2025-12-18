namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Linq;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal static class FilterElementFactory
    {
        public static FilterElement<T> Create<T, TValue>(Exposer<T, TValue> exposer, Comparer comparer, TValue value)
        {
            if (exposer is null)
            {
                throw new ArgumentNullException(nameof(exposer));
            }

            if (exposer is Exposer<T, string> stringExposer)
            {
                switch (comparer)
                {
                    case Comparer.Contains:
                        return stringExposer.Contains(value?.ToString());

                    case Comparer.NotEquals:
                        return stringExposer.NotContains(value?.ToString());
                }
            }

            switch (comparer)
            {
                case Comparer.Equals:
                    {
                        return new ManagedFilter<T, TValue>(
                            exposer,
                            Comparer.Equals,
                            value,
                            a => UniversalComparer.Equals(exposer.internalFunc(a), value));
                    }

                case Comparer.NotEquals:
                    {
                        return new ManagedFilter<T, TValue>(
                            exposer,
                            Comparer.Equals,
                            value,
                            a => !UniversalComparer.Equals(exposer.internalFunc(a), value));
                    }

                case Comparer.GT:
                    {
                        return new ManagedFilter<T, TValue>(
                            exposer,
                            Comparer.LT,
                            value,
                            a => UniversalComparer.Compare(exposer.internalFunc(a), value) > 0);
                    }

                case Comparer.GTE:
                    {
                        return new ManagedFilter<T, TValue>(
                            exposer,
                            Comparer.LT,
                            value,
                            a => UniversalComparer.Compare(exposer.internalFunc(a), value) > 0);
                    }

                case Comparer.LT:
                    {
                        return new ManagedFilter<T, TValue>(
                            exposer,
                            Comparer.LT,
                            value,
                            a => UniversalComparer.Compare(exposer.internalFunc(a), value) < 0);
                    }

                case Comparer.LTE:
                    {
                        return new ManagedFilter<T, TValue>(
                            exposer,
                            Comparer.LT,
                            value,
                            a => UniversalComparer.Compare(exposer.internalFunc(a), value) <= 0);
                    }

                default:
                    throw new NotSupportedException("This comparer option is not supported");
            }
        }

        public static FilterElement<T> Create<T, TValue>(DynamicListExposer<T, TValue> exposer, Comparer comparer, TValue value)
        {
            if (exposer is null)
            {
                throw new ArgumentNullException(nameof(exposer));
            }

            switch (comparer)
            {
                case Comparer.Equals:
                    return exposer.Equal(value);

                case Comparer.NotEquals:
                    return exposer.NotEqual(value);

                case Comparer.GT:
                    return exposer.GreaterThan(value);

                case Comparer.GTE:
                    return exposer.GreaterThanOrEqual(value);

                case Comparer.LT:
                    return exposer.LessThan(value);

                case Comparer.LTE:
                    return exposer.LessThanOrEqual(value);

                case Comparer.Contains:
                    return exposer.Contains(value);

                case Comparer.NotContains:
                    return exposer.NotContains(value);

                default:
                    throw new NotSupportedException("This comparer option is not supported");
            }
        }
    }
}
