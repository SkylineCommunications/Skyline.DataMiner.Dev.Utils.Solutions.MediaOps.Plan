namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Benchmarks.Workflow.Jobs
{
	using System;
	using System.Linq;

	using BenchmarkDotNet.Attributes;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	[MemoryDiagnoser]
	[InProcess]
	public class ReadJobsWithinTimespanBenchmarks
	{
		private static readonly DateTime SeedStartUtc = new DateTime(2026, 01, 01, 00, 00, 00, DateTimeKind.Utc);
		private static readonly DateTime BenchmarkWindowStartUtc = SeedStartUtc.AddDays(10);

		private IConnection? connection;
		private MediaOpsPlanApi? api;
		private FilterElement<DomInstance>? timespanFilter;

		[Params(1, 24, 168)]
		public int TimespanHours { get; set; }

		[GlobalSetup]
		public void GlobalSetup()
		{
			var dms = MediaOpsPlanSimulation.Create();
			connection = dms.CreateConnection();
			api = (MediaOpsPlanApi)(connection.GetMediaOpsPlanApi() ?? throw new InvalidOperationException("Unable to create MediaOpsPlanApi"));

			SeedJobs();

			var windowEndUtc = BenchmarkWindowStartUtc.AddHours(TimespanHours);
			timespanFilter = new ANDFilterElement<DomInstance>(
				DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Jobs.Id),
				DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobStart).GreaterThanOrEqual(BenchmarkWindowStartUtc),
				DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobEnd).LessThanOrEqual(windowEndUtc));
		}

		[Benchmark(Description = "Read all jobs within a timespan")]
		public Job[] ReadAllJobsWithinTimespan()
		{
			return api!.DomHelpers.SlcWorkflowHelper
				.GetJobs(timespanFilter!)
				.Select(domJob => new Job(api, domJob))
				.ToArray();
		}

		[GlobalCleanup]
		public void GlobalCleanup()
		{
			connection?.Dispose();
		}

		private void SeedJobs()
		{
			var jobs = Enumerable.Range(0, 24 * 30)
				.Select(index =>
				{
					var start = SeedStartUtc.AddHours(index);
					var end = start.AddMinutes(45);

					return new Job
					{
						Name = $"Benchmark Job {index:D4}",
						Start = start,
						End = end,
						PreRollStart = start,
						PostRollEnd = end,
					};
				})
				.ToArray();

			api!.Jobs.Create(jobs);
		}
	}
}
