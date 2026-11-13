namespace RT_MediaOps.Plan.RST.Resources
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Extensions;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	using PlanResource = Skyline.DataMiner.Solutions.MediaOps.Plan.API.Resource;
	using ResourcePool = Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool;

	/// <summary>
	/// Simulation-backed tests for the virtual signal group role check: the input virtual signal group of a resource must be a
	/// destination and the output virtual signal group must be a source, both on resource save and on job validation.
	/// </summary>
	[TestClass]
	public sealed class VirtualSignalGroupRoleSimulationTests
	{
		[TestMethod]
		public void Create_SourceUsedAsInput_ReportsInvalidRole()
		{
			var setup = CreateSetup();
			var source = setup.CreateVirtualSignalGroup(EndpointRole.Source);

			var exception = Assert.ThrowsException<MediaOpsException>(() => setup.Api.Resources.Create(new UnmanagedResource { Name = "Resource", VirtualSignalGroupInputId = source.ID }));

			var error = exception.TraceData.ErrorData.OfType<ResourceInvalidVirtualSignalGroupRoleError>().Single();
			Assert.AreEqual(source.ID, error.VirtualSignalGroupId);
			Assert.AreEqual(0, exception.TraceData.ErrorData.OfType<ResourceInvalidVirtualSignalGroupError>().Count(), "An existing virtual signal group must not be reported as not found.");
		}

		[TestMethod]
		public void Create_DestinationUsedAsOutput_ReportsInvalidRole()
		{
			var setup = CreateSetup();
			var destination = setup.CreateVirtualSignalGroup(EndpointRole.Destination);

			var exception = Assert.ThrowsException<MediaOpsException>(() => setup.Api.Resources.Create(new UnmanagedResource { Name = "Resource", VirtualSignalGroupOutputId = destination.ID }));

			var error = exception.TraceData.ErrorData.OfType<ResourceInvalidVirtualSignalGroupRoleError>().Single();
			Assert.AreEqual(destination.ID, error.VirtualSignalGroupId);
		}

		[TestMethod]
		public void Create_RolesSwapped_ReportsBothLinks()
		{
			var setup = CreateSetup();
			var source = setup.CreateVirtualSignalGroup(EndpointRole.Source);
			var destination = setup.CreateVirtualSignalGroup(EndpointRole.Destination);

			var exception = Assert.ThrowsException<MediaOpsException>(() => setup.Api.Resources.Create(new UnmanagedResource
			{
				Name = "Resource",
				VirtualSignalGroupInputId = source.ID,
				VirtualSignalGroupOutputId = destination.ID,
			}));

			CollectionAssert.AreEquivalent(
				new[] { source.ID, destination.ID },
				exception.TraceData.ErrorData.OfType<ResourceInvalidVirtualSignalGroupRoleError>().Select(x => x.VirtualSignalGroupId).ToArray());
		}

		[TestMethod]
		public void Create_MatchingRoles_Succeeds()
		{
			var setup = CreateSetup();
			var destination = setup.CreateVirtualSignalGroup(EndpointRole.Destination);
			var source = setup.CreateVirtualSignalGroup(EndpointRole.Source);

			var resource = setup.Api.Resources.Create(new UnmanagedResource
			{
				Name = "Resource",
				VirtualSignalGroupInputId = destination.ID,
				VirtualSignalGroupOutputId = source.ID,
			});

			Assert.AreEqual(destination.ID, resource.VirtualSignalGroupInputId);
			Assert.AreEqual(source.ID, resource.VirtualSignalGroupOutputId);
		}

		[TestMethod]
		public void Create_UnknownVirtualSignalGroup_StillReportsNotFoundOnly()
		{
			var setup = CreateSetup();
			var unknownId = Guid.NewGuid();

			var exception = Assert.ThrowsException<MediaOpsException>(() => setup.Api.Resources.Create(new UnmanagedResource { Name = "Resource", VirtualSignalGroupInputId = unknownId }));

			Assert.AreEqual(unknownId, exception.TraceData.ErrorData.OfType<ResourceInvalidVirtualSignalGroupError>().Single().VirtualSignalGroupId);
			Assert.AreEqual(0, exception.TraceData.ErrorData.OfType<ResourceInvalidVirtualSignalGroupRoleError>().Count());
		}

		[TestMethod]
		public void Update_RoleChangedToSourceAfterSave_ReportsInvalidRoleOnNextSave()
		{
			var setup = CreateSetup();
			var destination = setup.CreateVirtualSignalGroup(EndpointRole.Destination);
			var resource = setup.Api.Resources.Create(new UnmanagedResource { Name = "Resource", VirtualSignalGroupInputId = destination.ID });

			setup.ChangeRole(destination, EndpointRole.Source);
			resource.Name = "Renamed";

			var exception = Assert.ThrowsException<MediaOpsException>(() => setup.Api.Resources.Update(resource));
			Assert.AreEqual(destination.ID, exception.TraceData.ErrorData.OfType<ResourceInvalidVirtualSignalGroupRoleError>().Single().VirtualSignalGroupId);
		}

		[TestMethod]
		public void JobValidate_RoleChangedAfterResourceSave_ReportsInvalidRoleError()
		{
			var setup = CreateSetup();
			var destination = setup.CreateVirtualSignalGroup(EndpointRole.Destination);
			var source = setup.CreateVirtualSignalGroup(EndpointRole.Source);
			var job = setup.CreateJob(destination, source);

			Assert.IsFalse(setup.Api.Jobs.Validate([job]).Single().HasErrors, "A job on correctly linked virtual signal groups must validate.");

			setup.ChangeRole(source, EndpointRole.Destination);

			var result = setup.Api.Jobs.Validate([job]).Single();
			Assert.IsTrue(result.HasError(VirtualSignalGroupInvalidRoleJobValidationError.ErrorCode), "Expected the output virtual signal group with role Destination to be reported.");
			Assert.IsFalse(result.HasError(VirtualSignalGroupNotFoundJobValidationError.ErrorCode), "An existing virtual signal group must not be reported as not found.");
		}

		private static TestSetup CreateSetup()
		{
			var connection = MediaOpsPlanSimulation.Create(installMediaOpsLive: true).CreateConnection();
			return new TestSetup(connection.GetMediaOpsPlanApi(), connection.GetMediaOpsLiveApi());
		}

		private sealed class TestSetup
		{
			private int _counter;

			public TestSetup(IMediaOpsPlanApi api, IMediaOpsLiveApi liveApi)
			{
				Api = api;
				LiveApi = liveApi;
			}

			public IMediaOpsPlanApi Api { get; }

			public IMediaOpsLiveApi LiveApi { get; }

			public VirtualSignalGroup CreateVirtualSignalGroup(EndpointRole role)
			{
				return LiveApi.VirtualSignalGroups.Create(new VirtualSignalGroup { Name = $"VSG {++_counter} ({role})", Role = role });
			}

			public void ChangeRole(VirtualSignalGroup virtualSignalGroup, EndpointRole role)
			{
				virtualSignalGroup.Role = role;
				LiveApi.VirtualSignalGroups.Update(virtualSignalGroup);
			}

			public Job CreateJob(VirtualSignalGroup input, VirtualSignalGroup output)
			{
				var pool = Api.ResourcePools.Complete(Api.ResourcePools.Create(new ResourcePool { Name = "Pool" }));
				PlanResource resource = new UnmanagedResource
				{
					Name = "Resource",
					VirtualSignalGroupInputId = input.ID,
					VirtualSignalGroupOutputId = output.ID,
				}.AssignToPool(pool);
				resource = Api.Resources.Complete(Api.Resources.Create(resource));

				var start = DateTime.UtcNow.AddHours(1).RoundToNextSecond();
				var job = new Job
				{
					Name = "Job",
					PreRollStart = start,
					Start = start,
					End = start.AddHours(1),
					PostRollEnd = start.AddHours(1),
				};
				job.NodeGraph.Add(new JobResourceNode(pool, resource));

				return Api.Jobs.Create(job);
			}
		}
	}
}
