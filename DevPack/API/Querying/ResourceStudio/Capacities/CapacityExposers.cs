namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="Capacity"/> objects.
	/// </summary>
	public static class CapacityExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="ApiObject.Id"/> property.
		/// </summary>
		public static readonly Exposer<Capacity, Guid> Id = new Exposer<Capacity, Guid>((obj) => obj.Id, "Id");

		/// <summary>
		/// Gets an exposer for the <see cref="Parameter.IsMandatory"/> property.
		/// </summary>
		public static readonly Exposer<Capacity, bool> IsMandatory = new Exposer<Capacity, bool>((obj) => obj.IsMandatory, "IsMandatory");

		/// <summary>
		/// Gets an exposer for the <see cref="Parameter.Name"/> property.
		/// </summary>
		public static readonly Exposer<Capacity, string> Name = new Exposer<Capacity, string>((obj) => obj.Name, "Name");

		/// <summary>
		/// Gets an exposer for the <see cref="Capacity.Units"/> property.
		/// </summary>
		public static readonly Exposer<Capacity, string> Units = new Exposer<Capacity, string>((obj) => obj.Units, "Units");

		/// <summary>
		/// Gets an exposer for the <see cref="Capacity.Units"/> property.
		/// </summary>
		public static readonly Exposer<Capacity, bool> HasUnits = new Exposer<Capacity, bool>((obj) => !String.IsNullOrEmpty(obj.Units), "HasUnits");

		/// <summary>
		/// Gets an exposer for the <see cref="Capacity.RangeMin"/> property.
		/// </summary>
		public static readonly Exposer<Capacity, decimal> RangeMin = new Exposer<Capacity, decimal>((obj) => obj.RangeMin.GetValueOrDefault(), "RangeMin");

		/// <summary>
		/// Gets an exposer for the <see cref="Capacity.RangeMin"/> property.
		/// </summary>
		public static readonly Exposer<Capacity, bool> HasRangeMin = new Exposer<Capacity, bool>((obj) => obj.RangeMin.HasValue, "HasRangeMin");

		/// <summary>
		/// Gets an exposer for the <see cref="Capacity.RangeMax"/> property.
		/// </summary>
		public static readonly Exposer<Capacity, decimal> RangeMax = new Exposer<Capacity, decimal>((obj) => obj.RangeMax.GetValueOrDefault(), "RangeMax");

		/// <summary>
		/// Gets an exposer for the <see cref="Capacity.Units"/> property.
		/// </summary>
		public static readonly Exposer<Capacity, bool> HasRangeMax = new Exposer<Capacity, bool>((obj) => obj.RangeMax.HasValue, "HasRangeMax");

		/// <summary>
		/// Gets an exposer for the <see cref="Capacity.StepSize"/> property.
		/// </summary>
		public static readonly Exposer<Capacity, decimal> StepSize = new Exposer<Capacity, decimal>((obj) => obj.StepSize.GetValueOrDefault(), "StepSize");

		/// <summary>
		/// Gets an exposer for the <see cref="Capacity.StepSize"/> property.
		/// </summary>
		public static readonly Exposer<Capacity, bool> HasStepSize = new Exposer<Capacity, bool>((obj) => obj.StepSize.HasValue, "HasStepSize");

		/// <summary>
		/// Gets an exposer for the <see cref="Capacity.Decimals"/> property.
		/// </summary>
		public static readonly Exposer<Capacity, int> Decimals = new Exposer<Capacity, int>((obj) => obj.Decimals.GetValueOrDefault(), "Decimals");

		/// <summary>
		/// Gets an exposer for the <see cref="Capacity.Decimals"/> property.
		/// </summary>
		public static readonly Exposer<Capacity, bool> HasDecimals = new Exposer<Capacity, bool>((obj) => obj.Decimals.HasValue, "HasDecimals");
	}
}
