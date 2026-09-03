namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="RelationshipObjectType"/> objects.
	/// </summary>
	public static class RelationshipObjectTypeExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="ApiObject.Id"/> property.
		/// </summary>
		public static readonly Exposer<RelationshipObjectType, Guid> Id = new Exposer<RelationshipObjectType, Guid>((obj) => obj.Id, "Id");

		/// <summary>
		/// Gets an exposer for the <see cref="RelationshipObjectType.Name"/> property.
		/// </summary>
		public static readonly Exposer<RelationshipObjectType, string> Name = new Exposer<RelationshipObjectType, string>((obj) => obj.Name, "Name");
	}
}
