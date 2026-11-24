namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using Skyline.DataMiner.SDM;

	/// <summary>
	/// Defines methods for managing <see cref="RelationshipObjectType"/> objects.
	/// </summary>
	/// <remarks>
	/// Partial reads (<see cref="IPartialReadableRepository{T}.ReadPartial(IPartialFilterElement{T})"/>) support every
	/// field of <see cref="RelationshipObjectTypeExposers"/> and return the same values as a full read. Every field holds a single
	/// value, read with <see cref="PartialObject{T}.GetValue{TField}(Net.Messages.SLDataGateway.Exposer{T, TField})"/>, and
	/// no data stored outside the object type is read.
	/// <see cref="IPartialPageableRepository{T}.ReadPartialPaged(IPartialQuery{T})"/> returns the same objects in pages of 200.
	/// </remarks>
	public interface IRelationshipObjectTypesRepository : IRepository<RelationshipObjectType>, IPartialPageableRepository<RelationshipObjectType>
	{
	}
}
