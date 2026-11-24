namespace Skyline.DataMiner.SDM
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Represents a filter combined with the fields that should be returned by a partial read.
	/// </summary>
	/// <typeparam name="T">The type of the objects to filter.</typeparam>
	/// <remarks>
	/// Temporary definition that mirrors the interface announced by the SDM team. Remove once it ships in Skyline.DataMiner.Dev.Utils.SDM.Abstractions.
	/// Create instances with <see cref="PartialReadExtensions.WithFields{T}(FilterElement{T}, FieldExposer[])"/>.
	/// </remarks>
	public interface IPartialFilterElement<T> : IFilterElement
	{
		/// <summary>
		/// Gets the filter that selects the objects to read.
		/// </summary>
		FilterElement<T> Filter { get; }

		/// <summary>
		/// Gets the fields to return for each object.
		/// </summary>
		IReadOnlyCollection<FieldExposer> Fields { get; }
	}
}
