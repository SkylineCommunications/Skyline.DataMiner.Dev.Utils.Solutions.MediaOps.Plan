namespace RT_MediaOps.Plan.Workflow.Workflows
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;
	using RT_MediaOps.Plan.RST;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	using SLDataGateway.API.Querying;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class BasicTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public BasicTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void ReadAllWorkflows()
		{
			try
			{
				TestContext.Api.Workflows.Read().ToArray();
				return;
			}
			catch (Exception)
			{
				Assert.Fail();
			}
		}

		[TestMethod]
		public void ReadWorkflowById()
		{
			var firstWorkflow = TestContext.Api.Workflows.Read().First();
			var jobToVerify = TestContext.Api.Workflows.Read(firstWorkflow.Id);

			Assert.AreEqual(firstWorkflow, jobToVerify);
		}

		[TestMethod]
		public void ReadWorkflowByName()
		{
			var firstWorkflow = TestContext.Api.Workflows.Read().First();
			var jobToVerify = TestContext.Api.Workflows.Read(WorkflowExposers.Name.Equal(firstWorkflow.Name)).First();

			Assert.AreEqual(firstWorkflow, jobToVerify);
		}

		[TestMethod]
		public void ReadWithEmptyFilterReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Workflow>(idsToRetrieve.Select(x => WorkflowExposers.Id.Equal(x)).ToArray());

			var workflows = TestContext.Api.Workflows.Read(emptyFilter);
			Assert.IsNotNull(workflows);
			Assert.AreEqual(0, workflows.Count());
		}

		[TestMethod]
		public void CountWithEmptyFilterReturnsZero()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Workflow>(idsToRetrieve.Select(x => WorkflowExposers.Id.Equal(x)).ToArray());

			var count = ((WorkflowsRepository)TestContext.Api.Workflows).Count(emptyFilter);
			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public void ReadWithEmptyQueryReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Workflow>(idsToRetrieve.Select(x => WorkflowExposers.Id.Equal(x)).ToArray());
			var queryWithEmptyFilter = emptyFilter.ToQuery();

			var workflows = TestContext.Api.Workflows.Read(queryWithEmptyFilter);
			Assert.IsNotNull(workflows);
			Assert.AreEqual(0, workflows.Count());
		}

		[TestMethod]
		public void HappyPathCrud()
		{
			var prefix = Guid.NewGuid();
			var name = $"{prefix}_Workflow";

			var workflow = new Workflow
			{
				Name = name,
				Description = "Initial description",
				Priority = WorkflowPriority.Normal,
				IsFavorite = false,
				PreRoll = TimeSpan.FromSeconds(5),
				PostRoll = TimeSpan.FromSeconds(10),
				Notes = "Initial notes",
			};

			objectCreator.CreateWorkflow(workflow);

			// Read and validate created workflow.
			var created = TestContext.Api.Workflows.Read(workflow.Id);
			Assert.IsNotNull(created);
			Assert.AreEqual(name, created.Name);
			Assert.AreEqual("Initial description", created.Description);
			Assert.AreEqual(WorkflowPriority.Normal, created.Priority);
			Assert.IsFalse(created.IsFavorite);
			Assert.AreEqual(TimeSpan.FromSeconds(5), created.PreRoll);
			Assert.AreEqual(TimeSpan.FromSeconds(10), created.PostRoll);
			Assert.AreEqual("Initial notes", created.Notes);
			Assert.AreEqual(WorkflowState.Draft, created.State);
			Assert.AreEqual(0, created.NodeGraph.Nodes.Count);

			// Update and validate.
			var updatedName = $"{prefix}_Workflow_updated";
			created.Name = updatedName;
			created.Description = "Updated description";
			created.Priority = WorkflowPriority.High;
			created.IsFavorite = true;
			created.Notes = "Updated notes";

			((WorkflowsRepository)TestContext.Api.Workflows).Update(created);

			var updated = TestContext.Api.Workflows.Read(workflow.Id);
			Assert.IsNotNull(updated);
			Assert.AreEqual(updatedName, updated.Name);
			Assert.AreEqual("Updated description", updated.Description);
			Assert.AreEqual(WorkflowPriority.High, updated.Priority);
			Assert.IsTrue(updated.IsFavorite);
			Assert.AreEqual("Updated notes", updated.Notes);

			// Delete and validate it is gone.
			((WorkflowsRepository)TestContext.Api.Workflows).Delete(updated);

			var deleted = TestContext.Api.Workflows.Read(workflow.Id);
			Assert.IsNull(deleted);
		}

		[TestMethod]
		public void CreateOrUpdate_CreatesThenUpdates_SingleWorkflow()
		{
			var prefix = Guid.NewGuid();
			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
				Description = "Created via CreateOrUpdate",
			};

			var created = ((WorkflowsRepository)TestContext.Api.Workflows).CreateOrUpdate([workflow]).Single();

			try
			{
				var persisted = TestContext.Api.Workflows.Read(created.Id);
				Assert.IsNotNull(persisted);
				Assert.AreEqual($"{prefix}_Workflow", persisted.Name);
				Assert.AreEqual("Created via CreateOrUpdate", persisted.Description);

				persisted.Description = "Updated via CreateOrUpdate";
				var updated = ((WorkflowsRepository)TestContext.Api.Workflows).CreateOrUpdate([persisted]).Single();

				Assert.AreEqual(created.Id, updated.Id);
				Assert.AreEqual("Updated via CreateOrUpdate", updated.Description);
			}
			finally
			{
				((WorkflowsRepository)TestContext.Api.Workflows).Delete(created.Id);
			}
		}

		[TestMethod]
		public void Create_DuplicateName_Fails()
		{
			var prefix = Guid.NewGuid();
			var name = $"{prefix}_Workflow";

			objectCreator.CreateWorkflow(new Workflow { Name = name });

			try
			{
				objectCreator.CreateWorkflow(new Workflow { Name = name });
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var hasDuplicateNameError =
					ex.TraceData.ErrorData.OfType<WorkflowDuplicateNameError>().Any() ||
					ex.TraceData.ErrorData.OfType<WorkflowNameExistsError>().Any();
				Assert.IsTrue(hasDuplicateNameError, "Expected duplicate-name error was not reported.");
			}
		}

		[TestMethod]
		public void Create_EmptyName_Fails()
		{
			try
			{
				objectCreator.CreateWorkflow(new Workflow { Name = String.Empty });
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<WorkflowInvalidNameError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected invalid-name error was not reported.");
			}
		}

		[TestMethod]
		public void Create_NegativePreRoll_Fails()
		{
			var prefix = Guid.NewGuid();
			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
				PreRoll = TimeSpan.FromSeconds(-1),
			};

			try
			{
				objectCreator.CreateWorkflow(workflow);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<WorkflowInvalidPreRollError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected invalid pre-roll error was not reported.");
			}
		}

		[TestMethod]
		public void Create_PreRollNotMultipleOfSeconds_Fails()
		{
			var prefix = Guid.NewGuid();
			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
				PreRoll = TimeSpan.FromMilliseconds(1500), // 1.5 seconds — not a whole second
			};

			try
			{
				objectCreator.CreateWorkflow(workflow);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<WorkflowInvalidPreRollError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected invalid pre-roll error was not reported.");
			}
		}

		[TestMethod]
		public void Create_PostRollNotMultipleOfSeconds_Fails()
		{
			var prefix = Guid.NewGuid();
			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
				PostRoll = TimeSpan.FromMilliseconds(1500), // 1.5 seconds — not a whole second
			};

			try
			{
				objectCreator.CreateWorkflow(workflow);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<WorkflowInvalidPostRollError>().SingleOrDefault();
				Assert.IsNotNull(error, "Expected invalid post-roll error was not reported.");
			}
		}

		[TestMethod]
		public void Update_NewWorkflow_ThrowsInvalidOperation()
		{
			var workflow = new Workflow { Name = $"{Guid.NewGuid()}_Workflow" };

			Assert.ThrowsException<InvalidOperationException>(() => ((WorkflowsRepository)TestContext.Api.Workflows).Update(workflow));
		}

		[TestMethod]
		public void Create_ExistingWorkflow_ThrowsInvalidOperation()
		{
			var workflow = objectCreator.CreateWorkflow(new Workflow { Name = $"{Guid.NewGuid()}_Workflow" });

			Assert.ThrowsException<InvalidOperationException>(() => ((WorkflowsRepository)TestContext.Api.Workflows).Create(workflow));
		}

		[TestMethod]
		public void Delete_UnknownId_DoesNotThrow()
		{
			try
			{
				((WorkflowsRepository)TestContext.Api.Workflows).Delete(Guid.NewGuid());
			}
			catch (Exception ex)
			{
				Assert.Fail($"Expected no exception when deleting unknown workflow ID, but got: {ex}");
			}
		}
	}
}
