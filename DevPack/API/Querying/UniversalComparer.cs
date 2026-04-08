namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	internal static class UniversalComparer
	{
		internal static int Compare<T>(T x, T y)
		{
			if (Object.Equals(x, y))
			{
				return 0;
			}

			if (x == null)
			{
				return -1;
			}

			if (y == null)
			{
				return 1;
			}

			Type type = typeof(T);
			Type underlying = Nullable.GetUnderlyingType(type);

			if (underlying != null)
			{
				// Nullable value type like int?
				object xValue = Convert.ChangeType(x, underlying);
				object yValue = Convert.ChangeType(y, underlying);

				return ((IComparable)xValue).CompareTo(yValue);
			}

			var comparableT = x as IComparable<T>;
			if (comparableT != null)
			{
				return comparableT.CompareTo(y);
			}

			var comparable = x as IComparable;
			if (comparable != null)
			{
				return comparable.CompareTo(y);
			}

			throw new ArgumentException("Type " + type.FullName + " does not implement IComparable or IComparable<T>");
		}

		internal static bool Equals<T>(T x, T y)
		{
			if (Object.Equals(x, y))
			{
				return true;
			}

			if (x == null || y == null)
			{
				return false;
			}

			var equatableT = x as IEquatable<T>;
			if (equatableT != null)
			{
				return equatableT.Equals(y);
			}

			// Fall back to Object.Equals if no better option is available
			return x.Equals(y);
		}
	}
}
