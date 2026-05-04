namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="PropertyValueCollection"/> objects.
	/// </summary>
	public class PropertyValueCollectionExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="ApiObject.Id"/> property.
		/// </summary>
		public static readonly Exposer<PropertyValueCollection, Guid> Id = new Exposer<PropertyValueCollection, Guid>((obj) => obj.Id, "Id");

		/// <summary>
		/// Gets an exposer for the <see cref="PropertyValueCollection.LinkedObjectId"/> property.
		/// </summary>
		public static readonly Exposer<PropertyValueCollection, string> LinkedObjectId = new Exposer<PropertyValueCollection, string>((obj) => obj.LinkedObjectId, "LinkedObjectId");

		/// <summary>
		/// Gets an exposer for the <see cref="PropertyValueCollection.Scope"/> property.
		/// </summary>
		public static readonly Exposer<PropertyValueCollection, string> Scope = new Exposer<PropertyValueCollection, string>((obj) => obj.Scope, "Scope");

		/// <summary>
		/// Gets an exposer for the <see cref="PropertyValueCollection.SubId"/> property.
		/// </summary>
		public static readonly Exposer<PropertyValueCollection, string> SubId = new Exposer<PropertyValueCollection, string>((obj) => obj.SubId, "SubId");
	}
}
