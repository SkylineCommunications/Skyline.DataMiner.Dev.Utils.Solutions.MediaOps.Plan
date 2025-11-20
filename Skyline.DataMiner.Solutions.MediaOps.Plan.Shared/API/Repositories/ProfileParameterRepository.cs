namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.Profiles;

    using SLDataGateway.API.Types.Querying;

    internal abstract class ProfileParameterRepository<T> : Repository<T, Net.Profiles.Parameter> where T : Parameter
    {
        protected ProfileParameterRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        protected internal override FilterElement<Net.Profiles.Parameter> CreateFilter(string fieldName, Comparer comparer, object value)
        {
            switch (fieldName)
            {
                case nameof(Parameter.Id):
                    return FilterElementFactory<Net.Profiles.Parameter>.Create(ParameterExposers.ID, comparer, value);
                case nameof(Parameter.Name):
                    return FilterElementFactory<Net.Profiles.Parameter>.Create(ParameterExposers.Name, comparer, value);
                case nameof(Parameter.IsMandatory):
                    return FilterElementFactory<Net.Profiles.Parameter>.Create(ParameterExposers.IsOptional, comparer, !Convert.ToBoolean(value));
                default:
                    throw new NotImplementedException();
            }
        }

        protected internal override FilterElement<Net.Profiles.Parameter> CreateFilter(Type type, Comparer comparer)
        {
            throw new NotImplementedException();
        }

        protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
        {
            switch (fieldName)
            {
                case nameof(Parameter.Id):
                    return OrderByElementFactory.Create(ParameterExposers.ID, sortOrder, naturalSort);
                case nameof(Parameter.Name):
                    return OrderByElementFactory.Create(ParameterExposers.Name, sortOrder, naturalSort);
                case nameof(Parameter.IsMandatory):
                    return OrderByElementFactory.Create(ParameterExposers.IsOptional, sortOrder, naturalSort);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
