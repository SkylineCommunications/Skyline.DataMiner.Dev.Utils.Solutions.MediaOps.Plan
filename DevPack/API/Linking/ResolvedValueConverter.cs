namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

	using CoreParameter = Net.Profiles.Parameter;

	/// <summary>
	/// Converts a <see cref="ResolvedValue"/> into the value the parameter that is going to hold it can take.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A dropdown (discrete) parameter can only hold one of its own options, so a value that comes from somewhere else
	/// has to be mapped onto one first. The mapping is done on the display value: the option whose display name is
	/// exactly (case sensitive) the <see cref="ResolvedValue.DisplayValue"/> of the incoming value is selected, and the
	/// raw value of that option is what the dropdown ends up holding and what is passed on for actual usage (for
	/// example to an orchestration script). When no option matches, the dropdown has no value.
	/// </para>
	/// <para>
	/// Every other parameter keeps the incoming value as-is; a text parameter converts it to a string when it is used.
	/// </para>
	/// </remarks>
	public static class ResolvedValueConverter
	{
		private const string NotResolvedReason = "could not be resolved to a value.";

		/// <summary>
		/// Converts the specified value into the value the given target parameter can take.
		/// </summary>
		/// <param name="value">The value that was resolved from the reference.</param>
		/// <param name="target">The parameter that is going to hold the value, or <see langword="null"/> when it is unknown.</param>
		/// <param name="converted">When this method returns, contains the value as the target holds it.</param>
		/// <returns><see langword="true"/> when the target can hold the value; otherwise, <see langword="false"/>.</returns>
		public static bool TryConvert(ResolvedValue value, Parameter target, out ResolvedValue converted)
		{
			converted = value;

			if (value == null || !value.IsResolved)
			{
				return false;
			}

			switch (target)
			{
				case Capability capability:
					// Capability options have no separate display name, so the option is its own display value.
					return TryMatchDiscrete(value, capability.Discretes, x => x, x => x, x => new StringResolvedValue(x, x), out converted);

				case DiscreteTextConfiguration discreteText:
					return TryMatchDiscrete(value, discreteText.Discretes, x => x.DisplayName, x => x.Value, x => new StringResolvedValue(x.Value, x.DisplayName), out converted);

				case DiscreteNumberConfiguration discreteNumber:
					return TryMatchDiscrete(value, discreteNumber.Discretes, x => x.DisplayName, x => Convert.ToString(x.Value, CultureInfo.InvariantCulture), x => new DecimalResolvedValue(x.Value, x.DisplayName), out converted);

				case NumberCapacity numberCapacity:
					return TryMatchNumber(value, numberCapacity.RangeMin, numberCapacity.RangeMax, out converted);

				case NumberConfiguration numberConfiguration:
					return TryMatchNumber(value, numberConfiguration.RangeMin, numberConfiguration.RangeMax, out converted);

				default:
					// A range needs a minimum and a maximum, so a single value is left untouched for it.
					return true;
			}
		}

		/// <summary>
		/// Converts the specified value into the value the given target profile parameter can take.
		/// </summary>
		/// <param name="value">The value that was resolved from the reference.</param>
		/// <param name="target">The profile parameter that is going to hold the value, or <see langword="null"/> when it is unknown.</param>
		/// <param name="converted">When this method returns, contains the value as the target holds it.</param>
		/// <returns><see langword="true"/> when the target can hold the value; otherwise, <see langword="false"/>.</returns>
		public static bool TryConvert(ResolvedValue value, CoreParameter target, out ResolvedValue converted)
		{
			converted = value;

			if (value == null || !value.IsResolved)
			{
				return false;
			}

			if (target == null)
			{
				return true;
			}

			if (target.Type == CoreParameter.ParameterType.Number)
			{
				return TryMatchNumber(value, ToRangeBound(target.RangeMin, Double.MinValue), ToRangeBound(target.RangeMax, Double.MaxValue), out converted);
			}

			if (target.Type != CoreParameter.ParameterType.Discrete || target.Discretes == null)
			{
				return true;
			}

			var isNumber = target.IsNumberDiscrete();
			var options = BuildProfileParameterOptions(target);

			return TryMatchDiscrete(
				value,
				options,
				x => x.DisplayName,
				x => x.Value,
				x => isNumber && Double.TryParse(x.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
					? (ResolvedValue)new DoubleResolvedValue(number, x.DisplayName)
					: new StringResolvedValue(x.Value, x.DisplayName),
				out converted);
		}

		/// <summary>
		/// Converts the specified value into the value the given dynamic input field of an orchestration script can take.
		/// A discrete field is matched on the display text of its options first and on their value next.
		/// </summary>
		/// <param name="value">The value that was resolved from the reference.</param>
		/// <param name="target">The field that is going to hold the value.</param>
		/// <param name="converted">When this method returns <see langword="true"/>, contains the value as the field holds it.</param>
		/// <returns><see langword="true"/> when the field can hold the value; otherwise, <see langword="false"/>.</returns>
		public static bool TryConvert(ResolvedValue value, OrchestrationInputField target, out OrchestrationInputValue converted)
		{
			converted = null;

			if (value == null || !value.IsResolved || target == null || value.IsNullResolvedValue(out _))
			{
				return false;
			}

			switch (target)
			{
				case OrchestrationDiscreteInputField discrete:
					{
						var displayValue = value.DisplayValue;
						var match = discrete.Options.FirstOrDefault(x => String.Equals(x.Display, displayValue, StringComparison.Ordinal))
							?? discrete.Options.FirstOrDefault(x => String.Equals(x.Value?.ToString(), displayValue, StringComparison.Ordinal));

						converted = match?.Value;
						return converted != null;
					}

				case OrchestrationNumberInputField number:
					{
						if (!TryGetNumber(value, out var decimalValue))
						{
							return false;
						}

						var numberValue = (double)decimalValue;
						if ((number.Minimum.HasValue && numberValue < number.Minimum.Value) || (number.Maximum.HasValue && numberValue > number.Maximum.Value))
						{
							return false;
						}

						converted = OrchestrationInputValue.FromNumber(numberValue);
						return true;
					}

				case OrchestrationDateTimeInputField dateTime:
					{
						var text = Convert.ToString(value.GetRawValue(), CultureInfo.InvariantCulture);
						if (String.IsNullOrEmpty(text) || !OrchestrationInputValue.FromText(text).TryGetDateTime(out var parsed))
						{
							return false;
						}

						converted = OrchestrationInputValue.FromDateTime(parsed);
						return dateTime.IsValidValue(converted, out _);
					}

				case OrchestrationTimeSpanInputField timeSpan:
					{
						var candidate = TryGetNumber(value, out var seconds)
							? OrchestrationInputValue.FromNumber((double)seconds)
							: OrchestrationInputValue.FromText(Convert.ToString(value.GetRawValue(), CultureInfo.InvariantCulture) ?? String.Empty);

						if (!candidate.TryGetTimeSpan(out var parsed))
						{
							return false;
						}

						converted = OrchestrationInputValue.FromTimeSpan(parsed);
						return timeSpan.IsValidValue(converted, out _);
					}

				default:
					converted = OrchestrationInputValue.FromText(Convert.ToString(value.GetRawValue(), CultureInfo.InvariantCulture) ?? String.Empty);
					return true;
			}
		}

		/// <summary>
		/// Returns why the specified value cannot be used for the given target parameter, or <see langword="null"/>
		/// when it can. The text completes the sentence "Reference 'X' ...".
		/// </summary>
		/// <param name="value">The value that was resolved from the reference.</param>
		/// <param name="target">The parameter that is going to hold the value, or <see langword="null"/> when it takes any value.</param>
		internal static string GetFailureReason(ResolvedValue value, Parameter target)
		{
			if (value == null || !value.IsResolved)
			{
				return NotResolvedReason;
			}

			if (TryConvert(value, target, out _))
			{
				return null;
			}

			return TryGetNumberRange(target, out var rangeMin, out var rangeMax)
				? DescribeNumberFailure(value, target?.Name, rangeMin, rangeMax)
				: DescribeOptionFailure(value, target?.Name);
		}

		private static string DescribeNumberFailure(ResolvedValue value, string parameterName, decimal? rangeMin, decimal? rangeMax)
		{
			var parameter = DescribeParameter(parameterName);
			var displayValue = value.DisplayValue;

			if (!TryGetNumber(value, out _))
			{
				return String.IsNullOrEmpty(displayValue)
					? $"does not resolve to a number for {parameter}."
					: $"resolves to '{displayValue}', which is not a number for {parameter}.";
			}

			return $"resolves to '{displayValue}', which is outside the range of {parameter} ({FormatRange(rangeMin, rangeMax)}).";
		}

		private static string DescribeOptionFailure(ResolvedValue value, string parameterName)
		{
			var parameter = DescribeParameter(parameterName);

			return String.IsNullOrEmpty(value.DisplayValue)
				? $"does not resolve to one of the options of {parameter}."
				: $"resolves to '{value.DisplayValue}', which is not one of the options of {parameter}.";
		}

		private static string DescribeParameter(string parameterName)
		{
			return String.IsNullOrEmpty(parameterName) ? "the parameter" : $"'{parameterName}'";
		}

		private static bool TryGetNumberRange(Parameter target, out decimal? rangeMin, out decimal? rangeMax)
		{
			switch (target)
			{
				case NumberCapacity numberCapacity:
					rangeMin = numberCapacity.RangeMin;
					rangeMax = numberCapacity.RangeMax;
					return true;

				case NumberConfiguration numberConfiguration:
					rangeMin = numberConfiguration.RangeMin;
					rangeMax = numberConfiguration.RangeMax;
					return true;

				default:
					rangeMin = null;
					rangeMax = null;
					return false;
			}
		}

		private static string FormatRange(decimal? rangeMin, decimal? rangeMax)
		{
			if (rangeMin.HasValue && rangeMax.HasValue)
			{
				return $"{rangeMin} to {rangeMax}";
			}

			return rangeMin.HasValue ? $"minimum {rangeMin}" : $"maximum {rangeMax}";
		}

		private static decimal? ToRangeBound(double value, double sentinel)
		{
			return Double.IsNaN(value) || value.Equals(sentinel) ? (decimal?)null : (decimal)value;
		}

		private static bool TryMatchNumber(ResolvedValue value, decimal? rangeMin, decimal? rangeMax, out ResolvedValue converted)
		{
			converted = null;

			if (!TryGetNumber(value, out var number))
			{
				return false;
			}

			if ((rangeMin.HasValue && number < rangeMin.Value) || (rangeMax.HasValue && number > rangeMax.Value))
			{
				return false;
			}

			converted = new DecimalResolvedValue(number);
			return true;
		}

		private static bool TryGetNumber(ResolvedValue value, out decimal number)
		{
			switch (value)
			{
				case DecimalResolvedValue decimalValue:
					number = decimalValue.Value;
					return true;

				case DoubleResolvedValue doubleValue when doubleValue.Value >= (double)Decimal.MinValue && doubleValue.Value <= (double)Decimal.MaxValue:
					number = (decimal)doubleValue.Value;
					return true;

				default:
					// Thousands separators are not accepted, so '1,5' is rejected instead of silently becoming 15.
					return Decimal.TryParse(value.DisplayValue, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
			}
		}

		private static IReadOnlyCollection<TextDiscrete> BuildProfileParameterOptions(CoreParameter target)
		{
			var displayValues = target.DiscreetDisplayValues;
			var hasDisplayValues = displayValues != null && displayValues.Count == target.Discretes.Count;

			return target.Discretes
				.Select((x, i) => new TextDiscrete(x, hasDisplayValues ? displayValues[i] : x))
				.ToList();
		}

		private static bool TryMatchDiscrete<T>(
			ResolvedValue value,
			IReadOnlyCollection<T> options,
			Func<T, string> displayNameSelector,
			Func<T, string> rawValueSelector,
			Func<T, ResolvedValue> resultSelector,
			out ResolvedValue converted)
			where T : class
		{
			converted = value;

			if (options == null || options.Count == 0)
			{
				// Nothing to match against; the value is kept as-is so an incompletely defined parameter does not
				// silently drop a value that used to be applied.
				return true;
			}

			var displayValue = value.DisplayValue;
			if (String.IsNullOrEmpty(displayValue))
			{
				converted = null;
				return false;
			}

			// The display value is what the user sees, so that is what is matched on. An option is also matched on its
			// raw value so a value that was already stored as a raw option value keeps working.
			var match = options.FirstOrDefault(x => String.Equals(displayNameSelector(x), displayValue, StringComparison.Ordinal))
				?? options.FirstOrDefault(x => String.Equals(rawValueSelector(x), displayValue, StringComparison.Ordinal));

			if (match == null)
			{
				converted = null;
				return false;
			}

			converted = resultSelector(match);
			return true;
		}
	}
}
