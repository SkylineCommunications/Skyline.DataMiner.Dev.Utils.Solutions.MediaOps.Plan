namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using Skyline.DataMiner.SDM;

	/// <summary>
	/// Defines a repository for managing <see cref="Configuration"/> entities, providing  basic CRUD operations and
	/// additional functionality as defined by the  <see cref="IRepository{T}"/> interfaces.
	/// </summary>
	/// <remarks>
	/// Partial reads (<see cref="IPartialReadableRepository{T}.ReadPartial(IPartialQuery{T})"/>) support every
	/// field of <see cref="ConfigurationExposers"/>, <see cref="DiscreteTextConfigurationExposers"/> and
	/// <see cref="DiscreteNumberConfigurationExposers"/>. The configuration objects are stored as profile parameters, which can't be read partially,
	/// so they are read in full and the selected fields are projected from them. A partial read therefore has no performance
	/// gain for this repository, but keeps the API the same as for the other repositories. The discretes are read with
	/// <see cref="PartialObject{T}.GetValues{TField}(Net.Messages.SLDataGateway.DynamicListExposer{T, TField})"/>.
	/// Values are looked up by exposer instance, so read them with the exposers that were included in the query. The identifier
	/// is only returned when <see cref="ConfigurationExposers.Id"/> is included.
	/// <see cref="IPartialPageableRepository{T}.ReadPartialPaged(IPartialQuery{T})"/> returns the same objects in pages of 200.
	/// </remarks>
	public interface IConfigurationsRepository : IRepository<Configuration>, IPartialPageableRepository<Configuration>
	{
	}
}
