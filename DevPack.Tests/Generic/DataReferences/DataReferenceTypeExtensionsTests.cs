namespace RT_MediaOps.Plan.Generic.DataReferences
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Tests for <see cref="DataReferenceTypeExtensions"/>, which drives which reference types can be linked to a
	/// specific node in the configuration UI.
	/// </summary>
	[TestClass]
	public sealed class DataReferenceTypeExtensionsTests
	{
		private static readonly HashSet<DataReferenceType> NodeScopedTypes =
		[
			DataReferenceType.ResourceName,
			DataReferenceType.ResourceProperty,
			DataReferenceType.ResourceLinkedObjectID,
			DataReferenceType.CapabilityParameter,
			DataReferenceType.CapacityParameter,
			DataReferenceType.ConfigurationParameter,
		];

		public static IEnumerable<object[]> AllTypes => DataReferenceFactory.AllTypes;

		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceTypeExtensionsTests_IsNodeScoped_MatchesTheExpectedScope(DataReferenceType type)
		{
			Assert.AreEqual(NodeScopedTypes.Contains(type), type.IsNodeScoped(), $"Unexpected node scope for '{type}'.");
		}

		/// <summary>The job-level types are the only ones that describe the workflow / job as a whole.</summary>
		[TestMethod]
		public void DataReferenceTypeExtensionsTests_IsNodeScoped_JobLevelTypes_ReturnFalse()
		{
			var jobLevelTypes = Enum.GetValues(typeof(DataReferenceType))
				.Cast<DataReferenceType>()
				.Where(type => !type.IsNodeScoped())
				.ToList();

			CollectionAssert.AreEquivalent(new[] { DataReferenceType.JobName, DataReferenceType.JobProperty }, jobLevelTypes);
		}
	}
}
