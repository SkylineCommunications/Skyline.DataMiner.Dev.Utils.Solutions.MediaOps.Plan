namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// A <see cref="ReferenceResolver"/> that resolves <see cref="DataReference"/> instances in the context
	/// of a specific <see cref="Job"/>. Job-level and workflow-level references are resolved using the
	/// job's own data and its associated workflow.
	/// </summary>
	public class JobReferenceResolver : ReferenceResolver
	{
		private readonly Lazy<IDictionary<Guid, PropertySettingBase>> _lazyJobPropertyValues;
		private readonly IDictionary<Guid, Resource> _resourceCache;

		/// <summary>
		/// Initializes a new instance of the <see cref="JobReferenceResolver"/> class.
		/// </summary>
		/// <param name="planApi">The MediaOps Plan API used to retrieve definitions and property values.</param>
		/// <param name="job">The job whose context is used to resolve references.</param>
		public JobReferenceResolver(IMediaOpsPlanApi planApi, Job job) : base(planApi)
		{
			Job = job ?? throw new ArgumentNullException(nameof(job));

			_lazyJobPropertyValues = new Lazy<IDictionary<Guid, PropertySettingBase>>(() => ReadPropertyValues(Job.Id));
			_resourceCache = new Dictionary<Guid, Resource>();
		}

		/// <summary>
		/// Gets the job used as the resolution context.
		/// </summary>
		protected Job Job { get; }

		/// <summary>
		/// Gets the lazily-loaded dictionary of property values defined at the job level.
		/// </summary>
		protected IDictionary<Guid, PropertySettingBase> JobPropertyValues => _lazyJobPropertyValues.Value;

		/// <inheritdoc />
		protected override ResolvedValue ResolveJobName(JobNameReference reference)
		{
			return new StringResolvedValue(Job.Name);
		}

		/// <inheritdoc />
		protected override ResolvedValue ResolveJobPropertyValue(JobPropertyReference reference)
		{
			if (JobPropertyValues.TryGetValue(reference.JobPropertyId, out var value))
			{
				return ConvertPropertyValue(value);
			}

			return ResolvedValue.FromUnresolvedReference(reference);
		}

		/// <inheritdoc />
		protected override Resource GetResource(DataReference reference)
		{
			if (String.IsNullOrEmpty(reference.NodeId))
			{
				return null;
			}

			var node = Job.NodeGraph.Nodes.FirstOrDefault(n => String.Equals(n.Id, reference.NodeId, StringComparison.OrdinalIgnoreCase));

			if (!node.IsResourceNode(out var resourceNode))
			{
				return null;
			}

			var resourceId = resourceNode.ResourceId;

			if (resourceId == Guid.Empty)
			{
				return null;
			}

			if (_resourceCache.TryGetValue(resourceId, out var cachedResource))
			{
				return cachedResource;
			}

			var resource = PlanApi.Resources.Read(resourceId);
			_resourceCache[resourceId] = resource;

			return resource;
		}

		/// <inheritdoc />
		protected override OrchestrationSettings GetOrchestrationSettings(DataReference reference)
		{
			if (String.IsNullOrEmpty(reference.NodeId))
			{
				return Job.OrchestrationSettings;
			}

			var node = Job.NodeGraph.Nodes.FirstOrDefault(n => String.Equals(n.Id, reference.NodeId, StringComparison.OrdinalIgnoreCase));

			if (node != null)
			{
				return node.OrchestrationSettings;
			}

			return null;
		}
	}
}
