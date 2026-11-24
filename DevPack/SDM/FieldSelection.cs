namespace Skyline.DataMiner.SDM
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	internal static class FieldSelection
	{
		public static IReadOnlyCollection<FieldExposer> Create<T>(IEnumerable<FieldExposer> fields, string paramName)
		{
			if (fields == null)
			{
				throw new ArgumentNullException(paramName);
			}

			var selection = new List<FieldExposer>();
			var seen = new HashSet<FieldExposer>(FieldExposerComparer.Instance);

			foreach (var field in fields)
			{
				ValidateField<T>(field, paramName);

				if (seen.Add(field))
				{
					selection.Add(field);
				}
			}

			if (selection.Count == 0)
			{
				throw new ArgumentException("At least one field must be selected.", paramName);
			}

			return selection.AsReadOnly();
		}

		public static IReadOnlyCollection<FieldExposer> Merge<T>(IEnumerable<FieldExposer> existing, IEnumerable<FieldExposer> additional, string paramName)
		{
			if (additional == null)
			{
				throw new ArgumentNullException(paramName);
			}

			return Create<T>(existing.Concat(additional), paramName);
		}

		public static void ValidateField<T>(FieldExposer field, string paramName)
		{
			if (field == null)
			{
				throw new ArgumentNullException(paramName, "Fields can't contain null.");
			}

			if (field.FilterType == null || !field.FilterType.IsAssignableFrom(typeof(T)))
			{
				throw new ArgumentException($"Field '{field.fieldName}' belongs to '{field.FilterType?.Name}' instead of '{typeof(T).Name}'.", paramName);
			}
		}
	}
}
