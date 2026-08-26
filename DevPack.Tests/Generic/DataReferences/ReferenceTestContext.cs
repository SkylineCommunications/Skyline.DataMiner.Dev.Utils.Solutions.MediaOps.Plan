namespace RT_MediaOps.Plan.Generic.DataReferences
{
	using System;

	using RT_MediaOps.Plan.Extensions;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	/// <summary>
	/// Creates the definitions a <see cref="ReferenceResolver"/> needs, backed by an in-memory DataMiner simulation so
	/// the reference tests stay deterministic and self-contained.
	/// </summary>
	internal sealed class ReferenceTestContext
	{
		private readonly Guid prefix = Guid.NewGuid();

		private ReferenceTestContext(IMediaOpsPlanApi api)
		{
			Api = api;
		}

		public IMediaOpsPlanApi Api { get; }

		/// <summary>Gets a start time a job can be scheduled on; job validation rejects sub-second precision.</summary>
		public static DateTimeOffset ScheduleStart => new DateTimeOffset(DateTime.UtcNow.RoundToNextSecond()).AddHours(1);

		public static ReferenceTestContext Create()
		{
			var dms = MediaOpsPlanSimulation.Create();

			return new ReferenceTestContext(dms.CreateConnection().GetMediaOpsPlanApi());
		}

		public string Name(string suffix)
		{
			return $"{prefix}_{suffix}";
		}

		public ResourcePool CreatePool()
		{
			return Api.ResourcePools.Complete(Api.ResourcePools.Create(new ResourcePool { Name = Name("Pool") }));
		}

		public Resource CreateResource(ResourcePool pool, params ResourcePropertySettings[] properties)
		{
			return CreateResource(pool, "Resource", properties);
		}

		public Resource CreateResource(ResourcePool pool, string nameSuffix, params ResourcePropertySettings[] properties)
		{
			var resource = new UnmanagedResource { Name = Name(nameSuffix) };
			resource.AssignToPool(pool);

			foreach (var property in properties)
			{
				resource.AddProperty(property);
			}

			return Api.Resources.Complete(Api.Resources.Create(resource));
		}

		public Capability CreateCapability()
		{
			return Api.Capabilities.Create(new Capability { Name = Name("Capability") }.SetDiscretes(["Value 1", "Value 2"]));
		}

		public NumberCapacity CreateNumberCapacity()
		{
			return (NumberCapacity)Api.Capacities.Create(new NumberCapacity { Name = Name("NumberCapacity") });
		}

		public RangeCapacity CreateRangeCapacity()
		{
			return (RangeCapacity)Api.Capacities.Create(new RangeCapacity { Name = Name("RangeCapacity") });
		}

		public TextConfiguration CreateTextConfiguration()
		{
			return (TextConfiguration)Api.Configurations.Create(new TextConfiguration { Name = Name("TextConfiguration") });
		}

		public NumberConfiguration CreateNumberConfiguration()
		{
			return (NumberConfiguration)Api.Configurations.Create(new NumberConfiguration { Name = Name("NumberConfiguration") });
		}

		public DiscreteTextConfiguration CreateDiscreteTextConfiguration()
		{
			var configuration = new DiscreteTextConfiguration { Name = Name("DiscreteTextConfiguration") }
				.AddDiscrete(new TextDiscreet("A", "Option A"));

			return (DiscreteTextConfiguration)Api.Configurations.Create(configuration);
		}

		public DiscreteNumberConfiguration CreateDiscreteNumberConfiguration()
		{
			var configuration = new DiscreteNumberConfiguration { Name = Name("DiscreteNumberConfiguration") }
				.AddDiscrete(new NumberDiscreet(7, "Seven"));

			return (DiscreteNumberConfiguration)Api.Configurations.Create(configuration);
		}

		public ResourceProperty CreateResourceProperty()
		{
			return Api.ResourceProperties.Create(new ResourceProperty { Name = Name("ResourceProperty") });
		}

		public StringProperty CreateJobProperty()
		{
			return (StringProperty)Api.SchedulingProperties.Create(new StringProperty
			{
				Name = Name("JobProperty"),
				SectionName = "General",
			});
		}
	}
}
