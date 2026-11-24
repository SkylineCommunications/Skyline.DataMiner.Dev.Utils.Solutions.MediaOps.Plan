namespace Skyline.DataMiner.SDM
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Compares exposers on the type they belong to and their field name, because exposer instances don't implement value equality.
	/// </summary>
	internal sealed class FieldExposerComparer : IEqualityComparer<FieldExposer>
	{
		public static FieldExposerComparer Instance { get; } = new FieldExposerComparer();

		public bool Equals(FieldExposer x, FieldExposer y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}

			if (x == null || y == null)
			{
				return false;
			}

			return x.FilterType == y.FilterType && String.Equals(x.fieldName, y.fieldName, StringComparison.Ordinal);
		}

		public int GetHashCode(FieldExposer obj)
		{
			if (obj == null)
			{
				return 0;
			}

			unchecked
			{
				return ((obj.FilterType?.GetHashCode() ?? 0) * 397) ^ (obj.fieldName?.GetHashCode() ?? 0);
			}
		}
	}
}
