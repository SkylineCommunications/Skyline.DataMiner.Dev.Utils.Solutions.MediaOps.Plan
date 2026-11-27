namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using Skyline.DataMiner.SDM;

	/// <summary>
	/// Defines a repository for managing <see cref="Capacity"/> entities, providing  basic CRUD operations and
	/// additional functionality as defined by the  <see cref="IRepository{T}"/> interfaces.
	/// </summary>
	/// <remarks>
	/// Partial reads (<see cref="IPartialReadableRepository{T}.ReadPartial(IPartialQuery{T})"/>) support every
	/// field of <see cref="CapacityExposers"/>. The capacity objects are stored as profile parameters, which can't be read partially,
	/// so they are read in full and the selected fields are projected from them. A partial read therefore has no performance
	/// gain for this repository, but keeps the API the same as for the other repositories.
	/// Values are looked up by exposer instance, so read them with the exposers that were included in the query. The identifier
	/// is only returned when <see cref="CapacityExposers.Id"/> is included.
	/// A capacity without a range, step size or decimals returns 0 for <see cref="CapacityExposers.RangeMin"/>,
	/// <see cref="CapacityExposers.RangeMax"/>, <see cref="CapacityExposers.StepSize"/> or <see cref="CapacityExposers.Decimals"/>;
	/// select <see cref="CapacityExposers.HasRangeMin"/> and the other Has fields to know whether it has a value.
	/// <see cref="IPartialPageableRepository{T}.ReadPartialPaged(IPartialQuery{T})"/> returns the same objects in pages of 200.
	/// </remarks>
	public interface ICapacitiesRepository : IRepository<Capacity>, IPartialPageableRepository<Capacity>
	{
	}
}
