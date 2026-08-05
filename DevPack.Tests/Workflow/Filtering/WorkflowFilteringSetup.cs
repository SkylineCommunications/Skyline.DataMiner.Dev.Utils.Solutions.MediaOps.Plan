namespace RT_MediaOps.Plan.Workflow.Filtering
{
	using System;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	internal sealed class WorkflowFilteringSetup
	{
		private readonly TestObjectCreator objectCreator;
		private readonly IntegrationTestContext testContext;

		public WorkflowFilteringSetup(TestObjectCreator objectCreator, IntegrationTestContext testContext)
		{
			this.objectCreator = objectCreator;
			this.testContext = testContext;

			Prefix = Guid.NewGuid().ToString();

			CreateWorkflows();
		}

		public string Prefix { get; }

		public Workflow[] Workflows => new[]
		{
			DraftWorkflow1!,
			DraftWorkflow2!,
			CompleteWorkflow3!,
		};

		public Workflow? DraftWorkflow1 { get; private set; }

		public Workflow? DraftWorkflow2 { get; private set; }

		public Workflow? CompleteWorkflow3 { get; private set; }

		private void CreateWorkflows()
		{
			DraftWorkflow1 = objectCreator.CreateWorkflow(new Workflow
			{
				Name = $"Workflow_Draft_1_{Prefix}",
				Description = "First draft workflow",
				Priority = WorkflowPriority.High,
				IsFavorite = true,
				PreRoll = TimeSpan.FromSeconds(30),
				PostRoll = TimeSpan.FromSeconds(60),
				Notes = "Notes of the first workflow",
			});

			DraftWorkflow2 = objectCreator.CreateWorkflow(new Workflow
			{
				Name = $"Workflow_Draft_2_{Prefix}",
				Description = "Second draft workflow",
				Priority = WorkflowPriority.Normal,
				IsFavorite = false,
				PreRoll = TimeSpan.FromSeconds(45),
				PostRoll = TimeSpan.FromSeconds(90),
				Notes = "Notes of the second workflow",
			});

			CompleteWorkflow3 = objectCreator.CreateWorkflow(new Workflow
			{
				Name = $"Workflow_Complete_3_{Prefix}",
				Description = "Third workflow",
				Priority = WorkflowPriority.Low,
				IsFavorite = true,
				PreRoll = TimeSpan.FromSeconds(30),
				PostRoll = TimeSpan.FromSeconds(120),
				Notes = "Notes of the third workflow",
			});

			CompleteWorkflow3 = testContext.Api.Workflows.Complete(CompleteWorkflow3);

			DraftWorkflow1 = testContext.Api.Workflows.Read(DraftWorkflow1.Id);
			DraftWorkflow2 = testContext.Api.Workflows.Read(DraftWorkflow2.Id);
			CompleteWorkflow3 = testContext.Api.Workflows.Read(CompleteWorkflow3.Id);
		}
	}
}
