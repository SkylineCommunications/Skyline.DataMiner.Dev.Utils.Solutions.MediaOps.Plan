namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying
{
	using System;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;

	using SLDataGateway.API.Types.Querying;

	internal abstract class ParameterFilterTranslator<T> : FilterTranslator<T, Net.Profiles.Parameter> where T : ApiObject
	{
		protected abstract FilterElement<Net.Profiles.Parameter> ParameterTypeFilter { get; }

		protected static FilterElement<Net.Profiles.Parameter> HandleGuid(Comparer comparer, object value)
		{
			return FilterElementFactory.Create(ParameterExposers.ID, comparer, (Guid)value);
		}

		protected static FilterElement<Net.Profiles.Parameter> HandleName(Comparer comparer, object value)
		{
			return FilterElementFactory.Create(ParameterExposers.Name, comparer, (string)value);
		}

		protected static FilterElement<Net.Profiles.Parameter> HandleIsMandatory(Comparer comparer, object value)
		{
			return FilterElementFactory.Create(ParameterExposers.IsOptional, comparer, !(bool)value);
		}

		protected static IOrderByElement HandleGuid(SortOrder sortOrder, bool naturalSort)
		{
			return OrderByElementFactory.Create(ParameterExposers.ID, sortOrder, naturalSort);
		}

		protected static IOrderByElement HandleName(SortOrder sortOrder, bool naturalSort)
		{
			return OrderByElementFactory.Create(ParameterExposers.Name, sortOrder, naturalSort);
		}

		protected static IOrderByElement HandleIsMandatory(SortOrder sortOrder, bool naturalSort)
		{
			// IsMandatory is stored as the inverse of IsOptional, so the requested sort order has to be inverted as well.
			return OrderByElementFactory.Create(ParameterExposers.IsOptional, InvertSortOrder(sortOrder), naturalSort);
		}

		private static SortOrder InvertSortOrder(SortOrder sortOrder)
		{
			switch (sortOrder)
			{
				case SortOrder.Ascending:
					return SortOrder.Descending;
				case SortOrder.Descending:
					return SortOrder.Ascending;
				default:
					return sortOrder;
			}
		}

		public override FilterElement<Net.Profiles.Parameter> TranslateFilter(FilterElement<T> filter)
		{
			return base.TranslateFilter(filter).AND(ParameterTypeFilter);
		}
	}
}
