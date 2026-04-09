namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core
{
	using System;

	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using Parameter = Net.Profiles.Parameter;

	/// <summary>
	/// Provides extension methods for the <see cref="Parameter"/> class.
	/// </summary>
	internal static class ParameterExtensions
	{
		/// <summary>
		/// Determines whether the parameter has a default string value.
		/// </summary>
		/// <param name="parameter">The parameter to check.</param>
		/// <returns><c>true</c> if the parameter has a default string value; otherwise, <c>false</c>.</returns>
		public static bool HasDefaultStringValue(this Parameter parameter)
		{
			if (parameter.DefaultValue == null) return false;
			if (parameter.DefaultValue.Type != ParameterValue.ValueType.String) return false;
			if (parameter.DefaultValue.StringValue == null) return false;
			return true;
		}

		/// <summary>
		/// Determines whether the parameter has a default numeric value.
		/// </summary>
		/// <param name="parameter">The parameter to check.</param>
		/// <returns><c>true</c> if the parameter has a default numeric value; otherwise, <c>false</c>.</returns>
		public static bool HasDefaultNumericValue(this Parameter parameter)
		{
			if (parameter.DefaultValue == null) return false;
			if (parameter.DefaultValue.Type != ParameterValue.ValueType.Double) return false;
			if (Double.IsNaN(parameter.DefaultValue.DoubleValue)) return false;
			return true;
		}

		/// <summary>
		/// Determines whether the parameter has a minimum range value.
		/// </summary>
		/// <param name="parameter">The parameter to check.</param>
		/// <returns><c>true</c> if the parameter has a minimum range value; otherwise, <c>false</c>.</returns>
		public static bool HasMinRange(this Parameter parameter)
		{
			return !Double.IsNaN(parameter.RangeMin);
		}

		/// <summary>
		/// Determines whether the parameter has a maximum range value.
		/// </summary>
		/// <param name="parameter">The parameter to check.</param>
		/// <returns><c>true</c> if the parameter has a maximum range value; otherwise, <c>false</c>.</returns>
		public static bool HasMaxRange(this Parameter parameter)
		{
			return !Double.IsNaN(parameter.RangeMax);
		}

		/// <summary>
		/// Determines whether the parameter has a step size value.
		/// </summary>
		/// <param name="parameter">The parameter to check.</param>
		/// <returns><c>true</c> if the parameter has a step size value; otherwise, <c>false</c>.</returns>
		public static bool HasStepSize(this Parameter parameter)
		{
			return !Double.IsNaN(parameter.Stepsize);
		}

		/// <summary>
		/// Determines whether the parameter has a decimals value set.
		/// </summary>
		/// <param name="parameter">The parameter to check.</param>
		/// <returns><c>true</c> if the parameter has a decimals value set; otherwise, <c>false</c>.</returns>
		public static bool HasDecimals(this Parameter parameter)
		{
			return parameter.Decimals != int.MaxValue;
		}

		/// <summary>
		/// Determines whether the specified parameter is a Number parameter.
		/// </summary>
		/// <param name="parameter">The parameter to evaluate. Must not be <c>null</c>.</param>
		/// <returns><see langword="true"/> if the parameter is of type <see cref="Parameter.ParameterType.Number"/>; otherwise,
		/// <see langword="false"/>.</returns>
		public static bool IsNumber(this Parameter parameter)
		{
			return parameter.Type == Parameter.ParameterType.Number;
		}

		/// <summary>
		/// Determines whether the specified parameter is a Range parameter.
		/// </summary>
		/// <param name="parameter">The parameter to evaluate. Must not be <c>null</c>.</param>
		/// <returns><see langword="true"/> if the parameter is of type <see cref="Parameter.ParameterType.Range"/>; otherwise,
		/// <see langword="false"/>.</returns>
		public static bool IsRange(this Parameter parameter)
		{
			return parameter.Type == Parameter.ParameterType.Range;
		}

		/// <summary>
		/// Determines whether the parameter is a discrete number parameter.
		/// </summary>
		/// <param name="parameter">The parameter to check.</param>
		/// <returns><c>true</c> if the parameter is a discrete number parameter; otherwise, <c>false</c>.</returns>
		public static bool IsNumberDiscreet(this Parameter parameter)
		{
			if (parameter.Type != Parameter.ParameterType.Discrete) return false;
			if (parameter.InterpreteType?.RawType != InterpreteType.RawTypeEnum.NumericText) return false;
			if (parameter.InterpreteType?.Type != InterpreteType.TypeEnum.Double) return false;
			return true;
		}

		/// <summary>
		/// Determines whether the parameter is a discrete text parameter.
		/// </summary>
		/// <param name="parameter">The parameter to check.</param>
		/// <returns><c>true</c> if the parameter is a discrete text parameter; otherwise, <c>false</c>.</returns>
		public static bool IsTextDiscreet(this Parameter parameter)
		{
			if (parameter.Type != Parameter.ParameterType.Discrete) return false;
			if (parameter.InterpreteType?.RawType != InterpreteType.RawTypeEnum.Other) return false;
			if (parameter.InterpreteType?.Type != InterpreteType.TypeEnum.String) return false;
			return true;
		}

		/// <summary>
		/// Determines whether the parameter is a text parameter.
		/// </summary>
		/// <param name="parameter">The parameter to check.</param>
		/// <returns><c>true</c> if the parameter is a text parameter; otherwise, <c>false</c>.</returns>
		public static bool IsText(this Parameter parameter)
		{
			if (parameter.Type != Parameter.ParameterType.Text) return false;
			if (parameter.InterpreteType?.RawType != InterpreteType.RawTypeEnum.Undefined) return false;
			if (parameter.InterpreteType?.Type != InterpreteType.TypeEnum.Undefined) return false;
			return true;
		}

		/// <summary>
		/// Determines whether the parameter is a number parameter or a discrete number parameter.
		/// </summary>
		/// <param name="parameter">The parameter to check.</param>
		/// <returns><c>true</c> if the parameter is a number parameter or a discrete number parameter; otherwise, <c>false</c>.</returns>
		public static bool IsNumberParameter(this Parameter parameter)
		{
			return parameter.IsNumber() || parameter.IsNumberDiscreet();
		}

		/// <summary>
		/// Determines whether the parameter is a text parameter or a discrete text parameter.
		/// </summary>
		/// <param name="parameter">The parameter to check.</param>
		/// <returns><c>true</c> if the parameter is a text parameter or a discrete text parameter; otherwise, <c>false</c>.</returns>
		public static bool IsTextParameter(this Parameter parameter)
		{
			return parameter.IsText() || parameter.IsTextDiscreet();
		}

		/// <summary>
		/// Determines whether the parameter is a capability.
		/// </summary>
		/// <param name="parameter">The parameter to check.</param>
		/// <returns><c>true</c> if the parameter is a capability; otherwise, <c>false</c>.</returns>
		public static bool IsCapability(this Parameter parameter)
		{
			return parameter.Categories == ProfileParameterCategory.Capability;
		}

		/// <summary>
		/// Determines whether the parameter is a capacity.
		/// </summary>
		/// <param name="parameter">The parameter to check.</param>
		/// <returns><c>true</c> if the parameter is a capacity; otherwise, <c>false</c>.</returns>
		public static bool IsCapacity(this Parameter parameter)
		{
			return parameter.Categories == ProfileParameterCategory.Capacity;
		}

		/// <summary>
		/// Determines whether the parameter is a configuration.
		/// </summary>
		/// <param name="parameter">The parameter to check.</param>
		/// <returns><c>true</c> if the parameter is a configuration; otherwise, <c>false</c>.</returns>
		public static bool IsConfiguration(this Parameter parameter)
		{
			return parameter.Categories == ProfileParameterCategory.Configuration;
		}

		/// <summary>
		/// Determines whether the parameter is time-dependent.
		/// </summary>
		/// <param name="parameter">The parameter to check.</param>
		/// <returns><c>true</c> if the parameter is time-dependent; otherwise, <c>false</c>.</returns>
		public static bool IsTimeDependent(this Parameter parameter)
		{
			return IsTimeDependent(parameter, out _);
		}

		public static bool IsTimeDependent(this Parameter parameter, out TimeDependentCapabilityLink timeDependentCapabilityLink)
		{
			if (TimeDependentCapabilityLink.TryDeserialize(parameter.Remarks, out timeDependentCapabilityLink))
			{
				return timeDependentCapabilityLink.IsTimeDependent;
			}

			timeDependentCapabilityLink = null;
			return false;
		}

		/// <summary>
		/// Determines whether the parameter has a linked time-dependent parameter.
		/// </summary>
		/// <param name="parameter">The parameter to check.</param>
		/// <returns><c>true</c> if the parameter has a linked time-dependent parameter; otherwise, <c>false</c>.</returns>
		public static bool HasLinkedTimeDependentParameter(this Parameter parameter)
		{
			if (!TimeDependentCapabilityLink.TryDeserialize(parameter.Remarks, out TimeDependentCapabilityLink timeDependentCapabilityLink))
			{
				return false;
			}

			if (!timeDependentCapabilityLink.IsTimeDependent)
			{
				return false;
			}

			if (Guid.Empty.Equals(timeDependentCapabilityLink.LinkedParameterId))
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Gets the linked time-dependent parameter ID.
		/// </summary>
		/// <param name="parameter">The parameter to check.</param>
		/// <returns>The linked time-dependent parameter ID if available; otherwise, <see cref="Guid.Empty"/>.</returns>
		public static Guid LinkedTimeDependentParameterId(this Parameter parameter)
		{
			if (!TimeDependentCapabilityLink.TryDeserialize(parameter.Remarks, out TimeDependentCapabilityLink timeDependentCapabilityLink))
			{
				return Guid.Empty;
			}

			if (!timeDependentCapabilityLink.IsTimeDependent)
			{
				return Guid.Empty;
			}

			return timeDependentCapabilityLink.LinkedParameterId;
		}

		/// <summary>
		/// Gets the display name for the parameter, including a time-dependent indicator if applicable.
		/// </summary>
		/// <param name="parameter">The parameter to get the display name for.</param>
		/// <returns>The display name for the parameter.</returns>
		public static string GetDisplayName(this Parameter parameter)
		{
			if (parameter.IsTimeDependent())
			{
				return $"🕘 {parameter.Name}";
			}

			return parameter.Name;
		}
	}
}
