namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

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

				default:
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

			if (target == null || target.Type != CoreParameter.ParameterType.Discrete || target.Discretes == null)
			{
				return true;
			}

			var isNumber = target.IsNumberDiscreet();
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
		/// Returns why the specified value cannot be used for the given target parameter, or <see langword="null"/>
		/// when it can. The text completes the sentence "Reference 'X' ...".
		/// </summary>
		/// <param name="value">The value that was resolved from the reference.</param>
		/// <param name="target">The parameter that is going to hold the value, or <see langword="null"/> when it takes any value.</param>
		internal static string GetFailureReason(ResolvedValue value, Parameter target)
		{
			if (value == null || !value.IsResolved)
			{
				return "could not be resolved to a value.";
			}

			if (TryConvert(value, target, out _))
			{
				return null;
			}

			var parameter = String.IsNullOrEmpty(target?.Name) ? "the parameter" : $"'{target.Name}'";

			return String.IsNullOrEmpty(value.DisplayValue)
				? $"does not resolve to one of the options of {parameter}."
				: $"resolves to '{value.DisplayValue}', which is not one of the options of {parameter}.";
		}

		private static IReadOnlyCollection<TextDiscreet> BuildProfileParameterOptions(CoreParameter target)
		{
			var displayValues = target.DiscreetDisplayValues;
			var hasDisplayValues = displayValues != null && displayValues.Count == target.Discretes.Count;

			return target.Discretes
				.Select((x, i) => new TextDiscreet(x, hasDisplayValues ? displayValues[i] : x))
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
