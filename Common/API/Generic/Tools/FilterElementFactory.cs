namespace Skyline.DataMiner.MediaOps.API.Common
{
    using System;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    public static class FilterElementFactory
    {
        public static FilterElement<DomInstance> Create<T>(DynamicListExposer<DomInstance, T> exposer, Comparer comparer, T value)
        {
            if (exposer == null)
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

        public static FilterElement<DomInstance> Create<T>(Exposer<DomInstance, T> exposer, Comparer comparer, T value)
        {
            if (exposer == null)
            {
                throw new ArgumentNullException(nameof(exposer));
            }

            switch (comparer)
            {
                case Comparer.Equals:
                    return exposer.UncheckedEqual(value);

                case Comparer.NotEquals:
                    return exposer.UncheckedNotEqual(value);

                case Comparer.GT:
                    return exposer.UncheckedGreaterThan(value);

                case Comparer.GTE:
                    return exposer.UncheckedGreaterThanOrEqual(value);

                case Comparer.LT:
                    return exposer.UncheckedLessThan(value);

                case Comparer.LTE:
                    return exposer.UncheckedLessThanOrEqual(value);

                default:
                    throw new NotSupportedException("This comparer option is not supported");
            }
        }

        //internal static FilterElement<DomInstance> Create<T, TApi>(DynamicListExposer<DomInstance, T> exposer, Comparer comparer, ApiObjectReference<TApi> apiObjRef)
        //	where TApi : DomObject
        //{
        //	if (exposer == null)
        //	{
        //		throw new ArgumentNullException(nameof(exposer));
        //	}

        //	var value = (T)(object)apiObjRef.ID;
        //	return Create(exposer, comparer, value);
        //}

        //internal static FilterElement<DomInstance> Create<T, TApi>(Exposer<DomInstance, T> exposer, Comparer comparer, ApiObjectReference<TApi> apiObjRef)
        //	where TApi : DomObject
        //{
        //	if (exposer == null)
        //	{
        //		throw new ArgumentNullException(nameof(exposer));
        //	}

        //	var value = (T)(object)apiObjRef.ID;
        //	return Create(exposer, comparer, value);
        //}
    }
}