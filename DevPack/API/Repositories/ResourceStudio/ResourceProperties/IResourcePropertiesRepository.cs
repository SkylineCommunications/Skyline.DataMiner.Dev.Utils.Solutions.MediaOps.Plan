namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using Skyline.DataMiner.SDM;

	/// <summary>
	/// Defines methods for managing <see cref="ResourceProperty"/> objects.
	/// </summary>
	/// <remarks>
	/// Partial reads (<see cref="IPartialReadableRepository{T}.ReadPartial(IPartialQuery{T})"/>) support every
	/// field of <see cref="ResourcePropertyExposers"/> and return the same values as a full read. Values are looked up by
	/// exposer instance, so read them with the exposers that were included in the query. The identifier is only returned
	/// when <see cref="ResourcePropertyExposers.Id"/> is included.
	/// <see cref="IPartialPageableRepository{T}.ReadPartialPaged(IPartialQuery{T})"/> returns the same objects in pages of 200.
	/// </remarks>
	public interface IResourcePropertiesRepository : IRepository<ResourceProperty>, IPartialPageableRepository<ResourceProperty>
	{
	}
}
