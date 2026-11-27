namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	/// <summary>
	/// Defines methods for managing <see cref="Property"/> objects.
	/// </summary>
	/// <remarks>
	/// Partial reads (<see cref="IPartialReadableRepository{T}.ReadPartial(IPartialQuery{T})"/>) support every
	/// field of <see cref="PropertyExposers"/> and return the same values as a full read for every property type
	/// (<see cref="StringProperty"/>, <see cref="BooleanProperty"/>, <see cref="DiscreteProperty"/> and <see cref="FileProperty"/>).
	/// Every field holds a single value, read with
	/// <see cref="PartialObject{T}.GetValue{TField}(Net.Messages.SLDataGateway.Exposer{T, TField})"/>, and no data stored
	/// outside the property is read. Like a full read, properties without a property type are not returned. Values are looked
	/// up by exposer instance, so read them with the exposers that were included in the query. The identifier is only
	/// returned when <see cref="PropertyExposers.Id"/> is included.
	/// <see cref="IPartialPageableRepository{T}.ReadPartialPaged(IPartialQuery{T})"/> returns the same objects in pages of 200.
	/// </remarks>
	public interface IPropertiesRepository : IRepository<Property>, IPartialPageableRepository<Property>
	{
		/// <summary>
		/// Deletes the specified properties from the repository using the provided <see cref="PropertyDeleteOptions"/>.
		/// </summary>
		/// <param name="properties">The properties to delete.</param>
		/// <param name="options">Options specifying how the properties should be deleted.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="properties"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk deletion operation fails for one or more properties.</exception>
		void Delete(IEnumerable<Property> properties, PropertyDeleteOptions options);

		/// <summary>
		/// Deletes properties with the specified identifiers from the repository using the provided <see cref="PropertyDeleteOptions"/>.
		/// </summary>
		/// <param name="propertyIds">The unique identifiers of the properties to delete.</param>
		/// <param name="options">Options specifying how the properties should be deleted.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyIds"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk deletion operation fails for one or more properties.</exception>
		void Delete(IEnumerable<Guid> propertyIds, PropertyDeleteOptions options);

		/// <summary>
		/// Deletes the specified <see cref="Property"/> using the provided <see cref="PropertyDeleteOptions"/>.
		/// </summary>
		/// <param name="property">The property to delete.</param>
		/// <param name="options">Options specifying how the property should be deleted.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="property"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsException">Thrown when the deletion operation fails for the specified property.</exception>
		void Delete(Property property, PropertyDeleteOptions options);

		/// <summary>
		/// Deletes the specified <see cref="Property"/> from the repository using the provided <see cref="PropertyDeleteOptions"/>.
		/// </summary>
		/// <param name="propertyId">The unique identifier of the property to delete.</param>
		/// <param name="options">Options specifying how the property should be deleted.</param>
		/// <exception cref="MediaOpsException">Thrown when the deletion operation fails for the specified property.</exception>
		void Delete(Guid propertyId, PropertyDeleteOptions options);
	}
}
