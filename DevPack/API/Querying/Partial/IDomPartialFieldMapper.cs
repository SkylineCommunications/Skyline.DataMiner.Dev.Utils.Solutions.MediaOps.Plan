namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.ManagerStore.Select;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;

	using DomPartialObject = Skyline.DataMiner.Net.Apps.ManagerStore.Select.PartialObject<Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstance, Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstanceId>;

	/// <summary>
	/// Maps the exposers of an API object that is stored in DOM to the DOM fields they are read from in a partial read,
	/// and converts the read DOM values the same way the API object does when it parses a full DOM instance.
	/// </summary>
	/// <typeparam name="T">The type of the API object.</typeparam>
	internal interface IDomPartialFieldMapper<T>
		where T : ApiObject
	{
		/// <summary>
		/// Creates the DOM field selection needed to read the specified fields.
		/// </summary>
		/// <exception cref="NotSupportedException">Thrown when one of the fields can't be read partially.</exception>
		SelectedFields<DomInstance> CreateSelectedFields(IEnumerable<FieldExposer> fields);

		/// <summary>
		/// Converts a batch of partial DOM instances into partial objects containing the specified fields. Data stored
		/// outside the DOM instances should be read once for the whole batch.
		/// </summary>
		/// <exception cref="NotSupportedException">Thrown when one of the fields can't be read partially.</exception>
		IReadOnlyList<PartialObject<T>> CreatePartialObjects(MediaOpsPlanApi planApi, IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields);
	}
}
