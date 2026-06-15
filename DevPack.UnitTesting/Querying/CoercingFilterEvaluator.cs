namespace Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Querying
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using Comparer = Skyline.DataMiner.Net.Messages.SLDataGateway.Comparer;

	/// <summary>
	/// Evaluates a <see cref="FilterElement{T}"/> against an in-memory collection while coercing
	/// numeric and other compatible value types before comparing them.
	/// </summary>
	/// <remarks>
	/// A real DataMiner Agent stores values in typed database columns and lets the database engine
	/// coerce values (e.g. <see cref="int"/> versus <see cref="long"/>, or <see cref="decimal"/>
	/// versus <see cref="int"/>) when evaluating a filter. The default in-memory filter evaluation
	/// used by SLNet performs strict type comparisons, which causes filters built with a literal of
	/// a different runtime type than the stored value to silently return no matches. This evaluator
	/// mirrors the coercion behavior of a real DataMiner Agent so that the in-memory simulation
	/// returns the same results.
	/// </remarks>
	internal static class CoercingFilterEvaluator
	{
		private static readonly HashSet<TypeCode> NumericTypeCodes = new HashSet<TypeCode>
		{
			TypeCode.SByte, TypeCode.Byte, TypeCode.Int16, TypeCode.UInt16,
			TypeCode.Int32, TypeCode.UInt32, TypeCode.Int64, TypeCode.UInt64,
			TypeCode.Single, TypeCode.Double, TypeCode.Decimal,
		};

		/// <summary>
		/// Returns the items that match the supplied filter, applying type coercion during comparison.
		/// </summary>
		/// <typeparam name="T">The type of the items being filtered.</typeparam>
		/// <param name="filter">The filter to evaluate. When <see langword="null"/>, all items match.</param>
		/// <param name="items">The items to filter.</param>
		/// <returns>The matching items.</returns>
		public static List<T> Apply<T>(FilterElement<T> filter, IEnumerable<T> items)
		{
			if (items is null)
			{
				throw new ArgumentNullException(nameof(items));
			}

			if (filter is null)
			{
				return items.ToList();
			}

			return items.Where(item => Evaluate(filter, item)).ToList();
		}

		private static bool Evaluate<T>(FilterElement<T> filter, T item)
		{
			switch (filter)
			{
				case ANDFilterElement<T> and:
					return and.subFilters.All(sub => Evaluate(sub, item));

				case ORFilterElement<T> or:
					return or.subFilters.Length == 0 || or.subFilters.Any(sub => Evaluate(sub, item));

				case NOTFilterElement<T> not:
					return !Evaluate(not.original, item);

				case TRUEFilterElement<T> _:
					return true;

				case FALSEFilterElement<T> _:
					return false;

				case ManagedFilterIdentifier leaf when TryEvaluateLeaf(leaf, item, out var result):
					return result;

				default:
					return filter.getLambda()(item);
			}
		}

		private static bool TryEvaluateLeaf(ManagedFilterIdentifier leaf, object item, out bool result)
		{
			result = false;

			var comparer = leaf.getComparer();

			switch (comparer)
			{
				case Comparer.Equals:
				case Comparer.NotEquals:
				case Comparer.GT:
				case Comparer.GTE:
				case Comparer.LT:
				case Comparer.LTE:
					break;

				default:
					// Contains/NotContains/Regex semantics are left to the SLNet implementation.
					return false;
			}

			var exposer = leaf.getFieldName();
			var expected = leaf.getValue();

			object actual;

			try
			{
				actual = exposer.execute(item);
			}
			catch
			{
				// Mirror SLNet's fail-safe behavior: a field that cannot be read does not match.
				return false;
			}

			if (actual is IEnumerable list && !(actual is string))
			{
				result = EvaluateList(list, comparer, expected);
				return true;
			}

			result = ScalarMatches(actual, comparer, expected);
			return true;
		}

		private static bool EvaluateList(IEnumerable list, Comparer comparer, object expected)
		{
			if (comparer == Comparer.NotEquals)
			{
				// A list field is "not equal" to a value when none of its elements equal that value.
				foreach (var element in list)
				{
					if (CoercingEquals(element, expected))
					{
						return false;
					}
				}

				return true;
			}

			foreach (var element in list)
			{
				if (ScalarMatches(element, comparer, expected))
				{
					return true;
				}
			}

			return false;
		}

		private static bool ScalarMatches(object actual, Comparer comparer, object expected)
		{
			switch (comparer)
			{
				case Comparer.Equals:
					return CoercingEquals(actual, expected);

				case Comparer.NotEquals:
					return !CoercingEquals(actual, expected);

				case Comparer.GT:
					return TryCompare(actual, expected, out var gt) && gt > 0;

				case Comparer.GTE:
					return TryCompare(actual, expected, out var gte) && gte >= 0;

				case Comparer.LT:
					return TryCompare(actual, expected, out var lt) && lt < 0;

				case Comparer.LTE:
					return TryCompare(actual, expected, out var lte) && lte <= 0;

				default:
					return false;
			}
		}

		private static bool CoercingEquals(object a, object b)
		{
			if (Object.Equals(a, b))
			{
				return true;
			}

			if (a is null || b is null)
			{
				return false;
			}

			if (IsNumeric(a) && IsNumeric(b))
			{
				if (IsSpecialFloat(a) || IsSpecialFloat(b))
				{
					return false;
				}

				return ToDecimal(a) == ToDecimal(b);
			}

			if (a is string sa && b is string sb)
			{
				return String.Equals(sa, sb, StringComparison.OrdinalIgnoreCase);
			}

			// Fall back to an invariant string comparison, mirroring how a database coerces values
			// (e.g. a stored Guid versus a Guid serialized to a string).
			return String.Equals(
				Convert.ToString(a, CultureInfo.InvariantCulture),
				Convert.ToString(b, CultureInfo.InvariantCulture),
				StringComparison.OrdinalIgnoreCase);
		}

		private static bool TryCompare(object a, object b, out int comparison)
		{
			comparison = 0;

			if (a is null || b is null)
			{
				return false;
			}

			if (IsNumeric(a) && IsNumeric(b))
			{
				if (IsSpecialFloat(a) || IsSpecialFloat(b))
				{
					return false;
				}

				comparison = ToDecimal(a).CompareTo(ToDecimal(b));
				return true;
			}

			if (a.GetType() == b.GetType() && a is IComparable sameType)
			{
				comparison = sameType.CompareTo(b);
				return true;
			}

			try
			{
				if (a is IComparable comparable)
				{
					var converted = Convert.ChangeType(b, a.GetType(), CultureInfo.InvariantCulture);
					comparison = comparable.CompareTo(converted);
					return true;
				}
			}
			catch (Exception ex) when (ex is InvalidCastException || ex is FormatException || ex is OverflowException)
			{
				// Values are not comparable after coercion.
			}

			return false;
		}

		private static bool IsNumeric(object value)
		{
			return value != null && NumericTypeCodes.Contains(Convert.GetTypeCode(value));
		}

		private static bool IsSpecialFloat(object value)
		{
			return (value is double d && (double.IsNaN(d) || double.IsInfinity(d)))
				|| (value is float f && (float.IsNaN(f) || float.IsInfinity(f)));
		}

		private static decimal ToDecimal(object value)
		{
			return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		}
	}
}
