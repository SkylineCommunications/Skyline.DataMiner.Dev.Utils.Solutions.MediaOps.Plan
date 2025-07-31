namespace Skyline.DataMiner.MediaOps.Plan.Extensions
{
    using System;
    using System.Collections.Generic;

    internal static class IEnumerableExtensions
    {
        public static Dictionary<TKey, TElement> SafeToDictionary<TSource, TKey, TElement>(
             this IEnumerable<TSource> source,
             Func<TSource, TKey> keySelector,
             Func<TSource, TElement> elementSelector,
             IEqualityComparer<TKey> comparer = null)
        {
            var dictionary = new Dictionary<TKey, TElement>(comparer);

            if (source == null)
            {
                return dictionary;
            }

            foreach (TSource element in source)
            {
                dictionary[keySelector(element)] = elementSelector(element);
            }

            return dictionary;
        }

        public static Dictionary<TKey, TSource> SafeToDictionary<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer = null)
        {
            return source.SafeToDictionary(keySelector, x => x, comparer);
        }

        public static IEnumerable<TSource> Duplicates<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IEqualityComparer<TKey> comparer = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            var set = new HashSet<TKey>(comparer);

            foreach (var item in source)
            {
                if (!set.Add(selector(item)))
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<TSource> Duplicates<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource> comparer = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return Duplicates(source, x => x, comparer);
        }
    }
}
