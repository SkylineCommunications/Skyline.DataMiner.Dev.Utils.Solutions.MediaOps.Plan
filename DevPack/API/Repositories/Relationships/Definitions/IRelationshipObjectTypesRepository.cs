namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using Skyline.DataMiner.SDM;

	/// <summary>
	/// Defines methods for managing <see cref="RelationshipObjectType"/> objects.
	/// </summary>
	/// <remarks>
	/// Partial reads (<see cref="IPartialReadableRepository{T}.ReadPartial(IPartialQuery{T})"/>) support every
	/// field of <see cref="RelationshipObjectTypeExposers"/> and return the same values as a full read. Every field holds a single
	/// value, read with <see cref="PartialObject{T}.GetValue{TField}(Net.Messages.SLDataGateway.Exposer{T, TField})"/>, and
	/// no data stored outside the object type is read. Values are looked up by exposer instance, so read them with the
	/// exposers that were included in the query. The identifier is only returned when <see cref="RelationshipObjectTypeExposers.Id"/> is included.
	/// <see cref="IPartialPageableRepository{T}.ReadPartialPaged(IPartialQuery{T})"/> returns the same objects in pages of 200.
	/// </remarks>
	public interface IRelationshipObjectTypesRepository : IRepository<RelationshipObjectType>, IPartialPageableRepository<RelationshipObjectType>
	{
	}
}
