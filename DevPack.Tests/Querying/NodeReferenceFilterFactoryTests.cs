namespace RT_MediaOps.Plan.Querying
{
	using System;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	[TestClass]
	public sealed class NodeReferenceFilterFactoryTests
	{
		[TestMethod]
		public void CreateResourceFilter_NotContains_AlsoMatchesObjectsWithoutNodes()
		{
			var resourceId = Guid.NewGuid();

			var containsFilter = NodeReferenceFilterFactory.CreateResourceFilter(Comparer.Contains, resourceId, "Resources");
			var notContainsFilter = NodeReferenceFilterFactory.CreateResourceFilter(Comparer.NotContains, resourceId, "Resources");

			Assert.AreEqual(CreateExpectedNotContainsFilter(containsFilter).ToString(), notContainsFilter.ToString());
		}

		[TestMethod]
		public void CreateResourcePoolFilter_NotContains_AlsoMatchesObjectsWithoutNodes()
		{
			var resourcePoolId = Guid.NewGuid();

			var containsFilter = NodeReferenceFilterFactory.CreateResourcePoolFilter(Comparer.Contains, resourcePoolId, "ResourcePools");
			var notContainsFilter = NodeReferenceFilterFactory.CreateResourcePoolFilter(Comparer.NotContains, resourcePoolId, "ResourcePools");

			Assert.AreEqual(CreateExpectedNotContainsFilter(containsFilter).ToString(), notContainsFilter.ToString());
		}

		[TestMethod]
		public void CreateResourceFilter_UnsupportedComparer_ThrowsNotSupportedException()
		{
			Assert.ThrowsException<NotSupportedException>(
				() => NodeReferenceFilterFactory.CreateResourceFilter(Comparer.Equals, Guid.NewGuid(), "Resources"));
		}

		/// <summary>
		/// A negated filter on the field values of the nodes section is only evaluated for DOM instances that hold
		/// such a section, so objects without any node must be included explicitly.
		/// </summary>
		private static FilterElement<DomInstance> CreateExpectedNotContainsFilter(FilterElement<DomInstance> containsFilter)
		{
			return new ORFilterElement<DomInstance>(
				new NOTFilterElement<DomInstance>(containsFilter),
				DomInstanceExposers.SectionDefinitionIds.NotContains(SlcWorkflowIds.Sections.Nodes.Id.Id));
		}
	}
}
