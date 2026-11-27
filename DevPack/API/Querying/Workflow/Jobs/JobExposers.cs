namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM.Exposers;

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
		/// Gets an exposer that indicates whether a job has at least one error reported on it.
		/// </summary>
		public static readonly Exposer<Job, bool> HasError = new Exposer<Job, bool>((obj) => obj.Errors.Count > 0, "HasError");

		/// <summary>
		/// Gets a dynamic list exposer for the resources referenced by the nodes of the <see cref="Job.NodeGraph"/> property.
		/// </summary>
		/// <remarks>
		/// Only the <see cref="Comparer.Contains"/> and <see cref="Comparer.NotContains"/> comparisons are supported.
		/// </remarks>
		public static readonly DynamicListExposer<Job, Guid> Resources = DynamicListExposer<Job, Guid>.CreateFromListExposer(new Exposer<Job, IEnumerable>((obj) => obj.NodeGraph.Nodes.OfType<IResourceNode>().Select(x => x.ResourceId), "Resources"));

		/// <summary>
		/// Gets a collection exposer for the resources referenced by the nodes of the <see cref="Job.NodeGraph"/> property.
		/// </summary>
		/// <remarks>
		/// Use this exposer to select the field in a partial read, and <see cref="Resources"/> to filter on it.
		/// </remarks>
		public static readonly CollectionExposer<Job, Guid> ResourcesCollection = new CollectionExposer<Job, Guid>((obj) => obj.NodeGraph.Nodes.OfType<IResourceNode>().Select(x => x.ResourceId), "ResourcesCollection");

		/// <summary>
		/// Gets a dynamic list exposer for the resource pools referenced by the nodes of the <see cref="Job.NodeGraph"/> property.
		/// </summary>
		/// <remarks>
		/// A job matches when it has a resource pool node referencing the resource pool, or a resource node referencing
		/// a resource of that resource pool. Only the <see cref="Comparer.Contains"/> and
		/// <see cref="Comparer.NotContains"/> comparisons are supported.
		/// </remarks>
		public static readonly DynamicListExposer<Job, Guid> ResourcePools = DynamicListExposer<Job, Guid>.CreateFromListExposer(new Exposer<Job, IEnumerable>((obj) => obj.NodeGraph.Nodes.Select(NodeReferences.GetResourcePoolId).Where(x => x != Guid.Empty), "ResourcePools"));

		/// <summary>
		/// Gets a collection exposer for the resource pools referenced by the nodes of the <see cref="Job.NodeGraph"/> property.
		/// </summary>
		/// <remarks>
		/// Use this exposer to select the field in a partial read, and <see cref="ResourcePools"/> to filter on it.
		/// </remarks>
		public static readonly CollectionExposer<Job, Guid> ResourcePoolsCollection = new CollectionExposer<Job, Guid>((obj) => obj.NodeGraph.Nodes.Select(NodeReferences.GetResourcePoolId).Where(x => x != Guid.Empty), "ResourcePoolsCollection");

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

			/// <summary>
			/// Gets a collection exposer for the <see cref="JobNode.ConfigurationState"/> property of every node.
			/// </summary>
			/// <remarks>
			/// Use this exposer to select the field in a partial read, and <see cref="ConfigurationState"/> to filter on it.
			/// </remarks>
			public static readonly CollectionExposer<Job, ConfigurationState> ConfigurationStateCollection = new CollectionExposer<Job, ConfigurationState>((obj) => obj.NodeGraph.Nodes.Where(x => x != null).Select(x => x.ConfigurationState), "Nodes.ConfigurationStateCollection");
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
			/// Gets a collection exposer for capability IDs. Use it to select the field in a partial read, and <see cref="CapabilityId"/> to filter on it.
			/// </summary>
			public static readonly CollectionExposer<Job, Guid> CapabilityIdCollection = new CollectionExposer<Job, Guid>((obj) => obj.OrchestrationSettings.Capabilities.Where(x => x != null).Select(x => x.Id), "Capabilities.IdCollection");
			/// <summary>
			/// Gets a dynamic list exposer for capability discrete values.
			/// </summary>
			public static readonly DynamicListExposer<Job, string> Discretes = DynamicListExposer<Job, string>.CreateFromListExposer(new Exposer<Job, IEnumerable>((obj) => obj.OrchestrationSettings.Capabilities.Where(x => x != null).Select(x => x.Value).Where(x => x != null), "Capabilities.Discretes"));

			/// <summary>
			/// Gets a collection exposer for capability discrete values. Use it to select the field in a partial read, and <see cref="Discretes"/> to filter on it.
			/// </summary>
			public static readonly CollectionExposer<Job, string> DiscretesCollection = new CollectionExposer<Job, string>((obj) => obj.OrchestrationSettings.Capabilities.Where(x => x != null).Select(x => x.Value).Where(x => x != null), "Capabilities.DiscretesCollection");
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

			/// <summary>
			/// Gets a collection exposer for capacity IDs. Use it to select the field in a partial read, and <see cref="CapacityId"/> to filter on it.
			/// </summary>
			public static readonly CollectionExposer<Job, Guid> CapacityIdCollection = new CollectionExposer<Job, Guid>((obj) => obj.OrchestrationSettings.Capacities.Where(x => x != null).Select(x => x.Id), "Capacities.IdCollection");
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

			/// <summary>
			/// Gets a collection exposer for configuration IDs. Use it to select the field in a partial read, and <see cref="ConfigurationId"/> to filter on it.
			/// </summary>
			public static readonly CollectionExposer<Job, Guid> ConfigurationIdCollection = new CollectionExposer<Job, Guid>((obj) => obj.OrchestrationSettings.Configurations.Where(x => x != null).Select(x => x.Id), "Configurations.IdCollection");
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

			/// <summary>
			/// Gets a collection exposer for property IDs. Use it to select the field in a partial read, and <see cref="PropertyId"/> to filter on it.
			/// </summary>
			public static readonly CollectionExposer<Job, Guid> PropertyIdCollection = new CollectionExposer<Job, Guid>((obj) => obj.PropertySettings.Where(x => x != null).Select(x => x.Id), "Properties.IdCollection");
		}

		/// <summary>
		/// Provides exposers for querying and filtering the <see cref="Job.Errors"/> property.
		/// </summary>
		public static class Errors
		{
			/// <summary>
			/// Gets a dynamic list exposer for the <see cref="JobError.Code"/> property. A job matches when one of its errors matches.
			/// </summary>
			public static readonly DynamicListExposer<Job, string> Code = DynamicListExposer<Job, string>.CreateFromListExposer(new Exposer<Job, IEnumerable>((obj) => obj.Errors.Where(x => x != null).Select(x => x.Code).Where(x => x != null), "Errors.Code"));

			/// <summary>
			/// Gets a collection exposer for the <see cref="JobError.Code"/> property. Use it to select the field in a partial read, and <see cref="Code"/> to filter on it.
			/// </summary>
			public static readonly CollectionExposer<Job, string> CodeCollection = new CollectionExposer<Job, string>((obj) => obj.Errors.Where(x => x != null).Select(x => x.Code).Where(x => x != null), "Errors.CodeCollection");
			/// <summary>
			/// Gets a dynamic list exposer for the <see cref="JobError.Message"/> property. A job matches when one of its errors matches.
			/// </summary>
			public static readonly DynamicListExposer<Job, string> Message = DynamicListExposer<Job, string>.CreateFromListExposer(new Exposer<Job, IEnumerable>((obj) => obj.Errors.Where(x => x != null).Select(x => x.Message).Where(x => x != null), "Errors.Message"));

			/// <summary>
			/// Gets a collection exposer for the <see cref="JobError.Message"/> property. Use it to select the field in a partial read, and <see cref="Message"/> to filter on it.
			/// </summary>
			public static readonly CollectionExposer<Job, string> MessageCollection = new CollectionExposer<Job, string>((obj) => obj.Errors.Where(x => x != null).Select(x => x.Message).Where(x => x != null), "Errors.MessageCollection");
		}
	}
}
