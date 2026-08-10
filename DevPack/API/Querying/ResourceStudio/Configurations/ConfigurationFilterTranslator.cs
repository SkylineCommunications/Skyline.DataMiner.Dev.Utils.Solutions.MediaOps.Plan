namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

	using SLDataGateway.API.Types.Querying;

	using static Skyline.DataMiner.Net.Profiles.Parameter;

	internal class ConfigurationFilterTranslator : ParameterFilterTranslator<Configuration>
	{
		private readonly Dictionary<string, Func<Comparer, object, FilterElement<Net.Profiles.Parameter>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<Net.Profiles.Parameter>>>
		{
			[ConfigurationExposers.Id.fieldName] = HandleGuid,
			[ConfigurationExposers.Name.fieldName] = HandleName,
			[ConfigurationExposers.IsMandatory.fieldName] = HandleIsMandatory,
			[DiscreteTextConfigurationExposers.Discretes.fieldName] = (comparer, value) => FilterElementFactory.Create(ParameterExposers.Discretes, comparer, (string)value).AND(ParameterExposers.Type.Equal((int)ParameterType.Discrete)),
			[DiscreteNumberConfigurationExposers.Discretes.fieldName] = (comparer, value) => FilterElementFactory.Create(ParameterExposers.Discretes, comparer, Convert.ToString((decimal)value, CultureInfo.InvariantCulture)).AND(ParameterExposers.Type.Equal((int)ParameterType.Discrete)),
		};

		private readonly Dictionary<string, Func<SortOrder, bool, IOrderByElement>> orderByHandlers = new Dictionary<string, Func<SortOrder, bool, IOrderByElement>>
		{
			[ConfigurationExposers.Id.fieldName] = HandleGuid,
			[ConfigurationExposers.Name.fieldName] = HandleName,
			[ConfigurationExposers.IsMandatory.fieldName] = HandleIsMandatory,
		};

		protected override Dictionary<string, Func<Comparer, object, FilterElement<Net.Profiles.Parameter>>> FilterHandlers => handlers;

		protected override Dictionary<string, Func<SortOrder, bool, IOrderByElement>> OrderByHandlers => orderByHandlers;

		protected override FilterElement<Net.Profiles.Parameter> ParameterTypeFilter => ProfileProvider.AllConfigurationsFilter;
	}
}
