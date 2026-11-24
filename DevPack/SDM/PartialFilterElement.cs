namespace Skyline.DataMiner.SDM
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	internal sealed class PartialFilterElement<T> : IPartialFilterElement<T>
	{
		public PartialFilterElement(FilterElement<T> filter, IReadOnlyCollection<FieldExposer> fields)
		{
			Filter = filter;
			Fields = fields;
		}

		public FilterElement<T> Filter { get; }

		public IReadOnlyCollection<FieldExposer> Fields { get; }

		public override string ToString()
		{
			return $"{Filter} SELECT {string.Join(", ", Fields)}";
		}
	}
}
