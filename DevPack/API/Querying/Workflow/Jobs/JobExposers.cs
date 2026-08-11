namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="Job"/> objects.
	/// </summary>
	public static class JobExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="ApiObject.Id"/> property.
		/// </summary>
		public static readonly Exposer<Job, Guid> Id = new Exposer<Job, Guid>((obj) => obj.Id, "Id");

		/// <summary>
		/// Gets an exposer for the <see cref="ApiNamedObject.Name"/> property.
		/// </summary>
		public static readonly Exposer<Job, string> Name = new Exposer<Job, string>((obj) => obj.Name, "Name");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.Key"/> property.
		/// </summary>
		public static readonly Exposer<Job, string> Key = new Exposer<Job, string>((obj) => obj.Key, "Key");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.Description"/> property.
		/// </summary>
		public static readonly Exposer<Job, string> Description = new Exposer<Job, string>((obj) => obj.Description, "Description");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.Start"/> property.
		/// </summary>
		public static readonly Exposer<Job, DateTimeOffset> Start = new Exposer<Job, DateTimeOffset>((obj) => obj.Start, "Start");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.End"/> property.
		/// </summary>
		public static readonly Exposer<Job, DateTimeOffset> End = new Exposer<Job, DateTimeOffset>((obj) => obj.End, "End");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.Duration"/> property.
		/// </summary>
		public static readonly Exposer<Job, TimeSpan> Duration = new Exposer<Job, TimeSpan>((obj) => obj.Duration, "Duration");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.PreRollStart"/> property.
		/// </summary>
		public static readonly Exposer<Job, DateTimeOffset> PreRollStart = new Exposer<Job, DateTimeOffset>((obj) => obj.PreRollStart, "PreRollStart");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.PostRollEnd"/> property.
		/// </summary>
		public static readonly Exposer<Job, DateTimeOffset> PostRollEnd = new Exposer<Job, DateTimeOffset>((obj) => obj.PostRollEnd, "PostRollEnd");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.Notes"/> property.
		/// </summary>
		public static readonly Exposer<Job, string> Notes = new Exposer<Job, string>((obj) => obj.Notes, "Notes");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.RecurringJobId"/> property.
		/// </summary>
		public static readonly Exposer<Job, Guid> RecurringJobId = new Exposer<Job, Guid>((obj) => obj.RecurringJobId, "RecurringJobId");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.JobTypeCategoryId"/> property.
		/// </summary>
		public static readonly Exposer<Job, string> JobTypeCategoryId = new Exposer<Job, string>((obj) => obj.JobTypeCategoryId, "JobTypeCategoryId");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.Priority"/> property.
		/// </summary>
		public static readonly Exposer<Job, JobPriority> Priority = new Exposer<Job, JobPriority>((obj) => obj.Priority, "Priority");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.State"/> property.
		/// </summary>
		public static readonly Exposer<Job, JobState> State = new Exposer<Job, JobState>((obj) => obj.State, "State");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.OrganizationId"/> property.
		/// </summary>
		public static readonly Exposer<Job, Guid> OrganizationId = new Exposer<Job, Guid>((obj) => obj.OrganizationId, "OrganizationId");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.OwnerId"/> property.
		/// </summary>
		public static readonly Exposer<Job, Guid> OwnerId = new Exposer<Job, Guid>((obj) => obj.OwnerId, "OwnerId");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.ActionRequired"/> property.
		/// </summary>
		/// <remarks>
		/// The value is only calculated when the job is created or updated, so jobs that were never saved with this
		/// version of the API have no value stored and are not returned by this exposer.
		/// </remarks>
		public static readonly Exposer<Job, bool> ActionRequired = new Exposer<Job, bool>((obj) => obj.ActionRequired, "ActionRequired");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.ConfigurationState"/> property.
		/// </summary>
		/// <remarks>
		/// The value is only calculated when the job is created or updated, so jobs that were never saved with this
		/// version of the API have no value stored and are not returned by this exposer.
		/// </remarks>
		public static readonly Exposer<Job, ConfigurationState> ConfigurationState = new Exposer<Job, ConfigurationState>((obj) => obj.ConfigurationState, "ConfigurationState");

		/// <summary>
		/// Provides exposers for querying and filtering the nodes of the <see cref="Job.NodeGraph"/> property.
		/// </summary>
		public static class Nodes
		{
			/// <summary>
			/// Gets a dynamic list exposer for the <see cref="JobNode.ConfigurationState"/> property. A job matches when one of its nodes matches.
			/// </summary>
			/// <remarks>
			/// The value is only calculated when the job is created or updated, so nodes that were never saved with this
			/// version of the API have no value stored and are not returned by this exposer.
			/// </remarks>
			public static readonly DynamicListExposer<Job, ConfigurationState> ConfigurationState = DynamicListExposer<Job, ConfigurationState>.CreateFromListExposer(new Exposer<Job, IEnumerable>((obj) => obj.NodeGraph.Nodes.Where(x => x != null).Select(x => x.ConfigurationState), "Nodes.ConfigurationState"));
		}

		/// <summary>
		/// Provides exposers for querying and filtering the capabilities of the <see cref="Job.OrchestrationSettings"/> property.
		/// </summary>
		public static class Capabilities
		{
			/// <summary>
			/// Gets a dynamic list exposer for capability IDs.
			/// </summary>
			public static readonly DynamicListExposer<Job, Guid> CapabilityId = DynamicListExposer<Job, Guid>.CreateFromListExposer(new Exposer<Job, IEnumerable>((obj) => obj.OrchestrationSettings.Capabilities.Where(x => x != null).Select(x => x.Id), "Capabilities.Id"));

			/// <summary>
			/// Gets a dynamic list exposer for capability discrete values.
			/// </summary>
			public static readonly DynamicListExposer<Job, string> Discretes = DynamicListExposer<Job, string>.CreateFromListExposer(new Exposer<Job, IEnumerable>((obj) => obj.OrchestrationSettings.Capabilities.Where(x => x != null).Select(x => x.Value).Where(x => x != null), "Capabilities.Discretes"));
		}

		/// <summary>
		/// Provides exposers for querying and filtering the capacities of the <see cref="Job.OrchestrationSettings"/> property.
		/// </summary>
		public static class Capacities
		{
			/// <summary>
			/// Gets a dynamic list exposer for capacity IDs.
			/// </summary>
			public static readonly DynamicListExposer<Job, Guid> CapacityId = DynamicListExposer<Job, Guid>.CreateFromListExposer(new Exposer<Job, IEnumerable>((obj) => obj.OrchestrationSettings.Capacities.Where(x => x != null).Select(x => x.Id), "Capacities.Id"));
		}

		/// <summary>
		/// Provides exposers for querying and filtering the configurations of the <see cref="Job.OrchestrationSettings"/> property.
		/// </summary>
		public static class Configurations
		{
			/// <summary>
			/// Gets a dynamic list exposer for configuration IDs.
			/// </summary>
			public static readonly DynamicListExposer<Job, Guid> ConfigurationId = DynamicListExposer<Job, Guid>.CreateFromListExposer(new Exposer<Job, IEnumerable>((obj) => obj.OrchestrationSettings.Configurations.Where(x => x != null).Select(x => x.Id), "Configurations.Id"));
		}

		/// <summary>
		/// Provides exposers for querying and filtering the <see cref="Job.PropertySettings"/> property.
		/// </summary>
		public static class Properties
		{
			/// <summary>
			/// Gets a dynamic list exposer for property IDs.
			/// </summary>
			public static readonly DynamicListExposer<Job, Guid> PropertyId = DynamicListExposer<Job, Guid>.CreateFromListExposer(new Exposer<Job, IEnumerable>((obj) => obj.PropertySettings.Where(x => x != null).Select(x => x.Id), "Properties.Id"));
		}
	}
}
