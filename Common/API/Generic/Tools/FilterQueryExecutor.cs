namespace Skyline.DataMiner.MediaOps.API.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    public static class FilterQueryExecutor
    {
        public static IEnumerable<TResult> RetrieveFilteredItems<TId, TFilter, TResult>(IEnumerable<TId> ids, Func<TId, FilterElement<TFilter>> filterProvider, Func<FilterElement<TFilter>, IEnumerable<TResult>> filterResolver)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            if (filterProvider == null)
            {
                throw new ArgumentNullException(nameof(filterProvider));
            }

            if (filterResolver == null)
            {
                throw new ArgumentNullException(nameof(filterResolver));
            }

            var splitted = SplitFilter(ids, filterProvider).ToList();

            if (splitted.Count >= 10)
            {
                return splitted.AsParallel().SelectMany(filterResolver);
            }

            return splitted.SelectMany(filterResolver);
        }

        public static long CountFilteredItems<TId, TFilter>(IEnumerable<TId> ids, Func<TId, FilterElement<TFilter>> filterProvider, Func<FilterElement<TFilter>, long> filterCountResolver)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            if (filterProvider == null)
            {
                throw new ArgumentNullException(nameof(filterProvider));
            }

            if (filterCountResolver == null)
            {
                throw new ArgumentNullException(nameof(filterCountResolver));
            }

            var splitted = SplitFilter(ids, filterProvider).ToList();

            if (splitted.Count >= 10)
            {
                return splitted.AsParallel().Sum(filterCountResolver);
            }

            return splitted.Sum(filterCountResolver);
        }

        private static IEnumerable<FilterElement<TFilter>> SplitFilter<TId, TFilter>(IEnumerable<TId> ids, Func<TId, FilterElement<TFilter>> filterProvider)
        {
            var batch = new List<FilterElement<TFilter>>();
            var count = 0;

            // max_clause_count is by default set to minimum 1024
            // https://www.elastic.co/guide/en/elasticsearch/reference/7.17/search-settings.html
            const int limit = 1000;

            int CountSubFilters(FilterElement<TFilter> filter)
            {
                switch (filter)
                {
                    case ANDFilterElement<TFilter> and: return and.subFilters.Sum(CountSubFilters);
                    case ORFilterElement<TFilter> or: return or.subFilters.Sum(CountSubFilters);
                    default: return 1;
                }
            }

            foreach (var id in ids.Distinct())
            {
                var filter = filterProvider(id);
                var subfilterCount = filter.flatten().Sum(CountSubFilters);

                if (count + subfilterCount > limit && batch.Count > 0)
                {
                    yield return new ORFilterElement<TFilter>(batch.ToArray());

                    batch.Clear();
                    count = 0;
                }

                batch.Add(filter);
                count += subfilterCount;
            }

            // don't forget the last items
            if (batch.Count > 0)
            {
                yield return new ORFilterElement<TFilter>(batch.ToArray());
            }
        }
    }
}