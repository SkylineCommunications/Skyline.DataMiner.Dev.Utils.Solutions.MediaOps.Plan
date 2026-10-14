namespace Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Querying
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	/// <summary>
	/// Rewrites <see cref="DateTimeOffset"/> values in DOM instance filters to UTC <see cref="DateTime"/> values.
	/// </summary>
	/// <remarks>
	/// DOM stores date/time field values as <see cref="DateTime"/>. A real DataMiner Agent compares them with a
	/// <see cref="DateTimeOffset"/> filter value as the same instant, while the in-memory DOM message handler cannot
	/// compare both types and never matches.
	/// </remarks>
	internal static class DomFilterDateTimeNormalizer
	{
		public static void Normalize(DMSMessage message)
		{
			switch (message)
			{
				case ManagerStoreReadRequest<DomInstance> read:
					read.Query = Normalize(read.Query);
					break;

				case ManagerStoreCountRequest<DomInstance> count:
					count.Query = Normalize(count.Query);
					break;

				case ManagerStoreStartPagingRequest<DomInstance> paging:
					paging.Filter = Normalize(paging.Filter);
					break;
			}
		}

		private static IQuery<DomInstance> Normalize(IQuery<DomInstance> query)
		{
			if (query?.Filter == null)
			{
				return query;
			}

			var filter = Normalize(query.Filter);

			return ReferenceEquals(filter, query.Filter) ? query : query.WithFilter(filter);
		}

		private static FilterElement<DomInstance> Normalize(FilterElement<DomInstance> filter)
		{
			switch (filter)
			{
				case ANDFilterElement<DomInstance> and:
					var andSubFilters = and.subFilters.Select(Normalize).ToArray();
					return andSubFilters.SequenceEqual(and.subFilters) ? filter : new ANDFilterElement<DomInstance>(andSubFilters);

				case ORFilterElement<DomInstance> or:
					var orSubFilters = or.subFilters.Select(Normalize).ToArray();
					return orSubFilters.SequenceEqual(or.subFilters) ? filter : new ORFilterElement<DomInstance>(orSubFilters);

				case NOTFilterElement<DomInstance> not:
					var original = Normalize(not.original);
					return ReferenceEquals(original, not.original) ? filter : new NOTFilterElement<DomInstance>(original);

				case DynamicManagedListFilter<DomInstance, object> field when field.getValue() is DateTimeOffset value:
					return TryCreateFieldFilter(field, value.UtcDateTime, out var normalized) ? normalized : filter;

				default:
					return filter;
			}
		}

		private static bool TryCreateFieldFilter(DynamicManagedListFilter<DomInstance, object> field, DateTime value, out FilterElement<DomInstance> filter)
		{
			filter = null;

			const string Prefix = "FieldValues.";
			var fieldName = field.exposer?.fieldName;
			if (fieldName == null
				|| !fieldName.StartsWith(Prefix, StringComparison.Ordinal)
				|| !Guid.TryParse(fieldName.Substring(Prefix.Length), out var fieldDescriptorId))
			{
				return false;
			}

			var exposer = DomInstanceExposers.FieldValues.DomInstanceField(new FieldDescriptorID(fieldDescriptorId));

			switch (field.Comp)
			{
				case Comparer.Equals:
					filter = exposer.Equal(value);
					break;

				case Comparer.NotEquals:
					filter = exposer.NotEqual(value);
					break;

				case Comparer.GT:
					filter = exposer.GreaterThan(value);
					break;

				case Comparer.GTE:
					filter = exposer.GreaterThanOrEqual(value);
					break;

				case Comparer.LT:
					filter = exposer.LessThan(value);
					break;

				case Comparer.LTE:
					filter = exposer.LessThanOrEqual(value);
					break;
			}

			return filter != null;
		}
	}
}
