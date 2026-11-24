namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using Skyline.DataMiner.SDM;

	/// <summary>
	/// Defines a repository for managing <see cref="Capability"/> entities, providing  basic CRUD operations and
	/// additional functionality as defined by the  <see cref="IRepository{T}"/> interfaces.
	/// </summary>
	/// <remarks>
	/// Partial reads (<see cref="IPartialReadableRepository{T}.ReadPartial(IPartialFilterElement{T})"/>) support every
	/// field of <see cref="CapabilityExposers"/>. The capability objects are stored as profile parameters, which can't be read partially,
	/// so they are read in full and the selected fields are projected from them. A partial read therefore has no performance
	/// gain for this repository, but keeps the API the same as for the other repositories. List fields are read with
	/// <see cref="PartialObject{T}.GetValues{TField}(Net.Messages.SLDataGateway.DynamicListExposer{T, TField})"/>.
	/// <see cref="IPartialPageableRepository{T}.ReadPartialPaged(IPartialQuery{T})"/> returns the same objects in pages of 200.
	/// </remarks>
	public interface ICapabilitiesRepository : IRepository<Capability>, IPartialPageableRepository<Capability>
	{
		/// <summary>
		/// Gets the system-level capabilities.
		/// </summary>
		public SystemCapabilities SystemCapabilities { get; }
	}
}
