namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;

	using Comparer = Net.Messages.SLDataGateway.Comparer;

	internal abstract class DomInstanceFilterTranslator<T> : FilterTranslator<T, DomInstance> where T : ApiObject
	{
		protected DomInstanceFilterTranslator()
		{
		}

		protected abstract FilterElement<DomInstance> DomDefinitionFilter { get; }

		protected abstract Dictionary<string, Func<SortOrder, bool, IOrderByElement>> OrderByHandlers { get; }

		protected static FilterElement<DomInstance> HandleGuid(Comparer comparer, object value)
		{
			return FilterElementFactory.Create(DomInstanceExposers.Id, comparer, (Guid)value);
		}

		protected static IOrderByElement HandleGuid(SortOrder sortOrder, bool naturalSort)
		{
			return OrderByElementFactory.Create(DomInstanceExposers.Id, sortOrder, naturalSort);
		}

		public override FilterElement<DomInstance> TranslateFilter(FilterElement<T> filter)
		{
			return base.TranslateFilter(filter).AND(DomDefinitionFilter);
		}

		public virtual IOrderBy TranslateFullOrderBy(IOrderBy order)
		{
			if (order == null)
			{
				throw new ArgumentNullException(nameof(order));
			}

			var translatedElements = new List<IOrderByElement>();

			foreach (var orderByElement in order.Elements)
			{
				var translated = TranslateOrderBy(orderByElement);
				translatedElements.Add(translated);
			}

			return new OrderBy(translatedElements);
		}

		protected virtual IOrderByElement TranslateOrderBy(IOrderByElement orderByElement)
		{
			if (orderByElement == null)
			{
				throw new ArgumentNullException(nameof(orderByElement));
			}

			var fieldName = orderByElement.Exposer.fieldName;
			var sortOrder = orderByElement.SortOrder;
			var naturalSort = orderByElement.Options.NaturalSort;

			var translated = CreateOrderBy(fieldName, sortOrder, naturalSort);

			return translated;
		}

		protected internal virtual IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
		{
			if (!OrderByHandlers.ContainsKey(fieldName))
			{
				throw new NotSupportedException($"Creating an order by for field '{fieldName}' is not implemented.");
			}

			return OrderByHandlers[fieldName].Invoke(sortOrder, naturalSort);
		}
	}
}
