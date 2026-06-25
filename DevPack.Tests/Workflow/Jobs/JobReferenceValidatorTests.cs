namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	[TestClass]
	public sealed class JobReferenceValidatorTests
	{
		private static IMediaOpsPlanApi CreateApi()
		{
			var dms = MediaOpsPlanSimulation.Create();
			return dms.CreateConnection().GetMediaOpsPlanApi();
		}

		[TestMethod]
		public void Resolve_ResolvedAndUnresolvedReferences_ReportsBoth()
		{
			var api = CreateApi();

			var resolvedReference = new ResourcePropertyReference(Guid.NewGuid());
			var unresolvedReference = new ResourcePropertyReference(Guid.NewGuid());

			var job = new Job { Name = "Test job" };
			job.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()) { Reference = resolvedReference });
			job.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()) { Reference = unresolvedReference });

			var resolver = new FakeReferenceResolver(api, new Dictionary<DataReference, ResolvedValue>
			{
				[resolvedReference] = new StringResolvedValue("value"),
			});

			var result = new JobReferenceValidator(resolver).Resolve(job);

			Assert.IsFalse(result.IsValid);
			Assert.AreEqual(1, result.UnresolvedReferences.Count);
			Assert.AreEqual(unresolvedReference, result.UnresolvedReferences.Single());
			Assert.AreEqual(1, result.ResolvedReferences.Count);
			Assert.IsTrue(result.ResolvedReferences.ContainsKey(resolvedReference));
		}

		[TestMethod]
		public void Resolve_AllReferencesResolved_IsValid()
		{
			var api = CreateApi();

			var reference = new ResourcePropertyReference(Guid.NewGuid());

			var job = new Job { Name = "Test job" };
			job.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()) { Reference = reference });

			var resolver = new FakeReferenceResolver(api, new Dictionary<DataReference, ResolvedValue>
			{
				[reference] = new StringResolvedValue("value"),
			});

			var result = new JobReferenceValidator(resolver).Resolve(job);

			Assert.IsTrue(result.IsValid);
			Assert.AreEqual(0, result.UnresolvedReferences.Count);
			Assert.AreEqual(1, result.ResolvedReferences.Count);
		}

		[TestMethod]
		public void Resolve_CircularReference_TreatedAsUnresolved()
		{
			var api = CreateApi();

			var reference = new ResourcePropertyReference(Guid.NewGuid());

			var job = new Job { Name = "Test job" };
			job.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()) { Reference = reference });

			var resolver = new FakeReferenceResolver(
				api,
				new Dictionary<DataReference, ResolvedValue>(),
				new HashSet<DataReference> { reference });

			var result = new JobReferenceValidator(resolver).Resolve(job);

			Assert.IsFalse(result.IsValid);
			Assert.AreEqual(reference, result.UnresolvedReferences.Single());
			Assert.AreEqual(0, result.ResolvedReferences.Count);
		}

		[TestMethod]
		public void Resolve_SettingsWithoutReferences_IsValidAndEmpty()
		{
			var api = CreateApi();

			var job = new Job { Name = "Test job" };
			job.OrchestrationSettings.AddCapability(new CapabilitySetting(Guid.NewGuid()));

			var resolver = new FakeReferenceResolver(api, new Dictionary<DataReference, ResolvedValue>());

			var result = new JobReferenceValidator(resolver).Resolve(job);

			Assert.IsTrue(result.IsValid);
			Assert.AreEqual(0, result.UnresolvedReferences.Count);
			Assert.AreEqual(0, result.ResolvedReferences.Count);
		}

		private sealed class FakeReferenceResolver : ReferenceResolver
		{
			private readonly IReadOnlyDictionary<DataReference, ResolvedValue> resolvedValues;
			private readonly ISet<DataReference> circularReferences;

			public FakeReferenceResolver(
				IMediaOpsPlanApi planApi,
				IReadOnlyDictionary<DataReference, ResolvedValue> resolvedValues,
				ISet<DataReference> circularReferences = null)
				: base(planApi)
			{
				this.resolvedValues = resolvedValues;
				this.circularReferences = circularReferences ?? new HashSet<DataReference>();
			}

			public override ResolvedValue ResolveValue(DataReference reference)
			{
				if (circularReferences.Contains(reference))
				{
					throw new CircularReferenceException(reference);
				}

				return resolvedValues.TryGetValue(reference, out var value)
					? value
					: ResolvedValue.FromUnresolvedReference(reference);
			}

			public override string GetDisplayLabel(DataReference reference)
			{
				return reference?.ToString();
			}
		}
	}
}
