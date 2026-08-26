namespace RT_MediaOps.Plan.Generic.DataReferences
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Builds a <see cref="DataReference"/> for every <see cref="DataReferenceType"/> so the reference tests can be
	/// data-driven. Adding a new reference type without updating this factory makes those tests fail.
	/// </summary>
	internal static class DataReferenceFactory
	{
		public static IEnumerable<object[]> AllTypes =>
			Enum.GetValues(typeof(DataReferenceType))
				.Cast<DataReferenceType>()
				.Select(type => new object[] { type });

		/// <summary>Gets the reference types that carry an identifier next to the optional node id.</summary>
		public static IEnumerable<object[]> TypesWithPayload =>
			Enum.GetValues(typeof(DataReferenceType))
				.Cast<DataReferenceType>()
				.Where(type => GetPayloadKey(type) != null)
				.Select(type => new object[] { type });

		public static DataReference Create(DataReferenceType type, Guid payloadId, string? nodeId = null)
		{
			return type switch
			{
				DataReferenceType.ResourceName => new ResourceNameReference(nodeId),
				DataReferenceType.ResourceLinkedObjectID => new ResourceLinkedObjectIdReference(nodeId),
				DataReferenceType.ResourceProperty => new ResourcePropertyReference(payloadId, nodeId),
				DataReferenceType.CapabilityParameter => new CapabilityParameterReference(payloadId, nodeId),
				DataReferenceType.CapacityParameter => new CapacityParameterReference(payloadId, nodeId),
				DataReferenceType.ConfigurationParameter => new ConfigurationParameterReference(payloadId, nodeId),
				DataReferenceType.JobName => new JobNameReference(nodeId),
				DataReferenceType.JobProperty => new JobPropertyReference(payloadId, nodeId),
				_ => throw new NotSupportedException($"No reference implementation is registered for '{type}'."),
			};
		}

		/// <summary>
		/// Gets the storage key and <see cref="object.ToString"/> label of the identifier carried by the reference type,
		/// or <see langword="null"/> when the type only carries the optional node id.
		/// </summary>
		public static string? GetPayloadKey(DataReferenceType type)
		{
			return type switch
			{
				DataReferenceType.ResourceProperty => "ResourcePropertyId",
				DataReferenceType.JobProperty => "JobPropertyId",
				DataReferenceType.CapabilityParameter => "ParameterId",
				DataReferenceType.CapacityParameter => "ParameterId",
				DataReferenceType.ConfigurationParameter => "ParameterId",
				_ => null,
			};
		}

		/// <summary>Gets the identifier carried by the reference, or <see cref="Guid.Empty"/> when it carries none.</summary>
		public static Guid GetPayloadId(DataReference reference)
		{
			return reference switch
			{
				ResourcePropertyReference rpr => rpr.ResourcePropertyId,
				JobPropertyReference jpr => jpr.JobPropertyId,
				ParameterReference pr => pr.ParameterId,
				_ => Guid.Empty,
			};
		}
	}
}
