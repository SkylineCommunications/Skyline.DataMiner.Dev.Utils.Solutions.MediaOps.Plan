namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

	internal class CapacityFilterTranslator : ParameterFilterTranslator<Capacity>
	{
		private readonly Dictionary<string, Func<Comparer, object, FilterElement<Net.Profiles.Parameter>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<Net.Profiles.Parameter>>>
		{
			[CapacityExposers.Id.fieldName] = HandleGuid,
			[CapacityExposers.Name.fieldName] = HandleName,
			[CapacityExposers.IsMandatory.fieldName] = HandleIsMandatory,
			[CapacityExposers.Units.fieldName] = (comparer, value) => FilterElementFactory.Create(ParameterExposers.Units, comparer, (string)value),
			[CapacityExposers.RangeMin.fieldName] = (comparer, value) => FilterElementFactory.Create(ParameterExposers.RangeMin, comparer, Convert.ToDouble(value)).AND(ParameterExposers.RangeMin.NotEqual(double.NaN)),
			[CapacityExposers.RangeMax.fieldName] = (comparer, value) => FilterElementFactory.Create(ParameterExposers.RangeMax, comparer, Convert.ToDouble(value)).AND(ParameterExposers.RangeMax.NotEqual(double.NaN)),
			[CapacityExposers.StepSize.fieldName] = (comparer, value) => FilterElementFactory.Create(ParameterExposers.Stepsize, comparer, Convert.ToDouble(value)).AND(ParameterExposers.Stepsize.NotEqual(double.NaN)),
			[CapacityExposers.Decimals.fieldName] = (comparer, value) => FilterElementFactory.Create(ParameterExposers.Decimals, comparer, Convert.ToInt32(value)).AND(ParameterExposers.Decimals.NotEqual(int.MaxValue)),
			[CapacityExposers.HasUnits.fieldName] = (comparer, value) => HandleHasValue(ParameterExposers.Units, comparer, (bool)value, string.Empty),
			[CapacityExposers.HasRangeMin.fieldName] = (comparer, value) => HandleHasValue(ParameterExposers.RangeMin, comparer, (bool)value, double.NaN),
			[CapacityExposers.HasRangeMax.fieldName] = (comparer, value) => HandleHasValue(ParameterExposers.RangeMax, comparer, (bool)value, double.NaN),
			[CapacityExposers.HasStepSize.fieldName] = (comparer, value) => HandleHasValue(ParameterExposers.Stepsize, comparer, (bool)value, double.NaN),
			[CapacityExposers.HasDecimals.fieldName] = (comparer, value) => HandleHasValue(ParameterExposers.Decimals, comparer, (bool)value, int.MaxValue),
		};

		protected override Dictionary<string, Func<Comparer, object, FilterElement<Net.Profiles.Parameter>>> Handlers => handlers;

		protected override FilterElement<Net.Profiles.Parameter> ParameterTypeFilter => ProfileProvider.AllCapacitiesFilter;

		private static FilterElement<Net.Profiles.Parameter> HandleHasValue<T>(Exposer<Net.Profiles.Parameter, T> exposer, Comparer comparer, bool value, T defaultValue)
		{
			if (comparer == Comparer.Equals)
			{
				if (value)
				{
					return FilterElementFactory.Create(exposer, Comparer.NotEquals, defaultValue);
				}
				else
				{
					return FilterElementFactory.Create(exposer, Comparer.Equals, defaultValue);
				}
			}
			else if (comparer == Comparer.NotEquals)
			{
				if (value)
				{
					return FilterElementFactory.Create(exposer, Comparer.Equals, defaultValue);
				}
				else
				{
					return FilterElementFactory.Create(exposer, Comparer.NotEquals, defaultValue);
				}
			}
			else
			{
				throw new NotSupportedException($"Comparer {comparer} is not supported for boolean HasValue checks");
			}
		}
	}
}
