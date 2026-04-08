namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Provides extension methods for creating enum filters on exposers and collection exposers.
	/// </summary>
	public static class EnumFilterExtensions
	{
		/// <summary>
		/// Creates a filter that checks if the exposed field equals the specified value.
		/// </summary>
		/// <typeparam name="TFilter">The type of the filter.</typeparam>
		/// <typeparam name="TField">The type of the field being compared. Must be an Enum.</typeparam>
		/// <param name="exposer">The exposer that identifies the field to filter on.</param>
		/// <param name="value">The value to compare against.</param>
		/// <returns>A <see cref="ManagedFilter{TFilter, TField}"/> configured for equality comparison.</returns>
		public static ManagedFilter<TFilter, TField> Equal<TFilter, TField>(this Exposer<TFilter, TField> exposer, TField value)
			where TField : Enum
		{
			return exposer.UncheckedEqual(value);
		}

		/// <summary>
		/// Creates a filter that checks if the exposed field does not equal the specified value.
		/// </summary>
		/// <typeparam name="TFilter">The type of the filter.</typeparam>
		/// <typeparam name="TField">The type of the field being compared. Must be an Enum.</typeparam>
		/// <param name="exposer">The exposer that identifies the field to filter on.</param>
		/// <param name="value">The value to compare against.</param>
		/// <returns>A <see cref="ManagedFilter{TFilter, TField}"/> configured for inequality comparison.</returns>
		public static ManagedFilter<TFilter, TField> NotEqual<TFilter, TField>(this Exposer<TFilter, TField> exposer, TField value)
			where TField : Enum
		{
			return exposer.UncheckedNotEqual(value);
		}

		/// <summary>
		/// Creates a filter that checks if the exposed field is less than the specified value.
		/// </summary>
		/// <typeparam name="TFilter">The type of the filter.</typeparam>
		/// <typeparam name="TField">The type of the field being compared. Must be an Enum.</typeparam>
		/// <param name="exposer">The exposer that identifies the field to filter on.</param>
		/// <param name="value">The value to compare against.</param>
		/// <returns>A <see cref="ManagedFilter{TFilter, TField}"/> configured for less-than comparison.</returns>
		public static ManagedFilter<TFilter, TField> LessThan<TFilter, TField>(this Exposer<TFilter, TField> exposer, TField value)
			where TField : Enum
		{
			return new ManagedFilter<TFilter, TField>(
				exposer,
				Comparer.GTE,
				value,
				(obj) => Convert.ToInt32(exposer.internalFunc(obj)).CompareTo(Convert.ToInt32(value)) < 0);
		}

		/// <summary>
		/// Creates a filter that checks if the exposed field is less than or equal to the specified value.
		/// </summary>
		/// <typeparam name="TFilter">The type of the filter.</typeparam>
		/// <typeparam name="TField">The type of the field being compared. Must be an Enum.</typeparam>
		/// <param name="exposer">The exposer that identifies the field to filter on.</param>
		/// <param name="value">The value to compare against.</param>
		/// <returns>A <see cref="ManagedFilter{TFilter, TField}"/> configured for less-than-or-equal comparison.</returns>
		public static ManagedFilter<TFilter, TField> LessThanOrEqual<TFilter, TField>(this Exposer<TFilter, TField> exposer, TField value)
			where TField : Enum
		{
			return new ManagedFilter<TFilter, TField>(
				exposer,
				Comparer.GTE,
				value,
				(obj) => Convert.ToInt32(exposer.internalFunc(obj)).CompareTo(Convert.ToInt32(value)) <= 0);
		}

		/// <summary>
		/// Creates a filter that checks if the exposed field is greater than the specified value.
		/// </summary>
		/// <typeparam name="TFilter">The type of the filter.</typeparam>
		/// <typeparam name="TField">The type of the field being compared. Must be an Enum.</typeparam>
		/// <param name="exposer">The exposer that identifies the field to filter on.</param>
		/// <param name="value">The value to compare against.</param>
		/// <returns>A <see cref="ManagedFilter{TFilter, TField}"/> configured for greater-than comparison.</returns>
		public static ManagedFilter<TFilter, TField> GreaterThan<TFilter, TField>(this Exposer<TFilter, TField> exposer, TField value)
			where TField : Enum
		{
			return new ManagedFilter<TFilter, TField>(
				exposer,
				Comparer.GTE,
				value,
				(obj) => Convert.ToInt32(exposer.internalFunc(obj)).CompareTo(Convert.ToInt32(value)) > 0);
		}

		/// <summary>
		/// Creates a filter that checks if the exposed field is greater than or equal to the specified value.
		/// </summary>
		/// <typeparam name="TFilter">The type of the filter.</typeparam>
		/// <typeparam name="TField">The type of the field being compared. Must be an Enum.</typeparam>
		/// <param name="exposer">The exposer that identifies the field to filter on.</param>
		/// <param name="value">The value to compare against.</param>
		/// <returns>A <see cref="ManagedFilter{TFilter, TField}"/> configured for greater-than-or-equal comparison.</returns>
		public static ManagedFilter<TFilter, TField> GreaterThanOrEqual<TFilter, TField>(this Exposer<TFilter, TField> exposer, TField value)
			where TField : Enum
		{
			return new ManagedFilter<TFilter, TField>(
				exposer,
				Comparer.GTE,
				value,
				(obj) => Convert.ToInt32(exposer.internalFunc(obj)).CompareTo(Convert.ToInt32(value)) >= 0);
		}
	}

	/// <summary>
	/// Provides extension methods for creating type filters on exposers.
	/// </summary>
	public static class TypeFilterExtensions
	{
		/// <summary>
		/// Creates a filter that checks if the exposed field equals the specified value.
		/// </summary>
		/// <typeparam name="TFilter">The type of the filter.</typeparam>
		/// <typeparam name="TField">The type of the field being compared. Must be a Type.</typeparam>
		/// <param name="exposer">The exposer that identifies the field to filter on.</param>
		/// <param name="value">The value to compare against.</param>
		/// <returns>A <see cref="ManagedFilter{TFilter, TField}"/> configured for equality comparison.</returns>
		public static ManagedFilter<TFilter, TField> Equal<TFilter, TField>(this Exposer<TFilter, TField> exposer, TField value)
			where TField : Type
		{
			return exposer.UncheckedEqual(value);
		}

		/// <summary>
		/// Creates a filter that checks if the exposed field does not equal the specified value.
		/// </summary>
		/// <typeparam name="TFilter">The type of the filter.</typeparam>
		/// <typeparam name="TField">The type of the field being compared. Must be an Type.</typeparam>
		/// <param name="exposer">The exposer that identifies the field to filter on.</param>
		/// <param name="value">The value to compare against.</param>
		/// <returns>A <see cref="ManagedFilter{TFilter, TField}"/> configured for inequality comparison.</returns>
		public static ManagedFilter<TFilter, TField> NotEqual<TFilter, TField>(this Exposer<TFilter, TField> exposer, TField value)
			where TField : Type
		{
			return exposer.UncheckedNotEqual(value);
		}
	}
}
