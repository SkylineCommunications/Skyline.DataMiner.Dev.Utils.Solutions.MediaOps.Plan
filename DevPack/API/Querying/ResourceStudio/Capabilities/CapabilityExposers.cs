namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="Capability"/> objects.
	/// </summary>
	public class CapabilityExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="ApiObject.Id"/> property.
		/// </summary>
		public static readonly Exposer<Capability, Guid> Id = new Exposer<Capability, Guid>((obj) => obj.Id, "Id");

		/// <summary>
		/// Gets an exposer for the <see cref="Parameter.Name"/> property.
		/// </summary>
		public static readonly Exposer<Capability, string> Name = new Exposer<Capability, string>((obj) => obj.Name, "Name");

		/// <summary>
		/// Gets an exposer for the <see cref="Parameter.IsMandatory"/> property.
		/// </summary>
		public static readonly Exposer<Capability, bool> IsMandatory = new Exposer<Capability, bool>((obj) => obj.IsMandatory, "IsMandatory");

		/// <summary>
		/// Gets an exposer for the <see cref="Capability.IsTimeDependent"/> property.
		/// </summary>
		public static readonly Exposer<Capability, bool> IsTimeDependent = new Exposer<Capability, bool>((obj) => obj.IsTimeDependent, "IsTimeDependent");

		/// <summary>
		/// Gets a dynamic list exposer for the <see cref="Capability.Discretes"/> property.
		/// </summary>
		public static readonly DynamicListExposer<Capability, string> Discretes = DynamicListExposer<Capability, string>.CreateFromListExposer(new Exposer<Capability, IEnumerable>((obj) => obj.Discretes.Where(x => x != null), "Discretes"));
	}
}
