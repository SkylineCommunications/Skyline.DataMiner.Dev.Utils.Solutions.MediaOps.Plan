namespace Skyline.DataMiner.SDM
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	/// <summary>
	/// Represents a query combined with the fields that should be returned by a partial read.
	/// </summary>
	/// <typeparam name="T">The type of the objects to query.</typeparam>
	/// <remarks>
	/// Temporary definition that mirrors the interface announced by the SDM team. Remove once it ships in Skyline.DataMiner.Dev.Utils.SDM.Abstractions.
	/// Create instances with <see cref="PartialReadExtensions.WithFields{T}(IQuery{T}, FieldExposer[])"/>.
	/// The selected fields are kept when the query is modified with the <c>With</c> methods of <see cref="IQuery{T}"/>.
	/// </remarks>
	public interface IPartialQuery<T> : IQuery<T>
	{
		/// <summary>
		/// Gets the fields to return for each object.
		/// </summary>
		IReadOnlyCollection<FieldExposer> Fields { get; }
	}
}
