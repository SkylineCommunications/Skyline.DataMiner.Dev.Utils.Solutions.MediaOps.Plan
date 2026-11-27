namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using Skyline.DataMiner.SDM;

	/// <summary>
	/// Defines methods for managing <see cref="PropertySettingCollection"/> objects.
	/// </summary>
	/// <remarks>
	/// Partial reads (<see cref="IPartialReadableRepository{T}.ReadPartial(IPartialQuery{T})"/>) support every
	/// field of <see cref="PropertySettingCollectionExposers"/> and return the same values as a full read. Fields that hold a
	/// value per setting (e.g. <see cref="PropertySettingCollectionExposers.CustomPropertySettings.Name"/> or
	/// <see cref="PropertySettingCollectionExposers.PropertySettings.PropertyId"/>) are read with
	/// <see cref="PartialObject{T}.GetValues{TField}(Net.Messages.SLDataGateway.DynamicListExposer{T, TField})"/>.
	/// Selecting fields of the settings linked to a property reads those properties once per batch of collections.
	/// Values are looked up by exposer instance, so read them with the exposers that were included in the query. The identifier
	/// is only returned when <see cref="PropertySettingCollectionExposers.Id"/> is included.
	/// <see cref="IPartialPageableRepository{T}.ReadPartialPaged(IPartialQuery{T})"/> returns the same objects in pages of 200.
	/// </remarks>
	public interface IPropertySettingCollectionsRepository : IRepository<PropertySettingCollection>, IPartialPageableRepository<PropertySettingCollection>
	{
	}
}
