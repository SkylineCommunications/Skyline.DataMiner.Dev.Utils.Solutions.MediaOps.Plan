namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="Property"/> objects.
	/// </summary>
	public class PropertyExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="ApiObject.Id"/> property.
		/// </summary>
		public static readonly Exposer<Property, Guid> Id = new Exposer<Property, Guid>((obj) => obj.Id, "Id");

		/// <summary>
		/// Gets an exposer for the <see cref="Property.Name"/> property.
		/// </summary>
		public static readonly Exposer<Property, string> Name = new Exposer<Property, string>((obj) => obj.Name, "Name");

		/// <summary>
		/// Gets an exposer for the <see cref="Property.Scope"/> property.
		/// </summary>
		public static readonly Exposer<Property, string> Scope = new Exposer<Property, string>((obj) => obj.Scope, "Scope");

		/// <summary>
		/// Gets an exposer for the <see cref="Property.SectionName"/> property.
		/// </summary>
		public static readonly Exposer<Property, string> SectionName = new Exposer<Property, string>((obj) => obj.SectionName, "SectionName");

		/// <summary>
		/// Gets an exposer for the <see cref="Property.Order"/> property.
		/// </summary>
		public static readonly Exposer<Property, int> Order = new Exposer<Property, int>((obj) => obj.Order, "Order");
	}
}
